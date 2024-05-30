using UnityEngine;
using UnityEngine.UI;

public class WS_test : MonoBehaviour
{
    UnityWSConnection _ws;
    // Message PopUp (Set in editor):
    public GameObject popupPrefab;
    // Graphic UI objects:
    Text t_localIP;
    Image i_state;
    InputField if_ip;
    InputField if_data;

    // Use this for initialization
    void Start ()
    {
        _ws = GetComponent<UnityWSConnection>();
        // UI objects:
        t_localIP = transform.Find("PanelSetup").Find("LabelLocalIP").Find("Text").GetComponent<Text>();
        i_state = transform.Find("PanelSetup").Find("ImageState").GetComponent<Image>();
        if_ip = transform.Find("PanelData").Find("InputFieldIp").GetComponent<InputField>();
        if_data = transform.Find("PanelData").Find("InputFieldData").GetComponent<InputField>();
        if_data.text = "WebSocket Message";
        GetDefaultURL();
    }

    // Send:
    public void Send()
    {
        _ws.SendData(if_data.text);
    }

    // Set new connection settings:
    public void Setup()
    {
        _ws._serverURL = if_ip.text;
        _ws.Setup();
        // Setup forces the disconnection:
        i_state.color = Color.red;
        t_localIP.text = "";
    }

    // Connect and start:
    public void Connect()
    {
        Setup();
        _ws.Connect();
    }
    public void Disconnect()
    {
        _ws.Disconnect();
        i_state.color = Color.red;
        t_localIP.text = "";
    }

    // Gets the example default URL:
    public void GetDefaultURL()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            if_ip.text = "ws://217.76.155.231:60003/ws/";
        else
        {
            string defaultAddress = _ws.GetDefaultIPAddress();
            if (defaultAddress.Contains(":"))
                defaultAddress = "[" + defaultAddress + "]";
            if_ip.text = "ws://" + defaultAddress + ":60003/ws/";
        }
    }

    // Events assigned in editor to UnityUDPConnection:
    public void OnWSOpen(UnityWSConnection connection)
    {
        i_state.color = Color.green;
        t_localIP.text = _ws.GetURL();
    }
    public void OnWSMessage(byte[] message, UnityWSConnection connection)
    {
        // Shows received messages on top of the screen and disappears automatically after 10 seconds:
        string msg = connection.ByteArrayToString(message);
        // Filter "Stress test" protocol":
        if (!msg.StartsWith("PING") && !msg.StartsWith("PONG") && !msg.StartsWith("DATA"))
        {
            print("[WS_test].OnWSMessage Length: " + message.Length);
            GameObject popup = Instantiate(popupPrefab);
            popup.GetComponent<PopUp>().SetMessage("[WS received] " + msg, transform, 10f);
        }
    }
    public void OnWSError(int code, string message, UnityWSConnection connection)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[WS_test] Error (" + code.ToString() + "): " + message, transform, 10f);
    }
    public void OnWSClose(UnityWSConnection connection)
    {
        i_state.color = Color.red;
        t_localIP.text = "";
    }
}
