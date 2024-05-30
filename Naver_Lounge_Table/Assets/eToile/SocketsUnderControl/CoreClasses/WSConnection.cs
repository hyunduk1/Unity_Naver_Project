//#define BRIDGE_NET            // Enable this for Bridge.NET.

using System.Collections.Generic;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
using AOT;
// Static connection with the WSConnection plugin for WebGL (Manages all connections in runtime):
static class WSConnectionWGL
{
    static List<WSConnection> _connections = new List<WSConnection>();                          // Dinamic list of WSConnections (index matches the id).
    // Plugin methods:
    [DllImport("__Internal")] static extern int _NewWebSocket(OnOpenCallback onOpen, OnMessageCallback onMessage, OnErrorCallback onError, OnCloseCallback onClose);
    [DllImport("__Internal")] public static extern void WSConnect(int id, string url);
    [DllImport("__Internal")] public static extern void WSSend(int id, byte[] data, int length);
    [DllImport("__Internal")] public static extern void WSDisconnect(int id);
    [DllImport("__Internal")] public static extern void WSDispose(int id);
    [DllImport("__Internal")] public static extern string WSGetUrl(int id);
    [DllImport("__Internal")] public static extern int WSGetState(int id);

    // Event delegates:
    public delegate void OnOpenCallback(int instanceId);
    public delegate void OnMessageCallback(int instanceId, System.IntPtr msgPtr, int msgSize);
    public delegate void OnErrorCallback(int instanceId, System.IntPtr errorPtr);
    public delegate void OnCloseCallback(int instanceId, int closeCode);
    // C# side events are static, should distribute by 'id' internally.
    // IMPORTANT: Multi-threading is not supported in WebGL (javascript):
    [MonoPInvokeCallback(typeof(OnOpenCallback))]       // 'vi' = return: void, arguments: int
    public static void DelegateOnOpenEvent(int id)
    {
        _connections[id]._connected = true;
        _connections[id].onOpen?.Invoke(_connections[id]);
    }
    [MonoPInvokeCallback(typeof(OnMessageCallback))]    // 'viii' = return: void, arguments: int, int, int
    public static void DelegateOnMessageEvent(int id, System.IntPtr msgPtr, int msgSize)
    {
        if (msgSize > 0)
        {
            byte[] data = new byte[msgSize];
            Marshal.Copy(msgPtr, data, 0, msgSize);
            // Invoke the message event:
            if (_connections[id].onMessage != null)
            {
                _connections[id].onMessage.Invoke(data, _connections[id]);
            }
            else
            {
                lock (_connections[id]._rxBufferLock)
                {
                    _connections[id]._rxBuffer.Add(data);                                    // Save data in buffer.
                }
            }

        }
    }
    [MonoPInvokeCallback(typeof(OnErrorCallback))]      // 'vii' = return: void, arguments: int, int
    public static void DelegateOnErrorEvent(int id, System.IntPtr errorPtr)
    {
        string errorMsg = Marshal.PtrToStringAuto(errorPtr);
        WriteLine("[WSConnectionWGL.OnError] ID WebSocket:" + id.ToString() + " Error:" + errorMsg);
        _connections[id].onError?.Invoke(1, "[WSConnectionWGL.DelegateOnErrorEvent] " + errorMsg, _connections[id]);
    }
    [MonoPInvokeCallback(typeof(OnCloseCallback))]      // 'vii' = return: void, arguments: int, int
    public static void DelegateOnCloseEvent(int id, int code)
    {
        if (!_connections[id]._disconnecting && _connections[id].onClose != null)
            _connections[id].onClose.Invoke(_connections[id]);
    }

