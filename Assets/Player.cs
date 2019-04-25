using UnityEngine;

public class Player : MonoBehaviour
{
	public float Speed;
	public float AngleSpeed;

	void LateUpdate()
	{
		Vector3 rotation = transform.eulerAngles;
		rotation.y = rotation.y + AngleSpeed * Time.deltaTime
			* (Input.GetKey(KeyCode.RightArrow)
				? 1.0f
				: Input.GetKey(KeyCode.LeftArrow)
					? -1.0f
					: 0.0f);
		transform.eulerAngles = rotation;

		Vector3 position = transform.localPosition;
		position += transform.forward * Time.deltaTime
			* (Input.GetKey(KeyCode.UpArrow)
				? 1.0f
				: Input.GetKey(KeyCode.DownArrow)
					? -1.0f
					: 0.0f);
		transform.localPosition = position;
	}
}
