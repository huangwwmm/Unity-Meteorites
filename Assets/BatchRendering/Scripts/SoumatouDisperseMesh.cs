using UnityEngine;

namespace BatchRendering
{
	/// <summary>
	/// <see cref="https://ja.wikipedia.org/wiki/%E8%B5%B0%E9%A6%AC%E7%81%AF"/>
	/// </summary>
	[ExecuteInEditMode]
	public class SoumatouDisperseMesh : BaseDisperseMesh
	{
		[Header("Soumatou")]
		/// <summary>
		/// 生成Mesh的半径
		/// </summary>
		public float DisperseRadius;
		/// <summary>
		/// 缩放
		/// </summary>
		public Vector3 Scale;
		/// <summary>
		/// 更新的间隔时间，单位秒
		/// </summary>
		public float UpdateIntervalTime;
		/// <summary>
		/// shader中，ActiveIndex的变量名
		/// </summary>
		public string ActiveIndexPropertyName;

		private float m_LastUpdateTime;
		private int m_LastActiveItemIndex;
		private int m_ActiveIndexPropertyId;

		protected override void FillMeshStates()
		{
			Vector3 startPosition = new Vector3(DisperseRadius, 0, 0);
			float deltaRotation = 360.0f / Count;
			for (int iMesh = 0; iMesh < Count; iMesh++)
			{
				m_MeshStates[iMesh].LocalRotation = new Vector3(0, deltaRotation * iMesh, 0);
				m_MeshStates[iMesh].LocalPosition = Quaternion.Euler(m_MeshStates[iMesh].LocalRotation) * startPosition;
				m_MeshStates[iMesh].LocalScale = Scale;
			}
		}

		protected override void InitializeLimitBounds()
		{
			m_LimitBounds = new Bounds(Vector3.zero, Vector3.one * DisperseRadius * 2);
		}

		protected override void OnStartedRendering()
		{
			base.OnStartedRendering();

			m_LastUpdateTime = Time.time - UpdateIntervalTime;
			// -1: Next Index is 0
			m_LastActiveItemIndex = -1;
			m_ActiveIndexPropertyId = Shader.PropertyToID(ActiveIndexPropertyName);
		}

		protected override void OnReadyToDraw()
		{
			base.OnReadyToDraw();

			if (Time.time - m_LastUpdateTime >= UpdateIntervalTime)
			{
				m_LastUpdateTime = Time.time;
				m_LastActiveItemIndex = m_LastActiveItemIndex >= Count - 1 
					? 0 
					: m_LastActiveItemIndex + 1;
				Material.SetInt(m_ActiveIndexPropertyId, m_LastActiveItemIndex);
			}
		}
	}
}