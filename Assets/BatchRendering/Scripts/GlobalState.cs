using System.Runtime.InteropServices;
using UnityEngine;

namespace BatchRendering
{
	[StructLayout(LayoutKind.Sequential)]
	public struct GlobalState
	{
		public Matrix4x4 MatM;
		public Matrix4x4 MatMVP;
		public Vector3 CameraLocalPosition;
		public Vector3 CameraLocalForward;
	}
}
