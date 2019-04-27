using UnityEngine;

public static class RandomUtility
{
	/// <summary>
	/// 在球体内随机坐标
	/// </summary>
	public static Vector3 RandomInSphere(float radius)
	{
		// https://en.wikipedia.org/wiki/Spherical_coordinate_system
		// radius的平方再开方，是为了避免随机到的位置集中在球体中心
		float r = Mathf.Sqrt(Random.Range(0, radius * radius));
		// 这里用Mathf.Epsilon而不是0是为了避免随机出来的z坐标为0
		float theta = Random.Range(Mathf.Epsilon, 360);
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

	/// <summary>
	/// 在min~max范围内随机一个缩放
	/// xyz轴的缩放最大不超过offset
	/// 如果offset为0则是uniform scale
	/// </summary>
	public static Vector3 RandomScale(Vector3 min, Vector3 max, float offset)
	{
		Vector3 scale = Vector3.zero;
		scale.x = Random.Range(min.x, max.x);
		scale.y = Random.Range(Mathf.Max(scale.x - offset, min.y), Mathf.Min(scale.x + offset, max.y));
		scale.z = Random.Range(Mathf.Max(Mathf.Max(scale.x, scale.y) - offset, min.z), Mathf.Min(Mathf.Min(scale.x, scale.y) + offset, max.z));
		return scale;
	}
}