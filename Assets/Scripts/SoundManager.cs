using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class SoundManager : MonoBehaviour
{
	public AudioSource soundSource;
	public static SoundManager instance = null;     //Allows other scripts to call functions from SoundManager.				

	public AudioClip[] knifeSounds;
	public AudioClip appleSound;
	public AudioClip failSound;


	void Awake()
	{
		//Check if there is already an instance of SoundManager
		if (instance == null)
			//if not, set it to this.
			instance = this;
		//If instance already exists:
		else if (instance != this)
			//Destroy this, this enforces our singleton pattern so there can only be one instance of SoundManager.
			Destroy(gameObject);

		//Set SoundManager to DontDestroyOnLoad so that it won't be destroyed when reloading our scene.
		DontDestroyOnLoad(gameObject);
	}

	//Used to play single sound clips.
	public void PlaySingle(AudioClip clip, float volume)
	{
		soundSource.PlayOneShot(clip, volume);
	}

}

