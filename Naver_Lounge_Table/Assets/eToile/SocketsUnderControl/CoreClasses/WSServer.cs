using System.Collections.Generic;
// Communications:
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

/*
 * New implementation of a WebSocket server (because WebSocket Sharp ha demostrado ser una mierda).
 */

/* Event templates:
   // Server:
   void OnWSSOpen(WSServer server)
   {}
   void OnWSSNewConnection(WSConnection connection, WSServer server)
   {}
   void OnWSSError(int code, string message, WSServer server)
   {}
   void OnWSSClose(WSServer server)
   {}

   // Client (incoming connection):
   void OnWSMessage(byte[] message, WSConnection connection)
   {}
   void OnWSError(int code, string message, WSConnection connection)
   {}
   void OnWSClose(WSConnection connection)
   {}
*/

public partial class WSServer
{
    // Local connection settings:
    public volatile TCPServer _server;                                              // This listener receives the incoming connections.
    volatile List<WSConnection> _connections = new List<WSConnection>();            // List of accepted incoming connection.
    internal readonly object _connectionLock = new object();                        // Lock the connection list access to avoid concurrency.
    // Parameters:
    volatile string _localIP = "";                                                  // The requested local IP (optional, empty means default).
    volatile int _port;                                                             // The server listening port.
    volatile string _service = "";                                                  // The service extension to filter/accept connections.
    public int _maxConnections                                                      // Maximum number of persistent connections.
    {
        get
        {
            if (_server != null)
                return _server._maxConnections;
            return 1;
        }
        set
        {
            if (_server != null)
                _server._maxConnections = value;
        }
    }
    public volatile float _keepAliveTimeout;                                        // Time interval in seconds to probe connection activity.
    // Events:
    public delegate void EventConnection(WSConnection connection, WSServer server);
    public delegate void EventException(int code, string message, WSServer server);
    public delegate void EventVoid(WSServer server);
    public volatile EventVoid onOpen;                                               // The server has started to listen properly.
    public volatile EventConnection onNewConnection;                                // A new connection was accepted and established.
    public volatile EventException onError;                                         // There was an error (the argument contains the error description).
    public volatile EventVoid onClose;                                              // The server was closed.

    ///<summary>Constructor</summary>
    public WSServer() { }
    ///<summary>Constructor</summary>
    public WSServer(int port, string localIP = "", string service = "", EventVoid evOpen = null, EventConnection evConnection = null, EventException evError = null, EventVoid evClose = null, int maxConnections = 100, float keepAliveTimeout = 40f)
    {
        Setup(port, localIP, service, evOpen, evConnection, evError, evClose, maxConnections, keepAliveTimeout);
    }
    ///<summary>Constructor</summary>
    public WSServer(string localAddress, EventVoid evOpen = null, EventConnection evConnection = null, EventException evError = null, EventVoid evClose = null, int maxConnections = 100, float keepAliveTimeout = 40f)
    {
        Setup(localAddress, evOpen, evConnection, evError, evClose, maxConnections, keepAliveTimeout);
    }
    ///<summary>Destructor</summary>
    ~WSServer()
    {
        Dispose();
    }

