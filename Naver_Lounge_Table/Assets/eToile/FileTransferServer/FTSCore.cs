using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

/*
 * FTSCore helps to transfer files between different devices through UDP.
 * This class can work as a server or as a client at demand.
 * There's no need of any dedicated server in order to transfer files between the connected devices.
 * Every requested paths are relative to the shared folder (server has only one shared folder).
 * 
 * ----------------------------------------------------------------------
 * FTS protocol:                                                        :
 * ----------------------------------------------------------------------
 * F0;isServer;name;FTSVersion;OS                                       : Devices polling request (Broadcast query).
 * F1;isServer;name;FTSVersion;OS                                       : Device polling response (Responds within a random delay to avoid collisions).
 * ----------------------------------------------------------------------
 * F2;SourceFile;errorCode                                              : Requested transference is not possible.
 * F3;SourceFile;part;chunkSize                                         : Request a chunk, or confirms the reception requesting the next one ([0-N] to request, or [-1 = finished] [-2 = needs confirmation]).
 * F4;SourceFile;part;chunkSize;total;fileSize;BinaryChunk              : Some partial file (part of a requested file).
 * F5;SourceFile                                                        : Some Server requested a transference (can be broadcasted).
 * ----------------------------------------------------------------------
 * F2 error codes:
 * 0 - File not found.
 * 1 - The device is not enabled as a server.
 * 2 - File too large for the platform.
 * 
 * Events templates:
 
    void OnFTSError(int code, string message)
    {}
    void OnFTSDevicesUpdate(System.Collections.Generic.List<RemoteDevice> devices)
    {}
    
    void OnFTSDownload(FileRequest file)
    {}
    void OnFTSNotFound(FileRequest file)
    {}
    void OnFTSForcedDownload(FileRequest file)
    {}
    void OnFTSFileTimeout(FileRequest file)
    {}

    void OnFTSUploadBegin(FileUpload fileUpload)
    {}
    void OnFTSUpload(FileUpload fileUpload)
    {}
    void OnFTSUploadTimeout(FileUpload fileUpload)
    {}
 */

/*
 * V2.0 - V2.1:
 * - First release.
 * 
 * V2.2:
 * - New feature: Forced downloads can be paused to allow user aproval.
 * - New feature: Shared and Download folders can have an absolute path (Not forced to PersistentData anymore).
 * - New feature: As an option, a downloaded file can preserve its path of origin.
 * - New feature: Files can be send from any source (even outide the shared folder or drive).
 * - New feature: Tools to convert from absolute path to relative to SharedFolder path.
 * - BugFix: Downloads were not removing themselves in some cases.
 * - BugFix: Android fails to create large RX buffers in runtime.
 * 
 * V2.3:
 * - BugFix: Implements UDPConnection from SocketsUnderControl V1.1 (Apple issues).
 * - BugFix: Calculates the _chunkSize at Connect() instead of at the OnOpen event.
 * 
 * V2.5:
 * - New feature: Support for files larger than 2GB (Standalone platforms only).
 * - New feature: F2 message includes an error code.
 * - New feature: Allow to load resources from byte[] and files.
 */

// Main FTS class:
public class FTSCore
{
    volatile UDPConnection _connection;                                     // UDP connection object.
    volatile System.Random _rnd = new System.Random();                      // Random number generator (without a seed).

    // Settings:
    volatile public string _deviceName = "FTS Device";                      // Human friendly name for this device.
    volatile string _ftsVersion = "FTS 2.5";                                // The FTS protocol identifier.
    volatile public string _sharedFolder = "";                              // Shared folder (Full path or relative to persistentDataPath).
    volatile public bool _sharedFullPath = false;                           // If enabled the _sharedFolder is treated as a full path (losing access to StreamingAssets or embedded resources).
    volatile public bool _serverEnabled = true;                             // Enable or disable Server features only (client still working).
    volatile public bool _autoDownload = true;                              // Accept a forced download automatically.
    volatile public int _resourcesTimeout = 30000;                          // Time in milliseconds to keep resources in memory.
    volatile public string _downloadFolder = "";                            // Download folder (Full path or relative to persistentDataPath).
    volatile public bool _downloadFullPath = false;                         // If enabled the _downloadFolder is treated as a full path (can fail if no write permission).
    volatile public int _chunkSize = 65536;                                 // 64K is the highest possible size (Assigned automatically by the UDP port at start).

    // FTS Event definition:
    public delegate void EventString(string message);                       // Event delegate with a string as argument.
    public delegate void EventDevices(List<RemoteDevice> devices);          // Event delegate with RemoteDevices as argument.
    public delegate void EventFile(FileRequest file);                       // Event with the downloaded file information as argument.
    public delegate void EventError(int code, string message);              // Event with error code and error description.
    public delegate void EventUpload(FileUpload fileUpload);                // Event with the file status to be uploaded to a remote device.

    // FTS Events:
    volatile public EventError _onError;                                    // Some error has happened, the event includes the information.
    volatile public EventDevices _onDeviceListUpdate;                       // Devices were added or removed.

    volatile public EventFile _onFileDownload;                              // Event fired when a requested file was successfully downloaded.
    volatile public EventFile _onFileNotFound;                              // Event fired when a requested file wasn't found in server side.
    volatile public EventFile _onForcedDownload;                            // Event fired when a file started to be received without being requested.
    volatile public EventFile _onFileTimeout;                               // Event fired when a download hasn't response during a long period.

    volatile public EventUpload _onUploadBegin;                             // Event fired when an upload has started (requested remotelly).
    volatile public EventUpload _onFileUpload;                              // Event fired when an upload has finished.
    volatile public EventUpload _onUploadTimeout;                           // Event fired when a file wasn't transfered during a long period.

    // Remote devices:
    public class RemoteDevice
    {
        public volatile string ip = "";                                     // The ip address from some detected device (IPV4 or IPV6).
        public volatile string name = "";                                   // The device name.
        public volatile string os = "";                                     // Device platform.
        public volatile string ftsVersion = "";                             // The FTS version.
        public volatile bool isServer = false;                              // The device is enabled as server.
    }
    volatile public List<RemoteDevice> _devices = new List<RemoteDevice>(); // The list of all detected devices.
    private readonly object _lockDevices = new object();                    // Locks the access to the devices list.

