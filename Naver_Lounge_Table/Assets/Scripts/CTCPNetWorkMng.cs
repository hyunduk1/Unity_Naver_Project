using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System;
namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CTCPNetWorkMng : MonoBehaviour
    {
        private static CTCPNetWorkMng _instance;
        public static CTCPNetWorkMng Instance { get { return _instance; } }
        private bool m_bIsConnected = false;
        private UnityTCPConnection m_TCPClient;
        private Queue<string> m_MsgQuee;


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
            m_MsgQuee = new Queue<string>();
            InvokeRepeating("ReconnectCheck", 60.0f, 1.0f);
        }

        // Use this for initialization
        void Start()
        {
            m_TCPClient = GetComponent<UnityTCPConnection>();
            StartCoroutine("PacketGenerator");
        }
        public void ReconnectCheck()
        {

            if (m_TCPClient.IsConnected() == false)
            {
                m_TCPClient.Setup();
                m_TCPClient.Connect();
            }
        }
        public void OnTCPOpen(UnityTCPConnection connection)
        {
            m_bIsConnected = true;
            Debug.Log(m_TCPClient.GetIP());
            SendingID();
        }
        public void OnTCPMessage(byte[] message, UnityTCPConnection connection)
        {
            int msgLen = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == '}')
                {
                    msgLen = i + 1;
                    break;
                }
            }

            if (msgLen > 0)
            {
                byte[] msg = new byte[msgLen];
                System.Buffer.BlockCopy(message, 0, msg, 0, msgLen);
                m_MsgQuee.Enqueue(connection.ByteArrayToString(msg));
            }
        }
        public void OnTCPError(int code, string message, UnityTCPConnection connection)
        {
            Debug.Log("[TCP_test] Error (" + code.ToString() + "): " + message);
        }
        public void OnTCPClose(UnityTCPConnection connection)
        {
            m_bIsConnected = false;
        }

        public void Connect()
        {

            m_TCPClient._remoteIP = "10.73.20.122";
            m_TCPClient._remotePort = 3840;
            m_TCPClient.Setup();
            m_bIsConnected = false;
            m_TCPClient.Connect();

        }

        public void Disconnect()
        {
            m_TCPClient.Disconnect();
            m_bIsConnected = false;
        }

        // Send:
        public void SendData(string strData)
        {
            m_TCPClient.SendData(strData);
        }


        private IEnumerator PacketGenerator()
        {
            WaitForSeconds waitSec = new WaitForSeconds(0.001f);
            while (true)
            {
                if (m_MsgQuee.Count > 0)
                {
                    PacketParser(m_MsgQuee.Dequeue());
                }
                yield return waitSec;
            }
        }


        private void Update()
        {
          
        }

        public void SendDataPackt(PROTOCOL strPacket)
        {
            PacketParser(GetPacketProtocol("id", (int)strPacket));
        }
        public void PacketParser(string strPacket)
        {
            try
            {
                JsonData jData = JsonMapper.ToObject(strPacket);

                switch ((PROTOCOL)int.Parse(jData["id"].ToString()))
                {
                    /// <summary>
                    /// MAIN PAGE , SETTING PAGE
                    /// </summary>
                    case PROTOCOL.MSG_SETTING_POPUP: break;
                    case PROTOCOL.MSG_TOUR_START: break;
                    case PROTOCOL.MSG_LANGUAGE_ENGLISH_CONTROL:
                        Debug.Log("[EN --- MODE]");
                        CUIPanelMng.Instance.m_bIsLanguage = false;

                        break;

                        //----�ѱ�
                    case PROTOCOL.MSG_LANGUAGE_TOGGLE_EN_KR_CONTROL:
                        Debug.Log("[KR --- MODE]");
                        CUIPanelMng.Instance.m_bIsLanguage = true;
                        break;  


                    /// <summary>
                    /// LAUNG TABLE PAGE
                    /// </summary>
                    case PROTOCOL.MSG_LAUNG_TABLE_AUTO_START:
                        CUIPanelMng.Instance.m_bMenualMode = false;
                        Invoke("DelayAuto", 1.0f);
                        Debug.Log("[������]");
                        break;

                    case PROTOCOL.MSG_LAUNG_TABLE_MANUAL_START:
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        Debug.Log("[�޴�����]");
                        
                        break;

                    case PROTOCOL.MSG_LAUNG_DATA_CENTER_SELECT:
                        CUIPanelMng.Instance.m_nCurrentNum = 1;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;

                    case PROTOCOL.MSG_LAUNG_DATA_CENTER_SITE:
                        CUIPanelMng.Instance.m_nCurrentNum = 2;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_SOUTH_NORTH_SITE:
                        CUIPanelMng.Instance.m_nCurrentNum = 3;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_GROUND_STABILITY:
                        CUIPanelMng.Instance.m_nCurrentNum = 4;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_SELSMIC_DESIGN:
                        CUIPanelMng.Instance.m_nCurrentNum = 5;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_FIRE_FIGHTING:
                        CUIPanelMng.Instance.m_nCurrentNum = 6;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_ELECTRICITY_SUPPLY:
                        CUIPanelMng.Instance.m_nCurrentNum = 7;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_WIND_DIRECTION:
                        CUIPanelMng.Instance.m_nCurrentNum = 8;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_WASTE_HEAT_UTIILZATION:
                        CUIPanelMng.Instance.m_nCurrentNum = 9;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_COOLING_TOWER:
                        CUIPanelMng.Instance.m_nCurrentNum = 10;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_SOLAR_GEOTHERMAL_HEAT:
                        CUIPanelMng.Instance.m_nCurrentNum = 11;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_RENEWABLE_ENERGY:
                        CUIPanelMng.Instance.m_nCurrentNum = 12;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_RAINWATER_HEAVYWATER:
                        CUIPanelMng.Instance.m_nCurrentNum = 13;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_LEED:
                        CUIPanelMng.Instance.m_nCurrentNum = 14;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;
                    case PROTOCOL.MSG_LAUNG_OUTRO:
                        CUIPanelMng.Instance.m_nCurrentNum = 15;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;

                    //----------------------------------------------------------
                    case PROTOCOL.MSG_LAUNG_EXIT:
                        CUIPanelMng.Instance.m_nCurrentNum = 0;
                        CUIPanelMng.Instance.m_bMenualMode = true;
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                        break;


                    case PROTOCOL.MSG_LAUNG_PLAY_STOP:
                        switch(int.Parse(jData["value"].ToString()))
                        {
                            case 0: //stop
                                CUIPanelMng.Instance.PauseRecive(true);
                                break;
                            case 1: //play
                                CUIPanelMng.Instance.PauseRecive(false);
                                break;
                        }
                        
                        break;
                    case PROTOCOL.MSG_LAUNG_NEXT_PAGE:
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum); 
                        break;
                        

                    default: break;
                }

            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

        }
        void DelayAuto()
        {
            CUIPanelMng.Instance.m_bMenualMode = false;
        }
        //ȣ�� �Ǵ� ���� public void OnTCPOpen(UnityTCPConnection connection) �Լ� �ǾƷ��� �Ʒ��Լ� ȣ��
        public void SendingID() //
        {
            //�����
            SendData(GetPacketProtocol("id", (int)PROTOCOL.MSG_LOUNGE_PC_ID, "value", (int)CONTENTS_ID.PC_LAUNG_TABLE_CONTENTS));
        }
        public void ServerSendID() //
        {
            //�����
            SendData(GetPacketProtocol("id", (int)PROTOCOL.MSG_LOUNGE_PC_ID, "value", (int)CONTENTS_ID.PC_LAUNG_TABLE_CONTENTS));
        }

        //4. JSON �������� �ٲپ��ִ� �Լ� 2���߰�
        public string GetPacketProtocol(string strValueName, int nValue)
        {
            string tempString;

            tempString = "{" +
                "\"" +
                strValueName +
                "\"" +
                ":" +
                nValue.ToString() +
                "}";
            Debug.Log(tempString);
            return tempString;
        }
        public string GetPacketProtocol(string strValueName, int nValue, string strValueName2, int nValue2)
        {
            string tempString;

            tempString = "{" +
                "\"" +
                strValueName +
                "\"" +
                ":" +
                nValue.ToString() +
                "," +
                  "\"" +
                strValueName2 +
                "\"" +
                ":" +
                nValue2.ToString() +
                "}";
            Debug.Log(tempString);
            return tempString;
        }


        //5. ���� ���� ��ȣ �����ֱ�
        /*
        MSG_SET_CONTROL_STATE               =700,
        MSG_SET_NAMU_STATE                  =701,
        MSG_SET_LAUNG_STATE                 =702,
        MSG_SET_ROBOT_STATE                 =703
        */
        /// <summary>
        /// ���� 9990�� ���� �ε��� ��ȣ�̴� ������ ó���ź��� 0���� �ϸ� �ɰ� �̴�.
        /// -idle �� 0 �������(������) ó�� ��ư���� 1������ �ϸ� �ȴ�.
        /// </summary>  
        public void SendingContents(int nIndex)
        {
            //����  nIndex�� ���� ��ȣ�̴� ���������� 0�� 
            SendData(GetPacketProtocol("id", (int)PROTOCOL.MSG_SET_LAUNG_STATE, "value", nIndex));

        }
    }
}
