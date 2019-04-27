using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Meteorites : MonoBehaviour
{
	/// <summary>
	/// 用来渲染这个陨石群的相机
	/// </summary>
	public Camera Camera;
	/// <summary>
	/// 这个陨石的Mesh
	/// </summary>
	public Mesh Mesh;
	/// <summary>
	/// 渲染陨石用的材质
	/// </summary>
	public Material Material;
	/// <summary>
	/// 计算陨石顶点的MVP矩阵的Shader
	/// </summary>
	public ComputeShader ComputeShader;
	/// <summary>
	/// 陨石的数量
	/// </summary>
	public int Count;
	/// <summary>
	/// 生成陨石的半径
	/// </summary>
	public float DisperseRadius;
	/// <summary>
	/// 陨石的最小缩放
	/// </summary>
	public Vector3 MinScale;
	/// <summary>
	/// 陨石的最大缩放
	/// </summary>
	public Vector3 MaxScale;
	/// <summary>
	/// 缩放的xyz轴之间的最大Offset，如果为0那么就是uniform scale
	/// </summary>
	public float ScaleMaxOffset;
	/// <summary>
	/// 陨石到相机的最小距离，相机接近时陨石会被推开
	/// </summary>
	public float MinDisplayToCamera;
	/// <summary>
	/// 会显示陨石的最大距离，超过这个距离，陨石会被放在一个不可能看到的位置
	/// </summary>
	public float MaxDisplayDistanceToCamera;
#if UNITY_EDITOR
	/// <summary>
	/// Scene、Game窗口的MVP矩阵公用一块显存，而计算Scene在Game之后
	/// 会导致两个窗口都渲染时，Game窗口用的MVP矩阵和Scene窗口相同
	/// 解决方法：
	///		A：分配两块显存分别用于Scene、Game
	///			缺点：我能力有限，这样做可能会影响游戏Build后的运行性能
	///		B：只在一个窗口Rendering
	///			缺点：只能在一个窗口预览效果
	/// </summary>
	public RendererIn MyRendererIn;
#endif

	/// <summary>
	/// 所有陨石共用的状态，虽然是数组，但其实只有1个
	/// </summary>
	private GlobalState[] m_GlobalState;
	/// <summary>
	/// <see cref="m_GlobalState"/>在显存中的Buffer
	/// </summary>
	private ComputeBuffer m_CB_GlobalState;

	/// <summary>
	/// CPU计算出的陨石的Transform信息
	/// </summary>
	private MeteoriteState[] m_MeteoritesState;
	/// <summary>
	/// <see cref="m_MeteoritesState"/>在显存中的Buffer
	/// </summary>
	private ComputeBuffer m_CB_MeteoritesState;

	/// <summary>
	/// <see cref="Graphics.DrawMeshInstancedIndirect"/>需要用到的参数
	/// </summary>
	private uint[] m_BufferArgs;
	/// <summary>
	/// <see cref="m_BufferArgs"/>在显存中的Buffer
	/// </summary>
	private ComputeBuffer m_CB_BufferArgs;

	/// <summary>
	/// <see cref="ComputeShader"/>的Kernel
	/// </summary>
	private int m_CS_MainKernel;

	/// <summary>
	/// 陨石群在世界空间的AABB
	/// </summary>
	private Bounds m_LimitBounds;
	/// <summary>
	/// 当前节点
	/// </summary>
	private Transform m_Transform;

	/// <summary>
	/// 开始渲染，流程：
	///		随机分布陨石
	///		分配显存
	///		把陨石的Transform信息发送给显存
	/// </summary>
	public void StartRendering()
	{
		m_CS_MainKernel = ComputeShader.FindKernel("CSMain");

		m_GlobalState = new GlobalState[1];
		m_CB_GlobalState = new ComputeBuffer(1, Marshal.SizeOf(typeof(GlobalState)));

		m_MeteoritesState = new MeteoriteState[Count];
		for (int iRole = 0; iRole < Count; iRole++)
		{
			m_MeteoritesState[iRole].LocalPosition = RandomUtility.RandomInSphere(DisperseRadius);
			m_MeteoritesState[iRole].LocalRotation = RandomUtility.RandomEulerAngles() * Mathf.Deg2Rad;
			m_MeteoritesState[iRole].LocalScale = RandomUtility.RandomScale(MinScale, MaxScale, ScaleMaxOffset);
		}
		m_CB_MeteoritesState = new ComputeBuffer(Count, Marshal.SizeOf(typeof(MeteoriteState)));
		m_CB_MeteoritesState.SetData(m_MeteoritesState);

		m_BufferArgs = new uint[5] { Mesh.GetIndexCount(0), (uint)Count, 0, 0, 0 };
		m_CB_BufferArgs = new ComputeBuffer(1
			, (m_BufferArgs.Length * Marshal.SizeOf(typeof(uint)))
			, ComputeBufferType.IndirectArguments);
		m_CB_BufferArgs.SetData(m_BufferArgs);

		ComputeShader.SetBuffer(m_CS_MainKernel, "_GlobalState", m_CB_GlobalState);
		ComputeShader.SetBuffer(m_CS_MainKernel, "_MeteoritesState", m_CB_MeteoritesState);
		ComputeShader.SetVector("_Param1", new Vector4(MinDisplayToCamera, MaxDisplayDistanceToCamera, 0, 0));

		Material.SetBuffer("_MeteoritesState", m_CB_MeteoritesState);

		m_LimitBounds = new Bounds(Vector3.zero, Vector3.one * DisperseRadius * 2);
#if UNITY_EDITOR
		UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
	}

	/// <summary>
	/// 停止渲染，工作流程：
	///		释放显存
	/// </summary>
	public void StopRendering()
	{
#if UNITY_EDITOR
		UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif

		m_CB_GlobalState.Release();
		m_CB_MeteoritesState.Release();
		m_CB_BufferArgs.Release();
	}

	protected void OnEnable()
	{
		// TEMP
		Application.targetFrameRate = 2048;
		QualitySettings.vSyncCount = 0;
		Camera.depthTextureMode = DepthTextureMode.Depth;

		StartRendering();
	}

	protected void OnDisable()
	{
		StopRendering();
	}

#if UNITY_EDITOR
	/// <summary>
	/// 用于在Scene窗口Renderer
	/// </summary>
	protected void OnSceneGUI(UnityEditor.SceneView sceneView)
	{
		if (MyRendererIn == RendererIn.Scene)
		{
			DoUpdate(sceneView.camera);
			OnDraw(sceneView.camera);
		}
	}
#endif

	protected void LateUpdate()
	{
#if UNITY_EDITOR
		if (MyRendererIn == RendererIn.Game)
		{
#endif
			DoUpdate(Camera);
			OnDraw(Camera);
#if UNITY_EDITOR
		}
#endif
	}

	/// <summary>
	/// 做了以下工作：
	///		在ComputeShader中计算陨石顶点的MVP矩阵
	///		更新陨石群的Bounds
	/// </summary>
	private void DoUpdate(Camera camera)
	{
		m_LimitBounds.center = transform.position;

		Matrix4x4 mat_M = transform.localToWorldMatrix;
		Matrix4x4 mat_V = camera.worldToCameraMatrix;
		Matrix4x4 mat_P = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
		m_GlobalState[0].MatM = mat_M;
		m_GlobalState[0].MatMVP = mat_P * mat_V * mat_M;
		m_GlobalState[0].CameraLocalPosition = transform.InverseTransformPoint(camera.transform.position);
		m_GlobalState[0].CameraLocalForward = transform.InverseTransformDirection(camera.transform.forward);
		m_CB_GlobalState.SetData(m_GlobalState);

		ComputeShader.Dispatch(m_CS_MainKernel, Count, 1, 1);
	}

	/// <summary>
	/// 发送Draw的CommandBuffer(我个人理解，不知道对错)
	/// </summary>
	private void OnDraw(Camera camera)
	{
		Graphics.DrawMeshInstancedIndirect(Mesh
			, 0
			, Material
			, m_LimitBounds
			, m_CB_BufferArgs
			, 0
			, null
			, ShadowCastingMode.Off
			, false
			, 0
			, camera);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct GlobalState
	{
		public Matrix4x4 MatM;
		public Matrix4x4 MatMVP;
		public Vector3 CameraLocalPosition;
		public Vector3 CameraLocalForward;
	}

	/// <summary>
	/// 陨石的Transform信息
	/// 不命名为MeteoriteTransform是因为陨石以后可能会运动，那这里就需要存速度、角速度等信息
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct MeteoriteState
	{
		public Vector3 LocalPosition;
		public Vector3 LocalRotation;
		public Vector3 LocalScale;
		public Matrix4x4 Dummy1;
		public Matrix4x4 Dummy2;
		public int Dummy3;
	}

#if UNITY_EDITOR
	/// <summary>
	/// Rendering到哪个窗口
	/// </summary>
	public enum RendererIn
	{
		Game,
		Scene,
	}
#endif
}