    /// <summary>Create a new websocket in plugin side.</summary>
    public static int NewWebSocket(WSConnection connection)
    {
        // Create a new websocket in javascript and assign the static events on this class:
        int id = _NewWebSocket(DelegateOnOpenEvent, DelegateOnMessageEvent, DelegateOnErrorEvent, DelegateOnCloseEvent);
        if (id >= _connections.Count)
            _connections.Add(connection);
        else
            _connections[id] = connection;
        return id;
    }
    /// <summary>Writes to the console depending on the platform.</summary>
    static void WriteLine(string line, bool error = false)
    {
        if (error)
            UnityEngine.Debug.LogError(line);
        else
            UnityEngine.Debug.Log(line);
    }
}
#elif BRIDGE_NET
using Bridge.Html5;
using System.Threading;
#else
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Net.NetworkInformation;
using System.Threading;
#endif

/*
 * Build for .net 4.5 or later.
 * This class should be instantiated in order to work (not designed to be attached to a GameObject).
 * 
 * Events:
 * onOpen = Fired when the connection is properly established (In UDP itmeans "listening" instead of "connected").
 * onMessage(byte[]) = Fired when a message has arrived (if no event is assigned, the message is stored in _rxBuffer).
 * onError(int, string) = Fired when an error occurred.
 * onClose = Fired when the connection was aborted or timed out.
 * 
 * All events contains a reference to the object who has fired him.
 * Use this reference to differentiate the connection that generated the event from the others.
 * 
 * Provide the desired IP/URL to establish the connection, the AddressParser detects the right IPvX protocol.
 * This parameret allows the following formats:
 * For a remote IPV4: "ws://44.33.22.11:60000"
 * For a remote IPV6: "ws://[8888:7777::4444:3333:2222:1111]:60000" (Don't forget the square brackets)
 * For a remote URL: "ws://myserver.com:60000"
 * The header (ws:// in the examples) also alows: wss://, http:// and https://.
 */

/* Events templates (Copy those into your code, then assign to the events of this object):
 
    void OnWSOpen(WSConnection connection)
    {}
    void OnWSMessage(byte[] message, WSConnection connection)
    {}
    void OnWSError(int code, string message, WSConnection connection)
    {}
    void OnWSClose(WSConnection connection)
    {}
 */

public class WSConnection
{
    // Main objects:
#if UNITY_WEBGL
    int _id = -1;                                                                               // Internal ID assigned by the WebGL plugin.
#elif BRIDGE_NET
    volatile WebSocket _client;                                                                 // Connection object.
    volatile Timer _timeoutTimer;                                                               // Connection timeout timer.
    volatile Timer _keepAliveTimer;                                                             // Timer to keep the connection alive (Mono).
#else
    public volatile ClientWebSocket _client;                                                    // Connection object.
    volatile Thread _receiveThread;                                                             // Incoming messages listening thread.
    volatile internal Timer _timeoutTimer;                                                      // Connection timeout timer.
    volatile internal Timer _keepAliveTimer;                                                    // Timer to keep the connection alive.
#endif
    internal volatile List<byte[]> _rxBuffer = new List<byte[]>();                              // Input buffer.
    internal readonly object _rxBufferLock = new object();                                      // Lock the incoming buffer access.
    // Parameters:
    volatile string _remoteURL = string.Empty;                                                  // Currently connected url (internal).
    volatile string _serverURL = string.Empty;                                                  // Target URL provided on Setup().
    // Events:
    public delegate void EventMessage(byte[] data, WSConnection connection);                    // Event delegate with a byte array as arguments.
    public delegate void EventException(int code, string message, WSConnection connection);     // Event delegate with int and string as arguments.
    public delegate void EventVoid(WSConnection connection);                                    // Event delegate for generic events.
    public volatile EventVoid onOpen;                                                           // The connection was established properly (in UDP it means "no error" since there is no connection).
    public volatile EventMessage onMessage;                                                     // A datagram has been received.
    public volatile EventException onError;                                                     // There was an error (the argument contains the error description).
    public volatile EventVoid onClose;                                                          // The connection was closed.
    // Status:
    volatile bool _setup = false;                                                               // Flag: Remembers if Setup() was already called.
    internal volatile bool _connected = false;                                                  // Flag: Connection has been established.
    internal volatile bool _disconnecting = false;                                              // Flag: Disconnection requested by the user/server.
    internal readonly object _clientLock = new object();                                        // Lock the send method to avoid concurrency.
    volatile float _timeout;                                                                    // Timeout time in seconds until automatically firing onClose event.
    volatile internal float _keepAliveTimeout;                                                  // Time interval in seconds to send some "keep alive" message.
    volatile int _activityCnt;                                                                  // Activity counter to track if several consecutive PING/PONG fails.
    volatile int _activity = 3;                                                                 // How many times the PING/PONG should fail to validate inactivity?
    volatile bool _disableWatchdog = false;                                                     // Flag: Disables _activityCnt from decrement (and prevents automatic Disconnect).
    public volatile bool _fragmentation = false;                                                // Flag: Fragmentation was detected at least once.
    public volatile int _mtu = -1;                                                              // Biggest message size during fragmentation.

