using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/* Events templates (Copy those into your code, then assign to the events of this object):
 
    public void OnCOMOpen(UnityRS232Connection connection)
    { }
    public void OnCOMMessage(byte[] message, UnityRS232Connection connection)
    { }
    public void OnCOMError(int code, string message, UnityRS232Connection connection)
    { }
    public void OnCOMClose(UnityRS232Connection connection)
    { }
 */

public class UnityRS232Connection : MonoBehaviour
{
    volatile RS232Connection _connection;

    public string _port;                            // Port name or address (depends on the OS).
    public int _baudRate = 9600;
    public byte _eof;                               // Byte indicating the end of a message.
    public float _latency = 0.1f;                   // Latency time in seconds.
    public int _dataBits = 8;
    public Parity _parity = Parity.None;
    public StopBits _stopBits = StopBits.One;
    public Handshake _handshake = Handshake.None;
    public bool _connectOnAwake = false;            // Forces the connection to try to connect in the Awake().

    // Custom event to pass connection as arguments:
    [System.Serializable]
    public class BaseEvent : UnityEvent<UnityRS232Connection> { }
    class UnityEventBase
    {
        public BaseEvent _event;
        public UnityEventBase(BaseEvent callback)
        {
            _event = callback;
        }
        public void Invoke(UnityRS232Connection connection)
        {
            _event.Invoke(connection);
        }
    }
    // Custom event to pass byte array and connection as arguments:
    [System.Serializable]
    public class MessageEvent : UnityEvent<byte[], UnityRS232Connection> { }
    class UnityEventMessage
    {
        public MessageEvent _event;
        public byte[] _message;
        public UnityEventMessage(MessageEvent callback, byte[] message)
        {
            _event = callback;
            _message = message;
        }
        public void Invoke(UnityRS232Connection connection)
        {
            _event.Invoke(_message, connection);
        }
    }
    // Custom event to pass int, string and connection as arguments:
    [System.Serializable]
    public class ErrorEvent : UnityEvent<int, string, UnityRS232Connection> { }
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
        public void Invoke(UnityRS232Connection connection)
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
        _connection = new RS232Connection();
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
                    case "UnityRS232Connection+UnityEventBase":
                        (_eventList[0] as UnityEventBase).Invoke(this);
                        break;
                    case "UnityRS232Connection+UnityEventMessage":
                        (_eventList[0] as UnityEventMessage).Invoke(this);
                        break;
                    case "UnityRS232Connection+UnityEventError":
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

    // Fixed update allows synchronisation with physics:

    /******************************
     * The RS232Connection events *
     ******************************/
    void OnOpen(RS232Connection connection)
    {
        // Add the event to the list:
        if (_onOpen != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventBase(_onOpen));
            }
    }
    void OnMessage(byte[] message, RS232Connection connection)
    {
        // Add the event to the list:
        if (_onMessage != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventMessage(_onMessage, message));
            }
    }
    void OnError(int code, string message, RS232Connection connection)
    {
        // Add the event to the list:
        if (_onError != null)
            lock (_eventListLock)
            {
                _eventList.Add(new UnityEventError(_onError, code, message));
            }
    }
    void OnClose(RS232Connection connection)
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
        if (string.IsNullOrEmpty(_port)) _port = RS232Connection.GetAvailablePorts()[0];
        _connection.Setup(_port, _baudRate, _eof, OnOpen, OnMessage, OnError, OnClose, _latency, _parity, _dataBits, _stopBits, _handshake);
    }
    /// <summary>Connects</summary>
    public void Connect()
    {
        _connection.Connect();
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

    /// <summary>Checks if connected or not</summary>
    public bool IsConnected()
    {
        return _connection.IsConnected();
    }
    ///<summary>Get the list of system available ports</summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    /// <summary>UTF8 byte[] conversion to string</summary>
    public string ByteArrayToString(byte[] content)
    {
        return _connection.ByteArrayToString(content);
    }
    /// <summary>UTF8 string conversion to byte[]</summary>
    public byte[] StringToByteArray(string content)
    {
        return _connection.StringToByteArray(content);
    }
    ///<summary>Converts a float in seconds into an int in miliseconds</summary>
    public int SecondsToMiliseconds(float seconds)
    {
        return _connection.SecondsToMiliseconds(seconds);
    }
}
