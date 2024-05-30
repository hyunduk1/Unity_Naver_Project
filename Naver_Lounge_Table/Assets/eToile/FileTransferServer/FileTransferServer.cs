// Main namespaces:
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/*
 * File Transferer / File Server V2.0:
 * 
 * Transfer big files locally between devices through UDP.
 * You can send files of any size, the only limitation is the
 * available storage on destination device.
 * FileTransferServer uses a protocol for transferences.
 * 
 * This asset was downloaded from the Unity AssetStore:
 * https://www.assetstore.unity3d.com/#!/content/73518
 * 
 * V 1.1 Features:
 * - Transfer speed increased 400%.
 * 
 * V 1.2 Features:
 * - Connection error event (IP/PORT pair duplicated for UDP).
 * - New feature: Server can "Send" a file.
 * - New feature: Server can "Send" an update.
 * - Performance significantly improved for multiple simultaneus downloads.
 * - New feature: Files can include a path in server side.
 * - New feature: Files and updates can be saved into a custom path.
 * 
 * V 1.3 Features:
 * - New feature: Allows custom local IP (default if left empty).
 * 
 * V 2.0 Features (Major update):
 * - Completely remade!!!
 * - The protocol was improved and optimized.
 * - Works entirely in parallel threads.
 * - 100% event driven.
 * - Transfer speed improved very significantly (From 600KB/s in 1.X up to 10MB/s in 2.X).
 * - Served files are stored in memory during a certain time and then the memory is released.
 * - Files are restored completely in memory to improve speed.
 * - Client and server can check file progress simultaneously.
 * - IPV6 ready.
 * - Supports extended ASCII characters in file names.
 * - VR/AR friendly.
 * - WARNING: Not compatible with 1.X versions (Always upgrade to 2.X).
 * 
 * V 2.1 Features:
 * - New feature: In-game stardard control panel included. It allows most common actions without coding.
 * - New feature: Forced downloads can be paused to allow user aproval.
 * - New feature: Shared and Download folders can have an absolute path (Not forced to PersistentData anymore).
 * - New feature: As an option, a downloaded file can preserve its path of origin.
 * - New feature: Files can be send from any source (even from outide the shared folder or other drives).
 * - New feature: Tools to convert from absolute path to relative to SharedFolder path.
 * - New feature: Files to be uploaded are cached in memory in a parallel thread to grant application responsiveness with very big files.
 * - BugFix: Downloads were not removing themselves.
 * - BugFix: Android fails to create large RX buffers in runtime.
 * 
 * V 2.2 Features:
 * - BugFix: Set the "Sort Order" of the FileBrowser's PopUp on top of the FTSPanel.
 * - My fault: The FTS_Core_WinForms.zip file had useless data increasing the project weight.
 * - New Feature: Includes UDPConnection.cs documentation.
 * 
 * V2.4 Features:
 * - New Feature: Added Light and Dark color themes to the control panel prefab.
 * - BugFix: Implements UDPConnection from SocketsUnderControl V1.1 (Apple issues).
 * - BugFix: Calculates the _chunkSize at Connect() instead of at the OnOpen event.
 * - BugFix: Windows not sorting correctly when clicking the headers.
 * 
 * V2.5:
 * - New feature: Support for files larger than 2GB (Standalone platforms only).
 * - New feature: F2 message includes an error code.
 * - New feature: Allow to load resources from byte[] and files.
 */

public class FileTransferServer : MonoBehaviour
{
    volatile public FTSCore _fts;                               // The core File transfer server object.