    // A representation of a file to be downloaded, and all its internal control:
    public class FileRequest
    {
        volatile UDPConnection _connection;                                 // Main UDP connection object.
        volatile EventFile _downloadEvent;                                  // This event si called when the file is successfully downloaded.
        volatile EventFile _timeoutEvent;                                   // This event is called when the download doesn't starts.
        volatile EventFile _deleteMeCallBack;                               // Event to delete itself from the main list.
        // Source file:
        public volatile string _remoteIP = "";                              // The address from where the file is being downloaded.
        public volatile string _sourceName = "";                            // File path and name in server side.
        // Destination name:
        public volatile string _saveName = "";                              // File name (or rename) to save the downloaded file (should include the path relative to _downloadFolder).
        volatile string _downloadFolder = "";                               // The default downloads folder.
        volatile bool _downloadFullPath = false;                            // Indicates if _downloadFolder is a full path or is relative.
        // File properties:
        public volatile int _chunkSize;                                     // The chunk size of this download (_chunkSize received from server, equal or less than local _chunkSize).
        volatile FileStatus _status = FileStatus.inactive;                  // The current status of the download.
        volatile bool[] _chunkList;                                         // Flag-list of received chunks in order to grant completeness.
        volatile int _chunks = 0;                                           // The total count of chunks for this download.
        volatile byte[] _rFile = null;                                      // Received file reconstructed in memory.
        volatile MemoryMappedFile _rmmf = null;                             // Received file mapped to memory.
        volatile MemoryMappedViewAccessor _rfa = null;                      // Controller for the _rmmf.
        // Integrity timer:
        volatile Timer _requestTimer;                                       // Verifies file integrity and requests the missing parts.
        volatile int _timeout = 1500;                                       // Timeout in miliseconds to start the download.
        volatile int _timeoutCounter = 0;                                   // Counter to detect timeout (fixed to 10 times).
        // Stats:
        volatile Stopwatch _watch;                                          // Mesures the download time (from the first chunk, not from the request).
        public volatile float _elapsedTime = -1f;                           // Download time in seconds.
        public volatile float _transferRate = 0f;                           // Transfer rate in MB/s.
        public volatile int _retries = 0;                                   // Chunks requested separately (Missing chunks count).
        public long _size = 0;                                              // File size in bytes.

        ///<summary>Constructor</summary>
        public FileRequest(UDPConnection connection, EventFile downloadEvent, EventFile timeoutEvent, EventFile deleteMeCallback, string remoteIP, string sourceFile, string saveName, int chunkSize, string downloadFolder = "", bool downloadFullPath = false, bool autoDownload = true)
        {
            _connection = connection;                                       // The connection is not created in this object, it's created outside.
            _downloadEvent = downloadEvent;                                 // The event to be fired when the file is successfully downloaded.
            _timeoutEvent = timeoutEvent;                                   // The event to be fired when a request has not started in a long period.
            _deleteMeCallBack = deleteMeCallback;                           // The callback to dispose itself once finished or failed.
            _remoteIP = remoteIP;                                           // The IP from where the file will be downloaded.
            _sourceName = sourceFile;                                       // The source file name.
            _saveName = saveName;                                           // The name with which the file will be saved.
            _chunkSize = chunkSize;                                         // The local chunkSize (May be updated with the first incoming chunk).
            _downloadFolder = downloadFolder;                               // The default configured downloads folder.
            _downloadFullPath = downloadFullPath;                           // The interpretation of _downloadFolder (full path or relative).
            // Watch to measure the time it takes to be received:
            _watch = new Stopwatch();
            // Create the verification timer:
            if (autoDownload)
                _requestTimer = new Timer(RetryRequest, null, 0, _timeout);
            else
                _requestTimer = new Timer(RetryRequest, null, Timeout.Infinite, Timeout.Infinite);
        }
        /// <summary>Method to start the download when is created but inactive</summary>
        public void Start()
        {
            if (_status == FileStatus.inactive)
                _requestTimer.Change(0, _timeout);
        }
        /// <summary>Send "chunk confirmation" command</summary>
        public void SendChunkRequest(int part)
        {
            // Command example:
            // F3;SourceFile;part;chunkSize
            if (_connection != null)
            {
                string cmd = "F3;" + _sourceName + ";" + part.ToString() + ";" + _chunkSize.ToString();
                _connection.SendData(_remoteIP, _connection.StringToByteArray(cmd));
            }
        }
        ///<summary>Add a received chunk to the file</summary>
        public void AddReceivedChunk(ref string[] fields, ref byte[] message, int chunkIndex)
        {
            // Reset the retry timer:
            _requestTimer.Change(_timeout, _timeout);
            _timeoutCounter = 0;
            // F4;SourceFile;part;chunkSize;total;fileSize;BinaryChunk
            _chunkSize = int.Parse(fields[3]);                              // Maximum chunk size in bytes (server side, may be smaller).
            _chunks = int.Parse(fields[4]);                                 // Count of chunks in the file.
            _size = long.Parse(fields[5]);                                  // File size in bytes.
            // Set file status as started:
            if (_status == FileStatus.inactive || _status == FileStatus.requested)
            {
                // First chunk received:
                _status = FileStatus.started;
                _chunkList = new bool[_chunks];                             // Generate the status list of received chunks.
                if (_size <= int.MaxValue)
                {
                    _rFile = new byte[_size];                               // Get the necessary memory space to receive the file.
                }
                else
                {
                    // The file length excedes 2GB:
#if UNITY_IOS || UNITY_ANDROID
                    Abort();
                    return;
#else
                    string savePath = _downloadFullPath ? _downloadFolder : FileManagement.Combine(FileManagement.persistentDataPath, _downloadFolder);
                    FileManagement.CreateDirectory(savePath, true);
                    savePath = FileManagement.Combine(savePath, _saveName);
                    _rmmf = MemoryMappedFile.CreateFromFile(savePath, System.IO.FileMode.Create, null, _size, MemoryMappedFileAccess.ReadWrite);
                    _rfa = _rmmf.CreateViewAccessor(0, _size, MemoryMappedFileAccess.ReadWrite);
#endif
                }
                _watch.Start();                                             // Start time measurement.
            }
            int part = int.Parse(fields[2]);                                // Received part number (0 to total-1).
            if (_chunkList.Length > 0)
            {
                if (_chunkList[part] == true) return;                       // Discard already saved chunks.
                // Add the chunk:
                if(_rFile != null)
                {
                    System.Buffer.BlockCopy(message, chunkIndex, _rFile, part * _chunkSize, message.Length - chunkIndex);
                }
                else
                {
                    while (_rfa == null) { }
                    _rfa.WriteArray((long)_chunkSize * part, message, chunkIndex, message.Length - chunkIndex);
                }
                _chunkList[part] = true;                                    // Register received chunk.
                SendChunkRequest(GetMissingChunkNumber());
            }
            // Save the file if completed:
            if (GetMissingChunkNumber() < 0)
            {
                _requestTimer.Change(Timeout.Infinite, Timeout.Infinite);   // Stop the download timer, is no longer needed.
                // Update stats:
                _watch.Stop();                                              // Stop the time measurement.
                _elapsedTime = _watch.ElapsedMilliseconds / 1000f;          // Convert value to seconds.
                if (_elapsedTime > 0)                                       // Transfer rate in Mbytes per second (round to 1 decimal).
                    _transferRate = _size / _elapsedTime / 1048576f;
                // Save the file:
                _status = FileStatus.saving;
                if(_rFile != null)
                    FileManagement.SaveRawFile(FileManagement.NormalizePath(_downloadFolder + "/" + _saveName), _rFile, false, _downloadFullPath);
                _status = FileStatus.finished;
                // Finish the download or send the confirmation:
                Dispose();                                                  // Frees resources and fire download event.
            }
        }
        ///<summary>Checks file completeness and request missing parts (Timer)</summary>
        void RetryRequest(object state)
        {
            // Retry the initial request:
            if (_status == FileStatus.inactive || _status == FileStatus.requested)
            {
                // Initial request:
                _status = FileStatus.requested;
                SendChunkRequest(0);                                        // Make the first request to receive the complete file (default).
            }
            else if (_status == FileStatus.started)
            {
                // Retry chunk confirmation:
                _retries++;
                SendChunkRequest(GetMissingChunkNumber());
            }
            // Retry just a certain amount of times, then the request fails by timeout:
            _timeoutCounter++;
            if (_timeoutCounter >= 10)
            {
                Abort();                                                    // Reception can be considered as failed.
                if (_timeoutEvent != null)
                    ThreadPool.QueueUserWorkItem((object s) => _timeoutEvent?.Invoke(this));
            }
        }
        ///<summary>Returns the status of the file</summary>
        public FileStatus GetStatus()
        {
            return _status;
        }
        ///<summary>Checks equality on this request</summary>
        public bool IsThis(string remoteIP, string name)
        {
            return (remoteIP == _remoteIP && name == _sourceName);
        }
        ///<summary>Checks equality on this request</summary>
        public bool IsThis(FileRequest request)
        {
            return IsThis(request._remoteIP, request._sourceName);
        }
        ///<summary>Aborts the current download</summary>
        public void Abort()
        {
            _status = FileStatus.failed;
            _downloadEvent = null;
            Dispose();
        }
        ///<summary>Release resources</summary>
        public void Dispose()
        {
            if (_watch != null)
            {
                _watch.Stop();
                _watch = null;
            }
            // Stop the timer:
            if (_requestTimer != null)
            {
                _requestTimer.Dispose();
                _requestTimer = null;
            }
            // Delete allocated file data:
            if (_rfa != null)
            {
                _rfa.Dispose();
                _rfa = null;
            }
            if (_rmmf != null)
            {
                _rmmf.Dispose();
                _rmmf = null;
            }
            _rFile = null;
            // Fire the event only if the file was downloaded correctly:
            if (_downloadEvent != null && _status != FileStatus.inactive)
                ThreadPool.QueueUserWorkItem((object s) => _downloadEvent?.Invoke(this));
            // Purge itself (After a timeout time):
            _deleteMeCallBack?.Invoke(this);
        }
        ///<summary>Gets the current download proportion (0 to 1)</summary>
        public float GetProgress()
        {
            if (_chunks > 0 && _chunkList != null)
            {
                int _chunkCnt = 0;
                for (int i = 0; i < _chunkList.Length; i++)
                {
                    if (_chunkList[i] == true)
                        _chunkCnt++;
                }
                return (1f / _chunks) * _chunkCnt;
            }
            else
                return 0f;
        }
        ///<summary>Gets the current download percentage (0 to 100)</summary>
        public int GetProgressInt()
        {
            if (_chunks > 0 && _chunkList != null)
            {
                int _chunkCnt = 0;
                for (int i = 0; i < _chunkList.Length; i++)
                {
                    if (_chunkList[i] == true)
                        _chunkCnt++;
                }
                return (int)System.Math.Floor((1f / _chunks) * _chunkCnt * 100);
            }
            else
                return 0;
        }
        ///<summary>Checks the list of received chunks and returns the first missing one</summary>
        int GetMissingChunkNumber()
        {
            for (int i = 0; i < _chunkList.Length; i++)
            {
                if (_chunkList[i] == false)
                    return i;                                           // Returns the number of the missing part (0 to [_chunks-1]).
            }
            return -1;                                                  // -1 means the file is complete.
        }
    }
    volatile public List<FileRequest> _requests = new List<FileRequest>();  // The list of pending file requests.
    private readonly object _lockRequests = new object();                   // Locks the access to the requests list.

