using UnityEngine;
using UnityEngine.UI;

/*
 * Generic PopUp message control.
 * The reference parent is just to attach itself to any available canvas (otherwise it will not be shown)
 * The automatic destroy can be also set in seconds.
 */

public class PopUpFTS : MonoBehaviour
{
    Animator _anim;
    Text _msg;
    string _message = "";
    FTSCore.FileRequest _request;
    FTSCore.FileUpload _upload;

	// Use this for initialization
	void Awake ()
    {
        _anim = gameObject.GetComponent<Animator>();
        _msg = transform.Find("Message").GetComponent<Text>();
	}

    void Update()
    {
        if(_request != null)
        {
            if(_request.GetStatus() == FTSCore.FileStatus.started)
                _msg.text = _message + " - " + Mathf.FloorToInt(_request.GetProgress() * 100f) + "%" + "(R: " + _request._retries.ToString() + ")";
            else if (_request.GetStatus() == FTSCore.FileStatus.finished)
                _msg.text = _message + " - 100%" + "(R: " + _request._retries.ToString() + ")";

        }
        else if(_upload != null)
        {
            if(_upload.GetStatus() == FTSCore.FileStatus.started)
                _msg.text = _message + " - " + Mathf.FloorToInt(_upload.GetProgress() * 100f) + "%";
            else if (_upload.GetStatus() == FTSCore.FileStatus.finished)
                _msg.text = _message + " - 100%";
        }
    }

    /// <summary>Sets the message and shows the PopUp</summary>
    public void SetMessage(string message, Transform parent, float destroy = 0f, FTSCore.FileRequest request= null, FTSCore.FileUpload upload = null)
    {
        if (destroy > 0f) Invoke("Close", destroy);         // Hides the message automatically.
        _request = request;                                 // Used to show the progress.
        _upload = upload;                                   // Used to show the progress.
        transform.SetParent(parent.root, false);            // Sets the parent.
        _message = message;                                 // Sets the text memory for further updates.
        _msg.text = _message;                               // Sets the text.
        _anim.Play("FadeIn");                               // Starts the animation to be shown.
    }
	
    public void Close()
    {
        _anim.Play("FadeOut");
    }

    public void Destroy()
    {
        GameObject.Destroy(gameObject);
    }
}
