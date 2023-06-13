using UnityEngine;

public class Teleporter : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}

	private void OnTriggerEnter(Collider Col)
	{
		Col.transform.position = new Vector3(Random.Range(10f, 10f), Col.transform.position.y, Random.Range(10f, 10f));
	}
}
