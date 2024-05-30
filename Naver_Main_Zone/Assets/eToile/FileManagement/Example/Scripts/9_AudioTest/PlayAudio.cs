using UnityEngine;
using UnityEngine.UI;

/*
 * Example script to demonstrate the use of audio capabilities.
 * 
 * Loads and plays audio files from any selected path (Using the file browser).
 */

public class PlayAudio : MonoBehaviour {

    public GameObject fileBrowser;  // Drag the FileBrowser prefab here (in the editor).
    GameObject browserInstance;     // The opened browser instance.
    public Sprite captionIcon;      // Drag some Icon to be shown in the caption.
    AudioSource _source;
    Text playerText;

    // Use this for initialization
    void Start ()
    {
        _source = transform.GetComponent<AudioSource>();
        playerText = transform.Find("PlayerText").GetComponent<Text>();
    }

    // Instantiates a file browser and sets the path selection event:
    public void OpenFileBrowser()
    {
        // Creates a browser windows and sets its behaviour mode:
        if(browserInstance == null)
        {
            browserInstance = GameObject.Instantiate(fileBrowser);
            browserInstance.GetComponent<FileBrowser>().SetBrowserWindow(OnPathSelected, FileManagement.persistentDataPath, true);
            browserInstance.GetComponent<FileBrowser>().SetCaption("Audio file browser...");
            browserInstance.GetComponent<FileBrowser>().SetCaptionIcon(captionIcon);
            string[] filter = { ".wav", ".mp3", ".ogg" };
            browserInstance.GetComponent<FileBrowser>().SetBrowserWindowFilter(filter);
        }
    }

    // You should use this function signature in order to receive properly:
    void OnPathSelected(string path)
    {
        playerText.text = FileManagement.GetFileName(path);
        AudioClip _clip = FileManagement.ImportAudio(path, false, false, true);
        // The clip will be null if not parsed correctly:
        if(_clip != null)
        {
            _source.clip = _clip;
            _source.Play();
        }
        else
        {
            playerText.text = "-";
        }
    }

    public void Play()
    {
        if (_source.clip != null)
            _source.Play();
        else
            playerText.text = "-";
    }

    public void Pause()
    {
        if (_source.isPlaying)
            _source.Pause();
    }

    public void Stop()
    {
        if (_source.isPlaying)
            _source.Stop();
    }
}
