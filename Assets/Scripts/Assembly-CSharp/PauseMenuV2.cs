using UnityEngine;

public class PauseMenuV2 : MonoBehaviour
{
	[SerializeField]
	private GameObject pauseMenuUI;

	[SerializeField]
	private bool isPaused;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			isPaused = !isPaused;
		}
		if (isPaused)
		{
			ActivateMenu();
		}
		else
		{
			DeactivateMenu();
		}
	}

	private void ActivateMenu()
	{
		Time.timeScale = 0f;
		AudioListener.pause = true;
		pauseMenuUI.SetActive(value: true);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	private void DeactivateMenu()
	{
		Time.timeScale = 1f;
		AudioListener.pause = false;
		pauseMenuUI.SetActive(value: false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}