    ///<summary>Constructor (Events setup)</summary>
    public WSConnection() { }
    public WSConnection(EventVoid evOpen = null, EventMessage evMessage = null, EventException evError = null, EventVoid evClose = null)
    {
        Setup(evOpen, evMessage, evError, evClose);
    }
#if UNITY_WEBGL
    ///<summary>Sets the TCP client to a specific local IP and port</summary>
    public void Setup(EventVoid evOpen = null, EventMessage evMessage = null, EventException evError = null, EventVoid evClose = null)
    {
        Dispose();
        _disconnecting = false;
       _id = WSConnectionWGL.NewWebSocket(this);
        // Events:
        onOpen = evOpen;
        onMessage = evMessage;
        onError = evError;
        onClose = evClose;
        // Setup is done:
        _setup = true;
    }

    ///<summary>Set the connection (to send and receive)</summary>
    public void Connect(string serverURL, float timeout = 5f, float keepAliveTimeout = 15f, bool disableWatchdog = false)
    {
        if (_setup && _id >= 0)
        {
            _disconnecting = false;
            _remoteURL = _serverURL = serverURL.Replace(" ", string.Empty).ToLower();
            // Make the remote WebSocket URL:
            if (_serverURL.Contains("http"))
                _remoteURL = _serverURL.Replace("http", "ws");
            else if (_serverURL.Contains("https"))
                _remoteURL = _serverURL.Replace("https", "wss");
            // TODO in plugin: Start/stop the connection timeout timer:
            _timeout = timeout;
            // TODO in plugin: Force a message to be sent periodically in order to keep the connection alive (Mono):
            _keepAliveTimeout = keepAliveTimeout;
            _disableWatchdog = disableWatchdog;
            // Attempt the connection:
            WSConnectionWGL.WSConnect(_id, _remoteURL);
        }
        else
        {
            WriteLine("[WSConnection] You should call Setup() before attempt to connect.", true);
        }
    }
    ///<summary>Close the connection correctly</summary>
    public void Disconnect()
    {
        if (_id >= 0)
        {
            _disconnecting = true;
            _connected = false;
            _remoteURL = string.Empty;
            // TODO in plugin: Stop timeout timer:
            // TODO in plugin: Stop "keep alive" timer:
            WSConnectionWGL.WSDisconnect(_id);
        }
    }
    ///<summary>Close the connection correctly without firing events</summary>
    public void Dispose()
    {
        if (_id >= 0)
        {
            WSConnectionWGL.WSDispose(_id);
            _id = -1;
            // TODO in plugin: Stop timeout timer:
            // TODO in plugin: Stop "keep alive" timer:
            // Clear events:
            onOpen = null;
            onMessage = null;
            onClose = null;
            onError = null;
#if !UNITY_WEBGL
            onDeleteMe = null;
#endif
            // Clear setup:
            _serverURL = string.Empty;
            _remoteURL = string.Empty;
            _setup = false;
            _connected = false;
        }
    }

