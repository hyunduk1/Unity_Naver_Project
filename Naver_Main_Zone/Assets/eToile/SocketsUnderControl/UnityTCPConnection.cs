﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/*
 * This is the TCPConnection's wrapper for Unity.
 * Please check the Sockets Under Control documentation.
 */

/* Events templates (Copy those into your code, then assign to the events of this object):

    public void OnTCPOpen(UnityTCPConnection connection)
    {}
    public void OnTCPMessage(byte[] message, UnityTCPConnection connection)
    {}
    public void OnTCPError(int code, string message, UnityTCPConnection connection)
    {}
    public void OnTCPClose(UnityTCPConnection connection)
    {}
 */

public class UnityTCPConnection : MonoBehaviour
{
    TCPConnection _connection;

    public string _localIP;                         // The local IP to bind to (Left empty will get IPv4 and IPv6 addresses automatically).
    public bool _connectOnAwake = true;             // Forces the connection to try to connect in the Awake().
    public string _remoteIP;                        // The IP to connect to (This address must be provided).
    public int _remotePort = 60002;                 // The port to listen to, it's mandatory.
    public string _eof = "\n";                      // Sequence determining the end of a received message ('\n' by default).
    public float _timeout = 5f;                     // Time in seconds to retry the connection (if it fails for any reason).
    public float _keepAliveTimeout = 15f;           // Time in seconds to send a "ping" message to the server (it means "I'm still connected and active").
    public bool _disableWatchdog = false;           // Prevents the watchdog from closing the connection when no activity is detected (set to true for servers other than SUC).

    // Custom event to pass connection as arguments:
    [System.Serializable]
    public class BaseEvent : UnityEvent<UnityTCPConnection> { }
    class UnityEventBase
    {
        public BaseEvent _event;
        public UnityEventBase(BaseEvent callback)
        {
            _event = callback;
        }
        public void Invoke(UnityTCPConnection connection)
        {
            _event.Invoke(connection);
        }
    }
    // Custom event to pass byte array and connection as arguments:
    [System.Serializable]
    public class MessageEvent : UnityEvent<byte[], UnityTCPConnection> { }
    class UnityEventMessage
    {
        public MessageEvent _event;
        public byte[] _message;
        public UnityEventMessage(MessageEvent callback, byte[] message)
        {
            _event = callback;
            _message = message;
        }
        public void Invoke(UnityTCPConnection connection)
        {
            _event.Invoke(_message, connection);
        }
    }
    // Custom event to pass int, string and connection as arguments:
    [System.Serializable]
    public class ErrorEvent : UnityEvent<int, string, UnityTCPConnection> { }
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
        public void Invoke(UnityTCPConnection connection)
        {
            _event.Invoke(_code, _message, connection);
        }
    }

    // Connection events to be set in Unity editor (invoked from the Unity's main thread):
    public BaseEvent _onOpen;                       // Connection open event.
    public MessageEvent _onMessage;                 // Message received event.
    public ErrorEvent _onError;                     // Error event.
    public BaseEvent _onClose;                      // Connection closed event.
    // Event buffers:
    volatile List<object> _eventList;               // Events are accumulated in this list in the same order they were fired.
    readonly object _eventListLock = new object();

    // Unity coordination:
    void Awake()
    {
        // Event lists:
        _eventList = new List<object>();
        // Create the client:
        _connection = new TCPConnection(_localIP, OnOpen, OnMessage, OnError, OnClose);
        _connection.SetEOF(_eof);
        if (_connectOnAwake)
            Connect();
    }
    private void Start()
    {
        if (CConfigMng.Instance._bIsMediaServer == false)
            transform.GetComponent<UnityTCPConnection>().enabled = false;
            
            return;
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
                    case "UnityTCPConnection+UnityEventBase":
                        (_eventList[0] as UnityEventBase).Invoke(this);
                        break;
                    case "UnityTCPConnection+UnityEventMessage":
                        (_eventList[0] as UnityEventMessage).Invoke(this);
                        break;
                    case "UnityTCPConnection+UnityEventError":
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
     * The TCPConnection events *
     ****************************/
    void OnOpen(TCPConnection connection)
    {
        // Add the event to the list:
        if (_onOpen != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onOpen));
            }
    }
    void OnMessage(byte[] message, TCPConnection connection)
    {
        // Add the event to the list:
        if (_onMessage != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventMessage(_onMessage, message));
            }
    }
    void OnError(int code, string message, TCPConnection connection)
    {
        // Add the event to the list:
        if (_onError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventError(_onError, code, message));
            }
    }
    void OnClose(TCPConnection connection)
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
        _connection.Setup(_localIP, OnOpen, OnMessage, OnError, OnClose);
        _connection.SetEOF(_eof);
    }
    /// <summary>Connects</summary>
    public void Connect()
    {
        _connection.Connect(_remotePort, _remoteIP, _timeout, _keepAliveTimeout, _disableWatchdog);
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

    ///<summary>Get the local active IP</summary>
    public string GetIP()
    {
        return _connection.GetIP();
    }
    /// <summary>Checks if connected or not</summary>
    public bool IsConnected()
    {
        return _connection.IsConnected();
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