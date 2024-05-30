using UnityEngine;
using UnityEngine.UI;

/*************************************************************************************************************************
 * Ping-Pong protocol:
 * PING;ID;sendTime#fillUpToMessageSize
 * PONG;ID;sendTime#
 * 
 * PING or PONG identifies the message as origin or target.
 * ID is an incremental number to detect packet loss.
 * sendTime is the real time of the day (ms) when the message was sent (NTP synchronization is mandatory for accuracy).
 * fillUpToMessageSize unseless data to add weight to the test message.
 * 
 * Ping-Pong test:
 * Sends a PING message and awaits for the PONG message. Get calculations of what happened.
 * Sends a new PING message after a cool-down time.
 * This test can't be broadcasted.
 * 
 *************************************************************************************************************************
 * Streaming protocol:
 * DATA;ID;sendTime#fillUpToMessageSize
 * 
 * DATA identifies the message.
 * ID is an incremental number to detect packet loss in target side.
 * sendTime is the real time of the day (ms) when the message was sent (NTP synchronization is mandatory for accuracy).
 * fillUpToMessageSize unseless data to add weight to the test message.
 * 
 * Streaming test:
 * Sends PING messages each time interval.
 * Don't waits for any reply (it discards them).
 * The taget shows the statistics.
 * This test can be broadcasted.
 * 
 *************************************************************************************************************************
 * No need to set the target to perform the test, just activate the right connection.
 * 
 *************************************************************************************************************************/

public class UDP_StressTest : MonoBehaviour
{
    public GameObject popupPrefab;
    UnityUDPConnection _connection;
    // UI elements:
    Canvas _canvas;
    Dropdown _ddMode;
    InputField _ifSize;
    InputField _ifTarget;
    InputField _ifPeriod;
    Text _bStart;
    Text _tStats;
    InputField _ifPanelTarget;
    // Test parameters:
    bool _isTesting = false;
    int _msgType;
    float _time;
    uint _msgID;
    int _msgRetry;
    // Stats:
    double _delay;
    FM_Average _delayAverage;
    uint _sent;
    uint _received;
    double _rate;
    FM_Average _rateAverage;

    void Start()
    {
        _connection = transform.parent.Find("Panel").GetComponent<UnityUDPConnection>();
        _connection._onMessage.AddListener(OnMessage);
        _delayAverage = new FM_Average(100);
        _rateAverage = new FM_Average(100);
        // UI elements:
        _canvas = transform.GetComponent<Canvas>();
        _ddMode = transform.Find("Dropdown_Test").GetComponent<Dropdown>();
        _ifSize = transform.Find("InputField_Size").GetComponent<InputField>();
        _ifSize.text = "50";
        _ifTarget = transform.Find("InputField_Target").GetComponent<InputField>();
        _ifPeriod = transform.Find("InputField_Period").GetComponent<InputField>();
        _ifPeriod.text = "0.5";
        _bStart = transform.Find("Button_Start").Find("Text").GetComponent<Text>();
        _canvas.enabled = false;
        _tStats = transform.Find("Text_Stats").GetComponent<Text>();
        _ifPanelTarget = transform.parent.Find("Panel").Find("PanelData").Find("InputFieldIp").GetComponent<InputField>();
    }
    void Update()
    {
        if (_canvas.enabled)
        {
            _tStats.text = "NTPSync: " + NTP_RealTime._requestState.ToString() + " - " + NTP_RealTime._now.ToString("HH:mm:ss.fff") + System.Environment.NewLine;
            _tStats.text += "----- Stats -----" + System.Environment.NewLine;
            _tStats.text += "Sent:" + _sent + "\t Received:" + _received + "\t Missing:" + (_sent - _received) + System.Environment.NewLine;
            _tStats.text += "Delay:" + _delay.ToString("00") + "ms \t Average:" + _delayAverage._result.ToString("0.00") + "ms" + System.Environment.NewLine;
            _tStats.text += "Rate:" + _rate.ToString("000.00") + "msg/s \t Average:" + _rateAverage._result.ToString("000.00") + "msg/s" + System.Environment.NewLine;
        }
        else
        {
            _ifTarget.text = _ifPanelTarget.text;
        }
        if(Input.GetKeyDown(KeyCode.A))
        {

        }
    }