    ///<summary>Sends a byte array</summary>
    public void SendData(byte[] data)
    {
        if (IsConnected())
        {
            WSConnectionWGL.WSSend(_id, data, data.Length);
            // TODO in plugin: Reset the keepalive timer.
        }
    }
    ///<summary>TRUE if connected and listening</summary>
    public bool IsConnected()
    {
        return (_id >= 0 && WSConnectionWGL.WSGetState(_id) == 1 && _connected && !_disconnecting);
    }
#elif BRIDGE_NET
    ///<summary>Sets the WebSocket events</summary>
    public void Setup(EventVoid evOpen = null, EventMessage evMessage = null, EventException evError = null, EventVoid evClose = null)
    {
        Dispose();
        lock (_clientLock)
        {
            _disconnecting = false;
            // Create the timers:
            _timeoutTimer = new Timer(ConnectionTimedOut, null, Timeout.Infinite, Timeout.Infinite);
            _keepAliveTimer = new Timer(KeepAliveTimedOut, null, Timeout.Infinite, Timeout.Infinite);
            _activityCnt = 0;
            // Events:
            onOpen = evOpen;
            onMessage = evMessage;
            onError = evError;
            onClose = evClose;
            // Setup is done:
            _setup = true;
        }
    }
    ///<summary>Set the connection (to send and receive)</summary>
    public void Connect(string serverURL, float timeout = 5f, float keepAliveTimeout = 15f, bool disableWatchdog = false)
    {
        lock (_clientLock)
        {
            if (_setup && _client == null)
            {
                _disconnecting = false;
                _activityCnt = 0;
                _remoteURL = _serverURL = serverURL.Replace(" ", string.Empty).ToLower();
                // Make the remote WebSocket URL:
                if (_serverURL.Contains("http"))
                    _remoteURL = _serverURL.Replace("http", "ws");
                else if (_serverURL.Contains("https"))
                    _remoteURL = _serverURL.Replace("https", "wss");
                try
                {
                    // Start the connection:
                    _client = new WebSocket(_remoteURL);
                    _client.OnOpen = (Event e) => {
                        _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _connected = true;
                        // Force a message to be sent periodically in order to keep the connection alive:
                        _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
                        _activityCnt = _activity;
                        SafeOnOpen();
                    };
                    _client.OnMessage = (MessageEvent me) => {
                        _activityCnt = _activity;
                        FileReader fileReader = new FileReader();
                        fileReader.OnLoad = (Event ev) => {
                            Uint8Array buffer = new Uint8Array((ArrayBuffer)(ev.CurrentTarget as FileReader).Result);
                            if (buffer.ByteLength > 0)
                            {
                                byte[] data = new byte[buffer.ByteLength];
                                for (int i = 0; i < data.Length; i++)
                                {
                                    data[i] = buffer[i];                                                            // TODO: No better way? Really?
                                }
                                if (onMessage != null)
                                {
                                    SafeOnMessage(data);
                                }
                                else
                                {
                                    lock (_rxBufferLock)
                                    {
                                        _rxBuffer.Add(data);                                                        // Save data in buffer.
                                    }
                                }
                            }
                        };
                        fileReader.ReadAsArrayBuffer((Blob)me.Data);
                    };
                    _client.OnError = (Event e) => {
                        onError?.Invoke(0, "[WSConnection.SafeOnMessage] WebSocket error (Information not available).", this);
                    };   // TODO: Obtain error code and description.
                    _client.OnClose = (CloseEvent ce) => {
                        if (!_disconnecting)
                        {
                            // TODO: The connection retry fires this close event, it should be bypassed somehow.
                            Disconnect();
                            SafeOnClose();
                        }
                    };
                    // Start/stop the connection timeout timer:
                    _timeout = timeout;
                    _timeoutTimer.Change(_timeout > 0f ? SecondsToMiliseconds(_timeout) : Timeout.Infinite, Timeout.Infinite);
                    _keepAliveTimeout = keepAliveTimeout;
                    _disableWatchdog = disableWatchdog;
                }
                catch (System.Exception e)
                {
                    onError?.Invoke(e.HResult, "[WSConnection.Connect] " + e.Message, this);
                }
            }
            else
            {
                WriteLine("[WSConnection.Connect] You should call Setup() before attempt to connect.", true);
            }
        }
    }