    // A file to be sent to a certain device and some transmission status:
    public class FileUpload
    {
        volatile UDPConnection _connection;                                 // Main UDP connection object.
        volatile EventUpload _uploadEvent;                                  // This event is called when an upload has finished successfully.
        volatile EventUpload _timeoutEvent;                                 // This event is called when the transference doesn't starts.
        volatile EventUpload _deleteMeCallBack;                             // Event to delete itself from the main list.
        // Upload properties:
        volatile FileResource _sourceFile;                                  // The file to be sent.
        volatile string _remoteIP = "";                                     // The address to where the file is being sent.
        volatile public int _chunkSize = 0;                                 // Chunk size used to trasnfer this file.
        volatile FileStatus _status = FileStatus.inactive;                  // The current status of the upload (always is requested at start).
        volatile int _chunks = 0;                                           // The total count of chunks for this upload.
        volatile int _uploadCnt = 0;                                        // Last sent chunk.
        // Transference timeout:
        volatile Timer _sendTimer;                                          // Timer to retry the transference each _timeout milliseconds.
        volatile int _timeout = 1000;                                       // Timeout in miliseconds to retry transference.
        volatile int _timeoutCounter = -1;                                  // Counter to detect final timeout (fixed to 10 times).

        ///<summary>Constructor</summary>
        public FileUpload(UDPConnection connection, EventUpload uploadEvent, EventUpload timeoutEvent, EventUpload deleteMeCallback, string remoteIP, int chunkSize, FileResource sourceFile)
        {
            _connection = connection;                                               // The connection is not created in this object, it's created outside.
            _uploadEvent = uploadEvent;                                             // The event to be fired when an upload finishes successfully.
            _timeoutEvent = timeoutEvent;                                           // The event to be fired when a request has not started in a long period.
            _deleteMeCallBack = deleteMeCallback;                                   // The callback to dispose itself once finished or failed.
            _remoteIP = remoteIP;                                                   // The IP to where the file will be uploaded.
            _chunkSize = chunkSize;                                                 // The chunk size to transfer this file.
            _sourceFile = sourceFile;                                               // The source file loaded in memory.
            // Make the first request and start the timeout timer:
            _sendTimer = new Timer(SendUpload, null, 10, _timeout);
        }
        ///<summary>Send the transference message or the requested chunk</summary>
        void SendUpload(object state)
        {
            if (_status == FileStatus.inactive || _status == FileStatus.requested)
            {
                // F5;SourceFile
                string message = "F5;" + _sourceFile._name;
                _connection.SendData(_remoteIP, message);
                _status = FileStatus.requested;
            }
            else if (_status == FileStatus.started)
            {
                // F4;SourceFile;part;chunkSize;total;fileSize;BinaryChunk
                byte[] cmd = _connection.StringToByteArray("F4;" + _sourceFile._name + ";" + _uploadCnt.ToString() + ";" + _chunkSize.ToString() + ";" + _sourceFile.GetChunkCount(_chunkSize).ToString() + ";" + _sourceFile.GetFileSize().ToString() + ";");
                byte[] chunk = _sourceFile.GetChunk(_uploadCnt, _chunkSize);
                if (chunk != null)
                {
                    byte[] data = new byte[cmd.Length + chunk.Length];
                    System.Buffer.BlockCopy(cmd, 0, data, 0, cmd.Length);
                    System.Buffer.BlockCopy(chunk, 0, data, cmd.Length, chunk.Length);
                    // Send the requested chunk:
                    _connection.SendData(_remoteIP, data);
                }
                else
                {
                    string msg = "F2;" + _sourceFile._name + ";2";          // Error: File is too large.
                    _connection.SendData(_remoteIP, msg);
                    Dispose();                                              // Transmission can be considered as failed.
                }
            }
            // Retries just a certain amount of times, then the transference fails by timeout:
            _timeoutCounter++;
            if (_timeoutCounter >= 10)
            {
                _status = FileStatus.failed;
                if (_timeoutEvent != null)
                    ThreadPool.QueueUserWorkItem((object s) => _timeoutEvent?.Invoke(this));
                Dispose();                                                  // Transmission can be considered as failed.
            }
        }
        ///<summary>Returns the status of the upload</summary>
        public FileStatus GetStatus()
        {
            return _status;
        }
        ///<summary>Checks equality on this upload</summary>
        public bool IsThis(string remoteIP, string name)
        {
            return (remoteIP == _remoteIP && name == _sourceFile._name);
        }
        ///<summary>Checks equality on this upload</summary>
        public bool IsThis(FileUpload upload)
        {
            return IsThis(upload._remoteIP, upload.GetName());
        }
        ///<summary>Release resources</summary>
        public void Dispose()
        {
            // Stop the timer:
            if (_sendTimer != null)
            {
                _sendTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _sendTimer.Dispose();
                _sendTimer = null;
            }
            // Purge itself:
            _deleteMeCallBack?.Invoke(this);
        }
        ///<summary>Analizes the incoming confirmation and sends the requested chunk</summary>
        public void SendChunk(ref string[] fields)
        {
            // F3;SourceFile;part;chunkSize
            int part = int.Parse(fields[2]);
            if (part >= 0 && part < _uploadCnt) return;                     // Discard already sent chunks.
            _status = FileStatus.started;
            // Calculate the smaller chunk size:
            int chunkSize = int.Parse(fields[3]);
            if (chunkSize < _chunkSize) _chunkSize = chunkSize;
            _chunks = (int)_sourceFile.GetChunkCount(_chunkSize);
            // Reset the timer:
            _timeoutCounter = 0;
            _sendTimer.Change(_timeout, _timeout);
            // Analize the requested chunk index:
            switch (part)
            {
                case -1:    // Upload is complete.
                case -2:    // Upload requires client confirmation.
                case -3:    // Upload is rejected.
                    _status = FileStatus.finished;
                    // Upload finished event:
                    if (_uploadEvent != null)
                        ThreadPool.QueueUserWorkItem((object s) => _uploadEvent?.Invoke(this));
                    Dispose();
                    break;
                default:
                    // Send the requested chunk:
                    _uploadCnt = part;
                    SendUpload(null);                                       // 0 to [_chunks - 1] are chunk requests, -1 is a completed download confirmation.
                    break;
            }
        }
        ///<summary>Gets the current download proportion (0 to 1)</summary>
        public float GetProgress()
        {
            if (_chunks > 0)
                return (1f / _chunks) * _uploadCnt;
            else
                return 0f;
        }
        ///<summary>Gets the current download percentage (0 to 100)</summary>
        public int GetProgressInt()
        {
            if (_chunks > 0)
                return (int)System.Math.Floor((1f / _chunks) * _uploadCnt * 100);
            else
                return 0;
        }
        ///<summary>Gets the file name</summary>
        public string GetName()
        {
            return _sourceFile._name;
        }
    }
    volatile public List<FileUpload> _uploads = new List<FileUpload>();     // The list of pending file uploads.
    private readonly object _lockUploads = new object();                    // Locks the access to the uploads list.

