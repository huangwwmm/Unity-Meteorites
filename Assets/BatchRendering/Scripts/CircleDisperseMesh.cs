using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace BatchRendering
{
	/// <summary>
	/// 在一个圆形内随机分布Mesh
	/// </summary>
	[ExecuteInEditMode]
	public class CircleDisperseMesh : BaseDisperseMesh
	{
		[Header("Circle")]
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

		protected override void FillMeshStates()
		{
			for (int iMesh = 0; iMesh < Count; iMesh++)
			{
				Vector2 randomInCircle = Random.insideUnitCircle * DisperseRadius;
				m_MeshStates[iMesh].LocalPosition = new Vector3(randomInCircle.x, 0, randomInCircle.y);
				m_MeshStates[iMesh].LocalRotation = RandomUtility.RandomEulerAngles() * Mathf.Deg2Rad;
				m_MeshStates[iMesh].LocalScale = RandomUtility.RandomScale(MinScale, MaxScale, ScaleMaxOffset);
			}
		}

		protected override void InitializeLimitBounds()
		{
			m_LimitBounds = new Bounds(Vector3.zero, Vector3.one * DisperseRadius * 2);
		}
	}
}