using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System;
namespace DemolitionStudios.DemolitionMedia
{
    public class CTCPNetWorkMng : MonoBehaviour
    {
        private static CTCPNetWorkMng _instance;
        public static CTCPNetWorkMng Instance { get { return _instance; } }
        private bool m_bIsConnected = false;
        private UnityTCPConnection m_TCPClient;
        private Queue<string> m_MsgQuee;

        public bool m_bMbileIdleTrow = false;


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

        }

        // Use this for initialization
        void Start()
        {
            if (CConfigMng.Instance._bIsMediaServer == false)
            {
                transform.GetComponent<CTCPNetWorkMng>().enabled = false;
                transform.GetComponent<UnityTCPConnection>().enabled = false;
                return;
            }

            //CMS가 꺼져도 재접속하기 위한 함수
            InvokeRepeating("ReconnectCheck", 60.0f, 1.0f);
 
            m_TCPClient = GetComponent<UnityTCPConnection>();
            StartCoroutine("PacketGenerator");
            
        }
        /// <summary>
        /// 모바일 없을때 테스트용 테스트용
        /// </summary>
        /// <param name="strPacket"> 프로토콜 </param>
        public void SendDataPackt(PROTOCOL strPacket)
        {
            PacketParser(GetPacketProtocol("id", (int)strPacket));
            //PacketParser(strPacket);
        }
        public void OnTCPOpen(UnityTCPConnection connection)
        {
            if (CConfigMng.Instance._bIsMediaServer == false)
                return;
            m_bIsConnected = true;
            Debug.Log(m_TCPClient.GetIP());
            SendingID();
        }
        public void OnTCPMessage(byte[] message, UnityTCPConnection connection)
        {
            if (CConfigMng.Instance._bIsMediaServer == false)
                return;
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
            if (CConfigMng.Instance._bIsMediaServer == false)
                return;
            Debug.Log("[TCP_test] Error (" + code.ToString() + "): " + message);
        }
        public void OnTCPClose(UnityTCPConnection connection)
        {
            if (CConfigMng.Instance._bIsMediaServer == false)
                return;
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
        public void ReconnectCheck()
        {

            if (m_TCPClient.IsConnected() == false)
            {
                m_TCPClient.Setup();
                m_TCPClient.Connect();
            }
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

        //모바일로 받을때 패킷
        public void PacketParser(string strPacket)
        {
            try
            {
                if (CConfigMng.Instance._bIsMediaServer == false)
                    return;
                JsonData jData = JsonMapper.ToObject(strPacket);
                Debug.Log("---- TCP ---- Receive :   " + strPacket + " -> " + (PROTOCOL)int.Parse(jData["id"].ToString()));
                switch ((PROTOCOL)int.Parse(jData["id"].ToString()))
                {

                    case PROTOCOL.MSG_LANGUAGE_ENGLISH_CONTROL:
                        CUIPanelMng.Instance.m_bLangeMode = true;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_EN);
                        Debug.Log("[영문]");
                        break;

                    case PROTOCOL.MSG_LANGUAGE_TOGGLE_EN_KR_CONTROL:
                        CUIPanelMng.Instance.m_bLangeMode = false;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_KR);
                        Debug.Log("[한글]");
                        break;

                    /// <summary>
                    /// CONTROL CENTER PAGE
                    /// </summary>
                    case PROTOCOL.MSG_CONTROLL_AUTO_START:
                        Debug.Log("[오토모드]");
                        CUIPanelMng.Instance.m_MenualMode = false;
                        break;
                    case PROTOCOL.MSG_CONTROLL_MANUAL_START:
                        Debug.Log("[메뉴얼 모드]");
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;

                    case PROTOCOL.MSG_CONTROL_GUP_SERVER: // 302

                        if (CUIPanelMng.Instance.m_bIsIdle == true) // 이프로토콜을 받으면 idle이 트루면 시간을 앞으로 이동하고 사운드 재생
                        {
                            CUIPanelMng.Instance.m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
                            CUIPanelMng.Instance.m_objCurrentObject.GetComponentInChildren<CSound>().IdleAudioEvent();
                            CUIPanelMng.Instance.m_bIsIdle = false;
                            CUIPanelMng.Instance.m_bIsPause = false;
                           
                        }
                        else // 아이들이 false 면 씬 전환
                        {
                            
                            CUIPanelMng.Instance.m_nCurrentVideo = 0;
                            CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                            CUIPanelMng.Instance.InsertMovie(0);
                            m_bMbileIdleTrow = true;
                        }
                        
                        

                        break;
                    case PROTOCOL.MSG_CONTROL_SERVER_LAG:
                        CUIPanelMng.Instance.m_nCurrentVideo = 1;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(1);
                        CUIPanelMng.Instance.InsertMovie(1);
                        CUIPanelMng.Instance.m_MenualMode = true;

                        break;
                    case PROTOCOL.MSG_CONTROL_SERVER_ROOM:
                        CUIPanelMng.Instance.m_nCurrentVideo = 2;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(2);
                        CUIPanelMng.Instance.InsertMovie(2);
                        CUIPanelMng.Instance.m_MenualMode = true;

                        break;
                    case PROTOCOL.MSG_CONTROL_ROBOT:
                        CUIPanelMng.Instance.m_nCurrentVideo = 3;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(3);
                        CUIPanelMng.Instance.InsertMovie(3);
                        CUIPanelMng.Instance.m_MenualMode = true;

                        break;
                    case PROTOCOL.MSG_CONTROL_INFRA_STRUCTURE:
                        CUIPanelMng.Instance.m_nCurrentVideo = 4;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(4);
                        CUIPanelMng.Instance.InsertMovie(4);
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;
                    case PROTOCOL.MSG_CONTROL_POWER_REDUNDENCY:
                        CUIPanelMng.Instance.m_nCurrentVideo = 5;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(5);
                        CUIPanelMng.Instance.InsertMovie(5);
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;
                    case PROTOCOL.MSG_CONTROL_MULTIPLEXING:
                        CUIPanelMng.Instance.m_nCurrentVideo = 6;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(6);
                        CUIPanelMng.Instance.InsertMovie(6);
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;
                    case PROTOCOL.MSG_CONTROL_IDC_DUALIZATION:
                        CUIPanelMng.Instance.m_nCurrentVideo = 7;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(7);
                        CUIPanelMng.Instance.InsertMovie(7);
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;
                    case PROTOCOL.MSG_CONTROL_DOMESTIC_IDC:
                        CUIPanelMng.Instance.m_nCurrentVideo = 8;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(8);
                        CUIPanelMng.Instance.InsertMovie(8);
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;
                    case PROTOCOL.MSG_CONTROL_GLOBAL_REGION:
                        CUIPanelMng.Instance.m_nCurrentVideo = 9;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.InsertMovie(9);
                        CUIPanelMng.Instance.InsertMovie(9);
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;
                    case PROTOCOL.MSG_CONTROL_EXIT: 
                        CUIPanelMng.Instance.m_nCurrentVideo = 0;
                        CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_NUM);
                        CUIPanelMng.Instance.IdleVideoPlay();
                        CUIPanelMng.Instance.InsertMovie(0);
                        CUIPanelMng.Instance.m_bIsIdle = true;
                        CUIPanelMng.Instance.m_MenualMode = true;
                        break;

                    case PROTOCOL.MSG_CONTROL_PLAY_STOP:
                        switch (int.Parse(jData["value"].ToString()))
                        {
                            case 0: //stop
                                CUIPanelMng.Instance.NetWorkPauseEvent(true);
                                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_PAUSE_CAPTURE);
                                break;
                            case 1: //play
                                CUIPanelMng.Instance.NetWorkPauseEvent(false);
                                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_DESTROY_CAPTURE);
                                break;
                        }
                        break;
                    case PROTOCOL.MSG_CONTROL_NEXT_PAGE:
                        /*if(CUIPanelMng.Instance.m_bIsIdle == true) // 이거 함수 지움 일단 보류 지울예정
                        {
                            CUIPanelMng.Instance.m_objCurrentObject.GetComponent<CMediaVideoPlayer>()._EnetServer.SetTime(CConfigMng.Instance._fIdleSecondePlay);
                            CUIPanelMng.Instance.m_bIsIdle = false;
                        }
                        else
                        {
                            CUIPanelMng.Instance.NetWorkNext();
                        }*/
                        
