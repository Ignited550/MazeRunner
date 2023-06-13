using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger2 : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		SceneManager.LoadScene(3);
	}
}
