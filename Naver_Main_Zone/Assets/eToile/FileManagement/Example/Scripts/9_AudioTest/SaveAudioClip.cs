using UnityEngine;
using System.Collections;

public class SaveAudioClip : MonoBehaviour {

    AudioSource source;

	// Use this for initialization
	void Start () {
        source = gameObject.GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void SaveLoadedAudioClip()
    {
        if(source.clip != null)
        {
            FileManagement.SaveAudio("MyFile.wav", source.clip);
        }
    }
}