    ///<summary>Close the connection correctly</summary>
    public void Disconnect()
    {
        lock (_clientLock)
        {
            _disconnecting = true;
            _connected = false;
            _remoteURL = string.Empty;
            // Stop timeout timer:
            if (_timeoutTimer != null)
                _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            // Stop "keep alive" timer:
            if (_keepAliveTimer != null)
                _keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _activityCnt = 0;
            // Close client last (to avoid null reference exception):
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
        }
    }
    ///<summary>Close the connection correctly without firing events</summary>
    public void Dispose()
    {
        Disconnect();
        lock (_clientLock)
        {
            // Delete timers:
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
            }
            // Clear events:
            onOpen = null;
            onMessage = null;
            onClose = null;
            onError = null;
            // Clear setup:
            _serverURL = string.Empty;
            _remoteURL = string.Empty;
            _setup = false;
        }
    }

    ///<summary>Connection timeout event</summary>
    void ConnectionTimedOut(object state)
    {
        // Reset the connection
        Disconnect();
        Connect(_serverURL, _timeout, _keepAliveTimeout, _disableWatchdog);
    }
    ///<summary>Keep alive connection timeout event</summary>
    void KeepAliveTimedOut(object state)
    {
        if (_activityCnt > 0)
        {
            _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
            if(!_disableWatchdog)
                _activityCnt--;
            SendData(new byte[] { });               // Client ping message.
        }
        else
        {
            Disconnect();
            SafeOnClose();
        }
    }

    ///<summary>Sends a byte array</summary>
    public void SendData(byte[] data)
    {
        if (IsConnected())
        {
            lock (_clientLock)
            {
                try
                {
                    _client.Send(new Uint8Array(data).Buffer);
                    // TODO in plugin: Reset the keepalive timer.
                }
                catch (System.Exception e)
                {
                    onError?.Invoke(e.HResult, "[WSConnection.SendData] " + e.Message, this);
                }
            }
        }
    }

    ///<summary>Calls the onOpen event preventin crashes is external code fails</summary>
    void SafeOnOpen()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onOpen?.Invoke(this);
        }
        catch (System.Exception e)
        {
            onError?.Invoke(e.HResult, "[WSConnection.SafeOnOpen] " + e.Message, this);
        }
    }
    ///<summary>Calls the onMessage event preventin crashes is external code fails</summary>
    void SafeOnMessage(byte[] message)
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onMessage?.Invoke(message, this);
        }
        catch (System.Exception e)
        {
            onError?.Invoke(e.HResult, "[WSConnection.SafeOnMessage] " + e.Message + " - Received: " + (ByteArrayToString(message)), this);
        }
    }
    ///<summary>Calls the onClose event preventin crashes is external code fails</summary>
    void SafeOnClose()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onClose?.Invoke(this);
        }
        catch (System.Exception e)
        {
            onError?.Invoke(e.HResult, "[WSConnection.SafeOnClose] " + e.Message, this);
        }
    }

    ///<summary>TRUE if connected and listening</summary>
    public bool IsConnected()
    {
        if (_client != null)
            return (_client.ReadyState == WebSocket.State.Open && _connected && !_disconnecting);
        else
            return false;
    }
