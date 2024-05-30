using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/*
 * This class helps to get a reliable world wide real-time synchronization.
 * Use this function to make sure all your devices has the same current time, and the same current
 * time of your autoritative server.
 * 
 * The retrieved time is not static, it's set into an internal watch that keeps evolving, running
 * in parallel with the system date/time. You can compare, assign or convert at any moment.
 * The synchroization request is repeated until it gets a response (once the _sync flag is activated).
 * 
 * Do not send NTP requests more than 3 times per second or the server will reject successive connections.
 * 
 * NOTE: The port 123 is restricted in Unix platforms (Linux, Android, etc.) so a repeater is needed in all cases.
 * NOTE: NTP servers are UDP only, so for WebSocket connections (WebGL) a repeater is needed in all cases.
 * NOTE: The "IPV6 only network" requirement in iOS needs a repeater in all cases, because NTP servers are IPV4 only.
 */

/* Windows Server:
 * To allow NTP_RealTime to get synchronized in server side, the "Windows Time" service must be disabled:
 *  Server Manager>Tools>Services>"Windows Time" <- Stop this service.
 * Open de properties of the service and set it as "Disabled".
 * Make sure to open the UDP port 123 in the Firewall (inbound rule).
 */

/*
    // NTP:
    void OnNTPSync()
    { }
    void OnNTPError(int code, string message)
    { }
 */

/*
     Known NTPs (IPV4 only):    - Response delay:
     "time-a.nist.gov"          - 130ms
     "europe.pool.ntp.org"      - 100ms
     "time.facebook.com"        - 55ms - 200ms
     "time.google.com"          - 50ms - 200ms
     "time.windows.com"         - 50ms (* most stable)
     "fr.pool.ntp.org"          - 45ms
     "es.pool.ntp.org"          - 45ms
 */

public static class NTP_RealTime
{
    public enum RequestState
    {
        Default,
        Waiting,
        Ready,
        Error
    }
    // Local synchronized time:
    static DateTime _syncTime = DateTime.UtcNow;            // UTC time from the NTP server.
    static Stopwatch _realTimeWatch = new Stopwatch();      // Real time in milliseconds (Always running).
    static public DateTime _now                             // Returns the synchronized time in local time-zone (Read-only).
    {
        get
        {
            return GetUTCTime().ToLocalTime();
        }
    }
    // NTP clients:
    static TCPConnection _tcpConnection;                    // The TCP client for NTP request.
    static WSConnection _wsConnection;                      // The WS client for NTP request.
    static UDPConnection _udpConnection;                    // The UDP client for NTP request (main default client).
    static Timer _retryTimer = new Timer(SendNTPRequest);   // Timer to retry the UDP request every 1 second.
    static int _retryCount = 0;                             // Counts how many times it was retried until stops.
    static Stopwatch _delayWatch = new Stopwatch();         // Stopwatch to measure the response delay.
    // NTP server connection parameters:
    static string _ntpServer = "time.windows.com";          // Default NTP server URL (UDP).
    static int _ntpPort = 123;                              // Default NTP server port (UDP).
    static public RequestState _requestState;               // Status of the NTP request.
    static public string _errorMsg;                         // Last error message.
    static bool _onceSync = false;                          // Flag that determines if the client was synchronised correctly at least once.
    // NTP server replication:
    static List<object> _ntpRequests = new List<object>();  // List of NTP requests.
    static readonly object _requestLock = new object();     // Lock object for reques stack.
    static public UDPConnection _udpRepeater;               // This UDP connection will repeat the incoming request to the NTP server.
    static bool _udpEmulation = false;                      // Emulates a server response from the internal synchronized time.
    static public TCPServer _tcpRepeater;                   // This TCP server will repeat the incoming request to the NTP server.
    static bool _tcpEmulation = false;                      // Emulates a server response from the internal synchronized time.
    static public WSServer _wsRepeater;                     // This WS server will repeat the incoming request to the NTP server.
    static bool _wsEmulation = false;                       // Emulates a server response from the internal synchronized time.
    // Events:
    public delegate void Event();
    public delegate void EventException(int code, string message);
    static public volatile Event onSync;                    // The internal time has been synchronized properly.
    static public volatile EventException onError;          // There was an error.