    ///<summary>Creates the WebSocket server (clear events and connections)</summary>
    public void Setup(string localAddress, EventVoid evOpen = null, EventConnection evConnection = null, EventException evError = null, EventVoid evClose = null, int maxConnections = 100, float keepAliveTimeout = 40f)
    {
        try
        {
            // Decompose the provided URL into required parameters:
            string[] parts = localAddress.Split('/');
            string protocol = parts[0].TrimEnd(':');
            string localIP = "";
            int port = 0;
            string service = "";
            // Get ipv4/ipv6 address:
            string[] address;
            if (parts[2].Contains("]"))
            {
                // ipv6:
                address = parts[2].Split(']');
                address[0] = address[0].Trim('[');
                if (address.Length == 2)
                    address[1] = address[1].Trim(':');
            }
            else
            {
                // ipv4:
                address = parts[2].Split(':');
            }
            localIP = address[0];
            // Get the port:
            if (address.Length == 2)
                port = FileManagement.CustomParser<int>(address[1]);
            if (!IsValidPort(port))
            {
                ThreadPool.QueueUserWorkItem((object s) =>
                {
                    evError?.Invoke(10022, "[WSServer.Setup] A valid port must be provided.", this);
                });
                return;
            }
            // Get the process ID:
            if (parts.Length > 3)
                service = parts[3];
            // Check the default IP to allow ipv4+ipv6 simultaneity:
            if (localIP == GetDefaultIPAddress("ipv4") || localIP == GetDefaultIPAddress("ipv6"))
                Setup(port, "", service, evOpen, evConnection, evError, evClose, maxConnections, keepAliveTimeout);
            else
                Setup(port, localIP, service, evOpen, evConnection, evError, evClose, maxConnections, keepAliveTimeout);
        }
        catch
        {
            ThreadPool.QueueUserWorkItem((object s) =>
            {
                evError?.Invoke(3, "[WSServer.Setup] A valid local address must be provided.", this);
            });
        }
    }
    ///<summary>Creates the WebSocket server (clear events and connections)</summary>
    public void Setup(int port, string localIP = "", string service = "", EventVoid evOpen = null, EventConnection evConnection = null, EventException evError = null, EventVoid evClose = null, int maxConnections = 100, float keepAliveTimeout = 40f)
    {
        Dispose();
        // Local address:
        _port = port;
        _localIP = localIP.Replace(" ", string.Empty).ToLower();
        _service = service;
        _keepAliveTimeout = keepAliveTimeout;
        // Events:
        onOpen = evOpen;
        onNewConnection = evConnection;
        onError = evError;
        onClose = evClose;
        // Create the server:
        _server = new TCPServer(_port, _localIP, OnTCPSOpen, OnTCPSNewConnection, OnTCPSError, OnTCPSClose, _maxConnections, 0);
        _maxConnections = maxConnections;
    }
    ///<summary>Starts listening the incoming connections</summary>
    public void Connect()
    {
        // Connect the main server:
        if (_server != null && !IsConnected())
            _server.Connect();
        else
            WriteLine("[WSServer.Connect] You should call Setup() before attempt to connect.");
    }
    ///<summary>Close the server and all of its connections</summary>
    public void Disconnect()
    {
        Setup(_port, _localIP, _service, onOpen, onNewConnection, onError, onClose, _maxConnections);
    }
    ///<summary>Close the server and all of its connections without firing events</summary>
    public void Dispose()
    {
        // Clear events:
        onOpen = null;
        onNewConnection = null;
        onError = null;
        onClose = null;
        if (_server != null)
        {
            _server.Dispose();
            _server = null;
        }
        CloseAllConnections();
    }

    // Server:
    void OnTCPSOpen(TCPServer server)
    {
        SafeOnOpen();
    }
    void OnTCPSNewConnection(TCPConnection connection, TCPServer server)
    {
        new WSClient(connection, RemoveConnection, AddNewConnection, _service, _keepAliveTimeout);
    }
    void OnTCPSError(int code, string message, TCPServer server)
    {
        onError?.Invoke(code, "[WSServer._Server] " + message, this);
    }
    void OnTCPSClose(TCPServer server)
    {
        SafeOnClose();
    }
    ///<summary>The new connection has successfuly handshaked</summary>
    void AddNewConnection(WSConnection connection)
    {
        lock (_connectionLock)
        {
            _connections.Add(connection);
        }
        // Fire the connection event:
        ThreadPool.QueueUserWorkItem((object s) => { SafeOnNewConnection(connection); });
    }