#else
    ///<summary>Destructor</summary>
    ~WSConnection()
    {
        Dispose();
    }

    ///<summary>Sets the WebSocket events</summary>
    public void Setup(EventVoid evOpen = null, EventMessage evMessage = null, EventException evError = null, EventVoid evClose = null)
    {
        Dispose();
        lock (_clientLock)
        {
            _disconnecting = false;
            _timeoutTimer = new Timer(ConnectionTimedOut, null, Timeout.Infinite, Timeout.Infinite);
            _keepAliveTimer = new Timer(KeepAliveTimedOut, null, Timeout.Infinite, Timeout.Infinite);
            _activityCnt = 0;
            // Events:
            onOpen = evOpen;
            onMessage = evMessage;
            onError = evError;
            onClose = evClose;
            // Setup is done:
            _setup = true;
        }
    }
    ///<summary>Set the connection (to send and receive)</summary>
    public void Connect(string serverURL, float timeout = 5f, float keepAliveTimeout = 15f, bool disableWatchdog = false)
    {
        lock (_clientLock)
        {
            if (_setup && _client == null)
            {
                _disconnecting = false;
                _remoteURL = _serverURL = serverURL.Replace(" ", string.Empty).ToLower();
                // Make the remote WebSocket URL:
                if (_serverURL.Contains("http"))
                    _remoteURL = _serverURL.Replace("http", "ws");
                else if (_serverURL.Contains("https"))
                    _remoteURL = _serverURL.Replace("https", "wss");
                try
                {
                    _activityCnt = 0;
                    _keepAliveTimeout = keepAliveTimeout;
                    _disableWatchdog = disableWatchdog;
                    _client = new ClientWebSocket();
                    _client.Options.KeepAliveInterval = System.TimeSpan.FromSeconds(_keepAliveTimeout);
                    // Start the connection:
                    _client.ConnectAsync(new System.Uri(_remoteURL), CancellationToken.None);
                    // Start/stop the connection timeout timer:
                    _timeout = timeout;
                    _timeoutTimer.Change(_timeout > 0f ? SecondsToMiliseconds(_timeout) : Timeout.Infinite, Timeout.Infinite);
                    // Try the connection:
                    ThreadPool.QueueUserWorkItem(Connecting);
                }
                catch (System.Exception e)
                {
                    ThreadPool.QueueUserWorkItem((object s) => { onError?.Invoke(e.HResult, "[WSConnection.Connect] " + e.Message, this); });
                }
            }
            else
            {
                WriteLine("[WSConnection] You should call Setup() before attempt to connect.", true);
            }
        }
    }
    ///<summary>Close the connection correctly</summary>
    public void Disconnect()
    {
        lock (_clientLock)
        {
            _disconnecting = true;
            _connected = false;
            _remoteURL = string.Empty;
            // Stop timeout timer:
            if (_timeoutTimer != null)
                _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
            // Stop "keep alive" timer:
            if (_keepAliveTimer != null)
                _keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _activityCnt = 0;
            // Close clients:
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
            // Close listener:
            try
            {
                Thread aux = _receiveThread;
                _receiveThread = null;
                if (aux.IsAlive)
                    aux.Abort();
            }
            catch { }
        }
    }
    ///<summary>Close the connection correctly without firing events</summary>
    public void Dispose()
    {
        Disconnect();
        lock (_clientLock)
        {
            // Delete timers:
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
            }
            // Clear events:
            onOpen = null;
            onMessage = null;
            onClose = null;
            onError = null;
            // Clear setup:
            _serverURL = string.Empty;
            _remoteURL = string.Empty;
            _setup = false;
        }
    }

    ///<summary>Tries to connect and starts the listener</summary>
    void Connecting(object state)
    {
        try
        {
            while (!IsConnected())
            {
                if (!_connected && _client.State == WebSocketState.Open)
                {
                    _connected = true;
                    // Connection was established:
                    _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    // Start listening:
                    if (_receiveThread != null)
                    {
                        _receiveThread.Abort();
                        _receiveThread = null;
                    }
                    _receiveThread = new Thread(new ThreadStart(ReceiveData)) { IsBackground = true };
                    _receiveThread.Start();
                    // Fire onOpen event:
                    ThreadPool.QueueUserWorkItem((object s) => { SafeOnOpen(); });
                    // Force a message to be sent periodically in order to keep the connection alive:
                    _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
                    _activityCnt = _activity;
                }
            }
        }
        catch { }
    }
    ///<summary>Connection timeout event</summary>
    internal virtual void ConnectionTimedOut(object state)
    {
        // Reset the connection:
        Disconnect();
        Connect(_serverURL, _timeout, _keepAliveTimeout, _disableWatchdog);
    }
    ///<summary>Keep alive connection timeout event</summary>
    internal virtual void KeepAliveTimedOut(object state)
    {
        if (_activityCnt > 0)
        {
            // Client side keeps sending ping messages:
            _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
            if (!_disableWatchdog)
                _activityCnt--;
            if (!FileManagement.IsRunningOnMono())
                SendData(new byte[] { });               // Custom client "ping" message.
        }
        else
        {
            // Connection closed unexpectedly, allow to connect again:
            ThreadPool.QueueUserWorkItem((object s) =>
            {
                Disconnect();
                SafeOnClose();
            });
        }
    }

    ///<summary>Listening thread, gets the incoming datagrams and stores into a buffer</summary>
    void ReceiveData()
    {
        try
        {
            while (IsConnected())
            {
                // Receive the incoming data:
                WebSocketReceiveResult result;
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                do
                {
                    // Received synchronously:
                    System.ArraySegment<byte> bytesReceived = new System.ArraySegment<byte>(new byte[65535]);
                    result = _client.ReceiveAsync(bytesReceived, CancellationToken.None).Result;
                    // Save the incoming data segment:
                    ms.Write(bytesReceived.Array, bytesReceived.Offset, result.Count);
                    _activityCnt = _activity;
                    // Fragmentation detection:
                    if (!result.EndOfMessage)
                    {
                        _fragmentation = true;
                        if (result.Count > _mtu)
                            _mtu = result.Count;
                    }
                }
                while (!result.EndOfMessage);
                if (result.Count != 0)
                {
                    byte[] data = ms.ToArray();
                    ms.Dispose();
                    // Server has closed the connection? (WS#):
                    if (_client != null && _client.State == WebSocketState.Closed)
                        throw new System.Exception();
                    // Valid message received (Discard PONG messages):
                    if (!(data.Length == 2 && data[0] == 0x8A && data[1] == 0x00))
                        ThreadPool.QueueUserWorkItem((object s) => { SafeOnMessage(data); });
                }
            }
            if (!_disconnecting)
                throw new System.Exception();
        }
        catch
        {
            // The connection was lost:
            if (!_disconnecting)
            {
                _disconnecting = true;                          // So important to stop all parallel calls.
                // Connection closed unexpectedly, data should be preserved to retry the connection:
                ThreadPool.QueueUserWorkItem((object s) => {
                    SafeOnClose();
                });
                Disconnect();
            }
        }
    }

    ///<summary>Sends a byte array</summary>
    public virtual void SendData(byte[] data)
    {
        if (IsConnected())
        {
            try
            {
                lock(_clientLock)
                {
                    _client.SendAsync(new System.ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
                    Thread.Sleep(10);               // Delay to prefent the ClientWebSocket to abort itself.
                }
            }
            catch (System.Exception e)
            {
                ThreadPool.QueueUserWorkItem((object s) => {
                    onError?.Invoke(e.HResult, "[WSConnection.SendData] " + e.Message, this);
                });
            }
        }
    }

    ///<summary>Calls the onOpen event preventing crashes if external code fails</summary>
    internal void SafeOnOpen()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onOpen?.Invoke(this);
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[WSConnection.SafeOnOpen] " + e.Message, this);
            });
        }
    }
    ///<summary>Calls the onMessage event preventing crashes if external code fails</summary>
    internal void SafeOnMessage(byte[] message)
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            // If no event defined, save data in buffer:
            if (onMessage != null)
            {
                onMessage?.Invoke(message, this);
            }
            else
            {
                lock (_rxBufferLock)
                {
                    _rxBuffer.Add(message);             // Save data in buffer.
                }
            }
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[WSConnection.SafeOnMessage] " + e.Message + " - Received: " + (ByteArrayToString(message)), this);
            });
        }
    }
    ///<summary>Calls the onClose event preventing crashes if external code fails</summary>
    internal void SafeOnClose()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onClose?.Invoke(this);
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[WSConnection.SafeOnClose] " + e.Message, this);
            });
        }
    }

    ///<summary>TRUE if connected and listening</summary>
    public virtual bool IsConnected()
    {
        return _client != null && _client.State == WebSocketState.Open && _connected && _receiveThread != null && _receiveThread.IsAlive && !_disconnecting;
    }
