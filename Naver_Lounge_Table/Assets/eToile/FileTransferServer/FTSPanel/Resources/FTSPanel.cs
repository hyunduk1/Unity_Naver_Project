using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class FTSPanel : MonoBehaviour
{
    public GameObject fileBrowserPrefab;            // The prefab of the file browser.
    GameObject _fileBrowserInstance;

    FileTransferServer _fts;                        // The FTS associated to.
    
    // UI elements:
    Transform _panelUI;                             // The window itself.
    // Main window control:
    Text _caption;                                  // The window caption.
    Image _captionIcon;                             // The icon in the window caption.
    // Common controls:
    Dropdown _devicesDropdown;                      // The list of available FTS devices.
    // Available panels:
    Canvas _actionsCanvas;                          // The canvas containing the fts actions.
    Canvas _devicesCanvas;                          // The canvas containing the devices data.
    Canvas _settingsCanvas;                         // The canvas containing the fts settings.
    Canvas _connectionCanvas;                       // The canvas containing the connection status.
    Canvas _confirmCanvas;                          // The canvas containing the forced downloads confirmation window.

    // Actions panel controls:
    InputField _actInputLocal;
    Text _actProgressU;
    InputField _actInputRemote;
    Text _actProgressD;
    Text _actStats;
    Text _actTextInfo;
    List<string> _actLines = new List<string>();
    
    // Devices panel controls:
    Text _devIndex;
    Text _devName;
    Text _devIP;
    Text _devServer;
    Text _devVersion;
    Text _devOS;

    // Settings panel controls:
    InputField _cfgName;
    InputField _cfgShared;
    InputField _cfgDownloads;
    Toggle _cfgSharedFull;
    Toggle _cfgDownloadFull;
    Toggle _cfgServer;
    Toggle _cfgAuto;

    // Connection panel controls:
    Dropdown _cnnMode;
    InputField _cnnIP;
    InputField _cnnPort;
    Text _cnnStatus;
    Text _cnnChunk;

    // Confirmation panel controls:
    Text _winLabel;
    FTSCore.FileRequest _request;

    // Use this for initialization
    void Awake ()
    {
        // Connects every UI element in the window:
        _panelUI = transform.Find("FTSWindow");
        _caption = _panelUI.Find("Caption").GetComponent<Text>();
        _captionIcon = _panelUI.GetComponent<Image>();
        // Always visible controls:
        _devicesDropdown = _panelUI.Find("DevicesDropdown").GetComponent<Dropdown>();
        // Connect panels:
        _actionsCanvas = _panelUI.Find("CanvasActions").GetComponent<Canvas>();
        _devicesCanvas = _panelUI.Find("CanvasDevices").GetComponent<Canvas>();
        _settingsCanvas = _panelUI.Find("CanvasSettings").GetComponent<Canvas>();
        _connectionCanvas = _panelUI.Find("CanvasConnection").GetComponent<Canvas>();
        _confirmCanvas = _panelUI.Find("CanvasConfirm").GetComponent<Canvas>();
        // Connect the actions panel controls:
        _actInputLocal = _actionsCanvas.transform.Find("InputLocalFile").GetComponent<InputField>();
        _actProgressU = _actionsCanvas.transform.Find("LabelProgressU").GetComponent<Text>();
        _actInputRemote = _actionsCanvas.transform.Find("InputRemoteFile").GetComponent<InputField>();
        _actProgressD = _actionsCanvas.transform.Find("LabelProgressD").GetComponent<Text>();
        _actStats = _actionsCanvas.transform.Find("LabelStats").GetComponent<Text>();
        _actTextInfo = _actionsCanvas.transform.Find("LabelEvents").GetComponent<Text>();
        // Connect the devices panel controls:
        _devIndex = _devicesCanvas.transform.Find("LabelIndex").GetComponent<Text>();
        _devName = _devicesCanvas.transform.Find("LabelName").GetComponent<Text>();
        _devIP = _devicesCanvas.transform.Find("LabelIP").GetComponent<Text>();
        _devServer = _devicesCanvas.transform.Find("LabelServer").GetComponent<Text>();
        _devVersion = _devicesCanvas.transform.Find("LabelVersion").GetComponent<Text>();
        _devOS = _devicesCanvas.transform.Find("LabelOS").GetComponent<Text>();
        // Connect the settings panel controls:
        _cfgName = _settingsCanvas.transform.Find("InputName").GetComponent<InputField>();
        _cfgServer = _settingsCanvas.transform.Find("ToggleServer").GetComponent<Toggle>();
        _cfgAuto = _settingsCanvas.transform.Find("ToggleReceive").GetComponent<Toggle>();
        _cfgSharedFull = _settingsCanvas.transform.Find("ToggleSFull").GetComponent<Toggle>();
        _cfgDownloadFull = _settingsCanvas.transform.Find("ToggleDFull").GetComponent<Toggle>();
        _cfgShared = _settingsCanvas.transform.Find("InputShared").GetComponent<InputField>();
        _cfgDownloads = _settingsCanvas.transform.Find("InputDownloads").GetComponent<InputField>();
        // Connect the connection panel controls:
        _cnnMode = _connectionCanvas.transform.Find("IPDropdown").GetComponent<Dropdown>();
        _cnnIP = _connectionCanvas.transform.Find("InputIP").GetComponent<InputField>();
        _cnnPort = _connectionCanvas.transform.Find("InputPort").GetComponent<InputField>();
        _cnnStatus = _connectionCanvas.transform.Find("LabelStatus").GetComponent<Text>();
        _cnnChunk = _connectionCanvas.transform.Find("LabelChunk").GetComponent<Text>();
        // Connect the confirmation window controls:
        _winLabel = _confirmCanvas.transform.Find("Label").GetComponent<Text>();
        // Show the default panel:
        _confirmCanvas.enabled = false;
        ShowActions();
    }

    // Update all on-screen information:
    void Update()
    {
        if(_fts.IsConnected())
        {
            // Update uploads progress (It shows all available uploads):
            if (_fts._fts.UploadCount() > 0)
            {
                FTSCore.FileUpload upload = _fts._fts._uploads[0];
                _actProgressU.text = "Uploads: " + _fts._fts.UploadCount().ToString() + " [" + (upload.GetProgress() * 100).ToString("0.00") + "%]";
            }
            else
            {
                _actProgressU.text = "Uploads: 0";
            }
            // Update downloads progress (It shows all available downloads):
            if (_fts._fts.RequestCount() > 0)
            {
                FTSCore.FileRequest download = _fts._fts._requests[0];
                _actProgressD.text = "Downloads: " + _fts._fts.RequestCount().ToString() + " [" + (download.GetProgress() * 100).ToString("0.00") + "%]";
            }
            else
            {
                _actProgressD.text = "Downloads: 0";
            }
        }
    }

    /// <summary>Sets the FTS instance to work with.</summary>
    public void SetFTS(FileTransferServer fts)
    {
        _fts = fts;
        UpdateDevices();
    }
    /// <summary>Sets the caption text and color.</summary>
    public void SetCaption(string title)
    {
        _caption.text = title;
    }
    /// <summary>Sets the caption text and color.</summary>
    public void SetCaption(string title, Color32 colour)
    {
        _caption.text = title;
        _caption.color = colour;
    }
    /// <summary>Sets the caption icon and color.</summary>
    public void SetCaptionIcon(Sprite icon)
    {
        if (_captionIcon != null)
            _captionIcon.sprite = icon;
    }
    /// <summary>Sets the caption icon and color.</summary>
    public void SetCaptionIcon(Sprite icon, Color32 colour)
    {
        if (_captionIcon != null)
        {
            _captionIcon.sprite = icon;
            _captionIcon.color = colour;
        }
    }
    /// <summary>Closes the browser window</summary>
    public void Close()
    {
        GameObject.Destroy(gameObject);
    }

    /**************
     * Panel tabs *
     **************/
    /// <summary>Show the actions panel</summary>
    public void ShowActions()
    {
        _actionsCanvas.enabled = true;
        _devicesCanvas.enabled = false;
        _settingsCanvas.enabled = false;
        _connectionCanvas.enabled = false;
    }
    /// <summary>Show the devices panel</summary>
    public void ShowDevices()
    {
        _actionsCanvas.enabled = false;
        _devicesCanvas.enabled = true;
        _settingsCanvas.enabled = false;
        _connectionCanvas.enabled = false;
        D_UpdateDevicesPanel();
    }
    /// <summary>Show the settings panel</summary>
    public void ShowSettings()
    {
        _actionsCanvas.enabled = false;
        _devicesCanvas.enabled = false;
        _settingsCanvas.enabled = true;
        _connectionCanvas.enabled = false;
        S_UpdateSettings();
    }
    /// <summary>Show the connection panel</summary>
    public void ShowConnection()
    {
        _actionsCanvas.enabled = false;
        _devicesCanvas.enabled = false;
        _settingsCanvas.enabled = false;
        _connectionCanvas.enabled = true;
        C_UpdateConnection();
    }
    /// <summary>Update the available devices list</summary>
    public void UpdateDevices()
    {
        _devicesDropdown.ClearOptions();
        List<string> devices = _fts.GetDeviceNamesList();
        // Add ordered index:
        for (int i = 0; i < devices.Count; i++)
        {
            devices[i] = (i + 1).ToString() + " - " + devices[i];
        }
        _devicesDropdown.AddOptions(devices);
        D_UpdateDevicesPanel();
    }
    /// <summary>Check FTS devices</summary>
    public void CheckDevices()
    {
        _fts.SendPollRequest();
    }

    /*****************
     * Actions panel *
     *****************/
    // Open the file browser to select a local file:
    public void A_OpenLocalFileBrowser()
    {
        if(_fileBrowserInstance == null)
        {
            _fileBrowserInstance = GameObject.Instantiate(fileBrowserPrefab);
            FileBrowser browserControl = _fileBrowserInstance.GetComponent<FileBrowser>();
            browserControl.SetCaption("[FTS Panel] Select file to send...");
            browserControl.SetBrowserWindow(A_OnPathSelected, "", false, "F", false, _fts._sharedFolder);
            // Send the FileBrowser to front:
            _fileBrowserInstance.transform.GetChild(0).GetComponent<UIWindowBringToFront>().OnPointerDown(null);
        }
    }
    // File browser event:
    void A_OnPathSelected(string path)
    {
        _actInputLocal.text = path;
    }
    // Send selected file:
    public void A_SendFile()
    {
        if(!string.IsNullOrEmpty(_actInputLocal.text))
            _fts.SendFile(_devicesDropdown.value, _actInputLocal.text);
    }
    // Send a file to all listening devices:
    public void A_BroadcastFile()
    {
        if (!string.IsNullOrEmpty(_actInputLocal.text))
            _fts.BroadcastFile(_actInputLocal.text);
    }
    // Download from the selected device:
    public void A_DownloadFile()
    {
        if (!string.IsNullOrEmpty(_actInputRemote.text))
            _fts.RequestFile(_devicesDropdown.value, _actInputRemote.text);
    }
    // Update download statistics:
    public void A_UpdateStats(float time, float transferRate)
    {
        _actStats.text = "T:" + time.ToString("0.00") + "s. TR: " + transferRate.ToString("0.00") + "Mb/s.";
    }
    // Add event to list:
    public void A_AddTextLine(string textLine)
    {
        // Add the line and control the ammount of lines:
        if(_actLines.Count == 15)
            _actLines.RemoveAt(0);
        _actLines.Add(textLine);
        // Update information on screen:
        _actTextInfo.text = "";
        for(int i = _actLines.Count - 1; i >= 0; i--)
        {
            _actTextInfo.text += _actLines[i] + System.Environment.NewLine;
        }
    }

    /*****************
     * Devices panel *
     *****************/
    // Update devices data panel:
    public void D_UpdateDevicesPanel()
    {
        if(_devicesCanvas.enabled)
        {
            int index = _devicesDropdown.value;
            FTSCore.RemoteDevice device = _fts.GetDevice(index);
            if(device != null)
            {
                _devIndex.text = "Index: " + index.ToString();
                _devName.text = "Name: " + device.name;
                _devIP.text = "IP: " + device.ip;
                _devServer.text = "Server enabled: " + device.isServer.ToString();
                _devVersion.text = "FTS version: " + device.ftsVersion;
                _devOS.text = "OS: " + device.os;
            }
        }
    }

    /******************
     * Settings panel *
     ******************/
    // Update settings pannel:
    void S_UpdateSettings()
    {
        _cfgName.text = _fts._deviceName;
        _cfgShared.text = _fts._sharedFolder;
        _cfgDownloads.text = _fts._downloadFolder;
        _cfgSharedFull.isOn = _fts._sharedFullPath;
        _cfgDownloadFull.isOn = _fts._downloadFullPath;
        _cfgServer.isOn = _fts._serverEnabled;
        _cfgAuto.isOn = _fts._autoDownload;
    }
    // Set the device name dynamically:
    public void S_SetDeviceName()
    {
        _fts._deviceName = _cfgName.text;
    }
    // Enable the server capabilities:
    public void S_SetServer()
    {
        _fts._serverEnabled = _cfgServer.isOn;
    }
    // Enable the automatic download:
    public void S_SetAutoDownload()
    {
        _fts._autoDownload = _cfgAuto.isOn;
    }
    // Enable the to share a full path:
    public void S_FullPathShare()
    {
        _fts._sharedFullPath = _cfgSharedFull.isOn;
    }
    // Enable full path for the downloads folder:
    public void S_FullPathDownload()
    {
        _fts._downloadFullPath = _cfgDownloadFull.isOn;
    }
    // Open the file browser to select a shared folder:
    public void S_OpenSharedFolderBrowser()
    {
        if (_fileBrowserInstance == null)
        {
            _fileBrowserInstance = GameObject.Instantiate(fileBrowserPrefab);
            FileBrowser browserControl = _fileBrowserInstance.GetComponent<FileBrowser>();
            browserControl.SetCaption("[FTS Panel] Select new shared folder...");
            browserControl.SetBrowserWindow(S_OnSharedSelected, "", _fts._sharedFullPath, "D");
            // Set the FileBrowser sort order to render always on top of this:
            _fileBrowserInstance.GetComponent<Canvas>().sortingOrder = _panelUI.root.GetComponent<Canvas>().sortingOrder + 1;
        }
    }
    // File browser event:
    void S_OnSharedSelected(string path)
    {
        _fts._sharedFolder = path;
        _cfgShared.text = path;
    }
    // Set shared folder manualy:
    public void S_SetSharedFolder()
    {
        _fts._sharedFolder = _cfgShared.text;
    }
    // Open the file browser to select a shared folder:
    public void S_OpenDownloadsFolderBrowser()
    {
        if (_fileBrowserInstance == null)
        {
            _fileBrowserInstance = GameObject.Instantiate(fileBrowserPrefab);
            FileBrowser browserControl = _fileBrowserInstance.GetComponent<FileBrowser>();
            browserControl.SetCaption("[FTS Panel] Select new downloads folder...");
            browserControl.SetBrowserWindow(S_OnDownloadsSelected, "", _fts._downloadFullPath, "D");
            // Set the FileBrowser sort order to render always on top of this:
            _fileBrowserInstance.GetComponent<Canvas>().sortingOrder = _panelUI.root.GetComponent<Canvas>().sortingOrder + 1;
        }
    }
    // File browser event:
    void S_OnDownloadsSelected(string path)
    {
        _fts._downloadFolder = path;
        _cfgDownloads.text = path;
    }
    // Set downloads folder manualy:
    public void S_SetDownloadsFolder()
    {
        _fts._downloadFolder = _cfgDownloads.text;
    }
    // Save current settings:
    public void SaveCurrentSettings()
    {
        _fts.SaveSettings(_fts._settingsSaveFile);
    }

    /********************
     * Connection panel *
     ********************/
    // Update connection panel:
    void C_UpdateConnection()
    {
        if (_fts.IsConnected())
        {
            switch (_fts._ip)
            {
                case "":
                    _cnnMode.value = 0;
                    break;
                case "ipv4":
                    _cnnMode.value = 1;
                    break;
                case "ipv6":
                    _cnnMode.value = 2;
                    break;
                default:
                    _cnnMode.value = 3;
                    break;
            }
            _cnnIP.text = _fts.GetIP();
            _cnnPort.text = _fts._port.ToString();
            _cnnStatus.text = "Status: " + (_fts.IsConnected() ? "<color=lime>Connected</color> (Port:" + _fts._port + ")" : "<color=red>Disconnected.</color>");
            _cnnChunk.text = "ChunkSize: " + _fts._chunkSize + " bytes";
        }
        else
        {
            _cnnIP.text = _fts._ip;
            _cnnPort.text = _fts._port.ToString();
            _cnnStatus.text = "Status: <color=red>Disconnected.</color>";
            _cnnChunk.text = "ChunkSize: 0 bytes";
        }
    }
    // Set connection mode:
    public void C_SeletMode()
    {
        C_Disconnect();
        switch(_cnnMode.value)
        {
            case 0: // Auto.
                _cnnIP.interactable = false;
                _fts._ip = "";
                C_Connect();
                break;
            case 1: // Auto ipv4 only.
                _cnnIP.interactable = false;
                _fts._ip = "ipv4";
                C_Connect();
                break;
            case 2: // Auto ipv6 only.
                _cnnIP.interactable = false;
                _fts._ip = "ipv6";
                C_Connect();
                break;
            case 3: // Custom IP.
                _cnnIP.interactable = true;
                break;
        }
    }
    // Set the custom IP:
    public void C_SetCustomIP()
    {
        _fts._ip = _cnnIP.text;
    }
    // Open the connection (also save settings if settings file exists):
    public void C_Connect()
    {
        _cnnIP.interactable = false;
        _fts._port = FileManagement.CustomParser<int>(_cnnPort.text);
        _fts.Connect();
        C_UpdateConnection();
        // Save current settings if the file already exists:
        if (FileManagement.FileExists(_fts._settingsSaveFile))
            _fts.SaveSettings(_fts._settingsSaveFile);
    }
    // Close the connection:
    public void C_Disconnect()
    {
        _fts.Disconnect();
        if (_cnnMode.value == 3)
            _cnnIP.interactable = true;
        C_UpdateConnection();
    }

    /**********************
     * Confirmation panel *
     **********************/
    // Show the confirmation panel:
    public void W_AskForConfirmation(FTSCore.FileRequest request)
    {
        if(_request == null)
        {
            _request = request;
            _winLabel.text = "Confirm download? (" + request._sourceName + ")";
            _confirmCanvas.enabled = true;
        }
    }
    // Confirm the download:
    public void W_Confirm()
    {
        _request.Start();
        _request = null;
        _confirmCanvas.enabled = false;
    }
    // Reject the download:
    public void W_Reject()
    {
        _request.Dispose();
        _request = null;
        _confirmCanvas.enabled = false;
    }
}
