using UnityEngine;
using UnityEngine.UI;

public class LocalServerData : MonoBehaviour {

    FileTransferServer _fts;

    Text _localIPV4;
    Text _chunkSize;
    Text _localIPV6;

    // Use this for initialization
    void Start ()
    {
        _fts = GameObject.Find("FileTransferServer").GetComponent<FileTransferServer>();


        _localIPV4 = transform.Find("LabelLocalIPV4").Find("Text").GetComponent<Text>();
        _localIPV4.text = _fts.GetIP();

        _localIPV6 = transform.Find("LabelLocalIPV6").Find("Text").GetComponent<Text>();
        _localIPV6.text = _fts.GetIP(true);

        _chunkSize = transform.Find("LabelChunk").Find("Text").GetComponent<Text>();
        _chunkSize.text = "Chunksize: " + _fts._chunkSize.ToString();
    }
}