    // FTS Settings:
    [Header("File Transfer Server settings")]
    public string _settingsSaveFile = "FTS_Settings.ini";       // File to where all FTS settings will be saved.
    public string _ip = "";                                     // Local IP address (Uses default if none is provided).
    public int _port = 60000;                                   // File transfering port (set your own if you wish).
    [SerializeField] string deviceName = "FTS Device";          // Default _deviceName value for Unity Inspector only.
    public string _deviceName                                   // Friendly name for this device.
    {
        get
        {
            if (_fts != null)
                return _fts._deviceName;
            else
                return deviceName;
        }
        set
        {
            if (_fts != null)
                _fts._deviceName = value;
            deviceName = value;
        }
    }
    [SerializeField] string sharedFolder = "FTSShared";         // Default _sharedFolder value for Unity Inspector only.
    public string _sharedFolder
    {
        get
        {
            if (_fts != null)
                return _fts._sharedFolder;
            else
                return sharedFolder;
        }
        set
        {
            string path = FileManagement.NormalizePath(value);
            if (_fts != null)
                _fts._sharedFolder = path;
            sharedFolder = path;
        }
    }
    [SerializeField] bool sharedIsFullPath = false;             // Enables the shared folder to be a full absolute path.
    public bool _sharedFullPath
    {
        get
        {
            if (_fts != null)
                return _fts._sharedFullPath;
            else
                return sharedIsFullPath;
        }
        set
        {
            if (_fts != null)
                _fts._sharedFullPath = value;
            sharedIsFullPath = value;
        }
    }
    [SerializeField] string downloadFolder = "FTSDownloads";    // Default _downloadFolder value for Unity Inspector only.
    public string _downloadFolder
    {
        get
        {
            if (_fts != null)
                return _fts._downloadFolder;
            else
                return downloadFolder;
        }
        set
        {
            string path = FileManagement.NormalizePath(value);
            if (_fts != null)
                _fts._downloadFolder = path;
            downloadFolder = path;
        }
    }
    [SerializeField] bool downloadIsFullPath = false;           // Enables the download folder to be a full absolute path.
    public bool _downloadFullPath
    {
        get
        {
            if (_fts != null)
                return _fts._downloadFullPath;
            else
                return downloadIsFullPath;
        }
        set
        {
            if (_fts != null)
                _fts._downloadFullPath = value;
            downloadIsFullPath = value;
        }
    }
    public int _chunkSize                                       // UDP automatic maximum available chunksize.
    {
        get
        {
            if (_fts != null)
                return _fts._chunkSize;
            else
                return 0;
        }
    }
    [SerializeField] bool serverEnabled = true;                 // Default _serverEnabled value for Unity Inspector only.
    public bool _serverEnabled                                  // Enable or disable server capabilities.
    {
        get
        {
            if (_fts != null)
                return _fts._serverEnabled;
            else
                return serverEnabled;
        }
        set
        {
            if (_fts != null)
                _fts._serverEnabled = value;
            serverEnabled = value;
        }
    }
    [SerializeField] bool autoDownload = true;                  // Default _autoDownload value for Unity Inspector only.
    public bool _autoDownload                                   // Enable or disable the automatic forced download.
    {
        get
        {
            if (_fts != null)
                return _fts._autoDownload;
            else
                return autoDownload;
        }
        set
        {
            if (_fts != null)
                _fts._autoDownload = value;
            autoDownload = value;
        }
    }

    // Control panel:
    [Header("FTS panel settings")]
    public GameObject _ftsPanel;                                // Control panel for this fts instance.
    FTSPanel _ftsPanelInstance;                                 // This is the control of the current control panel instance.
    // FTS panel settings:
    public bool _showPanelAtStart = false;                      // Open the FTS control panel on awake.

