using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class Meteorites : MonoBehaviour
{
	private static readonly Bounds UNLIMITED_BOUNDS = new Bounds(Vector3.zero, new Vector3(999999f, 999999f, 999999f));

	public Camera Camera;
	public Mesh Mesh;
	public Material Material;
	public ComputeShader ComputeShader;
	public int Count;
	public float Radius;

	private MeteoriteState[] m_MeteoritesState;
	private ComputeBuffer m_CB_MeteoritesState;

	private uint[] m_BufferArgs;
	private ComputeBuffer m_CB_BufferArgs;

	private Vector3 m_LastPosition;

	private int m_CS_MainKernel;

	private Matrix4x4 m_ParentMatrix4X4;

	protected void OnEnable()
	{
		m_CS_MainKernel = ComputeShader.FindKernel("CSMain");

		m_MeteoritesState = new MeteoriteState[Count];
		for (int iRole = 0; iRole < Count; iRole++)
		{
			m_MeteoritesState[iRole].Position = RandomUtility.RandomInSphere(Radius);
			m_MeteoritesState[iRole].Rotation = RandomUtility.RandomEulerAngles() * Mathf.Deg2Rad;
		}
		m_CB_MeteoritesState = new ComputeBuffer(Count, Marshal.SizeOf(typeof(MeteoriteState)));
		m_CB_MeteoritesState.SetData(m_MeteoritesState);

		m_BufferArgs = new uint[5] { Mesh.GetIndexCount(0), (uint)Count, 0, 0, 0 };
		m_CB_BufferArgs = new ComputeBuffer(1
			, (m_BufferArgs.Length * Marshal.SizeOf(typeof(uint)))
			, ComputeBufferType.IndirectArguments);
		m_CB_BufferArgs.SetData(m_BufferArgs);

		ComputeShader.SetBuffer(m_CS_MainKernel, "MeteoritesState", m_CB_MeteoritesState);
		Material.SetBuffer("MeteoritesState", m_CB_MeteoritesState);

		m_LastPosition = transform.position;
		OnChangeTransform(m_LastPosition);

		m_ParentMatrix4X4 = new Matrix4x4(new Vector4(1, 0, 0, 0)
			, new Vector4(0, 1, 0, 0)
			, new Vector4(0, 0, 1, 0)
			, new Vector4(0, 0, 0, 1));

#if UNITY_EDITOR
		UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
	}

	protected void OnDisable()
	{
#if UNITY_EDITOR
		UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif

		m_CB_MeteoritesState.Release();
		m_CB_BufferArgs.Release();
	}

#if UNITY_EDITOR
	protected void OnSceneGUI(UnityEditor.SceneView sceneView)
	{
		OnDraw(sceneView.camera);
	}
#endif

	protected void LateUpdate()
	{
		Vector3 position = transform.position;
		if (position != m_LastPosition)
		{
			m_LastPosition = position;
			OnChangeTransform(m_LastPosition);
		}
		OnDraw(Camera);
	}

	private void OnChangeTransform(Vector3 position)
	{
		m_ParentMatrix4X4.m03 = position.x;
		m_ParentMatrix4X4.m13 = position.y;
		m_ParentMatrix4X4.m23 = position.z;

		ComputeShader.SetMatrix("ParentMat", m_ParentMatrix4X4);
		ComputeShader.Dispatch(m_CS_MainKernel, Count, 1, 1);
	}

	private void OnDraw(Camera camera)
	{
		Graphics.DrawMeshInstancedIndirect(Mesh
			, 0
			, Material
			, UNLIMITED_BOUNDS
			, m_CB_BufferArgs
			, 0
			, null
			, UnityEngine.Rendering.ShadowCastingMode.Off
			, false
			, 0
			, camera);
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MeteoriteState
	{
		public Vector3 Position;
		public Vector3 Rotation;
		public Matrix4x4 Mat;
	}
}
