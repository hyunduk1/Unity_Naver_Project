using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System;

namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CPlugInNetWorkMng : MonoBehaviour
    {
        private static CPlugInNetWorkMng _instance;
        public static CPlugInNetWorkMng Instance { get { return _instance; } }

        private UnityUDPConnection m_UdpConnection;

        public string m_strlocalIp = "127.0.0.1";
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
            m_UdpConnection._localIP = CConfigMng.Instance._strReceiveIp;
            m_UdpConnection._localPort = CConfigMng.Instance._nReceivePort;
            m_UdpConnection.Setup();
            m_UdpConnection.Connect();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
            }
        }
        public void Send(PROTOCOL protocol)
        {
            string SendString = "";
            switch (protocol)
            {
                /*                case PROTOCOL.MSG_NEXT_VIDEO:   SendString = "{ \"ID\":0 }"; break;
                                case PROTOCOL.MSG_IDLE_VIDEO:   SendString = "{ \"ID\":1 }"; break;
                                case PROTOCOL.MSG_VIDEO_00:     SendString = "{ \"ID\":2 }"; break;
                                case PROTOCOL.MSG_VIDEO_01:     SendString = "{ \"ID\":3 }"; break;
                                case PROTOCOL.MSG_VIDEO_02:     SendString = "{ \"ID\":4 }"; break;
                                case PROTOCOL.MSG_VIDEO_03:     SendString = "{ \"ID\":5 }"; break;
                                case PROTOCOL.MSG_VIDEO_04:     SendString = "{ \"ID\":6 }"; break;
                                case PROTOCOL.MSG_VIDEO_05:     SendString = "{ \"ID\":7 }"; break;
                                case PROTOCOL.MSG_VIDEO_06:     SendString = "{ \"ID\":8 }"; break;
                                case PROTOCOL.MSG_VIDEO_07:     SendString = "{ \"ID\":9 }"; break;
                                case PROTOCOL.MSG_VIDEO_08:     SendString = "{ \"ID\":10 }"; break;
                                case PROTOCOL.MSG_VIDEO_09:     SendString = "{ \"ID\":11 }"; break;
                                case PROTOCOL.MSG_VIDEO_10:     SendString = "{ \"ID\":12 }"; break;
                                case PROTOCOL.MSG_VIDEO_11:     SendString = "{ \"ID\":13 }"; break;
                                case PROTOCOL.MSG_VIDEO_12:     SendString = "{ \"ID\":14 }"; break;
                                case PROTOCOL.MSG_VIDEO_13:     SendString = "{ \"ID\":15 }"; break;
                                case PROTOCOL.MSG_VIDEO_14:     SendString = "{ \"ID\":16 }"; break;
                                case PROTOCOL.MSG_VIDEO_15:     SendString = "{ \"ID\":17 }"; break;
                                case PROTOCOL.MSG_VIDEO_16:     SendString = "{ \"ID\":18 }"; break;
                                case PROTOCOL.MSG_VIDEO_17:     SendString = "{ \"ID\":19 }"; break;
                                case PROTOCOL.MSG_VIDEO_18:     SendString = "{ \"ID\":20 }"; break;*/
            }
            m_UdpConnection.SendData(CConfigMng.Instance._strSendIp, SendString, CConfigMng.Instance._nSendPort);
        }

        private void PacketPerser(string Message)
        {
            try
            {
                JsonData jData = JsonMapper.ToObject(Message);
                Debug.Log("Receive :   " + Message + " -> " + (PROTOCOL)int.Parse(jData["ID"].ToString()));
                switch ((PROTOCOL)int.Parse(jData["id"].ToString()))
                {

                    case PROTOCOL.MSG_LANGUAGE_TOGGLE_EN_KR: //언어선택
                        switch (int.Parse(jData["value"].ToString()))
                        {
                            case 1:
                                CUIPanelMng.Instance.m_bIsLanguage = true;
                                Debug.Log("한국어");
                                break;
                            case 2:
                                CUIPanelMng.Instance.m_bIsLanguage = false;
                                Debug.Log("영어");
                                break;
                            default: break;
                        }

                        break;
                    case PROTOCOL.MSG_LAUNG_TABLE_AUTO_START: // 오토
                        CUIPanelMng.Instance.m_bMenualMode = false;
                        break;

                    case PROTOCOL.MSG_LAUNG_TABLE_MANUAL_START: // 메뉴얼
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        break;

                    case PROTOCOL.MSG_LAUNG_DATA_CENTER_SITE: // 섹션 1 ~
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 1;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_SOUTH_NORTH_SITE:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 2;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_GROUND_STABILITY:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 3;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_SELSMIC_DESIGN:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 4;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_FIRE_FIGHTING:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 5;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_ELECTRICITY_SUPPLY:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 6;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_WIND_DIRECTION:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 7;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_WASTE_HEAT_UTIILZATION:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 8;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_COOLING_TOWER:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 9;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_SOLAR_GEOTHERMAL_HEAT:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 10;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;

                    case PROTOCOL.MSG_LAUNG_RENEWABLE_ENERGY:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 11;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;
                    case PROTOCOL.MSG_LAUNG_RAINWATER_HEAVYWATER:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 12;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;
                    case PROTOCOL.MSG_LAUNG_LEED:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 13;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;
                    case PROTOCOL.MSG_LAUNG_OUTRO:
                        CUIPanelMng.Instance.m_nCurrentCountVideo = 14;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentCountVideo);
                        break;
                    case PROTOCOL.MSG_LAUNG_EXIT:

                        break;
                    case PROTOCOL.MSG_LAUNG_PLAY_STOP:

                        break;
                    case PROTOCOL.MSG_LAUNG_NEXT_PAGE:

                        break;

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