    // Custom event to pass an int and a string as arguments:
    [System.Serializable]
    public class ExceptionEvent : UnityEvent<int, string> { }
    class UnityEventError
    {
        public ExceptionEvent _event;
        public int _code;
        public string _message;
        public UnityEventError(ExceptionEvent callback, int code, string message)
        {
            _event = callback;
            _code = code;
            _message = message;
        }
        public void Invoke()
        {
            _event.Invoke(_code, _message);
        }
    }
    // Custom event to pass a List of RemoteDevice as argument:
    [System.Serializable]
    public class FTSDeviceEvent : UnityEvent<List<FTSCore.RemoteDevice>> { }
    class UnityEventFTSDevice
    {
        public FTSDeviceEvent _event;
        public List<FTSCore.RemoteDevice> _deviceList;
        public UnityEventFTSDevice(FTSDeviceEvent callback, List<FTSCore.RemoteDevice> deviceList)
        {
            _event = callback;
            _deviceList = deviceList;
        }
        public void Invoke()
        {
            _event.Invoke(_deviceList);
        }
    }
    // Custom event to pass a FileRequest as argument:
    [System.Serializable]
    public class FTSFileEvent : UnityEvent<FTSCore.FileRequest> { }
    class UnityEventFTSFile
    {
        public FTSFileEvent _event;
        public FTSCore.FileRequest _fileRequest;
        public UnityEventFTSFile(FTSFileEvent callback, FTSCore.FileRequest fileRequest)
        {
            _event = callback;
            _fileRequest = fileRequest;
        }
        public void Invoke()
        {
            _event.Invoke(_fileRequest);
        }
    }
    // Custom event to pass au FileUpload as argument:
    [System.Serializable]
    public class FTSUploadEvent : UnityEvent<FTSCore.FileUpload> { }
    class UnityEventFTSUpload
    {
        public FTSUploadEvent _event;
        public FTSCore.FileUpload _upload;
        public UnityEventFTSUpload(FTSUploadEvent callback, FTSCore.FileUpload upload)
        {
            _event = callback;
            _upload = upload;
        }
        public void Invoke()
        {
            _event.Invoke(_upload);
        }
    }

    // FTS events to be set in Unity editor (invoked from the Unity's main thread):
    [Header("File Transfer Server events")]
    public volatile ExceptionEvent onError;                     // When an error has occurred.
    public volatile FTSDeviceEvent onDevicesListUpdate;         // There are new devices available in the network.
    public volatile FTSFileEvent onFileDownload;                // A file was downloaded properly.
    public volatile FTSFileEvent onFileNotFound;                // Server doesn't has the requested file.
    public volatile FTSFileEvent onForcedDownload;              // A forced download has started.
    public volatile FTSFileEvent onFileTimeout;                 // Server is not responding to file request after retrying "rxFileRetryMaxCnt" times.
    public volatile FTSUploadEvent onUploadBegin;               // The remote device has started a download.
    public volatile FTSUploadEvent onUpload;                    // The remote device has finished receiving the upload.
    public volatile FTSUploadEvent onUploadTimeout;             // The upload couldn't be sent.

    // Event buffer:
    volatile List<object> _eventList = new List<object>();

