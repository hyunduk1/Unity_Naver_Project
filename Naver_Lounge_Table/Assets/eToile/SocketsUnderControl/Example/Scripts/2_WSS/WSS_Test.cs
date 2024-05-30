using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WSS_Test : MonoBehaviour
{
    UnityWSServer _wsServer;
    // Message PopUp (Set in editor):
    public GameObject popupPrefab;
    // Graphic UI objects:
    Text t_localAddress;
    Image i_state;
    Text t_clients;
    InputField if_data;
    // Use this for initialization
    void Start ()
    {
		_wsServer = GetComponent<UnityWSServer>();
        // UI objects:
        t_localAddress = transform.Find("PanelSetup").Find("LabelLocalAddress").Find("Text").GetComponent<Text>();
        t_localAddress.text = _wsServer.GetURL() + System.Environment.NewLine + _wsServer.GetURL(true);
        i_state = transform.Find("PanelSetup").Find("ImageState").GetComponent<Image>();
        t_clients = transform.Find("PanelData").Find("LabelClients").Find("Text").GetComponent<Text>();
        if_data = transform.Find("PanelData").Find("InputFieldData").GetComponent<InputField>();
        if_data.text = "WebSocket Server Message";
        Setup();
    }

    // Set server local bound settings:
    public void Setup()
    {
        _wsServer.Setup();
        // Setup forces the disconnection:
        i_state.color = Color.red;
    }
    // Connect and start the server:
    public void Connect()
    {
        Setup();
        _wsServer.Connect();
    }
    // Disconnect the server:
    public void Disconnect()
    {
        _wsServer.Disconnect();
        i_state.color = Color.red;
        t_clients.text = "Clients: 0";
    }
    // Send:
    public void Send()
    {
        _wsServer.Distribute(if_data.text);
    }

    // Events assigned in editor to UnityWSServer (Server events):
    public void OnWSSOpen(UnityWSServer server)
    {
        i_state.color = Color.green;
    }
    public void OnWSSNewConnection(WSConnection connection, UnityWSServer server)
    {
        t_clients.text = "Clients: " + _wsServer.GetConnectionsCount().ToString();
    }
    public void OnWSSError(int code, string message, UnityWSServer server)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[WSServer] Error (" + code.ToString() + "): " + message, transform, 10f);
    }
    public void OnWSSClose(UnityWSServer server)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[WSServer] Unexpectedly disconnected.", transform, 10f);
        i_state.color = Color.red;
        t_clients.text = "Clients: 0";
    }

    // Events assigned in editor to UnityWSServer (Connection events):
    public void OnWSMessage(byte[] message, WSConnection connection)
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
            popup.GetComponent<PopUp>().SetMessage("[WS_Server received] " + connection.ByteArrayToString(message), transform, 10f);
        }
    }
    public void OnWSError(int code, string message, WSConnection connection)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[WSServer.WSConnection] Error (" + code.ToString() + " - " + connection.GetURL() + "): " + message, transform, 10f);
        t_clients.text = "Clients: " + _wsServer.GetConnectionsCount().ToString();
    }
    public void OnWSClose(WSConnection connection)
    {
        t_clients.text = "Clients: " + _wsServer.GetConnectionsCount().ToString();
    }
}
