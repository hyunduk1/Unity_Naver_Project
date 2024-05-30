using UnityEngine;
using UnityEngine.UI;

public class UDP_test : MonoBehaviour
{
    UnityUDPConnection _udp;
    // Message PopUp (Set in editor):
    public GameObject popupPrefab;
    // Graphic UI objects:
    Text t_localIP;
    Image i_state;
    InputField if_port;
    InputField if_ip;
    InputField if_data;

    // Use this for initialization
    void Start ()
    {
        _udp = GetComponent<UnityUDPConnection>();
        print("[UDP_test] IPV4 = " + _udp.GetIP(false));
        print("[UDP_test] IPV6 = " + _udp.GetIP(false));
        // UI objects:
        t_localIP = transform.Find("PanelSetup").Find("LabelLocalIP").Find("Text").GetComponent<Text>();
        t_localIP.text = _udp.GetIP(false) + System.Environment.NewLine;
        t_localIP.text += _udp.GetIP(true);
        if_port = transform.Find("PanelSetup").Find("InputFieldPort").GetComponent<InputField>();
        if_port.text = "60001";
        i_state = transform.Find("PanelSetup").Find("ImageState").GetComponent<Image>();
        if_ip = transform.Find("PanelData").Find("InputFieldIp").GetComponent<InputField>();
        if_data = transform.Find("PanelData").Find("InputFieldData").GetComponent<InputField>();
        if_data.text = "UDP Message";
    }

    // Set new connection settings:
    public void Setup()
    {
        _udp._localPort = int.Parse(if_port.text);
        _udp.Setup();
        // Setup forces the disconnection:
        i_state.color = Color.red;
    }

    // Connect (In UDP it means "start listening"):
    public void Connect()
    {
        Setup();
        _udp.Connect();
    }

    // Disconnects the port:
    public void Disconnect()
    {
        _udp.Disconnect();
        i_state.color = Color.red;
    }

    // Send (In UDP you don't need to connect to start sending data):
    public void Send()
    {
        _udp.SendData(if_ip.text, if_data.text);
    }

    // Writes the IPv4 broadcast IP in the target IP:
    public void GetBroadcastIPv4Address()
    {
        if_ip.text = _udp.GetIPv4BroadcastAddress();
    }

    // Events assigned in editor to UnityUDPConnection:
    public void OnUDPOpen(UnityUDPConnection connection)
    {
        i_state.color = Color.green;
    }
    public void OnUDPMessage(byte[] message, string remoteIP, UnityUDPConnection connection)
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
                    connection.SendData(remoteIP, pong);
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
    public void OnUDPError(int code, string message, UnityUDPConnection connection)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[UDP_test] Error(" + code.ToString() + "): " + message, transform, 10f);
    }
    public void OnUDPClose(UnityUDPConnection connection)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[UDP_test] Connection closed unexpectedly.", transform, 10f);
        if(_udp.IsConnected())
            i_state.color = Color.green;
        else
            i_state.color = Color.red;
    }
}
