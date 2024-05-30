using System.Collections.Generic;
using System.Threading;
using System.Net.WebSockets;
using System.Security.Cryptography;

/*
 * Custom implementation of a WebSocket client to extend WSConnection.
 * 
 * Messages must be masked when received from clients (unmasked messages should be
 * rejected and connection aborted), but unmasked when sent from server.
 * 
 * Websocket package structure:
 * ----------------------------
 *  0                   1                   2                   3
 *  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
 * +-+-+-+-+-------+-+-------------+-------------------------------+
 * |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
 * |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
 * |N|V|V|V|       |S|             |   (if payload len==126/127)   |
 * | |1|2|3|       |K|             |                               |
 * +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
 * |     Extended payload length continued, if payload len == 127  |
 * + - - - - - - - - - - - - - - - +-------------------------------+
 * |                               |Masking-key, if MASK set to 1  |
 * +-------------------------------+-------------------------------+
 * | Masking-key (continued)       |          Payload Data         |
 * +-------------------------------- - - - - - - - - - - - - - - - +
 * :                     Payload Data continued ...                :
 * + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
 * |                     Payload Data continued ...                |
 * +---------------------------------------------------------------+
 * 
 * Opcodes:
 * --------
 * opcode 0x00: Fragment        \
 * opcode 0x01: Text message    | This 3 values are for fragmentation control.
 * opcode 0x02: MBinary message /
 * opcode 0x03 a 0x07: No use
 * opcode 0x08: Close handshake
 * opcode 0x09: PING \
 * opcode 0x0A: PONG /
 * 
 * Error codes:
 * ------------
 * 400 - Bad request.
 * 401 - Unauthorized.
 * 403 - Forbidden.
 * 418 - I'm a teapot. (Used as "Maximum of connections reached")
 *
 * Fragmentation behaviour:
 * ------------------------
 * Client: FIN=1, opcode=0x1, msg="hello"
 * Server: (process complete message immediately) Hi.
 * Client: FIN=0, opcode=0x1, msg="and a"
 * Server: (listening, new message containing text started)
 * Client: FIN=0, opcode=0x0, msg="happy new"
 * Server: (listening, payload concatenated to previous message)
 * Client: FIN=1, opcode=0x0, msg="year!"
 * Server: (process complete message) Happy new year to you too!
 */

partial class WSServer
{
#if UNITY_WEBGL
    class WSClient : WSConnection
    {
        ///<summary>Dummy constructor</summary>
        public WSClient(TCPConnection client, EventVoid deleteMeCallback, EventVoid onOpenCallback, string service, float keepAliveTimeout) { }
        ///<summary>Dummy Close method</summary>
        internal void Close(WebSocketState state) { }
    }
#else
    class WSClient : WSConnection
    {
        // Main objects:
        public volatile TCPConnection _tcpClient;
        public volatile HandShake _handShake;
        volatile System.Random rnd = new System.Random();
        // Events:
        volatile EventVoid onImReady;                                   // The connection was handshaked correctly.
        volatile EventVoid onDeleteMe;                                  // The connection was closed.
        // Parameters:
        volatile string _serverSvc = "";                                // Server active "service" to filter connections.
        public volatile WebSocketState _state = WebSocketState.None;
        byte[] _buffer = { };                                           // Buffer to recompose segmented packages.
        byte[] _message = { };                                          // Buffer to recompose segmented messages.
        // Status:
        volatile bool _activity;

        ///<summary>Constructor</summary>
        public WSClient(TCPConnection client, EventVoid deleteMeCallback, EventVoid onOpenCallback, string service, float keepAliveTimeout)
        {
            client.SetEOF("\r\n\r\n");
            client.onMessage = OnTCPMessage;
            client.onError = OnTCPError;
            client.onClose = OnTCPClose;
            // Status:
            _tcpClient = client;
            _serverSvc = service;
            _state = WebSocketState.Connecting;
            onDeleteMe = deleteMeCallback;
            onImReady = onOpenCallback;
            // Timers:
            _keepAliveTimeout = keepAliveTimeout;
            _timeoutTimer = new Timer(ConnectionTimedOut, null, 3000, Timeout.Infinite);
            _keepAliveTimer = new Timer(KeepAliveTimedOut, null, Timeout.Infinite, Timeout.Infinite);
        }
        ///<summary>Destructor</summary>
        ~WSClient()
        {
            if(_state == WebSocketState.Connecting || _state == WebSocketState.Open)
                Close(WebSocketState.Closed);
        }