    /******************
     * MonoBehaviour: *
     ******************/
    void Awake()
    {
        Debug.Log("[FileTransferServer.Awake] Persitent data path: " + FileManagement.persistentDataPath);
        // Set editor custom user values:
        _deviceName = deviceName;
        _serverEnabled = serverEnabled;
        _autoDownload = autoDownload;
        // Load settings (Saved settings are prioritary over editor settings):
        LoadSettings(_settingsSaveFile);
        // File transfer server:
        Connect();
        Debug.Log("[FileTransferServer.Awake] ChunkSize: " + _fts._chunkSize.ToString());
        Debug.Log("[FileTransferServer.Awake] IP: " + _fts.GetIP());
        Debug.Log("[FileTransferServer.Awake] SecIP: " + _fts.GetIP(true));
        // Open the panel:
        if (_showPanelAtStart)
            OpenControlPanel();
    }
    void Update()
    {
        // Invoke all the UnityEvents in the main thread:
        while (_eventList.Count > 0)
        {
            List<object> tempList = new List<object>(_eventList);
            _eventList.RemoveRange(0, tempList.Count);

            for (int i = 0; i < tempList.Count; i++)
            {
                switch (tempList[i].ToString())
                {
                    case "FileTransferServer+UnityEventError":
                        UnityEventError errorEvent = tempList[i] as UnityEventError;
                        if (_ftsPanelInstance != null) _ftsPanelInstance.A_AddTextLine("[" + errorEvent._code.ToString() + "] " + errorEvent._message);
                        errorEvent.Invoke();
                        break;
                    case "FileTransferServer+UnityEventFTSDevice":
                        if (_ftsPanelInstance != null) _ftsPanelInstance.UpdateDevices();
                        (tempList[i] as UnityEventFTSDevice).Invoke();
                        break;
                    case "FileTransferServer+UnityEventFTSFile":
                        UnityEventFTSFile downloadEvent = tempList[i] as UnityEventFTSFile;
                        if (_ftsPanelInstance != null)
                        {
                            // Differentiate between regular and forced downloads.
                            if(downloadEvent._fileRequest.GetStatus() == FTSCore.FileStatus.inactive && !_autoDownload)
                            {
                                // Forced download request, needs confirmation:
                                _ftsPanelInstance.W_AskForConfirmation(downloadEvent._fileRequest);
                            }
                            else if(downloadEvent._fileRequest.GetStatus() == FTSCore.FileStatus.finished)
                            {
                                // Regular download:
                                _ftsPanelInstance.A_AddTextLine("Download: " + downloadEvent._fileRequest._sourceName + " (T:" + downloadEvent._fileRequest._elapsedTime.ToString("0.00") + "s. TR:" + downloadEvent._fileRequest._transferRate.ToString("0.00") + "Mb/s)");
                                _ftsPanelInstance.A_UpdateStats(downloadEvent._fileRequest._elapsedTime, downloadEvent._fileRequest._transferRate);
                            }
                        }
                        downloadEvent.Invoke();
                        break;
                    case "FileTransferServer+UnityEventFTSUpload":
                        UnityEventFTSUpload uploadEvent = tempList[i] as UnityEventFTSUpload;
                        if (_ftsPanelInstance != null) _ftsPanelInstance.A_AddTextLine("Upload: " + uploadEvent._upload.GetName() + " (" + uploadEvent._upload.GetStatus() + ")");
                        uploadEvent.Invoke();
                        break;
                    default:
                        (tempList[i] as UnityEvent).Invoke();
                        break;
                }
            }
        }
    }
    void OnApplicationQuit()
    {
        if(_fts != null)
            _fts.Dispose();
    }
    void OnDestroy()
    {
        if (_fts != null)
            _fts.Dispose();
    }

    /***********************
     * Connection control: *
     ***********************/
    public void Connect()
    {
        if (!IsConnected())
        {
            if (_fts != null)
                _fts.Dispose();
            _fts = new FTSCore(_port, ErrorEvent, DeviceListUpdatedEvent, FileDownloadEvent, FileNotFoundEvent, ForcedDownloadEvent, FileTimeoutEvent, UploadBeginEvent, UploadEvent, UploadTimeoutEvent, _ip, _sharedFolder, _downloadFolder, _sharedFullPath, _downloadFullPath);
            _fts._deviceName = deviceName;
            _fts._serverEnabled = serverEnabled;
            _fts._autoDownload = autoDownload;
        }
        else
            Debug.Log("[FileTransferServer.Connect] Already connected.");
    }
    public void Disconnect()
    {
        if (IsConnected())
        {
            _fts.Dispose();
            _fts = null;
        }
        else
            Debug.Log("[FileTransferServer.Disconnect] Already disconnected.");
    }
    public string GetIP(bool secondary = false)
    {
        if (IsConnected())
            return _fts.GetIP(secondary);
        else
        {
            Debug.Log("[FileTransferServer.GetIP] FTS is disconnected.");
            return "";
        }
    }
    public bool IsConnected()
    {
        if (_fts != null)
            return _fts.IsConnected();
        else
            return false;
    }