#endif

    ///<summary>Returns true if there is any data into the buffer</summary>
    public bool DataAvailable()
    {
        return (_rxBuffer.Count > 0);
    }
    ///<summary>Get the next received message</summary>
    public byte[] GetMessage()
    {
        byte[] message = null;
        if (DataAvailable())
        {
            lock (_rxBufferLock)
            {
                message = _rxBuffer[0];
                _rxBuffer.RemoveAt(0);
            }
        }
        return message;
    }
    ///<summary>Flush the input message buffer</summary>
    public void ClearInputBuffer()
    {
        lock (_rxBufferLock)
        {
            _rxBuffer.Clear();
        }
    }

    ///<summary>Sends a string</summary>
    public void SendData(string data)
    {
        if (IsConnected())
            SendData(StringToByteArray(data));
    }

    ///<summary>Get the target active URL</summary>
    public string GetURL()
    {
        if (_connected)
            return _remoteURL;
        else
            return "";
    }

    ///<summary>Gets the default IP address (IPv4 or IPv6)</summary>
    public string GetDefaultIPAddress(string ipMode = "")
    {
#if UNITY_WEBGL || BRIDGE_NET
        WriteLine("[UnityWSConnection] Reading local IP is not possible in WebGL.");
        return string.Empty;
#else
        string ipv4 = "";
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                ipv4 = (socket.LocalEndPoint as IPEndPoint).Address.ToString();
            }
        }
        catch { }
        string ipv6 = "";
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, 0))
            {
                socket.Connect("2001::1", 65530);
                ipv6 = (socket.LocalEndPoint as IPEndPoint).Address.ToString();
            }
        }
        catch { }
        switch (ipMode.ToLower())
        {
            case "ipv4":
                return ipv4;
            case "ipv6":
                return ipv6;
            default:
                return string.IsNullOrEmpty(ipv4) ? ipv6 : ipv4;
        }
#endif
    }
    ///<summary>Get the list of physical MAC addresses</summary>
    public string[] GetMacAddress()
    {
#if UNITY_WEBGL || BRIDGE_NET
        WriteLine("[UnityWSConnection] Reading local MAC is not possible in WebGL.");
        return null;
#else
        List<string> macAdress = new List<string>();
        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in nics)
        {
            PhysicalAddress address = adapter.GetPhysicalAddress();
            if (address.ToString() != "")
            {
                macAdress.Add(address.ToString());
            }
        }
        return macAdress.ToArray();
#endif
    }

    /// <summary>UTF8 byte[] conversion to string</summary>
    public string ByteArrayToString(byte[] content)
    {
        return System.Text.Encoding.UTF8.GetString(content);
    }
    /// <summary>UTF8 string conversion to byte[]</summary>
    public byte[] StringToByteArray(string content)
    {
        return System.Text.Encoding.UTF8.GetBytes(content);
    }
    ///<summary>Converts a float in seconds into an int in miliseconds</summary>
    public int SecondsToMiliseconds(float seconds)
    {
        return (int)System.Math.Floor(1000f * seconds);
    }
    /// <summary>Writes to the console depending on the platform.</summary>
    internal void WriteLine(string line, bool error = false)
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