        ///<summary>Connection timeout event</summary>
        internal override void ConnectionTimedOut(object state)
        {
            // There is not handshake, abort the connection:
            Close(WebSocketState.Aborted);
        }
        ///<summary>Keep alive connection timeout event</summary>
        internal override void KeepAliveTimedOut(object state)
        {
            // Server side checks the activity:
            if (_activity)
            {
                _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
                _activity = false;
            }
            else
            {
                Close(WebSocketState.Closed);
            }
        }

        ///<summary>TCPConnection events</summary>
        void OnTCPMessage(byte[] message, TCPConnection connection)
        {
            lock (_rxBufferLock)
            {
                // Incoming message accumulation (TCP may be fragmented):
                int offset = _buffer.Length;
                System.Array.Resize(ref _buffer, _buffer.Length + message.Length);
                System.Buffer.BlockCopy(message, 0, _buffer, offset, message.Length);
                // Analyze handshake:
                if (_handShake == null)
                {
                    // HTTP request interpretation:
                    string msg = connection.ByteArrayToString(_buffer);
                    _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _handShake = new HandShake(msg);
                    if (_handShake.IsValid() && _handShake.service == _serverSvc)
                    {
                        // Handshake successful:
                        _state = WebSocketState.Open;
                        connection.SendData(_handShake.GetAcceptResponse());
                        System.Array.Resize(ref _buffer, 0);
                        connection.ClearInputBuffer();
                        connection.ClearEOF();
                        // Connection open event:
                        ThreadPool.QueueUserWorkItem((object s) => { onImReady?.Invoke(this); });
                        _activity = true;
                        _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
                    }
                    else
                    {
                        // Handshake failed, abort connection:
                        connection.SendData(_handShake.Get400Response());
                        ThreadPool.QueueUserWorkItem((object s) => { onError?.Invoke(400, "[WSClient.HandShake] Non WebSocket connection automatically aborted.", this); });
                        Close(WebSocketState.Aborted);
                    }
                }
                else
                {
                    // Analize package:
                    bool fin;
                    byte[] data;
                    // Is package complete? :
                    while (_buffer.Length > 0 && DecodePackage(ref _buffer, out fin, out data))
                    {
                        // Package analysis:
                        if (data.Length > 0)
                        {
                            // Incoming WS message accumulation (WS may be also fragmented on top of TCP):
                            offset = _message.Length;
                            System.Array.Resize(ref _message, _message.Length + data.Length);
                            System.Buffer.BlockCopy(data, 0, _message, offset, data.Length);
                            // Call the message event if the data is complete:
                            if (fin)
                            {
                                byte[] auxBuffer = new byte[_message.Length];
                                System.Buffer.BlockCopy(_message, 0, auxBuffer, 0, _message.Length);
                                System.Array.Resize(ref _message, 0);
                                ThreadPool.QueueUserWorkItem((object s) => { SafeOnMessage(auxBuffer); });
                            }
                        }
                    }
                }
            }
        }
        void OnTCPError(int code, string message, TCPConnection connection)
        {
            onError?.Invoke(code, "[WSClient.OnTCPError] " + message, this);
            Close(WebSocketState.Aborted);
        }
        void OnTCPClose(TCPConnection connection)
        {
            if (_state != WebSocketState.Closed)
                Close(WebSocketState.Closed);
        }

        ///<summary>Synchronous method to send data</summary>
        public override void SendData(byte[] data)
        {
            if (IsConnected())
            {
                try
                {
                    _tcpClient.SendData(EncodePackage(data, true, 0x02, false));
                }
                catch (System.Exception e)
                {
                    ThreadPool.QueueUserWorkItem((object s) => {
                        onError?.Invoke(e.HResult, "[WSClient.SendData] " + e.Message, this);
                    });
                }
            }
        }

        ///<summary>TRUE if connected and listening</summary>
        public override bool IsConnected()
        {
            return _tcpClient != null && _tcpClient.IsConnected() && _state == WebSocketState.Open && !_disconnecting;
        }

        ///<summary>Encode a byte array into a WS package</summary>
        byte[] EncodePackage(byte[] data, bool fin, int opcode, bool masking = true)
        {
            // Get header length:
            int packageLen = 2;
            byte[] extLength = { };
            if (data.Length >= 126 && data.Length <= ushort.MaxValue)
            {
                extLength = System.BitConverter.GetBytes((ushort)data.Length);
                System.Array.Reverse(extLength);
                packageLen += 2;
            }
            else if (data.Length > ushort.MaxValue)
            {
                extLength = System.BitConverter.GetBytes((ulong)data.Length);
                System.Array.Reverse(extLength);
                packageLen += 8;
            }
            // Get a new mask (always 4 bytes):
            byte[] mask = { };
            if (masking)
            {
                mask = new byte[4];
                rnd.NextBytes(mask);
                packageLen += 4;
                // Apply mask to the data:
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)(data[i] ^ mask[i % 4]);
                }
            }
            packageLen += data.Length;

