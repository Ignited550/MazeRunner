using UnityEngine;

public class MouseLook : MonoBehaviour
{
	public float mouseSensitivy = 100f;

	public Transform playerBody;

	private void Start()
	{
	}

	private void Update()
	{
		float num = Input.GetAxis("Mouse X") * mouseSensitivy * Time.deltaTime;
		Input.GetAxis("Mouse Y");
		_ = mouseSensitivy;
		_ = Time.deltaTime;
		playerBody.Rotate(Vector3.up * num);
	}
}