                        break;
                    default: break;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }


        }

        //호출 되는 곳은 public void OnTCPOpen(UnityTCPConnection connection) 함수 맨아래에 아래함수 호출
        public void SendingID() //
        {
            //관제
            SendData(GetPacketProtocol("id", (int)PROTOCOL.MSG_SET_CONTENTS_ID, "value", (int)CONTENTS_ID.PC_CONTROL_CENTER));
        }

//4. JSON 포맷으로 바꾸어주는 함수 2개추가
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


//5. 현재 영상 번호 날려주기
 /*
 MSG_SET_CONTROL_STATE               =700,
 MSG_SET_NAMU_STATE                  =701,
 MSG_SET_LAUNG_STATE                 =702,
 MSG_SET_ROBOT_STATE                 =703
 */
        /// <summary>
        /// 숫자 9990는 영상 인덱스 번호이다 위에서 처음거부터 0으로 하면 될것 이다.
        /// -idle 은 0 으로잡고(대기상태) 처음 버튼부터 1번으로 하면 된다.
        /// </summary>  
        public void SendingContents(int nIndex)
        {
            //관제  nIndex는 영상 번호이다 위에서버터 0번 
            SendData(GetPacketProtocol("id", (int)PROTOCOL.MSG_SET_CONTROL_STATE, "value", nIndex));

        }
    }
}