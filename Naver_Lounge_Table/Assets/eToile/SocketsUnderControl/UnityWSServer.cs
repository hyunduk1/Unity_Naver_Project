using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/*
 * This WebSocket server was developped by eToile entirely based on TCPServer.
 * WSServer uses the secondary script "WSClient.cs" to control incoming connection directly,
 * being this other class fully integrated in a transparent way with WSConnection.
 */

/* Event templates:
   // Server:
   public void OnWSSOpen(UnityWSServer server)
   {}
   public void OnWSSNewConnection(WSConnection connection, UnityWSServer server)
   {}
   void OnWSSError(int code, string message, UnityWSServer server)
   {}
   public void OnWSSClose(UnityWSServer server)
   {}

   // Client (incoming connection):
   public void OnWSMessage(byte[] message, WSConnection connection)
   {}
   public void OnWSError(int code, string message, WSConnection connection)
   {}
   public void OnWSClose(WSConnection connection)
   {}
*/

public class UnityWSServer : MonoBehaviour
{
    WSServer _server;
    public string _serverURL;                           // The public URL to accept incoming connections.
    public int _port = 60003;
    public string _service = "ws";
    public bool _connectOnAwake = true;                 // Forces the connection to try to connect in the Awake().
    public int _maxConnections = 100;                   // The maximum allowed count of incoming connections.
    public float _keepAliveTimeout = 40f;               // Time interval in seconds to probe connection activity.

    // Custom event to pass server as arguments:
    [System.Serializable]
    public class BaseEvent : UnityEvent<UnityWSServer> { }
    class UnityEventBase
    {
        public BaseEvent _event;
        public UnityEventBase(BaseEvent callback)
        {
            _event = callback;
        }
        public void Invoke(UnityWSServer server)
        {
            _event.Invoke(server);
        }
    }
    // Custom event to pass connection and server as arguments:
    [System.Serializable]
    public class ConnectionEvent : UnityEvent<WSConnection, UnityWSServer> { }
    class UnityEventConnection
    {
        public ConnectionEvent _event;
        public WSConnection _connection;
        public UnityEventConnection(ConnectionEvent callback, WSConnection connection)
        {
            _event = callback;
            _connection = connection;
        }
        public void Invoke(UnityWSServer server)
        {
            _event.Invoke(_connection, server);
        }
    }
    // Custom event to pass int, string and server as arguments:
    [System.Serializable]
    public class ErrorEvent : UnityEvent<int, string, UnityWSServer> { }
    class UnityEventError
    {
        public ErrorEvent _event;
        public int _code;
        public string _message;
        public UnityEventError(ErrorEvent callback, int code, string message)
        {
            _event = callback;
            _code = code;
            _message = message;
        }
        public void Invoke(UnityWSServer server)
        {
            _event.Invoke(_code, _message, server);
        }
    }

    // Custom event to pass connection as arguments:
    [System.Serializable]
    public class ConnectionBaseEvent : UnityEvent<WSConnection> { }
    class UnityEventConnectionBase
    {
        public ConnectionBaseEvent _event;
        public WSConnection _connection;
        public UnityEventConnectionBase(ConnectionBaseEvent callback, WSConnection connection)
        {
            _event = callback;
            _connection = connection;
        }
        public void Invoke()
        {
            _event.Invoke(_connection);
        }
    }
    // Custom event to pass byte array and connection as arguments:
    [System.Serializable]
    public class ConnectionMessageEvent : UnityEvent<byte[], WSConnection> { }
    class UnityEventConnectionMessage
    {
        public ConnectionMessageEvent _event;
        public WSConnection _connection;
        public byte[] _message;
        public UnityEventConnectionMessage(ConnectionMessageEvent callback, byte[] message, WSConnection connection)
        {
            _event = callback;
            _connection = connection;
            _message = message;
        }
        public void Invoke()
        {
            _event.Invoke(_message, _connection);
        }
    }
    // Custom event to pass int, string and connection as arguments:
    [System.Serializable]
    public class ConnectionErrorEvent : UnityEvent<int, string, WSConnection> { }
    class UnityEventConnectionError
    {
        public ConnectionErrorEvent _event;
        public WSConnection _connection;
        public int _code;
        public string _message;
        public UnityEventConnectionError(ConnectionErrorEvent callback, int code, string message, WSConnection connection)
        {
            _event = callback;
            _connection = connection;
            _code = code;
            _message = message;
        }
        public void Invoke()
        {
            _event.Invoke(_code, _message, _connection);
        }
    }

    // Server events to be set in Unity editor (invoked from the Unity's main thread):
    [Header("Server events:")]
    public BaseEvent _onOpen;                           // Connection open event.
    public ConnectionEvent _onNewConnection;            // New connection event.
    public ErrorEvent _onError;                         // Error event.
    public BaseEvent _onClose;                          // Connection closed event.

    // Connection events to be set in Unity editor (invoked from the Unity's main thread):
    [Header("Remote connection events:")]
    public ConnectionMessageEvent _onConnectionMessage; // The remote connection has sent a message.
    public ConnectionErrorEvent _onConnectionError;     // The remote connection triggered an exception.
    public ConnectionBaseEvent _onConnectionClose;      // The remote connection has been closed.

    // Event buffers:
    volatile List<object> _eventList;                   // Events are accumulated in this list in the same order they were fired.
    readonly object _eventListLock = new object();

