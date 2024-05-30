using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectFile : MonoBehaviour {

    public GameObject fileBrowser;

    FileTransferServer fts;
    Dropdown validServerList;
    Toggle unrestricted;
    Text selection;

    // Use this for initialization
    void Start ()
    {
        fts = GameObject.Find("FileTransferServer").GetComponent<FileTransferServer>();
        selection = transform.Find("Selection").Find("Text").GetComponent<Text>();
        validServerList = transform.Find("DropdownDevices").GetComponent<Dropdown>();
        unrestricted = transform.Find("ToggleFullPath").GetComponent<Toggle>();
    }

    // Create a FileBrowser window (with default options):
    public void OpenFileBrowser()
    {
        GameObject browserInstance = GameObject.Instantiate(fileBrowser);
        string iniPath = "";
        string lockPath = fts._sharedFolder;
        if (unrestricted.isOn)
        {
            iniPath = FileManagement.persistentDataPath;
            lockPath = "";
        }
        browserInstance.GetComponent<FileBrowser>().SetBrowserWindow(OnPathSelected, iniPath, unrestricted.isOn || fts._sharedFullPath, "F", false, lockPath);
    }
    // File browser event:
    void OnPathSelected(string path)
    {
        selection.text = path;
        // Remove shared folder from the selected full path (file requests must be relative to shared folder):
        if (fts._sharedFullPath && fts.IsIntoSharedFolder(path))
            selection.text = fts.GetRelativeToSharedFolder(path);
    }

    // FTS event: On Devices List Update (List`1)
    public void UpdateDevicesList(List<FTSCore.RemoteDevice> devices)
    {
        validServerList.ClearOptions();
        // Updates the available server list:
        validServerList.AddOptions(fts.GetDeviceIPList());
    }

    public string GetSelection()
    {
        return selection.text;
    }

    public bool IsFullPath()
    {
        return unrestricted.isOn;
    }
}
