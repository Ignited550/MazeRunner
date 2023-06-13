using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonScripts : MonoBehaviour
{
	public void NewGame()
	{
		SceneManager.LoadScene("GameScene");
	}

	public void QuitGame()
	{
		Application.Quit();
	}
}
