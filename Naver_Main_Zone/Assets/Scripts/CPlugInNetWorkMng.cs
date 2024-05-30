using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System;

namespace DemolitionStudios.DemolitionMedia
{
    public class CPlugInNetWorkMng : MonoBehaviour
    {
        private static CPlugInNetWorkMng _instance;
        public static CPlugInNetWorkMng Instance { get { return _instance; } }

        private UnityUDPConnection m_UdpConnection;

        public string m_strlocalIp = "127.0.0.1";
        public short m_nCurrentCap = 0;
        // Start is called before the first frame update
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            m_UdpConnection = gameObject.GetComponent<UnityUDPConnection>();
        }
        void Start()
        {
            m_UdpConnection._localIP = CConfigMng.Instance._strUdpRececiveIp;
            m_UdpConnection._localPort = CConfigMng.Instance._nReceivePort;
            m_UdpConnection.Setup();
            m_UdpConnection.Connect();
        }

        // Update is called once per frame
        void Update()
        {
        }
        public void Send(PROTOCOL protocol, bool bIsBrodCasting = true)
        {
            string SendString = "";
            switch (protocol)
            {
                case PROTOCOL.MSG_CLIENT_CAPTURE: SendString = "{ \"ID\":0 }"; break;
                case PROTOCOL.MSG_CLIENT_DESTROY_CAPTURE: SendString = "{ \"ID\":1 }"; break;
                case PROTOCOL.MSG_CLIENT_PAUSE_CAPTURE: SendString = "{ \"ID\":2 }"; break;
                case PROTOCOL.MSG_VIDEO_00: SendString = "{ \"ID\":3 }"; break;
                case PROTOCOL.MSG_VIDEO_01: SendString = "{ \"ID\":4 }"; break;
                case PROTOCOL.MSG_VIDEO_02: SendString = "{ \"ID\":5 }"; break;
                case PROTOCOL.MSG_VIDEO_03: SendString = "{ \"ID\":6 }"; break;
                case PROTOCOL.MSG_VIDEO_04: SendString = "{ \"ID\":7 }"; break;
                case PROTOCOL.MSG_VIDEO_05: SendString = "{ \"ID\":8 }"; break;
                case PROTOCOL.MSG_VIDEO_06: SendString = "{ \"ID\":9 }"; break;
                case PROTOCOL.MSG_VIDEO_07: SendString = "{ \"ID\":10 }"; break;
                case PROTOCOL.MSG_VIDEO_08: SendString = "{ \"ID\":11 }"; break;
                case PROTOCOL.MSG_VIDEO_09: SendString = "{ \"ID\":12 }"; break;
                case PROTOCOL.MSG_EN:       SendString = "{ \"ID\":20 }"; break;
                case PROTOCOL.MSG_KR:       SendString = "{ \"ID\":21 }"; break;
                case PROTOCOL.MSG_NUM: SendString = "{ \"CAPTURE\":"+ CUIPanelMng.Instance.m_nCurrentVideo.ToString() +" }"; break;

            }
            Debug.Log(SendString);
            if(bIsBrodCasting == true)
                m_UdpConnection.SendData(CConfigMng.Instance._StrSendIP, SendString, CConfigMng.Instance._nMediaServerPort);
            else
                m_UdpConnection.SendData(CConfigMng.Instance._StrSendIP, SendString, CConfigMng.Instance._nMediaServerPort);

        }
        private void ValuePacketPerser(string Message)
        {
            try
            {
                JsonData jData = JsonMapper.ToObject(Message);
                CUIPanelMng.Instance.m_nCurrentVideo = short.Parse(jData["CAPTURE"].ToString());
                Debug.Log("UDP로 받음 캡쳐 넘버 : " + jData["CAPTURE"].ToString());
            }
            catch(Exception e)
            {
                Debug.Log(e);
            }
        }


