using UnityEngine;
using UnityEngine.SceneManagement;

public class TheEndOfTheGame : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		SceneManager.LoadScene("TheEndOfTheGame");
	}
}
