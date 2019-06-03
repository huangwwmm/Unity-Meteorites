using System.Runtime.InteropServices;
using UnityEngine;

namespace BatchRendering
{
	/// <summary>
	/// Mesh的Transform信息
	/// 不命名为MeshTransform是因为Mesh以后可能会运动，那这里就需要存速度、角速度等信息
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct MeshState
	{
		public Vector3 LocalPosition;
		public Vector3 LocalRotation;
		public Vector3 LocalScale;
		/// <summary>
		/// GPU中用的数据，CPU中完全不需要关心
		/// 不知道怎么在ComputeShader中动态分配vram，所以就通过CPU来分配
		/// </summary>
		public Matrix4x4 Dummy1;
		public Matrix4x4 Dummy2;
		public int Dummy3;
	}
}