            // Build the package:
            byte[] package = new byte[packageLen];
            // Add head:
            package[0] = fin ? (byte)0x80 : (byte)0xFF;
            package[0] |= (byte)opcode;
            // Add length:
            if (extLength.Length == 0)
                package[1] = (byte)data.Length;
            else if (extLength.Length >= 2)
            {
                // Set 16bit(126) or 64bit(127) length:
                package[1] = extLength.Length == 2 ? (byte)126 : (byte)127;
                // Extended length:
                System.Buffer.BlockCopy(extLength, 0, package, 2, extLength.Length);
            }
            // Add mask:
            if (masking)
            {
                // Flag & masking-key:
                package[1] |= (byte)0x80;
                System.Buffer.BlockCopy(mask, 0, package, 2 + extLength.Length, mask.Length);
            }
            // Add data:
            int dataIndex = 2 + extLength.Length + mask.Length;
            System.Buffer.BlockCopy(data, 0, package, dataIndex, data.Length);
            return package;
        }
        ///<summary>Decode a WS package and extract the data (return true if data available)</summary>
        bool DecodePackage(ref byte[] buffer, out bool fin, out byte[] data)
        {
            _activity = true;
            _keepAliveTimer.Change(_keepAliveTimeout > 0f ? SecondsToMiliseconds(_keepAliveTimeout) : Timeout.Infinite, Timeout.Infinite);
            // Extract the first package (if it's complete) and decode preserving the rest of the buffer:
            fin = false;
            data = new byte[] { };
            try
            {
                fin = (buffer[0] & 0x80) != 0;                  // bit7
                byte rsv123 = (byte)((buffer[0] & 0x70) >> 4);  // bit6 to bit4
                uint opcode = (byte)(buffer[0] & 0x0F);         // bit4 to bit0
                bool masked = (buffer[1] & 0x80) != 0;          // bit7
                int packageSize = 0;
                switch (opcode)
                {
                    case 0:     // Message fragment.
                    case 1:     // Text message.
                    case 2:     // Binary message.
                        ulong length = (uint)buffer[1] & 0x7F;  // bit6 to bit0 (Length: 0 to 125)
                        if (masked)
                        {
                            int maskIndex = 2;
                            if (length == 126)
                            {
                                // Length: 126 to 65.535:
                                length = System.BitConverter.ToUInt16(new byte[] { buffer[3], buffer[2] }, 0);
                                maskIndex = 4;
                            }
                            else if (length == 127)
                            {
                                // Length: 65.536 to 18.446.744.073.709.551.615:
                                length = System.BitConverter.ToUInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                                maskIndex = 10;
                            }
                            // Get masking key:
                            byte[] mask = new byte[4];
                            System.Buffer.BlockCopy(buffer, maskIndex, mask, 0, mask.Length);
                            // Get decoded data:
                            data = new byte[length];
                            for (int i = 0; i < data.Length; i++)
                            {
                                data[i] = (byte)(buffer[maskIndex + mask.Length + i] ^ mask[i % 4]);
                            }
                            packageSize = maskIndex + mask.Length + data.Length;
                            // Custom PING request:
                            if (data.Length == 0)
                                SendData(new byte[] { });       // Custom server PONG
                        }
                        else
                        {
                            // Package not masked, connection should be terminated:
                            ThreadPool.QueueUserWorkItem((object s) => { onError?.Invoke(400, "[WSClient.HandShake] Package not masked.", this); });
                            Close(WebSocketState.Aborted);
                            return false;
                        }
                        break;
                    case 8:     // Close connection normally.
                        if (_state != WebSocketState.Closed)
                            Close(WebSocketState.Closed);
                        return false;
                    case 9:     // PING.
                        packageSize = 6; // head(2) + mask(4)
                        // TODO: Include the PING payload data in the PONG message.
                        byte[] pong = new byte[] { 0x8A, 0x00 };
                        SendData(pong);
                        break;
                    case 10:    // PONG.
                        packageSize = 6; // head(2) + mask(4)
                        break;
                    default:
                        break;
                }
                // Preserve remaining buffer:
                byte[] message = new byte[buffer.Length - packageSize];
                System.Buffer.BlockCopy(buffer, packageSize, message, 0, message.Length);
                buffer = message;
                return true;
            }
            catch
            {
                // The package is not yet complete:
                return false;
            }
        }

        ///<summary>True if the handshake is valid</summary>
        bool IsWebsocket()
        {
            return _handShake.IsValid();
        }
        ///<summary>Close the connection without dialog</summary>
        internal void Close(WebSocketState state)
        {
            _state = state;
            System.Array.Resize(ref _buffer, 0);
            System.Array.Resize(ref _message, 0);
            // Events:
            onImReady = null;
            if (_tcpClient != null && _tcpClient.IsConnected())
            {
                _tcpClient.Dispose();
                _tcpClient = null;
            }
            onDeleteMe?.Invoke(this);
            onDeleteMe = null;
            if (!_disconnecting)
                SafeOnClose();
            Dispose();
        }
    }

    // Clase de control de inicio de conexión:
    class HandShake
    {
        public string service = "";                             // Should match some local server service (and should not be null).
        public string host = "";                                // Should be the local server.
        public string SecWsKey = "";
        public string Origin = "";
        List<string> connection = new List<string>();           // May contain several values.
        string upgrade = "";                                    // Should be "websocket"
        int secWsVer = 0;                                       // Should be 13
        string newLine = "\r\n";
        // TODO: Error event if something went wrong on client side:
        public string errorMessage = "";
        public int errorCode = 0;

        /// <summary>Parses the client request</summary>
        public HandShake(string handShake)
        {
            try
            {
                string[] eol = { "\r\n", "\n", "\r" };
                string[] lines = handShake.Split(eol, System.StringSplitOptions.None);
                if (lines.Length >= 5)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string[] cmd = lines[i].Split(' ');
                        switch (cmd[0])
                        {
                            case "GET":
                                service = cmd[1].Trim('/');
                                break;
                            case "Connection:":
                                connection.AddRange(cmd[1].Split(','));
                                break;
                            case "Upgrade:":
                                upgrade = cmd[1];
                                break;
                            case "Sec-WebSocket-Key:":
                                SecWsKey = cmd[1];
                                break;
                            case "Sec-WebSocket-Version:":
                                secWsVer = int.Parse(cmd[1]);
                                break;
                            case "Host:":
                                host = cmd[1];
                                break;
                            case "Origin:":
                                Origin = cmd[1];
                                break;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                errorCode = e.HResult;
                errorMessage = e.Message;
            }
        }
        /// <summary>Check if the request is compatible with this server</summary>
        public bool IsValid()
        {
            for (int i = 0; i < connection.Count; i++)
            {
                if (connection[i] == "Upgrade")
                    return !string.IsNullOrEmpty(service) && upgrade == "websocket" && secWsVer == 13 && !string.IsNullOrEmpty(SecWsKey) && errorCode == 0 && string.IsNullOrEmpty(errorMessage);
            }
            return false;
        }

        /// <summary>Construct the response to an accepted connection</summary>
        public byte[] GetAcceptResponse()
        {
            string accept = System.Convert.ToBase64String(SHA1.Create().ComputeHash(StringToByteArray(SecWsKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            byte[] response = StringToByteArray("HTTP/1.1 101 Switching Protocols" + newLine
                                + "Server: SocketsUnderControl.WSServer" + newLine
                                + "Connection: Upgrade" + newLine
                                + "Upgrade: websocket" + newLine
                                + "Sec-WebSocket-Accept: " + accept);
            return response;
        }
        /// <summary>Error 400 response: Bad request</summary>
        public byte[] Get400Response()
        {
            byte[] response = StringToByteArray("HTTP/1.1 400 Bad Request" + newLine
                                + "Server: [eToile] SocketsUnderControl.WSServer" + newLine
                                + "Sec-WebSocket-Version: 13");
            return response;
        }
        /// <summary>Error 401 response: Unauthorized</summary>
        public byte[] Get401Response()
        {
            byte[] response = StringToByteArray("HTTP/1.1 401 Unauthorized" + newLine
                                + "Server: [eToile] SocketsUnderControl.WSServer" + newLine
                                + "Sec-WebSocket-Version: 13");
            return response;
        }
        /// <summary>Error 403 response: Forbidden</summary>
        public byte[] Get403Response()
        {
            byte[] response = StringToByteArray("HTTP/1.1 403 Forbidden" + newLine
                                + "Server: [eToile] SocketsUnderControl.WSServer" + newLine
                                + "Sec-WebSocket-Version: 13");
            return response;
        }
        /// <summary>Error 418 response: I'm a teapot</summary>
        public byte[] Get418Response()
        {
            byte[] response = StringToByteArray("HTTP/1.1 418 I'm a teapot" + newLine
                                + "Server: [eToile] SocketsUnderControl.WSServer" + newLine
                                + "Sec-WebSocket-Version: 13");
            return response;
        }

        /// <summary>UTF8 string conversion to byte[]</summary>
        public byte[] StringToByteArray(string content)
        {
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
    }
#endif
}