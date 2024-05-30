using UnityEngine;
using UnityEngine.UI;

public class NTP_Test : MonoBehaviour
{
    // NTP client:
    InputField _ntpServer;
    InputField _ntpPort;
    Text _text;
    RectTransform _imageTransform;

    // UDP repeater:
    InputField _udpAddress;
    InputField _udpPort;
    Image _udpStatus;
    Toggle _udpEmu;

    // TCP repeater:
    InputField _tcpAddress;
    InputField _tcpPort;
    Image _tcpStatus;
    Toggle _tcpEmu;

    // WS repeater:
    InputField _wsAddress;
    Image _wsStatus;
    Toggle _wsEmu;

    void Start()
    {
        // NTP client status:
        _text = transform.Find("NTP_Client").Find("Text_Time").GetComponent<Text>();
        _imageTransform = transform.Find("NTP_Client").Find("Image_Time").GetComponent<RectTransform>();
        _ntpServer = transform.Find("NTP_Client").Find("InputField_Server").GetComponent<InputField>();
        _ntpPort = transform.Find("NTP_Client").Find("InputField_Port").GetComponent<InputField>();
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            _ntpServer.text = "ws://217.76.155.231:60012/ntp/";
        }
        else
        {
            _ntpServer.text = "time.windows.com";
            _ntpPort.text = "123";
        }
        // Repeaters:
        Transform repeaters = transform.Find("NTP_Repeaters");
        // UDP repeater:
        _udpAddress = repeaters.Find("UDP_Repeater").Find("InputField_Address").GetComponent<InputField>();
        _udpPort = repeaters.Find("UDP_Repeater").Find("InputField_Port").GetComponent<InputField>();
        _udpPort.text = "60010";
        _udpStatus = repeaters.Find("UDP_Repeater").Find("Image_Active").GetComponent<Image>();
        _udpEmu = repeaters.Find("UDP_Repeater").Find("Toggle_Emulation").GetComponent<Toggle>();
        // TCP repeater:
        _tcpAddress = repeaters.Find("TCP_Repeater").Find("InputField_Address").GetComponent<InputField>();
        _tcpPort = repeaters.Find("TCP_Repeater").Find("InputField_Port").GetComponent<InputField>();
        _tcpPort.text = "60011";
        _tcpStatus = repeaters.Find("TCP_Repeater").Find("Image_Active").GetComponent<Image>();
        _tcpEmu = repeaters.Find("TCP_Repeater").Find("Toggle_Emulation").GetComponent<Toggle>();
        // WS repeater:
        _wsAddress = repeaters.Find("WS_Repeater").Find("InputField_Address").GetComponent<InputField>();
        _wsStatus = repeaters.Find("WS_Repeater").Find("Image_Active").GetComponent<Image>();
        _wsAddress.text = "http://" + NTP_RealTime.GetDefaultIPAddress() + ":60012/ws/";
        _wsEmu = repeaters.Find("WS_Repeater").Find("Toggle_Emulation").GetComponent<Toggle>();
    }
    void Update()
    {
        // Show the current time:
        _text.text = "Local time: " + NTP_RealTime._now.ToString("HH:mm:ss.fff") + " - (" + NTP_RealTime._requestState.ToString() + " " + NTP_RealTime._errorMsg + ")";
        _imageTransform.eulerAngles = new Vector3(0f, 0f, 360f * (NTP_RealTime._now.Millisecond / 1000f));
        // Status of all repeaters:
        if (NTP_RealTime._udpRepeater != null)
        {
            _udpStatus.color = NTP_RealTime._udpRepeater.IsConnected() ? Color.green : Color.red;
            _udpAddress.text = NTP_RealTime._udpRepeater.GetIP();
        }
        if (NTP_RealTime._tcpRepeater != null)
        {
            _tcpStatus.color = NTP_RealTime._tcpRepeater.IsConnected() ? Color.green : Color.red;
            _tcpAddress.text = NTP_RealTime._tcpRepeater.GetIP();
        }
        if (NTP_RealTime._wsRepeater != null)
        {
            _wsStatus.color = NTP_RealTime._wsRepeater.IsConnected() ? Color.green : Color.red;
            _wsAddress.text = NTP_RealTime._wsRepeater.GetURL();
        }
    }

    // UI event for request synchronisation in real time:
    public void RequestUDPSync()
    {
        int port = string.IsNullOrEmpty(_ntpPort.text) ? 0 : int.Parse(_ntpPort.text);
        NTP_RealTime.SendUDPRequest(_ntpServer.text, port);
    }
    public void RequestTCPSync()
    {
        NTP_RealTime.SendTCPRequest(_ntpServer.text, int.Parse(_ntpPort.text));
    }
    public void RequestWSSync()
    {
        NTP_RealTime.SendWSRequest(_ntpServer.text);
    }

    // UI events for UDP repeater:
    public void StartUDPRepeater()
    {
        NTP_RealTime.StartUDPRepeater(int.Parse(_udpPort.text), _udpAddress.text, _udpEmu.isOn);
    }
    public void StopUDPRepeater()
    {
        NTP_RealTime.StopUDPRepeater();
        _udpStatus.color = Color.red;
    }

    // UI events for TCP repeater:
    public void StartTCPRepeater()
    {
        NTP_RealTime.StartTCPRepeater(int.Parse(_tcpPort.text), _tcpAddress.text, _tcpEmu.isOn);
    }
    public void StopTCPRepeater()
    {
        NTP_RealTime.StopTCPRepeater();
        _tcpStatus.color = Color.red;
    }

    // UI events for WS repeater:
    public void StartWSRepeater()
    {
        NTP_RealTime.StartWSRepeater(_wsAddress.text, _wsEmu.isOn);
    }
    public void StopWSRepeater()
    {
        NTP_RealTime.StopWSRepeater();
        _wsStatus.color = Color.red;
    }

    // Close al connections when loading a new scene or exit the application:
    private void OnApplicationQuit()
    {
        NTP_RealTime.Dispose();
    }
    private void OnDestroy()
    {
        NTP_RealTime.Dispose();
    }
}