        //클라이언트 끼리 브로드 캐스팅을 위한 패킷
        private void PacketPerser(string Message)
        {
            try
            {
                JsonData jData = JsonMapper.ToObject(Message);
                Debug.Log("Receive :   " + Message + " -> " + (PROTOCOL)int.Parse(jData["ID"].ToString()));
                switch ((PROTOCOL)int.Parse(jData["ID"].ToString()))
                {

                    /// <summary>
                    /// SERVER, CLIENT BrodCasting----------------------------------------------
                    /// </summary>
                    /// 

                    case PROTOCOL.MSG_CLIENT_CAPTURE:
                        if (m_nCurrentCap > CConfigMng.Instance._nVideoNum)
                            m_nCurrentCap = 0;
                        CUIPanelMng.Instance.CaptureVideo(true);
                        m_nCurrentCap++;
                        Debug.Log("-------------- 마지막 이미지 캡쳐------------");
                        break;

                    case PROTOCOL.MSG_CLIENT_DESTROY_CAPTURE:
                        CUIPanelMng.Instance.m_bClientPause = false;
                        CUIPanelMng.Instance.ClientDestroyCapture();
                        
                        break;
                        
                    case PROTOCOL.MSG_CLIENT_PAUSE_CAPTURE:
                        CUIPanelMng.Instance.m_bClientPause = true;
                        CUIPanelMng.Instance.PauseScreenShotCapture();
                        
                        break;

                    case PROTOCOL.MSG_VIDEO_00:
                        m_nCurrentCap = 0;
                        CUIPanelMng.Instance.InsertMovie(0);
                        CTCPNetWorkMng.Instance.SendingContents(1);
                        break;
                    case PROTOCOL.MSG_VIDEO_01:
                        m_nCurrentCap = 1;
                        CUIPanelMng.Instance.InsertMovie(1);
                        CTCPNetWorkMng.Instance.SendingContents(2);
                        break;
                    case PROTOCOL.MSG_VIDEO_02:
                        m_nCurrentCap = 2;
                        CUIPanelMng.Instance.InsertMovie(2);
                        CTCPNetWorkMng.Instance.SendingContents(3);
                        break;
                    case PROTOCOL.MSG_VIDEO_03:
                        m_nCurrentCap = 3;
                        CUIPanelMng.Instance.InsertMovie(3);
                        CTCPNetWorkMng.Instance.SendingContents(4);
                        break;
                    case PROTOCOL.MSG_VIDEO_04:
                        m_nCurrentCap = 4;
                        CUIPanelMng.Instance.InsertMovie(4);
                        CTCPNetWorkMng.Instance.SendingContents(5);

                        break;
                    case PROTOCOL.MSG_VIDEO_05:
                        m_nCurrentCap = 5;
                        CUIPanelMng.Instance.InsertMovie(5);
                        CTCPNetWorkMng.Instance.SendingContents(6);

                        break;
                    case PROTOCOL.MSG_VIDEO_06:
                        m_nCurrentCap = 6;
                        CUIPanelMng.Instance.InsertMovie(6);
                        CTCPNetWorkMng.Instance.SendingContents(7);
                        break;
                    case PROTOCOL.MSG_VIDEO_07:
                        m_nCurrentCap = 7;
                        CUIPanelMng.Instance.InsertMovie(7);
                        CTCPNetWorkMng.Instance.SendingContents(8);
                        break;
                    case PROTOCOL.MSG_VIDEO_08:
                        m_nCurrentCap = 8;
                        CUIPanelMng.Instance.InsertMovie(8);
                        CTCPNetWorkMng.Instance.SendingContents(9);
                        break;
                    case PROTOCOL.MSG_VIDEO_09:
                        m_nCurrentCap = 9;
                        CUIPanelMng.Instance.InsertMovie(9);
                        CTCPNetWorkMng.Instance.SendingContents(10);
                        break;

                    case PROTOCOL.MSG_EN:
                        CUIPanelMng.Instance.LangeModeEN();
                        break;
                    case PROTOCOL.MSG_KR:
                        CUIPanelMng.Instance.LangeModeKR();
                        break;
                    //-------------------------------------------------------------------------------



                    default: break;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        //--------------------------------------------------------------------------------------------------------
        #region netWorkSet

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
                PacketPerser(connection.ByteArrayToString(message));
                ValuePacketPerser(connection.ByteArrayToString(message));
            
            }
        }
        public void OnUDPError(int code, string message, UnityUDPConnection connection)
        {
            Debug.Log("네트워크 에러 : " + code.ToString() + " / " + message);
        }
        public void OnUDPOpen(UnityUDPConnection connection)
        {
            //Debug.Log("Send Protocol : ");
        }
        public void OnUDPClose(UnityUDPConnection connection)
        {
            if (m_UdpConnection.IsConnected())                                                                                                                                                                                                                                                                                                                                               
                Debug.Log("네트워크 생성");
            else
                Debug.Log("네트워크 삭제");
        }
        #endregion
    }
}
