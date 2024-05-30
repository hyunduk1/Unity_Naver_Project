using UnityEngine;
using UnityEngine.UI;

public class TCPS_test : MonoBehaviour
{
    UnityTCPServer _tcpServer;
    // Message PopUp (Set in editor):
    public GameObject popupPrefab;
    // Graphic UI objects:
    Text t_localIP;
    Image i_state;
    Text t_clients;
    InputField if_port;
    InputField if_data;
    
    // Use this for initialization
    void Start ()
    {
        _tcpServer = GetComponent<UnityTCPServer>();
        // UI objects:
        t_localIP = transform.Find("PanelSetup").Find("LabelLocalIP").Find("Text").GetComponent<Text>();
        t_localIP.text = _tcpServer.GetIP(false) + System.Environment.NewLine;
        t_localIP.text += _tcpServer.GetIP(true);
        if_port = transform.Find("PanelSetup").Find("InputFieldPort").GetComponent<InputField>();
        if_port.text = "60002";
        i_state = transform.Find("PanelSetup").Find("ImageState").GetComponent<Image>();
        t_clients = transform.Find("PanelData").Find("LabelClients").Find("Text").GetComponent<Text>();
        if_data = transform.Find("PanelData").Find("InputFieldData").GetComponent<InputField>();
        if_data.text = "TCPServer Message";
    }

    // Set server local bound settings:
    public void Setup()
    {
        _tcpServer._localPort = int.Parse(if_port.text);
        _tcpServer.Setup();
        // Setup forces the disconnection:
        i_state.color = Color.red;
    }
    // Connect and start the server:
    public void Connect()
    {
        Setup();
        _tcpServer.Connect();
    }
    // Diconnect the server:
    public void Disconnect()
    {
        _tcpServer.Disconnect();
        i_state.color = Color.red;
        t_clients.text = "Clients: 0";
    }
    // Send:
    public void Send()
    {
        _tcpServer.Distribute(if_data.text);
    }

    // Events assigned in editor to UnityTCPServer (Server events):
    public void OnTCPSOpen(UnityTCPServer server)
    {
        i_state.color = Color.green;
    }
    public void OnTCPSNewConnection(TCPConnection connection, UnityTCPServer server)
    {
        t_clients.text = "Clients: " + _tcpServer.GetConnectionsCount().ToString();
    }
    public void OnTCPSError(int code, string message, UnityTCPServer server)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[TCPServer] Error (" + code.ToString() + "): " + message, transform, 10f);
    }
    public void OnTCPSClose(UnityTCPServer server)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[TCPServer] Unexpectedly disconnected.", transform, 10f);
        i_state.color = Color.red;
        t_clients.text = "Clients: 0";
    }

    // Events assigned in editor to UnityTCPServer (Connection events):
    public void OnTCPMessage(byte[] message, TCPConnection connection)
    {
        // Get the content up to char 35 (#):
        int msgLen = 0;
        for (int i = 0; i < message.Length; i++)
        {
            if (message[i] == '#')
            {
                msgLen = i;         // '#' is excluded.
                break;
            }
        }
        if (msgLen > 0)
        {
            // Stress test protocol:
            byte[] msg = new byte[msgLen];
            System.Buffer.BlockCopy(message, 0, msg, 0, msgLen);
            string[] fields = connection.ByteArrayToString(msg).Split(';');
            switch (fields[0])
            {
                case "PING":
                    // Send the PONG message back to remoteIP:
                    string pong = "PONG;" + fields[1] + ";" + fields[2] + ";" + NTP_RealTime.GetUTCTime().TimeOfDay.TotalMilliseconds + "#";
                    connection.SendData(pong);
                    break;
            }
        }
        else
        {
            // Shows received messages on top of the screen and disappears automatically after 10 seconds:
            GameObject popup = Instantiate(popupPrefab);
            popup.GetComponent<PopUp>().SetMessage("[TCP_Server received] " + connection.ByteArrayToString(message), transform, 10f);
        }
    }
    public void OnTCPError(int code, string message, TCPConnection connection)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[TCPServer.TCPConnection] Error (" + code.ToString() + " - " + connection.GetIP() + "): " + message, transform, 10f);
        t_clients.text = "Clients: " + _tcpServer.GetConnectionsCount().ToString();
    }
    public void OnTCPClose(TCPConnection connection)
    {
        t_clients.text = "Clients: " + _tcpServer.GetConnectionsCount().ToString();
    }
}