    // Class containing some requested file to be served (resources are loaded in memory):
    public class FileResource
    {
        // This class lasts in memory for a period of time and then (if no requests are made) it's destroyed.
        volatile public string _name = "";                                  // The name of the resource (and relative or full pathpath).
        volatile byte[] _file = null;                                       // The content of the requested file (Up to 2GB).
        volatile MemoryMappedFile _mmf = null;                              // File mapped to memory (more than 2GB).
        volatile MemoryMappedViewAccessor _fa = null;                       // Controller for the _mmf.
        volatile Timer _timer;                                              // Requests to delete this object if no longer needed.
        volatile int _timeout = 30000;                                      // Time in miliseconds before object deletion (default).

        ///<summary>Constructor (load from disk)</summary>
        public FileResource(string name, TimerCallback deleteMeCallback, string sharedFolder, bool sharedFullPath = false, int resTimeout = 0)
        {
            _name = name;
            string filePath = FileManagement.NormalizePath(sharedFolder + "/" + _name);
#if UNITY_IOS || UNITY_ANDROID
            _file = FileManagement.ReadRawFile(filePath, false, true, sharedFullPath);
#else
            // Detect if the file is over 2GB in size.
            try
            {
                _file = FileManagement.ReadRawFile(filePath, false, true, sharedFullPath);
            }
            catch
            {
                // File too big (e.HResult == -2146232800).
                if (FileManagement.FileExists(_name, false, sharedFullPath))
                {
                    // Load into _mmf:
                    if (!sharedFullPath)
                        filePath = FileManagement.NormalizePath(FileManagement.persistentDataPath + "/" + filePath);
                    _mmf = MemoryMappedFile.CreateFromFile(filePath);
                    _fa = _mmf.CreateViewAccessor();
                }
                else
                {
                    // SA big files not supported:
                    WriteLine("[FTS] Files larger than 2GB in StreamingAssets aren't supported", true);
                }
            }
#endif
            // Set the requested timeout (gets the default if == 0):
            if (resTimeout != 0) _timeout = resTimeout;
            // Free the memory when this resource is no longer needed:
            _timer = new Timer(deleteMeCallback, this, _timeout, Timeout.Infinite);
        }
        ///<summary>Constructor (load from memory)</summary>
        public FileResource(string name, TimerCallback deleteMeCallback, byte[] content, int resTimeout = 0)
        {
            _name = name;
            _file = content;
            // Set the requested timeout (gets the default if == 0):
            if (resTimeout != 0) _timeout = resTimeout;
            // Free the memory when this resource is no longer needed:
            _timer = new Timer(deleteMeCallback, this, _timeout, Timeout.Infinite);
        }
        ///<summary>Ensures content deletion</summary>
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            if (_fa != null)
            {
                _fa.Dispose();
                _fa = null;
            }
            if (_mmf != null)
            {
                _mmf.Dispose();
                _mmf = null;
            }
            _file = null;
        }
        ///<summary>Checks if name matches</summary>
        public bool IsThis(string name)
        {
            return (FileManagement.NormalizePath(name) == _name);
        }
        ///<summary>Gets the file size</summary>
        public long GetFileSize()
        {
            if (_file != null)
                return _file.Length;
            else if (_fa != null)
                return _fa.Capacity;
            else
                return 0;
        }
        ///<summary>Calculates the chunk count</summary>
        public long GetChunkCount(int chunkSize)
        {
            return (long)System.Math.Ceiling(GetFileSize() / (double)chunkSize);
        }
        ///<summary>Gets a particular chunk by index (index starts with 0)</summary>
        public byte[] GetChunk(long part, int maxChunkSize)
        {
            // Reset the timer:
            _timer.Change(_timeout, Timeout.Infinite);
            // Discard not valid part:
            if (part < 0 || part > GetChunkCount(maxChunkSize)) return null;
            // Calculate the partial file's start index:
            long _index = part * maxChunkSize;
            // Set the buffer size acordingly:
            int _size = maxChunkSize;                                       // Maximum size by default.
            if ((_index + maxChunkSize) > GetFileSize())
                _size = (int)(GetFileSize() - _index);                      // When the partial file is smaller than the chunk size.
            byte[] _buffer = new byte[_size];
            // Extract the partial file:
            if (_file != null)
            {
                System.Buffer.BlockCopy(_file, (int)_index, _buffer, 0, _buffer.Length);
                return _buffer;
            }
            else if (_fa != null)
            {
                _fa.ReadArray(_index, _buffer, 0, _buffer.Length);
                return _buffer;
            }
            return null;                                                    // The resource wasn't loaded because it's too large.
        }
        ///<summary>Gets the checksum of this resource (ADD - 8bit)</summary>
        public uint CheckSumA()
        {
            uint sum = 0;
            if (_file != null)
            {
                for (int i = 0; i < _file.Length; i++)
                {
                    sum += _file[i];                                            // Addition.
                }
            }
            else
            {
                for (long i = 0; i < _fa.Capacity; i++)
                {
                    sum += _fa.ReadByte(i);                                     // Addition.
                }
            }
            return sum;
        }
        ///<summary>Gets the checksum of this resource (XOR - 8bit)</summary>
        public byte CheckSumX()
        {
            byte sum = 0x00;
            if (_file != null)
            {
                for (int i = 0; i < _file.Length; i++)
                {
                    sum ^= _file[i];                                            // Xor.
                }
            }
            else
            {
                for (long i = 0; i < _fa.Capacity; i++)
                {
                    sum ^= _fa.ReadByte(i);                                     // Xor.
                }
            }
            return sum;
        }
    }
    volatile List<FileResource> _resources = new List<FileResource>();      // List of loaded resources.
    static readonly object _lockResources = new object();                   // To avoid load a resource twice.

    public enum FileStatus                                                  // File status (download or upload).
    {
        inactive,                                                           // The default status (when just created).
        requested,                                                          // The first request was sent.
        started,                                                            // This file has received at least one chunk.
        saving,                                                             // The file was downloaded and is being saved to disk.
        finished,                                                           // The file was saved.
        failed                                                              // The requested file not found or is timed out.
    };

    /**************************
     * 1 - Connection control *
     **************************/
    ///<summary> Constructor </summary>
    public FTSCore(int port, EventError onError = null, EventDevices onDeviceListUpdate = null, EventFile onFileDownload = null, EventFile onFileNotFound = null, EventFile onForcedDownload = null, EventFile onFileTimeout = null, EventUpload onUploadBegin = null, EventUpload onFileUpload = null, EventUpload onUploadTimeout = null, string localIP = "", string defaultSharedFolder = "FTSShared", string defaultDownloadFolder = "FTSDownloads", bool sharedFullPath = false, bool downloadFullPath = false)
    {
        // Set the events:
        _onError = onError;
        _onDeviceListUpdate = onDeviceListUpdate;
        _onFileDownload = onFileDownload;
        _onFileNotFound = onFileNotFound;
        _onForcedDownload = onForcedDownload;
        _onFileTimeout = onFileTimeout;
        _onUploadBegin = onUploadBegin;
        _onFileUpload = onFileUpload;
        _onUploadTimeout = onUploadTimeout;
        // Creates the UDP connection:
        _connection = new UDPConnection(port, localIP, OnOpen, OnMessage, OnError, OnClose);
        _connection.Connect(port, "", 10f);
        // Get the maximum available chunksize:
        int udpBuffer = _connection.GetIOBufferSize();
        if (udpBuffer < _chunkSize)
            _chunkSize = udpBuffer - 500;                                           // Reserved space for FTS protocol: 500bytes.
        // Save the user defined shared folder (Empty full path is not allowed):
        _sharedFolder = FileManagement.NormalizePath(defaultSharedFolder);
        if (!string.IsNullOrEmpty(_sharedFolder) && sharedFullPath)
            _sharedFullPath = true;
        // Save the user defined download folder:
        _downloadFolder = FileManagement.NormalizePath(defaultDownloadFolder);
        if (!string.IsNullOrEmpty(_downloadFolder) && downloadFullPath)
            _downloadFullPath = true;
        // Creates folders if not exists:
        if (!FileManagement.DirectoryExists(_sharedFolder, true, _sharedFullPath))
            FileManagement.CreateDirectory(_sharedFolder, _sharedFullPath);
        if (!FileManagement.DirectoryExists(_downloadFolder, false, _downloadFullPath))
            FileManagement.CreateDirectory(_downloadFolder, _downloadFullPath);
    }
    ///<summary> Destructor </summary>
    ~FTSCore()
    {
        Dispose();
    }
    ///<summary> Force complete disposal </summary>
    public void Dispose()
    {
        // Disable the connection:
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }

        // Free events:
        _onError = null;
        _onDeviceListUpdate = null;

        _onFileDownload = null;
        _onFileNotFound = null;
        _onForcedDownload = null;
        _onFileTimeout = null;

        _onUploadBegin = null;
        _onFileUpload = null;
        _onUploadTimeout = null;

        // Free memory:
        _devices.Clear();
        for (int i = 0; i < _requests.Count; i++)
        {
            if (_requests[i] != null)
                _requests[i].Dispose();
        }
        _requests.Clear();
        for (int i = 0; i < _uploads.Count; i++)
        {
            if (_uploads[i] != null)
                _uploads[i].Dispose();
        }
        _uploads.Clear();
        for (int i = 0; i < _resources.Count; i++)
        {
            if (_resources[i] != null)
                _resources[i].Dispose();
        }
        _resources.Clear();
    }
    ///<summary> Gets the IP address </summary>
    public string GetIP(bool secondary = false)
    {
        return _connection.GetIP(secondary);
    }
    ///<summary> Gets the connection port </summary>
    public int GetPort()
    {
        return _connection.GetPort();
    }
    ///<summary> Gets UDP connection status </summary>
    public bool IsConnected()
    {
        return _connection.IsConnected();
    }

    /*********************
     * Connection events *
     *********************/
    void OnOpen(UDPConnection connection)
    {
    }
    void OnMessage(byte[] message, string remoteIP, UDPConnection connection)
    {
        string header = new string(new char[] { (char)message[0], (char)message[1] });
        string[] fields;                                                    // Temporary container for the fields of the message.
        FileRequest request;                                                // Temporary FileRequest container.
        FileUpload upload;                                                  // Temporary FileUpload container.
        // Execute received message:
        switch (header)
        {
            case "F0":  // Someone is requesting the information of this device. 
                // F0;isServer;name;FTSVersion;OS
                fields = _connection.ByteArrayToString(message).Split(';'); // Retrieves message fields.
                AddNewDevice(remoteIP, fields[2], fields[1], fields[4], fields[3]);
                new Timer(SendPollingResponse, remoteIP, _rnd.Next(5, 500), Timeout.Infinite);  // Randomly delayed reply.
                break;
            case "F1":  // Remote device polling reply.
                // F1;isServer;name;FTSVersion;OS
                fields = _connection.ByteArrayToString(message).Split(';'); // Retrieves message fields.
                AddNewDevice(remoteIP, fields[2], fields[1], fields[4], fields[3]);
                break;

            case "F2":  // The requested file can't be transferred.
                // F2;SourceFile;errorCode
                fields = _connection.ByteArrayToString(message).Split(';'); // Retrieves message fields.
                request = FindRequest(remoteIP, fields[1]);
                if (request != null)
                {
                    request.Abort();
                    int errorCode = fields.Length > 2 ? FileManagement.CustomParser<int>(fields[2]) : 0;
                    switch (errorCode)
                    {
                        case 1:     // The device is not a server.
                            if (_onError != null)
                                ThreadPool.QueueUserWorkItem((object s) => _onError?.Invoke(errorCode, "The remote device is not enabled as a server."));
                            break;
                        case 2:     // File not found.
                            if (_onError != null)
                                ThreadPool.QueueUserWorkItem((object s) => _onError?.Invoke(errorCode, "The remote device can't handle files larger than 2GB."));
                            break;
                        default:    // File not found.
                            if (_onFileNotFound != null)
                                ThreadPool.QueueUserWorkItem((object s) => _onFileNotFound?.Invoke(request));
                            break;
                    }
                }
                break;

            case "F3":  // Someone requested a file.
                // F3;SourceFile;part;chunkSize
                fields = _connection.ByteArrayToString(message).Split(';'); // Retrieves message fields.
                if (_serverEnabled == true)
                {
                    string sourceFile = FileManagement.NormalizePath(FileManagement.Combine(_sharedFolder, fields[1]));
                    upload = FindUpload(remoteIP, fields[1]);               // Firts check if the upload already exists.
                    if (FileManagement.FileExists(sourceFile, true, _sharedFullPath) || upload != null)
                    {
                        // Send the requested file:
                        if (upload == null)
                        {
                            upload = SendFile(remoteIP, fields[1]);         // Starts a new upload.
                            if (_onUploadBegin != null)
                                ThreadPool.QueueUserWorkItem((object s) => _onUploadBegin?.Invoke(upload));
                        }
                        upload.SendChunk(ref fields);                       // Send the requested chunk.
                    }
                    else
                    {
                        // Requested file not found:
                        SendFileError(remoteIP, fields[1], 0);
                    }
                }
                else
                {
                    // This device is not a server:
                    SendFileError(remoteIP, fields[1], 1);
                }
                break;
            case "F4":  // Receiving a file.
                // F4;SourceFile;part;chunkSize;total;fileSize;BinaryChunk
                int chunkIndex = 0;
                for (int i = 0; i < message.Length; i++)
                {
                    if (message[i] == ';')
                    {
                        chunkIndex++;                                       // Use the index as temporary counter.
                        if (chunkIndex == 6)
                        {
                            chunkIndex = i + 1;                             // Save the chunk index.
                            break;
                        }
                    }
                }
                // Get the command fields:
                byte[] cmd = new byte[chunkIndex - 1];
                System.Buffer.BlockCopy(message, 0, cmd, 0, chunkIndex - 1);
                fields = _connection.ByteArrayToString(cmd).Split(';');
                // Find or create the request:
                request = FindRequest(remoteIP, fields[1]);
                if (request == null)
                {
                    // If the request doesn't exists, just reject the download sending -3:
                    string confirmation = "F3;" + fields[1] + ";-3;" + _chunkSize.ToString();   // Command example: F3;SourceFile;part;chunkSize
                    _connection.SendData(remoteIP, confirmation);
                }
                else
                {
                    request.AddReceivedChunk(ref fields, ref message, chunkIndex);              // Save the received chunk.
                    if (request.GetStatus() == FileStatus.failed && _onError != null)
                        ThreadPool.QueueUserWorkItem((object s) => _onError?.Invoke(2, "This device can't handle files larger than 2GB (download aborted)."));
                }
                break;

            case "F5":  // Server has sent an upload.
                // F5;SourceFile
                fields = _connection.ByteArrayToString(message).Split(';'); // Retrieves message fields.
                request = FindRequest(remoteIP, fields[1]);
                if (request == null)
                {
                    // Create the forced request:
                    request = RequestFile(remoteIP, fields[1], "", _autoDownload);
                    // Fire the forced request event:
                    if (_onForcedDownload != null)
                        ThreadPool.QueueUserWorkItem((object s) => _onForcedDownload?.Invoke(request));
                }
                if (!_autoDownload)
                    request.SendChunkRequest(-2);                           // Confirmation to first chunk, -2 means that it needs user confirmation.
                else
                    request.Start();                                        // The request already exists, start it if the seting is active.
                break;

            default:
                WriteLine("[FTSCore.OnMessage()] Unknown: " + _connection.ByteArrayToString(message));
                break;
        }
    }
    void OnClose(UDPConnection connection)
    {
        WriteLine("[FTSCore] FTS connection closed.");
    }
    void OnError(int errorCode, string message, UDPConnection connection)
    {
        WriteLine("[FTSCore.UDPConnection] Error: " + message, true);
        _onError?.Invoke(errorCode, "[UDPConnection] " + message);
    }

    /***********************
     * 2 - Devices control *
     ***********************/
    ///<summary>Send detect available devices</summary>
    public void SendPollRequest(string remoteIP = "")
    {
        // Command example:
        // F0;isServer;name;FTSVersion;OS
        if (_connection != null)
        {
            if (remoteIP == "") remoteIP = _connection.IPV4BroadcastAddress();
            string cmd = "F0;" + _serverEnabled.ToString() + ";" + _deviceName + ";" + _ftsVersion + ";" + System.Environment.OSVersion.ToString();
            _connection.SendData(remoteIP, cmd);
        }
    }
    ///<summary>Send reply to polling request</summary>
    void SendPollingResponse(object remoteIP)
    {
        // Command example:
        // F1;isServer;name;FTSVersion;OS
        if (_connection != null)
        {
            string cmd = "F1;" + _serverEnabled.ToString() + ";" + _deviceName + ";" + _ftsVersion + ";" + System.Environment.OSVersion.Platform.ToString();
            _connection.SendData((string)remoteIP, cmd);
        }
    }
    ///<summary>Adds, updates or deletes detected devices</summary>
    public void AddNewDevice(string ip, string name, string enabled, string os = "", string ftsVer = "")
    {
        // Update the device if already exists:
        for (int i = 0; i < _devices.Count; i++)
        {
            if (_devices[i].ip == ip)
            {
                _devices[i].name = name;                                            // Updates the name of the server (can change dynamically).
                _devices[i].os = os;                                                // Remembers the OS of the device.
                _devices[i].ftsVersion = ftsVer;                                    // FTS version: 1.X is not compatible with 2.X
                _devices[i].isServer = bool.Parse(enabled);                         // Remembers if the device is enabled as server (can change dynamically).
                // The list of devices was modified:
                if (_onDeviceListUpdate != null)
                    ThreadPool.QueueUserWorkItem((object s) => _onDeviceListUpdate?.Invoke(_devices));
                return;                                                             // Some device was updated (don't add a new one).
            }
        }

        // Add a new device:
        RemoteDevice rd = new RemoteDevice
        {
            ip = ip,
            name = name,
            os = os,
            ftsVersion = ftsVer,
            isServer = bool.Parse(enabled)
        };
        lock (_lockDevices)
        {
            _devices.Add(rd);
        }
        // The list of devices was modified:
        if (_onDeviceListUpdate != null)
            ThreadPool.QueueUserWorkItem((object s) => _onDeviceListUpdate?.Invoke(_devices));
    }
    ///<summary>Clears devices lists</summary>
    public void ResetDeviceList()
    {
        lock (_lockDevices)
        {
            _devices.Clear();
        }
        // The list of devices was modified:
        if (_onDeviceListUpdate != null)
            ThreadPool.QueueUserWorkItem((object s) => _onDeviceListUpdate?.Invoke(_devices));
    }

    /*********************
     * 3 - Cache control *
     *********************/
    /// <summary>Loads a new file in memory or returns the same if it's already loaded</summary>
    FileResource GetResource(string name, bool fullPath = false, int timeout = 0)
    {
        // Lock file creation.
        lock (_lockResources)
        {
            // Check all loaded resources to find the requested one:
            for (int i = 0; i < _resources.Count; i++)
            {
                if (_resources[i].IsThis(name))
                    return _resources[i];                                   // File already loaded.
            }
            // Load the provided timeout, or keep default if not provided:
            int t = _resourcesTimeout;
            if (timeout != 0) t = timeout;
            try
            {
                // Load the resource accordingly (fullPath or not):
                FileResource res;
                if (fullPath)
                    res = new FileResource(FileManagement.GetFileName(name), RemoveFile, FileManagement.GetParentDirectory(name), fullPath, t);
                else
                    res = new FileResource(name, RemoveFile, _sharedFolder, _sharedFullPath, t);
                _resources.Add(res);
                return res;
            }
            catch { }
            // File not found, not exists or too large:
            return null;
        }
    }
    /// <summary>Loads a resource manually during a specified time in seconds</summary>
    public void LoadResource(string name, float timeout = Timeout.Infinite, bool fullPath = false)
    {
        int t = Timeout.Infinite;                                           // This method provides an infinite default timeout.
        if (timeout > 0) t = _connection.SecondsToMiliseconds(timeout);     // Apply the provided timeout if available.
        FileResource res = GetResource(name, fullPath, t);                  // Force the load of a resource to memory.
        if (res == null && _onError != null)
            ThreadPool.QueueUserWorkItem((object s) => _onError?.Invoke(2, "This device can't handle files larger than 2GB."));
    }
    /// <summary>Loads a resource manually during a specified time in seconds</summary>
    public void LoadResource(string name, byte[] content, float timeout = Timeout.Infinite)
    {
        int t = Timeout.Infinite;                                           // This method provides an infinite default timeout.
        if (timeout > 0) t = _connection.SecondsToMiliseconds(timeout);     // Apply the provided timeout if available.
        FileResource res = new FileResource(name, RemoveFile, content, t);  // Force the load of a resource to memory.
        if (res == null && _onError != null)
            ThreadPool.QueueUserWorkItem((object s) => _onError?.Invoke(2, "This device can't handle files larger than 2GB."));
    }
    /// <summary>Checks if the file is already loaded</summary>
    bool IsAlreadyLoaded(string name)
    {
        for (int i = 0; i < _resources.Count; i++)
        {
            if (_resources[i].IsThis(name))
            {
                return true;                                                        // File already loaded.
            }
        }
        return false;
    }
    /// <summary>Removes a resource from the list by timeout</summary>
    void RemoveFile(object res)
    {
        FileResource _res = (FileResource)res;
        if (_resources.Contains(_res))
            _resources.Remove(_res);
    }
    /// <summary>Gets the number of respurces in memory</summary>
    public int GetResourcesCount()
    {
        return _resources.Count;
    }
    /// <summary>Gets the size in memory of the resources</summary>
    public long GetResourcesSize()
    {
        long size = 0;
        for (int c = 0; c < _resources.Count; c++)
        {
            size += _resources[c].GetFileSize();
        }
        return size;
    }

    /*********************
     * 4 - File download *
     *********************/
    ///<summary>Request a file to download by device list index</summary>
    public FileRequest RequestFile(int device, string name, string saveName = "", bool autoDownload = true, bool preservePath = false)
    {
        // Requests only if the device is registered and if it is a server (Downloads from non servers devices will fail):
        if (device >= 0 && device < _devices.Count && _devices[device].isServer)
            return RequestFile(_devices[device].ip, name, saveName, autoDownload, preservePath);
        else
            return null;
    }
    ///<summary>Request a file to download by IP</summary>
    public FileRequest RequestFile(string serverIP, string name, string saveName = "", bool autoDownload = true, bool preservePath = false)
    {
        // This can't be broadcasted:
        if (serverIP != _connection.IPV4BroadcastAddress() && !string.IsNullOrEmpty(serverIP))
        {
            // Copies the source name if no saveName is provided:
            saveName = string.IsNullOrEmpty(saveName) ? name : saveName;
            if (!preservePath) saveName = FileManagement.GetFileName(saveName);
            // Checks if the download already exists:
            FileRequest fr = FindRequest(serverIP, name);
            if (fr == null)
            {
                // Creates the request only if not exists:
                fr = new FileRequest(_connection, _onFileDownload, _onFileTimeout, PurgeRequest, serverIP, name, saveName, _chunkSize, _downloadFolder, _downloadFullPath, autoDownload);
                lock (_lockRequests)
                {
                    _requests.Add(fr);
                }
            }
            return fr;
        }
        else
        {
            return null;
        }
    }
    ///<summary>Deletes every finished or failed FileRequest (The download list must be purged manually)</summary>
    void PurgeRequest(FileRequest request)
    {
        lock (_lockRequests)
        {
            _requests.Remove(request);
        }
    }
    ///<summary>Returns the number of FileRequest in progress</summary>
    public int RequestCount()
    {
        return _requests.Count;
    }
    ///<summary>Finds a FileRequest by name</summary>
    FileRequest FindRequest(string remoteIP, string name)
    {
        FileRequest request = null;
        lock (_lockRequests)
        {
            for (int i = 0; i < _requests.Count; i++)
            {
                if (_requests[i].IsThis(remoteIP, name))
                    request = _requests[i];
            }
        }
        return request;
    }

    /*******************
     * 5 - File upload *
     *******************/
    /// <summary>Send error message when file can't be transferred</summary>
    void SendFileError(string remoteIP, string name, int errCode)
    {
        // Command example:
        // F2;SourceFile;errorCode
        if (_connection != null)
        {
            string cmd = "F2;" + name + ";" + errCode;
            _connection.SendData(remoteIP, cmd);
        }
    }
    ///<summary>Sends a file to a remote device with upload confirmation</summary>
    public FileUpload SendFile(string deviceIP, string name, bool fullPath = false)
    {
        if (deviceIP != _connection.IPV4BroadcastAddress() && !string.IsNullOrEmpty(deviceIP))
        {
            // Send a single file and track its status:
            FileUpload upload = FindUpload(deviceIP, name);
            // Get the right file path depending on access settings:
            string sourceFile = FileManagement.NormalizePath(_sharedFolder + "/" + name);
            if (fullPath) sourceFile = name;
            if (upload == null && FileManagement.FileExists(sourceFile, true, _sharedFullPath || fullPath))
            {
                lock (_lockUploads)
                {
                    FileResource res = GetResource(name, fullPath);
                    if (res != null)
                    {
                        upload = new FileUpload(_connection, _onFileUpload, _onUploadTimeout, PurgeUpload, deviceIP, _chunkSize, res);
                        _uploads.Add(upload);
                    }
                    else if (_onError != null)
                    {
                        ThreadPool.QueueUserWorkItem((object s) => _onError?.Invoke(2, "This device can't handle files larger than 2GB (upload aborted)."));
                        SendFileError(deviceIP, name, 2);
                    }
                }
            }
            return upload;
        }
        else
        {
            // If no IP is provided, will try to send the file in broadcast (IPV4 only):
            BroadcastFile(name);
            return null;
        }
    }
    ///<summary>Sends a file to a remote device with upload confirmation</summary>
    public FileUpload SendFile(int device, string name, bool fullPath = false)
    {
        // Requests only if the device is registered and if it is a server (Downloads from non servers devices will fail):
        if (device >= 0 && device < _devices.Count && _devices[device].isServer)
            return SendFile(_devices[device].ip, name, fullPath);
        else
            return null;
    }
    ///<summary>Sends a file request in broadcast (experimental)</summary>
    public void BroadcastFile(string name)
    {
        /*
         * Sending in broadcast can't be tracked.
         * Instead of sending the file chunks in broadcast, it sends an upload request.
         * There will be simultaneous uploads, this may result in a heavy task for the device.
         * Broadcasted files can't use fullpath.
         */
        string remoteIP = _connection.IPV4BroadcastAddress();
        if (!string.IsNullOrEmpty(remoteIP))
        {
            string cmd = "F5;" + name;
            _connection.SendData(remoteIP, cmd);
        }
    }

    ///<summary>Deletes certain FileUpload</summary>
    void PurgeUpload(FileUpload upload)
    {
        lock (_lockUploads)
        {
            _uploads.Remove(upload);
        }
    }
    ///<summary>Returns the number of FileUpload in progress</summary>
    public int UploadCount()
    {
        return _uploads.Count;
    }
    ///<summary>Finds a FileRequest by name</summary>
    FileUpload FindUpload(string remoteIP, string name)
    {
        for (int i = 0; i < _uploads.Count; i++)
        {
            if (_uploads[i].IsThis(remoteIP, name))
                return _uploads[i];
        }
        return null;
    }

    ///<summary> Obtains the relative path to the current SharedFolder </summary>
    public string GetRelativeToSharedFolder(string absolutePath)
    {
        // Obtain the absolute path of the shared folder:
        string absoluteSharedFolder = _sharedFullPath ? _sharedFolder : FileManagement.NormalizePath(FileManagement.persistentDataPath + "/" + _sharedFolder);
        string relativePath = "";
        if (IsIntoSharedFolder(absolutePath))
            relativePath = FileManagement.NormalizePath(FileManagement.NormalizePath(absolutePath).Replace(absoluteSharedFolder, ""));
        return relativePath;
    }
    ///<summary> Checks if the provided path is into the current shared folder </summary>
    public bool IsIntoSharedFolder(string absolutePath)
    {
        string absuluteSharedFolder = _sharedFullPath ? _sharedFolder : FileManagement.NormalizePath(FileManagement.persistentDataPath + "/" + _sharedFolder);
        return FileManagement.NormalizePath(absolutePath).Contains(absuluteSharedFolder);
    }

    /// <summary>Writes to the console depending on the platform.</summary>
    internal static void WriteLine(string line, bool error = false)
    {
#if UNITY_5_3_OR_NEWER
        if (error)
            UnityEngine.Debug.LogError(line);
        else
            UnityEngine.Debug.Log(line);
#else
        System.Console.WriteLine(line);
#endif
    }
}