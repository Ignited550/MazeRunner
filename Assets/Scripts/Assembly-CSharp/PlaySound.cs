using UnityEngine;

public class PlaySound : MonoBehaviour
{
	public AudioClip SoundToPlay;

	public float Volume;

	private AudioSource audio;

	public bool alreadyPlayed;

	private void Start()
	{
		audio = GetComponent<AudioSource>();
	}

	private void OnTriggerEnter()
	{
		if (!alreadyPlayed)
		{
			audio.PlayOneShot(SoundToPlay, Volume);
			alreadyPlayed = true;
		}
	}
}