    ///<summary>Force the close of all received connections</summary>
    void CloseAllConnections()
    {
        lock (_connectionLock)
        {
            while (_connections.Count > 0)
            {
                (_connections[0] as WSClient).Close(System.Net.WebSockets.WebSocketState.Closed);
            }
        }
    }
    ///<summary>Forces the close of the connection and removes it from the list</summary>
    public void CloseConnection(WSConnection connection)
    {
        lock (_connectionLock)
        {
            connection._disconnecting = true;           // Prevent the client OnClose event.
            (connection as WSClient).Close(System.Net.WebSockets.WebSocketState.Closed);
        }
    }
    ///<summary>Removes the connection from the list</summary>
    void RemoveConnection(WSConnection connection)
    {
        lock (_connectionLock)
        {
            if (_connections.Contains(connection))
                _connections.Remove(connection);
        }
    }

    ///<summary>Send a message to all available connections</summary>
    public void Distribute(byte[] data)
    {
        lock (_connectionLock)
        {
            for (int i = 0; i < _connections.Count; i++)
            {
                _connections[i].SendData(data);
            }
        }
    }
    ///<summary>Send a message to all available connections</summary>
    public void Distribute(string data)
    {
        Distribute(System.Text.Encoding.UTF8.GetBytes(data));
    }
    ///<summary>Gets how many connections were established</summary>
    public int GetConnectionsCount()
    {
        int count = 0;
        lock (_connectionLock)
        {
            count = _connections.Count;
        }
        return count;
    }
    ///<summary>Gets a specific connection</summary>
    public WSConnection GetConnection(int index)
    {
        lock (_connectionLock)
        {
            if (index >= 0 && index < _connections.Count)
                return _connections[index];
            else
                return null;
        }
    }

    ///<summary>Calls the onOpen event preventing crashes if external code fails</summary>
    void SafeOnOpen()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onOpen?.Invoke(this);
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[WSServer.SafeOnOpen] " + e.Message, this);
            });
        }
    }
    ///<summary>Calls the onNewConnection event preventing crashes if external code fails</summary>
    void SafeOnNewConnection(WSConnection connection)
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onNewConnection?.Invoke(connection, this);
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[WSServer.SafeOnNewConnection] " + e.Message, this);
            });
        }
    }
    ///<summary>Calls the onClose event preventing crashes if external code fails</summary>
    void SafeOnClose()
    {
        // This is to detect erros in the code assigned by the user to this event:
        try
        {
            onClose?.Invoke(this);
        }
        catch (System.Exception e)
        {
            ThreadPool.QueueUserWorkItem((object s) => {
                onError?.Invoke(e.HResult, "[WSServer.SafeOnClose] " + e.Message, this);
            });
        }
    }

    ///<summary>Get the local active IP (allows to get the secondary address)</summary>
    public string GetIP(bool secondary = false)
    {
        return _server.GetIP(secondary);
    }
    ///<summary>Get the captured application port</summary>
    public int GetPort()
    {
        return _port;
    }
    ///<summary>Get the local URL (allows to get the secondary address)</summary>
    public string GetURL(bool secondary = false)
    {
        if(_server != null)
        {
            string ip = _server.GetIP(secondary);
            if (ip.Contains(":"))
                return "ws://[" + _server.GetIP(secondary) + "]:" + _port + (string.IsNullOrEmpty(_service) ? "/" : "/" + _service + "/");      // ipv6
            else
                return "ws://" + _server.GetIP(secondary) + ":" + _port + (string.IsNullOrEmpty(_service) ? "/" : "/" + _service + "/");  // ipv4
        }
        return "";
    }
    ///<summary>TRUE if connected and listening</summary>
    public bool IsConnected()
    {
        return _server != null && _server.IsConnected();
    }

    ///<summary>Returns a valid IpEndPoint from the provided IP or mode or URL</summary>
    public IPEndPoint AddressParser(string ipOrUrl = "ipv4", int port = 0)
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
                    ThreadPool.QueueUserWorkItem((object s) => {
                        onError?.Invoke(e.HResult, "[WSServer.AddressParser] " + e.Message, this);
                    });
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
    ///<summary>Checks if the provided port is a valid number</summary>
    bool IsValidPort(int port)
    {
        if (port >= 1 && port <= 65535)
            return true;
        return false;
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

    /// <summary>Writes to the console depending on the platform.</summary>
    void WriteLine(string line, bool error = false)
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