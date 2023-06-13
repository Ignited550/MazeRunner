using UnityEngine;

public class Footsteps : MonoBehaviour
{
	private CharacterController cc;

	private void Start()
	{
		cc = GetComponent<CharacterController>();
	}

	private void Update()
	{
		if (cc.isGrounded && cc.velocity.magnitude > 2f && !GetComponent<AudioSource>().isPlaying)
		{
			GetComponent<AudioSource>().Play();
		}
	}
}
