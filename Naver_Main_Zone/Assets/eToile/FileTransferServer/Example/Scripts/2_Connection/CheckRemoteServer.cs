using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CheckRemoteServer : MonoBehaviour
{
    FileTransferServer _fts;

    InputField _serverIP;
    Dropdown _validServerList;
    InputField _inputName;

    // Use this for initialization
    void Start()
    {
        _fts = GameObject.Find("FileTransferServer").GetComponent<FileTransferServer>();
        _serverIP = transform.Find("InputIP").GetComponent<InputField>();
        _validServerList = transform.Find("DropdownServers").GetComponent<Dropdown>();
        _inputName = transform.Find("InputName").GetComponent<InputField>();
        _inputName.text = _fts._deviceName;
    }

    // ButtonCheck (UI):
    public void CheckStatus()
    {
        _fts.SendPollRequest(_serverIP.text);
        ResetColor();
    }
    void ResetColor()
    {
        _serverIP.image.color = new Color32(237, 240, 211, 255);
    }
    
    // ButtonReset (UI):
    public void ResetServerIP()
    {
        // Clear the server list:
        _fts.ResetDeviceList();
        _validServerList.ClearOptions();
        ResetColor();
    }
    
    // InputFiel UI event:
    public void SetDeviceName()
    {
        _fts._deviceName = _inputName.text;
    }
    
    // FTS event: On Device List Update ()
    public void UpdateDevicesList(List<FTSCore.RemoteDevice> devices)
    {
        List<string> list = _fts.GetDeviceIPList();
        if (list.Count > 0)
        {
            // One or more devices are responding:
            _serverIP.image.color = Color.green;
            _validServerList.ClearOptions();
            _validServerList.AddOptions(list);
        }
        else
        {
            // The list is empty:
            ResetColor();
        }
    }
}
