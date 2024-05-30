using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/*
 * This is the UDPConnection's wrapper for Unity.
 * Please check the Sockets Under Control documentation.
 * 
 * Known problems:
 * - BUFFER: Setting the _localIP reduces the RX/TX buffer (to about 8K). The auto ("") option is recommended to use the available ~64K.
 * - IPV6 Unity: In Editor (only), it's mandatory to send a message to a valid IPV6 address in order to be able to receive further IPV6 messages.
 */

/* Events templates (Copy those into your code, then assign to the events of this object):

    public void OnUDPOpen(UnityUDPConnection connection)
    {}
    public void OnUDPMessage(byte[] message, string remoteIP, UnityUDPConnection connection)
    {}
    public void OnUDPError(int code, string message, UnityUDPConnection connection)
    {}
    public void OnUDPClose(UnityUDPConnection connection)
    {}
 */

public class UnityUDPConnection : MonoBehaviour
{
    volatile UDPConnection _connection;

    public string _localIP;                         // The local IP to bind to (Left empty will get IPv4 and IPv6 addresses automatically).
    public int _localPort = 60001;                  // The local port to bind to.
    public bool _connectOnAwake = true;             // Forces the connection to try to connect in the Awake().
    public string _remoteIP;                        // The IP to listen to (If no IP is provided, will listen any IP).
    public int _remotePort = 60001;                 // The port to listen to, it's mandatory.
    public float _keepAliveTimeout = 15f;           // Time in seconds to send a "ping" message to the server (it means "I'm still connected and active").

    // Custom event to pass connection as arguments:
    [System.Serializable]
    public class BaseEvent : UnityEvent<UnityUDPConnection> { }
    class UnityEventBase
    {
        public BaseEvent _event;
        public UnityEventBase(BaseEvent callback)
        {
            _event = callback;
        }
        public void Invoke(UnityUDPConnection connection)
        {
            _event.Invoke(connection);
        }
    }
    // Custom event to pass byte array, strings and connection as arguments:
    [System.Serializable]
    public class MessageEvent : UnityEvent<byte[], string, UnityUDPConnection> { }
    class UnityEventMessage
    {
        public MessageEvent _event;
        public byte[] _message;
        public string _remoteIP;
        public UnityEventMessage(MessageEvent callback, byte[] message, string remoteIP)
        {
            _event = callback;
            _message = message;
            _remoteIP = remoteIP;
        }
        public void Invoke(UnityUDPConnection connection)
        {
            _event.Invoke(_message, _remoteIP, connection);
        }
    }
    // Custom event to pass int, string and connection as arguments:
    [System.Serializable]
    public class ErrorEvent : UnityEvent<int, string, UnityUDPConnection> { }
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
        public void Invoke(UnityUDPConnection connection)
        {
            _event.Invoke(_code, _message, connection);
        }
    }

    // Connection events to be set in Unity editor (invoked from the Unity's main thread):
    public BaseEvent _onOpen;                       // Connection open event.
    public MessageEvent _onMessage;                 // Message received event.
    public ErrorEvent _onError;                     // Error event.
    public BaseEvent _onClose;                      // Connection closed event.
    // Event buffer:
    volatile List<object> _eventList;               // Events are accumulated in this list in the same order they were fired.
    readonly object _eventListLock = new object();

    // Unity coordination:
    void Awake()
    {

        // Event lists:
        _eventList = new List<object>();
        // Create the client:


        _connection = new UDPConnection(_localPort, _localIP, OnOpen, OnMessage, OnError, OnClose);
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
                    case "UnityUDPConnection+UnityEventBase":
                        (_eventList[0] as UnityEventBase).Invoke(this);
                        break;
                    case "UnityUDPConnection+UnityEventMessage":
                        (_eventList[0] as UnityEventMessage).Invoke(this);
                        break;
                    case "UnityUDPConnection+UnityEventError":
                        (_eventList[0] as UnityEventError).Invoke(this);
                        break;
                }
                _eventList.RemoveAt(0);
            }
        }
        catch { }
    }

    // Disconnect the ports silently when being destroyed:
    void OnApplicationQuit()
    {
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }
    }
    void OnDestroy()
    {
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }
    }

    /****************************
     * The UDPConnection events *
     ****************************/
    void OnOpen(UDPConnection connection)
    {
        // Add the event to the list:
        if (_onOpen != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onOpen));
            }
    }
    void OnMessage(byte[] message, string remoteIP, UDPConnection connection)
    {
        // Add the event to the list:
        if (_onMessage != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventMessage(_onMessage, message, remoteIP));
            }
    }
    void OnError(int code, string message, UDPConnection connection)
    {
        // Add the event to the list:
        if (_onError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventError(_onError, code, message));
            }
    }
    void OnClose(UDPConnection connection)
    {
        // Add the event to the list:
        if (_onClose != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onClose));
            }
    }

    /*********************
     * Available methods *
     *********************/
    /// <summary>Applies new connection data</summary>
    public void Setup()
    {
        _connection.Setup(_localPort, _localIP, OnOpen, OnMessage, OnError, OnClose);
    }
    /// <summary>Connects</summary>
    public void Connect()
    {
        _connection.Connect(_remotePort, _remoteIP, _keepAliveTimeout);
        Debug.Log("네트워크 ----- 생성");
    }
    /// <summary>Disconnects</summary>
    public void Disconnect()
    {
        _connection.Disconnect();
        lock (_eventListLock)
        {
            _eventList.Clear();
            System.Threading.Thread.Sleep(100);
        }
    }

    ///<summary>Returns true if there is any data into the buffer</summary>
    public bool DataAvailable()
    {
        return _connection.DataAvailable();
    }
    ///<summary>Get the next received message</summary>
    public byte[] GetMessage()
    {
        return _connection.GetMessage();
    }
    ///<summary>Flush the input message buffer</summary>
    public void ClearInputBuffer()
    {
        _connection.ClearInputBuffer();
    }
    ///<summary>Sends a byte array chosing destination IP and port (UDP only)</summary>
    public void SendData(string ip, byte[] data, int port = 0)
    {
        _connection.SendData(ip, data, port);
    }
    ///<summary>Sends a byte array chosing destination (UDP only)</summary>
    public void SendData(string ip, string data, int port = 0)
    {
        _connection.SendData(ip, data, port);
    }
    ///<summary>Sends a byte array</summary>
    public void SendData(byte[] data)
    {
        _connection.SendData(data);
    }
    ///<summary>Sends a string</summary>
    public void SendData(string data)
    {
        _connection.SendData(data);
    }

    ///<summary>Get the local active IP (allows to get the secondary address)</summary>
    public string GetIP(bool secondary = false)
    {
        return _connection.GetIP(secondary);
    }
    /// <summary>Checks if connected or not</summary>
    public string GetIPv4BroadcastAddress()
    {
        return _connection.IPV4BroadcastAddress();
    }
    /// <summary>Checks if connected or not</summary>
    public bool IsConnected()
    {
        return _connection.IsConnected();
    }
    ///<summary>Gets the input/output buffer</summary>
    public int GetIOBufferSize()
    {
        return _connection.GetIOBufferSize();
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
}