    // Unity coordination:
    void Awake()
    {
        // Event lists:
        _eventList = new List<object>();
        // Create the server:
        if(string.IsNullOrEmpty(_serverURL))
            _server = new WSServer(_port, "", _service, OnWSSOpen, OnWSSNewConnection, OnWSSError, OnWSSClose, _maxConnections, _keepAliveTimeout);
        else
            _server = new WSServer(_serverURL, OnWSSOpen, OnWSSNewConnection, OnWSSError, OnWSSClose, _maxConnections, _keepAliveTimeout);
        if (_connectOnAwake)
            Connect();
    }
    void Update()
    {
        try
        {
            // Fire the accumulated events (FIFO):
            while (_eventList.Count > 0)
            {
                switch (_eventList[0].ToString())
                {
                    // Custom server events:
                    case "UnityWSServer+UnityEventBase":
                        (_eventList[0] as UnityEventBase).Invoke(this);
                        break;
                    case "UnityWSServer+UnityEventConnection":
                        (_eventList[0] as UnityEventConnection).Invoke(this);
                        break;
                    case "UnityWSServer+UnityEventError":
                        (_eventList[0] as UnityEventError).Invoke(this);
                        break;
                    // Custom connection events:
                    case "UnityWSServer+UnityEventConnectionMessage":
                        (_eventList[0] as UnityEventConnectionMessage).Invoke();
                        break;
                    case "UnityWSServer+UnityEventConnectionError":
                        (_eventList[0] as UnityEventConnectionError).Invoke();
                        break;
                    case "UnityWSServer+UnityEventConnectionBase":
                        (_eventList[0] as UnityEventConnectionBase).Invoke();
                        break;
                }
                _eventList.RemoveAt(0);
            }
        }
        catch { }
    }
    
    // Disconnect the server silently when being destroyed:
    void OnApplicationQuit()
    {
        Disconnect();
    }
    void OnDestroy()
    {
        Disconnect();
    }

    /******************************
     * The WebSocketServer events *
     ******************************/
    void OnWSSOpen(WSServer server)
    {
        // Add the event to the list:
        if (_onOpen != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onOpen));
            }
    }
    void OnWSSNewConnection(WSConnection connection, WSServer server)
    {
        if (_onNewConnection != null)
            lock (_eventListLock)
            {
                // Set the connection events:
                connection.onMessage = OnConnectionMessage;
                connection.onError = OnConnectionError;
                connection.onClose = OnConnectionClose;
                // Add the event to the list:
                _eventList.Add(new UnityEventConnection(_onNewConnection, connection));
            }
    }
    void OnWSSError(int code, string message, WSServer server)
    {
        if (_onError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventError(_onError, code, message));
            }
    }
    void OnWSSClose(WSServer server)
    {
        if (_onClose != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onClose));
            }
    }

    /***************************
     * The WSConnection events *
     ***************************/
    void OnConnectionMessage(byte[] message, WSConnection connection)
    {
        if (_onConnectionMessage != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventConnectionMessage(_onConnectionMessage, message, connection));
            }
    }
    void OnConnectionError(int code, string message, WSConnection connection)
    {
        if (_onConnectionError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventConnectionError(_onConnectionError, code, message, connection));
            }
    }
    void OnConnectionClose(WSConnection connection)
    {
        if (_onConnectionClose != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventConnectionBase(_onConnectionClose, connection));
            }
    }

    /*********************
     * Available methods *
     *********************/
    /// <summary>Applies new connection data</summary>
    public void Setup()
    {
        if (string.IsNullOrEmpty(_serverURL))
            _server.Setup(_port, "", _service, OnWSSOpen, OnWSSNewConnection, OnWSSError, OnWSSClose, _maxConnections, _keepAliveTimeout);
        else
            _server.Setup(_serverURL, OnWSSOpen, OnWSSNewConnection, OnWSSError, OnWSSClose, _maxConnections, _keepAliveTimeout);
    }
    /// <summary>Connects</summary>
    public void Connect()
    {
        _server.Connect();
    }
    /// <summary>Disconnects</summary>
    public void Disconnect()
    {
        _server.Disconnect();
        lock (_eventListLock)
        {
            _eventList.Clear();
            System.Threading.Thread.Sleep(100);
        }
    }

    ///<summary>Forces the close of the connection and removes it from the list</summary>
    public void CloseConnection(WSConnection connection)
    {
        _server.CloseConnection(connection);
    }

    ///<summary>Send a message to all available connections</summary>
    public void Distribute(byte[] data)
    {
        _server.Distribute(data);
    }
    ///<summary>Send a message to all available connections</summary>
    public void Distribute(string data)
    {
        _server.Distribute(data);
    }
    ///<summary>Gets how many connections were established</summary>
    public int GetConnectionsCount()
    {
        return _server.GetConnectionsCount();
    }
    ///<summary>Gets a specific connection</summary>
    public WSConnection GetConnection(int index)
    {
        return _server.GetConnection(index);
    }

    ///<summary>Get the active public URL</summary>
    public string GetURL(bool secondary = false)
    {
        return _server.GetURL(secondary);
    }
    /// <summary>Checks if connected or not</summary>
    public bool IsConnected()
    {
        return _server.IsConnected();
    }

    ///<summary>Gets the default IP address (IPv4 or IPv6)</summary>
    public string GetDefaultIPAddress(string ipMode = "")
    {
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
    }
    ///<summary>Get the list of physical MAC addresses</summary>
    public string[] GetMacAddress()
    {
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
    }
}