    /********************
     * Event callbacks: *
     ********************/
    void ErrorEvent(int code, string message)
    {
        //print("[FTS] Error: " + code + " - " + message);
        _eventList.Add(new UnityEventError(onError, code, message));
    }
    void DeviceListUpdatedEvent(List<FTSCore.RemoteDevice> deviceList)
    {
        //print("[FTS] List update.");
        _eventList.Add(new UnityEventFTSDevice(onDevicesListUpdate, deviceList));
    }
    void FileDownloadEvent(FTSCore.FileRequest file)
    {
        //print("[FTS] Download." + file._sourceName);
        _eventList.Add(new UnityEventFTSFile(onFileDownload, file));
    }
    void FileNotFoundEvent(FTSCore.FileRequest file)
    {
        //print("[FTS] File not found.");
        _eventList.Add(new UnityEventFTSFile(onFileNotFound, file));
    }
    void ForcedDownloadEvent(FTSCore.FileRequest file)
    {
        //print("[FTS] Forced download." + file._sourceName);
        _eventList.Add(new UnityEventFTSFile(onForcedDownload, file));
    }
    void FileTimeoutEvent(FTSCore.FileRequest file)
    {
        //print("[FTS] File timeout.");
        _eventList.Add(new UnityEventFTSFile(onFileTimeout, file));
    }
    void UploadBeginEvent(FTSCore.FileUpload upload)
    {
        //print("[FTS] Upload started.");
        _eventList.Add(new UnityEventFTSUpload(onUploadBegin, upload));
    }
    void UploadEvent(FTSCore.FileUpload upload)
    {
        //print("[FTS] Upload finished.");
        _eventList.Add(new UnityEventFTSUpload(onUpload, upload));
    }
    void UploadTimeoutEvent(FTSCore.FileUpload upload)
    {
        //print("[FTS] Upload timeout.");
        _eventList.Add(new UnityEventFTSUpload(onUploadTimeout, upload));
    }

