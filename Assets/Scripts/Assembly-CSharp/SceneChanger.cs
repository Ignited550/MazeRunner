using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		SceneManager.LoadScene(2);
	}
}
