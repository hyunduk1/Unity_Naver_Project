using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/*
 * This is the TCPServer's wrapper for Unity.
 * Please check the Sockets Under Control documentation.
 */

/* Event templates:

    // Server:
    public void OnTCPSOpen(UnityTCPServer server)
    {}
    public void OnTCPSNewConnection(TCPConnection connection, UnityTCPServer server)
    {}
    public void OnTCPSError(int code, string message, UnityTCPServer server)
    {}
    public void OnTCPSClose(UnityTCPServer server)
    {}

    // Client (incoming connection):
    public void OnTCPMessage(byte[] message, TCPConnection connection)
    {}
    public void OnTCPError(int code, string message, TCPConnection connection)
    {}
    public void OnTCPClose(TCPConnection connection)
    {}
 */

public class UnityTCPServer : MonoBehaviour
{
    TCPServer _server;

    public string _localIP;                             // The local IP to bind to (Left empty will get IPv4 and IPv6 addresses automatically).
    public int _localPort = 60002;                      // The local port to bind to.
    public bool _connectOnAwake = true;                 // Forces the connection to try to connect in the Awake().
    public int _maxConnections = 100;                   // The maximum allowed count of incoming connections.
    public float _keepAliveTimeout = 40f;               // Time interval in seconds to probe connection activity.

    // Custom event to pass server as arguments:
    [System.Serializable]
    public class BaseEvent : UnityEvent<UnityTCPServer> { }
    class UnityEventBase
    {
        public BaseEvent _event;
        public UnityEventBase(BaseEvent callback)
        {
            _event = callback;
        }
        public void Invoke(UnityTCPServer server)
        {
            _event.Invoke(server);
        }
    }
    // Custom event to pass connection and server as arguments:
    [System.Serializable]
    public class ConnectionEvent : UnityEvent<TCPConnection, UnityTCPServer> { }
    class UnityEventConnection
    {
        public ConnectionEvent _event;
        public TCPConnection _connection;
        public UnityEventConnection(ConnectionEvent callback, TCPConnection connection)
        {
            _event = callback;
            _connection = connection;
        }
        public void Invoke(UnityTCPServer server)
        {
            _event.Invoke(_connection, server);
        }
    }
    // Custom event to pass int, string and server as arguments:
    [System.Serializable]
    public class ErrorEvent : UnityEvent<int, string, UnityTCPServer> {}
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
        public void Invoke(UnityTCPServer server)
        {
            _event.Invoke(_code, _message, server);
        }
    }

    // Custom event to pass connection as arguments:
    [System.Serializable]
    public class ConnectionBaseEvent : UnityEvent<TCPConnection> { }
    class UnityEventConnectionBase
    {
        public ConnectionBaseEvent _event;
        public TCPConnection _connection;
        public UnityEventConnectionBase(ConnectionBaseEvent callback, TCPConnection connection)
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
    public class ConnectionMessageEvent : UnityEvent<byte[], TCPConnection> { }
    class UnityEventConnectionMessage
    {
        public ConnectionMessageEvent _event;
        public TCPConnection _connection;
        public byte[] _message;
        public UnityEventConnectionMessage(ConnectionMessageEvent callback, byte[] message, TCPConnection connection)
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
    public class ConnectionErrorEvent : UnityEvent<int, string, TCPConnection> { }
    class UnityEventConnectionError
    {
        public ConnectionErrorEvent _event;
        public TCPConnection _connection;
        public int _code;
        public string _message;
        public UnityEventConnectionError(ConnectionErrorEvent callback, int code, string message, TCPConnection connection)
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
    public ConnectionMessageEvent _onConnectionMessage; // The remote connection has received a message.
    public ConnectionErrorEvent _onConnectionError;     // The remote connection triggered an exception.
    public ConnectionBaseEvent _onConnectionClose;      // The remote connection has been closed.

    // Event buffers:
    volatile List<object> _eventList;                   // Events are accumulated in this list in the same order they were fired.
    readonly object _eventListLock = new object();

    // Unity coordination:
    void Awake ()
    {
        // Event lists:
        _eventList = new List<object>();
        // Create the server:
        _server = new TCPServer(_localPort, _localIP, OnOpen, OnNewConnection, OnError, OnClose, _maxConnections, _keepAliveTimeout);
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
                    case "UnityTCPServer+UnityEventBase":
                        (_eventList[0] as UnityEventBase).Invoke(this);
                        break;
                    case "UnityTCPServer+UnityEventConnection":
                        (_eventList[0] as UnityEventConnection).Invoke(this);
                        break;
                    case "UnityTCPServer+UnityEventError":
                        (_eventList[0] as UnityEventError).Invoke(this);
                        break;
                    // Custom connection events:
                    case "UnityTCPServer+UnityEventConnectionMessage":
                        (_eventList[0] as UnityEventConnectionMessage).Invoke();
                        break;
                    case "UnityTCPServer+UnityEventConnectionError":
                        (_eventList[0] as UnityEventConnectionError).Invoke();
                        break;
                    case "UnityTCPServer+UnityEventConnectionBase":
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
        if (_server != null)
        {
            _server.Dispose();
            _server = null;
        }
    }
    void OnDestroy()
    {
        if (_server != null)
        {
            _server.Dispose();
            _server = null;
        }
    }

    /************************
     * The TCPServer events *
     ************************/
    void OnOpen(TCPServer server)
    {
        if (_onOpen != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onOpen));
            }
    }
    void OnNewConnection(TCPConnection connection, TCPServer server)
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
    void OnError(int code, string message, TCPServer server)
    {
        if (_onError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventError(_onError, code, message));
            }
    }
    void OnClose(TCPServer server)
    {
        if (_onClose != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onClose));
            }
    }

    /****************************
     * The TCPConnection events *
     ****************************/
    void OnConnectionMessage(byte[] message, TCPConnection connection)
    {
        if (_onConnectionMessage != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventConnectionMessage(_onConnectionMessage, message, connection));
            }
    }
    void OnConnectionError(int code, string message, TCPConnection connection)
    {
        if (_onConnectionError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventConnectionError(_onConnectionError, code, message, connection));
            }
    }
    void OnConnectionClose(TCPConnection connection)
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
        _server.Setup(_localPort, _localIP, OnOpen, OnNewConnection, OnError, OnClose, _maxConnections, _keepAliveTimeout);
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
    public void CloseConnection(TCPConnection connection)
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
    ///<summary>Gets the connection from the list in the provided index</summary>
    public TCPConnection GetConnection(int index)
    {
        return _server.GetConnection(index);
    }

    ///<summary>Get the local active IP (allows to get the secondary address)</summary>
    public string GetIP(bool secondary = false)
    {
        return _server.GetIP(secondary);
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