    /*******************
     * Remote devices: *
     *******************/
    /// <summary> Send the message to discover device(s) </summary>
    public void SendPollRequest(string ip = "")
    {
        if(IsConnected())
            _fts.SendPollRequest(ip);
        else
            Debug.Log("[FileTransferServer.SendPollRequest] FTS is disconnected.");
    }
    /// <summary> Clear the devices list </summary>
    public void ResetDeviceList()
    {
        if(IsConnected())
            _fts.ResetDeviceList();
        else
            Debug.Log("[FileTransferServer.ResetDeviceList] FTS is disconnected.");
    }
    /// <summary> Get the list of devices </summary>
    public List<FTSCore.RemoteDevice> GetDevicesList()
    {
        if (IsConnected())
            return _fts._devices;
        else
            return new List<FTSCore.RemoteDevice>();
    }
    /// <summary> Gets a particular device from the list </summary>
    public FTSCore.RemoteDevice GetDevice(int index)
    {
        if (_fts != null && index >= 0 && index < _fts._devices.Count)
            return _fts._devices[index];
        else
            return null;
    }
    /// <summary> Get the list of device names </summary>
    public List<string> GetDeviceNamesList()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < _fts._devices.Count; i++)
        {
            if (_fts._devices[i] != null)
                names.Add(_fts._devices[i].name);
        }
        return names;
    }
    /// <summary> Get the list of device IPs </summary>
    public List<string> GetDeviceIPList()
    {
        List<string> ips = new List<string>();
        for (int i = 0; i < _fts._devices.Count; i++)
        {
            if (_fts._devices[i] != null)
                ips.Add(_fts._devices[i].ip);
        }
        return ips;
    }

    /*******************
     * Transfer files: *
     *******************/
    /// <summary> Request a file by Server IP </summary>
    public FTSCore.FileRequest RequestFile(string deviceIP, string file, string saveName = "")
    {
        if(IsConnected())
            return _fts.RequestFile(deviceIP, file, saveName, autoDownload);
        Debug.Log("[FileTransferServer.RequestFile] FTS is disconnected.");
        return null;
    }
    /// <summary> Request a file by Server index </summary>
    public FTSCore.FileRequest RequestFile(int deviceIndex, string file, string saveName = "")
    {
        if(IsConnected())
            return _fts.RequestFile(deviceIndex, file, saveName, autoDownload);
        Debug.Log("[FileTransferServer.RequestFile] FTS is disconnected.");
        return null;
    }
    /// <summary> Sends a file to a known client by IP </summary>
    public FTSCore.FileUpload SendFile(string deviceIP, string name, bool fullPath = false)
    {
        if(IsConnected())
            return _fts.SendFile(deviceIP, name, fullPath);
        Debug.Log("[FileTransferServer.SendFile] FTS is disconnected.");
        return null;
    }
    /// <summary> Sends a file to a known client by index </summary>
    public FTSCore.FileUpload SendFile(int deviceIndex, string name, bool fullPath = false)
    {
        if(IsConnected())
            return _fts.SendFile(deviceIndex, name, fullPath);
        Debug.Log("[FileTransferServer.SendFile] FTS is disconnected.");
        return null;
    }
    /// <summary> Sends a file to all devices </summary>
    public void BroadcastFile(string name)
    {
        if(IsConnected())
            _fts.BroadcastFile(name);
        else
            Debug.Log("[FileTransferServer.BroadcastFile] FTS is disconnected.");
    }

    /**********************
     * Resources control: *
     **********************/
    /// <summary> Loads a resource manually </summary>
    public void LoadResource(string name, float timeout = System.Threading.Timeout.Infinite, bool fullPath = false)
    {
        if(IsConnected())
            _fts.LoadResource(name, timeout, fullPath);
        else
            Debug.Log("[FileTransferServer.LoadResource] FTS is disconnected.");
    }
    /// <summary> Loads a resource manually </summary>
    public void LoadResource(string name, byte[] content, float timeout = System.Threading.Timeout.Infinite)
    {
        if (IsConnected())
            _fts.LoadResource(name, content, timeout);
        else
            Debug.Log("[FileTransferServer.LoadResource] FTS is disconnected.");
    }

    /**********
     * Tools: *
     **********/
    /// <summary> Open the integrated control panel </summary>
    public void OpenControlPanel()
    {
        if(_ftsPanelInstance == null)
        {
            GameObject temp = GameObject.Instantiate(_ftsPanel);
            _ftsPanelInstance = temp.GetComponent<FTSPanel>();
            _ftsPanelInstance.SetFTS(this);
        }
    }
    /// <summary> Open the integrated control panel </summary>
    public void LoadSettings(string fileName)
    {
        if (FileManagement.FileExists(fileName))
        {
            FM_IniFile _settings = FileManagement.ImportIniFile(fileName);
            _ip = _settings.GetKey("LocalIP", _ip);
            _port = _settings.GetKey("Port", _port);
            _deviceName = _settings.GetKey("DeviceName", _deviceName);
            _sharedFolder = _settings.GetKey("SharedFolder", _sharedFolder);
            _sharedFullPath = _settings.GetKey("SharedFullPath", _sharedFullPath);
            _downloadFolder = _settings.GetKey("DownloadFolder", _downloadFolder);
            _downloadFullPath = _settings.GetKey("DownloadFullPath", _downloadFullPath);
            _serverEnabled = _settings.GetKey("ServerEnabled", _serverEnabled);
            _autoDownload = _settings.GetKey("AutoDownload", _autoDownload);
        }
    }
    /// <summary> Open the integrated control panel </summary>
    public void SaveSettings(string fileName)
    {
        FM_IniFile _settings = new FM_IniFile(fileName);
        _settings.AddKey("LocalIP", _ip);
        _settings.AddKey("Port", _port);
        _settings.AddKey("DeviceName", _deviceName);
        _settings.AddKey("SharedFolder", _sharedFolder);
        _settings.AddKey("SharedFullPath", _sharedFullPath);
        _settings.AddKey("DownloadFolder", _downloadFolder);
        _settings.AddKey("DownloadFullPath", _downloadFullPath);
        _settings.AddKey("ServerEnabled", _serverEnabled);
        _settings.AddKey("AutoDownload", _autoDownload);
        _settings.Save(fileName);
    }
    ///<summary> Obtains the relative path to the current SharedFolder </summary>
    public string GetRelativeToSharedFolder(string absolutePath)
    {
        return _fts.GetRelativeToSharedFolder(absolutePath);
    }
    ///<summary> Checks if the provided path is into the current shared folder </summary>
    public bool IsIntoSharedFolder(string absolutePath)
    {
        return _fts.IsIntoSharedFolder(absolutePath);
    }
}