    ///<summary>Event for test purposes</summary>
    public void OnMessage(byte[] message, string remoteIP, UnityUDPConnection connection)
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
                case "DATA":
                    if (!_isTesting)
                    {
                        // DATA;ID;sendTime#fillUpToMessageSize
                        _sent = FileManagement.CustomParser<uint>(fields[1]);
                        // Update stats:
                        _received++;
                        if (_sent < _received)
                            _received = _sent;
                    }
                    break;
                case "PING":
                    if (!_isTesting)
                    {
                        // PING;ID;sendTime#fillUpToMessageSize
                        // Reply back to the remoteIP ( PONG;ID;sendTime# ):
                        string pong = "PONG;" + fields[1] + ";" + fields[2] + "#";
                        _sent = FileManagement.CustomParser<uint>(fields[1]);
                        connection.SendData(remoteIP, pong);
                        // Update stats:
                        _received++;
                        if (_sent < _received)
                            _received = _sent;
                    }
                    _received++;
                    break;
                case "PONG":
                    _msgID++;
                    _received++;
                    _msgRetry = 5;
                    double now = NTP_RealTime.GetUTCTime().TimeOfDay.TotalMilliseconds;
                    _delay = now - FileManagement.CustomParser<double>(fields[2]);
                    _delayAverage.AddSample(_delay);
                    _rate = 1000f / _delay;
                    _rateAverage.AddSample(_rate);
                    // PONG;ID;sendTime#
                    if (_isTesting)
                    {
                        CancelInvoke("Send");
                        Send();     // Send next PING immediately.
                        InvokeRepeating("Send", _time, _time);
                    }
                    break;
            }
        }
    }

    ///<summary>[UI]Start the test (send messages)</summary>
    public void StartTest()
    {
        if (_connection.IsConnected())
        {
            if (string.IsNullOrEmpty(_ifTarget.text))
            {
                GameObject popup = Instantiate(popupPrefab);
                popup.GetComponent<PopUp>().SetMessage("[UDP Stress test] Please provide a target address.", transform, 10f);
            }
            else
            {
                if (_isTesting)
                {
                    StopTest();
                }
                else
                {
                    _time = FileManagement.CustomParser<float>(_ifPeriod.text);
                    _msgID = 0;
                    _msgRetry = 5;
                    _sent = 0;
                    _received = 0;
                    _delayAverage.Clear();
                    _rateAverage.Clear();
                    _bStart.text = "Stop";
                    _isTesting = true;
                    _msgType = _ddMode.value;
                    InvokeRepeating("Send", _time, _time);
                }
            }
        }
        else
        {
            GameObject popup = Instantiate(popupPrefab);
            popup.GetComponent<PopUp>().SetMessage("[UDP Stress test] The UDP connection is closed.", transform, 10f);
        }
    }
    ///<summary>[UI]Stop the test (stop sending)</summary>
    public void StopTest()
    {
        CancelInvoke("Send");
        _isTesting = false;
        _bStart.text = "Start";
    }
    ///<summary>Sent test message</summary>
    void Send()
    {
        // <CMD>;ID;sendTime#<fillUpToMessageSize>
        byte[] cmd = { };
        int size = FileManagement.CustomParser<int>(_ifSize.text);
        // Generate the command:
        switch (_msgType)
        {
            case 0:     // PING-PONG
                cmd = _connection.StringToByteArray("PING;" + _msgID + ";" + NTP_RealTime.GetUTCTime().TimeOfDay.TotalMilliseconds + "#");
                _msgRetry--;
                if (_msgRetry <= 0)
                {
                    StopTest();
                    ShowAlert("The target device is not responding.");
                }
                break;
            case 1:     // STREAM
                _msgID++;
                cmd = _connection.StringToByteArray("DATA;" + _msgID + ";" + NTP_RealTime.GetUTCTime().TimeOfDay.TotalMilliseconds + "#");
                break;
        }
        // Convert the command in byte array message:
        if (cmd.Length > size)
        {
            // Size error:
            StopTest();
            ShowAlert("The message size is too small (current:" + size + " - minimum:" + cmd.Length + ").");
        }
        else
        {
            byte[] message = new byte[size];
            System.Buffer.BlockCopy(cmd, 0, message, 0, cmd.Length);
            for (int i = cmd.Length; i < message.Length; i++)
            {
                message[i] = (byte)'-';     // Fill with useless visible data.
            }
            _connection.SendData(_ifTarget.text, message);
            _sent++;
        }
    }
    ///<summary>[UI] Synchronizes the local time (UDP only)</summary>
    public void NTPSync()
    {
        NTP_RealTime.SendUDPRequest();
    }
    ///<summary>[UI] Shows a message on screen</summary>
    void ShowAlert(string message)
    {
        GameObject popup = Instantiate(popupPrefab);
        popup.GetComponent<PopUp>().SetMessage("[UDP Stress test] " + message, transform, 10f);
    }
}