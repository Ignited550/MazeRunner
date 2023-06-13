using UnityEngine;
using UnityEngine.SceneManagement;

public class L3 : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		SceneManager.LoadScene("Level 3");
	}
}
