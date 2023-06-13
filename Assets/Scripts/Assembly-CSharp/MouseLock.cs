using UnityEngine;

public class MouseLock : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown("1"))
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		if (Input.GetKeyDown("2"))
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
