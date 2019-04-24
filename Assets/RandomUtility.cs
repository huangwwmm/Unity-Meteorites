using UnityEngine;

public static class RandomUtility
{
	/// <summary>
	/// 在球体内随机坐标
	/// </summary>
	public static Vector3 RandomInSphere(float radius)
	{
		// https://en.wikipedia.org/wiki/Spherical_coordinate_system
		float r = Random.Range(0, radius);
		float theta = Random.Range(0, 360);
		float phi = Random.Range(0, 360);
		float sinTheta = Mathf.Sin(theta);
		return new Vector3(r * sinTheta * Mathf.Cos(phi)
			, r * sinTheta * Mathf.Sin(phi)
			, r * Mathf.Cos(theta));
	}

	/// <summary>
	/// 随机一个欧拉角
	/// </summary>
	public static Vector3 RandomEulerAngles()
	{
		return new Vector3(Random.Range(-360, 360)
			, Random.Range(-360, 360)
			, Random.Range(-360, 360));
	}
}