    ///<summary>Perform the synchronization request</summary>
    static void SendNTPRequest(object connection)
    {
        _delayWatch.Stop();
        _delayWatch.Reset();
        // Reset conditions:
        _requestState = RequestState.Waiting;
        _errorMsg = string.Empty;
        // NTP message (RFC 2030): 
        byte[] ntpRequest = new byte[48];
        ntpRequest[0] = 0x1B;                               // LeapIndicator = 0 (no leap applied), VersionNumber = 3 (V4), Mode = 3 (Client)
        // Send the NTP message depending on the connection type:
        switch (connection.GetType().ToString())
        {
            case "TCPConnection":
                (connection as TCPConnection).SendData(ntpRequest);
                break;
            case "WSConnection":
                (connection as WSConnection).SendData(ntpRequest);
                break;
            default:
                _retryCount++;
                if (_retryCount < 5)
                {
                    _udpConnection.SendData(_ntpServer, ntpRequest);
                }
                else
                {
                    _errorMsg = "NTP server not responding.";
                    OnUDPError(1, _errorMsg, connection as UDPConnection);
                }
                break;
        }
        // Start status control methods:
        _delayWatch.Start();                                // Start measuring the response delay.
    }
    ///<summary>Converts the NTP message into a DateTime object</summary>
    static DateTime NTPmessageToDateTime(byte[] ntpMessage, long delay = 0)
    {
        // Transmit Timestamp: This is the time at which the reply departed the server for the client, in 64 - bit timestamp format.
        uint intPart = BitConverter.ToUInt32(ntpMessage, 40);
        intPart = SwapEndianness(intPart);                  // Get the seconds.
        uint fractPart = BitConverter.ToUInt32(ntpMessage, 40 + 4);
        fractPart = SwapEndianness(fractPart);              // Get the seconds fraction.
        // Get the total time in milliseconds (adding delay correction):
        ulong milliseconds = (ulong)Math.Round(intPart * 1000d + fractPart * 1000d / 4294967296d + delay / 2d, 0, MidpointRounding.AwayFromZero);
        // Get the UTC time:
        return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds);
    }
    ///<summary>Converts the NTP message into a DateTime object</summary>
    static byte[] LocalUTCTimeToNTPMessage()
    {
        byte[] message = new byte[48] {
            0x1C, 0x03, 0x00, 0xE9,                         // LI, STRATUM, POLL, PRECISION
            0x00, 0x00, 0x02, 0xAE,                         // ROOT DELAY
            0x00, 0x00, 0x06, 0x93,                         // ROOT DISPERSION
            0x19, 0x42, 0xE6, 0x00,                         // REFERENCE ID
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // REFERENCE TIMESTAMP
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ORIGINATE TIMESTAMP
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // RECEIVE TIMESTAMP
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // TRANSMIT TIMESTAMP
        };
        DateTime refTime = new DateTime(1900, 1, 1, 0, 0, 0);
        ulong tms = (ulong)(GetUTCTime() - refTime).TotalMilliseconds;
        ulong result = SwapEndianness(tms / 1000L);
        result |= (ulong)SwapEndianness(tms % (tms / 1000L) * (4294967296L / 1000L)) << 32;
        // Add TRANSMIT TIMESTAMP only:
        byte[] tTimeStamp = BitConverter.GetBytes(result);
        Buffer.BlockCopy(tTimeStamp, 0, message, 40, tTimeStamp.Length);
        return message;
    }
    ///<summary>Swap big-endian to little-endian</summary>
    static uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) + ((x & 0x0000ff00) << 8) + ((x & 0x00ff0000) >> 8) + ((x & 0xff000000) >> 24));
    }
    ///<summary>Get the current UTC time</summary>
    static public DateTime GetUTCTime()
    {
        // If synchronization wasn't possible returns the local UTC time:
        if (_onceSync)
            return _syncTime.AddMilliseconds(_realTimeWatch.ElapsedMilliseconds);
        else
            return DateTime.UtcNow;
    }
    ///<summary>Distribute the received NTP message</summary>
    static void Distribute(byte[] message)
    {
        // Send the incoming message to all the connections in the stack.
        lock (_requestLock)
        {
            for (int i = 0; i < _ntpRequests.Count; i++)
            {
                switch (_ntpRequests[i].GetType().ToString())
                {
                    case "System.String":
                        _udpRepeater.SendData((string)_ntpRequests[i], message);
                        break;
                    case "TCPConnection":
                        (_ntpRequests[i] as TCPConnection).SendData(message);
                        break;
                    case "WSConnection":
                        (_ntpRequests[i] as WSConnection).SendData(message);
                        break;
                }
            }
            _ntpRequests.Clear();
        }
    }

    ///<summary>Request the UTC synchronization through UDP</summary>
    static public void SendUDPRequest(string ntpServer = "", int port = 0)
    {
        // Takes new parameters as default if available:
        if (port > 0) _ntpPort = port;
        if (!string.IsNullOrEmpty(ntpServer)) _ntpServer = ntpServer;
        // Validate the remote address:
        EndPoint ep = AddressParser(ntpServer, port);
        // Send the NTP request:
        if (_requestState != RequestState.Waiting && ep != null)
        {
            // Setup the UDP connection and the retry timer:
            if (_udpConnection == null)
                _udpConnection = new UDPConnection(_ntpPort, "", OnUDPOpen, OnUDPMessage, OnUDPError, OnUDPClose);
            else
                _udpConnection.Setup(_ntpPort, "", OnUDPOpen, OnUDPMessage, OnUDPError, OnUDPClose);
            _udpConnection.Connect(_ntpPort);               // This connection is only needed to listen the NTP server.
            _retryTimer.Change(1000, 1000);                 // Retry the request every second (if no response).
        }
    }
    ///<summary>UDP client events</summary>
    static void OnUDPOpen(UDPConnection connection)
    {
        _retryCount = 0;
        SendNTPRequest(connection);
    }
    static void OnUDPMessage(byte[] message, string remoteIP, UDPConnection connection)
    {
        _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Distribute(message);
        _delayWatch.Stop();                                 // Stop measuring the response delay.
        _realTimeWatch.Stop();
        _realTimeWatch.Reset();
        _syncTime = NTPmessageToDateTime(message, _delayWatch.ElapsedMilliseconds);
        _realTimeWatch.Start();
        _requestState = RequestState.Ready;                 // Time is correctly synchronised.
        _errorMsg = string.Empty;
        // Time synchronization is ready, so the connection isn't needed anymore:
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        WriteLine("[NTP_RealTime.UDPClient] Sync OK. UTC time: " + _syncTime.ToString("HH:mm:ss.fff") + " (Response delay:" + _delayWatch.ElapsedMilliseconds + ")");
        _onceSync = true;
        SafeOnSync();
    }
    static void OnUDPError(int code, string message, UDPConnection connection)
    {
        _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _delayWatch.Stop();
        _errorMsg = message;
        _requestState = RequestState.Error;
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        WriteLine("[NTP_RealTime.UDPClient] Error(" + code + "): " + message, true);
        onError?.Invoke(code, "[NTP_RealTime.UDPClient] Error: " + message);
    }
    static void OnUDPClose(UDPConnection connection)
    {
        _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _delayWatch.Stop();
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        if (_requestState == RequestState.Waiting)
        {
            _requestState = RequestState.Error;
            _errorMsg = "[NTP_RealTime.UDPClient] UDP connection lost without NTP server reply.";
            WriteLine(_errorMsg, true);
            onError?.Invoke(1, _errorMsg);
        }
    }

    ///<summary>Request the UTC synchronization through TCP</summary>
    static public void SendTCPRequest(string ntpServer, int port)
    {
        if (!string.IsNullOrEmpty(ntpServer))
        {
            // Setup the UDP connection and the retry timer:
            if (_tcpConnection == null)
                _tcpConnection = new TCPConnection("", OnTCPOpen, OnTCPMessage, OnTCPError, OnTCPClose);
            else
                _tcpConnection.Setup("", OnTCPOpen, OnTCPMessage, OnTCPError, OnTCPClose);
            _tcpConnection.ClearEOF();
            _tcpConnection.Connect(port, ntpServer);        // This connection is only needed to listen the NTP server.
        }
    }
    ///<summary>TCP client events</summary>
    static void OnTCPOpen(TCPConnection connection)
    {
        SendNTPRequest(connection);
    }
    static void OnTCPMessage(byte[] message, TCPConnection connection)
    {
        Distribute(message);
        _delayWatch.Stop();                                 // Stop measuring the response delay.
        _realTimeWatch.Stop();
        _realTimeWatch.Reset();
        _syncTime = NTPmessageToDateTime(message, _delayWatch.ElapsedMilliseconds);
        _realTimeWatch.Start();
        _requestState = RequestState.Ready;                 // Time is correctly synchronised.
        _errorMsg = string.Empty;
        // Time synchronization is ready, so the connection isn't needed anymore:
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        WriteLine("[NTP_RealTime.TCPClient] Sync OK. UTC time: " + _syncTime.ToString("HH:mm:ss.fff") + " (Response delay:" + _delayWatch.ElapsedMilliseconds + ")");
        _onceSync = true;
        SafeOnSync();
    }
    static void OnTCPError(int code, string message, TCPConnection connection)
    {
        _delayWatch.Stop();
        _errorMsg = message;
        _requestState = RequestState.Error;
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        WriteLine("[NTP_RealTime.TCPClient] Error(" + code + "): " + message, true);
        onError?.Invoke(code, "[NTP_RealTime.TCPClient] Error: " + message);
    }
    static void OnTCPClose(TCPConnection connection)
    {
        _delayWatch.Stop();
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        if (_requestState == RequestState.Waiting)
        {
            _requestState = RequestState.Error;
            _errorMsg = "[NTP_RealTime.TCPClient] TCP connection lost without NTP server reply.";
            WriteLine(_errorMsg, true);
            onError?.Invoke(1, _errorMsg);
        }
    }

    ///<summary>Request the UTC synchronization through WebSocket</summary>
    static public void SendWSRequest(string ntpServer)
    {
        if (!string.IsNullOrEmpty(ntpServer))
        {
            // Setup the UDP connection and the retry timer:
            if (_wsConnection == null)
                _wsConnection = new WSConnection(OnWSOpen, OnWSMessage, OnWSError, OnWSClose);
            else
                _wsConnection.Setup(OnWSOpen, OnWSMessage, OnWSError, OnWSClose);
            _wsConnection.Connect(ntpServer, 5f, 5f);       // This connection is only needed to listen the NTP server.
        }
    }
    ///<summary>WS client events</summary>
    static void OnWSOpen(WSConnection connection)
    {
        SendNTPRequest(connection);
    }
    static void OnWSMessage(byte[] message, WSConnection connection)
    {
        Distribute(message);
        _delayWatch.Stop();                              // Stop measuring the response delay.
        _realTimeWatch.Stop();
        _realTimeWatch.Reset();
        _syncTime = NTPmessageToDateTime(message, _delayWatch.ElapsedMilliseconds);
        _realTimeWatch.Start();
        _requestState = RequestState.Ready;                 // Time is correctly synchronised.
        _errorMsg = string.Empty;
        // Time synchronization is ready, so the connection isn't needed anymore:
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        WriteLine("[NTP_RealTime.WSClient] Sync OK. UTC time: " + _syncTime.ToString("HH:mm:ss.fff") + " (Response delay:" + _delayWatch.ElapsedMilliseconds + ")");
        _onceSync = true;
        SafeOnSync();
    }
    static void OnWSError(int code, string message, WSConnection connection)
    {
        _delayWatch.Stop();
        _errorMsg = message;
        _requestState = RequestState.Error;
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        WriteLine("[NTP_RealTime.WSClient] Error(" + code + "): " + message, true);
        onError?.Invoke(code, "[NTP_RealTime.WSClient] Error: " + message);
    }
    static void OnWSClose(WSConnection connection)
    {
        _delayWatch.Stop();
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
        if(_requestState == RequestState.Waiting)
        {
            _requestState = RequestState.Error;
            WriteLine("[NTP_RealTime.WSClient] WS connection lost without NTP server reply.", true);
            onError?.Invoke(1, "[NTP_RealTime.WSClient] WS connection lost without NTP server reply.");
        }
    }

    ///<summary>The UDP repeater server allows to have a NTP server in a different port (also supports IPV6)</summary>
    static public void StartUDPRepeater(int port, string localIP = "", bool ntpEmulation = false)
    {
        if (_udpRepeater == null)
            _udpRepeater = new UDPConnection(port, localIP, OnUDPSOpen, OnUDPSMessage, OnUDPSError, OnUDPSClose);
        else
            _udpRepeater.Setup(port, localIP, OnUDPSOpen, OnUDPSMessage, OnUDPSError, OnUDPSClose);
        _udpEmulation = ntpEmulation;
        _udpRepeater.Connect(port);
    }
    ///<summary>Stop the UDP repeater server</summary>
    static public void StopUDPRepeater()
    {
        if (_udpRepeater != null)
        {
            _udpRepeater.Dispose();
            _udpRepeater = null;
        }
        WriteLine("[NTP_RealTime] UDP server closed.");
    }
    ///<summary>Checks if the UDP repeater is running</summary>
    static public bool IsUDPRepeaterRunning()
    {
        return _udpRepeater != null && _udpRepeater.IsConnected();
    }
    ///<summary>UDP repeater server events</summary>
    static void OnUDPSOpen(UDPConnection connection)
    {
        WriteLine("[NTP_RealTime] UDP server ready.");
    }
    static void OnUDPSMessage(byte[] message, string remoteIP, UDPConnection connection)
    {
        if (message.Length == 48 && message[0] == 0x1B)
        {
            if (_udpEmulation)
            {
                WriteLine("[NTP_RealTime] Emulating NTP server through UDP.");
                // Reply with the synchronised local UTC time:
                connection.SendData(remoteIP, LocalUTCTimeToNTPMessage());
            }
            else
            {
                WriteLine("[NTP_RealTime] Repeating UDP request.");
                // A client has requested the time synchronization.
                // From the OnUDPMessage() event every connection in the stack receives its answer.
                // Add the connection to the stack (UDP has no independent connections, it saves the remote IP).
                lock (_requestLock)
                {
                    if (!_ntpRequests.Contains(remoteIP))
                        _ntpRequests.Add(remoteIP);
                }
                // Start the request if not in process.
                // This is done to avoid simultaneous requests to the NTP server, they are grouped automatically.
                if (_requestState != RequestState.Waiting)
                    SendUDPRequest();
            }
        }
    }
    static void OnUDPSError(int code, string message, UDPConnection connection)
    {
        WriteLine("[NTP_RealTime.UDPRepeater] Error(" + code + "): " + message, true);
        if (_udpRepeater != null)
        {
            _udpRepeater.Dispose();
            _udpRepeater = null;
        }
        onError?.Invoke(code, "[NTP_RealTime.UDPRepeater] Error: " + message);
    }
    static void OnUDPSClose(UDPConnection connection)
    {
        WriteLine("[NTP_RealTime] UDP server closed.");
        if (_udpRepeater != null)
        {
            _udpRepeater.Dispose();
            _udpRepeater = null;
        }
    }

    ///<summary>The TCP repeater server allows to have a NTP server in TCP (also supports IPV6)</summary>
    static public void StartTCPRepeater(int port, string localIP = "", bool ntpEmulation = false)
    {
        if (_tcpRepeater == null)
            _tcpRepeater = new TCPServer(port, localIP, OnTCPSOpen, OnTCPSNewConnection, OnTCPSError, OnTCPSClose);
        else
            _tcpRepeater.Setup(port, localIP, OnTCPSOpen, OnTCPSNewConnection, OnTCPSError, OnTCPSClose);
        _tcpEmulation = ntpEmulation;
        _tcpRepeater.Connect();
    }
    ///<summary>Stop the TCP repeater server</summary>
    static public void StopTCPRepeater()
    {
        if (_tcpRepeater != null)
        {
            _tcpRepeater.Dispose();
            _tcpRepeater = null;
        }
        WriteLine("[NTP_RealTime] TCP server closed.");
    }
    ///<summary>Checks if the TCP repeater is running</summary>
    static public bool IsTCPRepeaterRunning()
    {
        return _tcpRepeater != null && _tcpRepeater.IsConnected();
    }
    ///<summary>TCP repeater server events</summary>
    static void OnTCPSOpen(TCPServer server)
    {
        WriteLine("[NTP_RealTime] TCP server ready.");
    }
    static void OnTCPSNewConnection(TCPConnection connection, TCPServer server)
    {
        WriteLine("[NTP_RealTime] New TCP connection.");
        // Set the events in the incoming connection:
        connection.ClearEOF();
        connection.onMessage = OnTCPCMessage;
        connection.onError = OnTCPCError;
        connection.onClose = OnTCPCClose;
    }
    static void OnTCPSError(int code, string message, TCPServer server)
    {
        WriteLine("[NTP_RealTime.TCPRepeater] TCP server error(" + code + "): " + message, true);
        if (_tcpRepeater != null && !message.Contains("OnErrorAux"))
        {
            _tcpRepeater.Dispose();
            _tcpRepeater = null;
        }
        onError?.Invoke(code, "[NTP_RealTime.TCPRepeater] Error: " + message);
    }
    static void OnTCPSClose(TCPServer server)
    {
        WriteLine("[NTP_RealTime] TCP server closed.");
        if (_tcpRepeater != null)
        {
            _tcpRepeater.Dispose();
            _tcpRepeater = null;
        }
    }
    ///<summary>TCP repeater incoming connection events</summary>
    static void OnTCPCMessage(byte[] message, TCPConnection connection)
    {
        if (message.Length == 48 && message[0] == 0x1B)
        {
            if (_tcpEmulation)
            {
                WriteLine("[NTP_RealTime] Emulating NTP server through TCP.");
                // Reply with the synchronised local UTC time:
                connection.SendData(LocalUTCTimeToNTPMessage());
            }
            else
            {
                WriteLine("[NTP_RealTime] Repeating TCP request.");
                // A client has requested the time synchronization.
                // From the OnUDPMessage() event every connection in the stack receives its answer.
                // Add the connection to the stack.
                lock (_requestLock)
                {
                    if (!_ntpRequests.Contains(connection))
                        _ntpRequests.Add(connection);
                }
                // Start the request if not in process.
                // This is done to avoid simultaneous requests to the NTP server, they are grouped automatically.
                if (_requestState != RequestState.Waiting)
                    SendUDPRequest();
            }
        }
    }
    static void OnTCPCError(int code, string message, TCPConnection connection)
    {
        WriteLine("[NTP_RealTime.TCPRepeater.Client] TCP connection error (" + code + "): " + message, true);
        onError?.Invoke(code, "[NTP_RealTime.TCPRepeater.Client] Error: " + message);
    }
    static void OnTCPCClose(TCPConnection connection)
    {
        WriteLine("[NTP_RealTime] Closed TCP connection.");
    }

    ///<summary>The WS repeater server allows to have a NTP server in WebSockets (also supports IPV6)</summary>
    static public void StartWSRepeater(string address, bool ntpEmulation = false)
    {
        if (_wsRepeater == null)
            _wsRepeater = new WSServer(address, OnWSSOpen, OnWSSNewConnection, OnWSSError, OnWSSClose);
        else
            _wsRepeater.Setup(address, OnWSSOpen, OnWSSNewConnection, OnWSSError, OnWSSClose);
        _wsEmulation = ntpEmulation;
        _wsRepeater.Connect();
    }
    ///<summary>Stop the WS repeater server</summary>
    static public void StopWSRepeater()
    {
        if (_wsRepeater != null)
        {
            _wsRepeater.Dispose();
            _wsRepeater = null;
        }
        WriteLine("[NTP_RealTime] WS server closed.");
    }
    ///<summary>Checks if the WS repeater is running</summary>
    static public bool IsWSRepeaterRunning()
    {
        return _wsRepeater != null && _wsRepeater.IsConnected();
    }
    ///<summary>WS repeater server events</summary>
    static void OnWSSOpen(WSServer server)
    {
        WriteLine("[NTP_RealTime] WS server ready.");
    }
    static void OnWSSNewConnection(WSConnection connection, WSServer server)
    {
        WriteLine("[NTP_RealTime] New WS connection.");
        // Set the events in the incoming connection:
        connection.onMessage = OnWSCMessage;
        connection.onError = OnWSCError;
        connection.onClose = OnWSCClose;
    }
    static void OnWSSError(int code, string message, WSServer server)
    {
        WriteLine("[NTP_RealTime.WSRepeater] WS server error(" + code + "): " + message, true);
        if (_wsRepeater != null && !message.Contains("OnErrorAux"))
        {
            _wsRepeater.Dispose();
            _wsRepeater = null;
        }
        onError?.Invoke(code, "[NTP_RealTime.WSRepeater] Error: " + message);
    }
    static void OnWSSClose(WSServer server)
    {
        WriteLine("[NTP_RealTime] WS server closed.");
        if (_wsRepeater != null)
        {
            _wsRepeater.Dispose();
            _wsRepeater = null;
        }
    }
    ///<summary>WS repeater incoming connection events</summary>
    static void OnWSCMessage(byte[] message, WSConnection connection)
    {
        if (message.Length == 48 && message[0] == 0x1B)
        {
            if (_wsEmulation)
            {
                WriteLine("[NTP_RealTime] Emulating NTP server through WS.");
                // Reply with the synchronised local UTC time:
                connection.SendData(LocalUTCTimeToNTPMessage());
            }
            else
            {
                WriteLine("[NTP_RealTime] Repeating WS request.");
                // A client has requested the time synchronization.
                // From the OnUDPMessage() event every connection in the stack receives its answer.
                // Add the connection to the stack.
                lock (_requestLock)
                {
                    if (!_ntpRequests.Contains(connection))
                        _ntpRequests.Add(connection);
                }
                // Start the request if not in process.
                // This is done to avoid simultaneous requests to the NTP server, they are grouped automatically.
                if (_requestState != RequestState.Waiting)
                    SendUDPRequest();
            }
        }
    }
    static void OnWSCError(int code, string message, WSConnection connection)
    {
        WriteLine("[NTP_RealTime.WSRepeater.Client] WS connection error (" + code + "): " + message, true);
        onError?.Invoke(code, "[NTP_RealTime.WSRepeater.Client] Error: " + message);
    }
    static void OnWSCClose(WSConnection connection)
    {
        WriteLine("[NTP_RealTime] Closed WS connection.");
    }

    ///<summary>Not a real Dispose, it stops all activity and clear data</summary>
    static public void Dispose()
    {
        onSync = null;
        onError = null;
        _realTimeWatch.Stop();
        _delayWatch.Stop();
        if (_udpConnection != null)
        {
            _udpConnection.Dispose();
            _udpConnection = null;
        }
        if (_tcpConnection != null)
        {
            _tcpConnection.Dispose();
            _tcpConnection = null;
        }
        if (_wsConnection != null)
        {
            _wsConnection.Dispose();
            _wsConnection = null;
        }
        StopUDPRepeater();
        StopTCPRepeater();
        StopWSRepeater();
    }

    ///<summary>Calls the onSync event preventing crashes if external code fails</summary>
    static void SafeOnSync()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onSync?.Invoke();
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[NTP_RealTime.SafeOnSync] " + e.Message);
            });
        }
    }

    ///<summary>Returns a valid IpEndPoint from the provided IP or mode or URL</summary>
    static public IPEndPoint AddressParser(string ipOrUrl = "ipv4", int port = 0)
    {
        IPEndPoint endPoint = null;
        if (string.IsNullOrEmpty(ipOrUrl))
            ipOrUrl = "";
        else
            ipOrUrl = ipOrUrl.Replace(" ", string.Empty).ToLower();
        // Creates a "defaul" remote endpoint:
        if (string.IsNullOrEmpty(ipOrUrl) || ipOrUrl == "ipv4")
            endPoint = new IPEndPoint(IPAddress.Any, port);
        else if (ipOrUrl == "ipv6")
            endPoint = new IPEndPoint(IPAddress.IPv6Any, port);
        else
        {
            // Create an instance of IPAddress for IP V4 or V6:
            IPAddress address = null;
            try
            {
                address = IPAddress.Parse(ipOrUrl);
            }
            catch { }

            if (address == null)
            {
                // IP parsing has failed, so parse as URL:
                try
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(ipOrUrl);     // Gets the IP from a URL
                    address = ipHostInfo.AddressList[0];
                    endPoint = new IPEndPoint(address, port);
                }
                catch (System.Exception e)
                {
                    ThreadPool.QueueUserWorkItem((object s) => { onError?.Invoke(e.HResult, e.Message); });
                }
            }
            else
            {
                // IP succedded to be parsed:
                endPoint = new IPEndPoint(address, port);
            }
        }
        return endPoint;
    }
    ///<summary>Gets the default IP address (IPv4 or IPv6)</summary>
    static public string GetDefaultIPAddress(string ipMode = "")
    {
#if UNITY_WEBGL
        WriteLine("[NTP_RealTime] Reading local IP is not possible in WebGL.");
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
    static public string[] GetMacAddress()
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
    /// <summary>Writes to the console depending on the platform.</summary>
    static void WriteLine(string line, bool error = false)
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