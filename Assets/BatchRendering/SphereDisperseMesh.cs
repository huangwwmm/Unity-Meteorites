using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace BatchRendering
{
	/// <summary>
	/// 在一个球体内随机分布Mesh
	/// 利用GPU instancing和Compute Shader实现的，占用非常低
	/// </summary>
	[ExecuteInEditMode]
	public class SphereDisperseMesh : MonoBehaviour
	{
		private const string CS_MAIN_KERNEL_NAME = "CSMain";
		private const string CS_PARAM1_NAME = "_Param1";
		private const string CS_GLOBAL_STATE_NAME = "_GlobalState";
		private const string CS_MESH_STATES_NAME = "_MeshStates";

		/// <summary>
		/// 用来渲染这个Meshs的相机
		/// </summary>
		public Camera Camera;
		/// <summary>
		/// 这个Mesh的Mesh
		/// </summary>
		public Mesh Mesh;
		/// <summary>
		/// 渲染Mesh用的材质
		/// </summary>
		public Material Material;
		/// <summary>
		/// 计算Mesh顶点的MVP矩阵的Shader
		/// </summary>
		public ComputeShader ComputeShader;
		/// <summary>
		/// Mesh的数量
		/// </summary>
		public int Count;
		/// <summary>
		/// 生成Mesh的半径
		/// </summary>
		public float DisperseRadius;
		/// <summary>
		/// Mesh的最小缩放
		/// </summary>
		public Vector3 MinScale;
		/// <summary>
		/// Mesh的最大缩放
		/// </summary>
		public Vector3 MaxScale;
		/// <summary>
		/// 缩放的xyz轴之间的最大Offset，如果为0那么就是uniform scale
		/// </summary>
		public float ScaleMaxOffset;
		/// <summary>
		/// Mesh到相机的最小距离，相机接近时Mesh会被推开
		/// </summary>
		public float MinDisplayToCamera;
		/// <summary>
		/// 会显示Mesh的最大距离，超过这个距离，Mesh会被放在一个不可能看到的位置
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
		/// <summary>
		/// true: 每次更新都会Dispatch
		/// false: 只有MVP改变时Dispatch
		/// </summary>
		public bool IsUpdateDispatch = false;
#endif

		/// <summary>
		/// 所有Mesh共用的状态，虽然是数组，但其实只有1个
		/// </summary>
		private GlobalState[] m_GlobalState;
		/// <summary>
		/// <see cref="m_GlobalState"/>在显存中的Buffer
		/// </summary>
		private ComputeBuffer m_CB_GlobalState;

		/// <summary>
		/// CPU计算出的Mesh的Transform信息
		/// </summary>
		private MeshState[] m_MeshStates;
		/// <summary>
		/// <see cref="m_MeshStates"/>在显存中的Buffer
		/// </summary>
		private ComputeBuffer m_CB_MeshStates;

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
		/// Mesh在世界空间的AABB
		/// </summary>
		private Bounds m_LimitBounds;
		/// <summary>
		/// 当前节点
		/// </summary>
		private Transform m_Transform;
		/// <summary>
		/// 上一帧的MVP矩阵
		/// </summary>
		private Matrix4x4 m_LastMVP;

		/// <summary>
		/// 开始渲染，流程：
		///		随机分布Mesh
		///		分配显存
		///		把Mesh的Transform信息发送给显存
		/// </summary>
		public void StartRendering()
		{
			m_CS_MainKernel = ComputeShader.FindKernel(CS_MAIN_KERNEL_NAME);

			m_GlobalState = new GlobalState[1];
			m_CB_GlobalState = new ComputeBuffer(1, Marshal.SizeOf(typeof(GlobalState)));

			m_MeshStates = new MeshState[Count];
			for (int iRole = 0; iRole < Count; iRole++)
			{
				m_MeshStates[iRole].LocalPosition = Random.insideUnitSphere * DisperseRadius;
				m_MeshStates[iRole].LocalRotation = RandomUtility.RandomEulerAngles() * Mathf.Deg2Rad;
				m_MeshStates[iRole].LocalScale = RandomUtility.RandomScale(MinScale, MaxScale, ScaleMaxOffset);
			}

			m_CB_MeshStates = new ComputeBuffer(Count, Marshal.SizeOf(typeof(MeshState)));
			m_CB_MeshStates.SetData(m_MeshStates);

			m_BufferArgs = new uint[5] { Mesh.GetIndexCount(0), (uint)Count, 0, 0, 0 };
			m_CB_BufferArgs = new ComputeBuffer(1
				, (m_BufferArgs.Length * Marshal.SizeOf(typeof(uint)))
				, ComputeBufferType.IndirectArguments);
			m_CB_BufferArgs.SetData(m_BufferArgs);

			ComputeShader.SetBuffer(m_CS_MainKernel, CS_GLOBAL_STATE_NAME, m_CB_GlobalState);
			ComputeShader.SetBuffer(m_CS_MainKernel, CS_MESH_STATES_NAME, m_CB_MeshStates);
			ComputeShader.SetVector(CS_PARAM1_NAME, new Vector4(MinDisplayToCamera, MaxDisplayDistanceToCamera, 0, 0));

			Material.SetBuffer(CS_MESH_STATES_NAME, m_CB_MeshStates);

			m_LimitBounds = new Bounds(Vector3.zero, Vector3.one * DisperseRadius * 2);

			m_LastMVP = Matrix4x4.identity;
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
			m_CB_MeshStates.Release();
			m_CB_BufferArgs.Release();
		}

		protected void OnEnable()
		{
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
		///		在ComputeShader中计算Mesh顶点的MVP矩阵
		///		更新Mesh群的Bounds
		/// </summary>
		private void DoUpdate(Camera camera)
		{
			m_LimitBounds.center = transform.position;

			Matrix4x4 mat_M = transform.localToWorldMatrix;
			Matrix4x4 mat_V = camera.worldToCameraMatrix;
			Matrix4x4 mat_P = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
			Matrix4x4 mat_MVP = mat_P * mat_V * mat_M;
			if (
#if UNITY_EDITOR
				IsUpdateDispatch
#endif
				|| mat_MVP != m_LastMVP)
			{
				m_LastMVP = mat_MVP;

				m_GlobalState[0].MatM = mat_M;
				m_GlobalState[0].MatMVP = m_LastMVP;
				m_GlobalState[0].CameraLocalPosition = transform.InverseTransformPoint(camera.transform.position);
				m_GlobalState[0].CameraLocalForward = transform.InverseTransformDirection(camera.transform.forward);
				m_CB_GlobalState.SetData(m_GlobalState);

				ComputeShader.Dispatch(m_CS_MainKernel, Count, 1, 1);
			}
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
		/// Mesh的Transform信息
		/// 不命名为MeshTransform是因为Mesh以后可能会运动，那这里就需要存速度、角速度等信息
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct MeshState
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
}