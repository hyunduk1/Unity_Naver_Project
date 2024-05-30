using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine.Events;
using System.Timers;
using System.Runtime.InteropServices;
using LitJson;
using UnityEngine.SceneManagement;
using System.IO;


namespace DemolitionStudios.DemolitionMedia
{
	public class CNetWorkMng : MonoBehaviour
	{
		#region SingleTon
		private static CNetWorkMng _instance;
		public static CNetWorkMng Instance { get { return _instance; } }

		#endregion
		//public GameObject _ReConnectPopUp;

		private CUDPNetWork m_UdpNetwork;
		private Queue<string> m_MsgQuee;

		private bool m_bPauseEnable = false;


		private bool m_IsServer; public bool _IsServer { get { return m_IsServer; } set { m_IsServer = value; } }


		void Awake()
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
			m_IsServer = false;
			m_UdpNetwork = gameObject.AddComponent<CUDPNetWork>();
		}


		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				m_bPauseEnable = true;
			}
			else
			{
				if (m_bPauseEnable)
				{
					m_bPauseEnable = false;
				}
			}
		}

		public void ResetNetWork()
		{
			m_UdpNetwork.ReleaseNetWorkUdp();

			Destroy(m_UdpNetwork);
			m_UdpNetwork = null;
			m_MsgQuee.Clear();
		}

		public void ReConnetNetWork()
		{
			m_UdpNetwork = gameObject.AddComponent<CUDPNetWork>();
			m_UdpNetwork.Initialize((x) => ReceiveData(x));
		}

		void Start()
		{
			m_IsServer = m_UdpNetwork.Initialize((x) => ReceiveData(x));
			StartCoroutine("PacketGenerator");
		}
		private void Update()
		{
		}

        public void Send(PROTOCOL protocol, int nFrameValue = 0)
        {
			string SendString = "";
            int nValue = (int)protocol;

            switch (protocol)
            {
                /*case PROTOCOL.MSG_NEXT_VIDEO: SendString = "{ \"ID\":0 }"; break;
                case PROTOCOL.MSG_IDLE_VIDEO: SendString = "{ \"ID\":1 }"; break;*/
            }

            m_UdpNetwork.Send(StringToByte(SendString), CConfigMng.Instance._StrSendIP, CConfigMng.Instance._nMediaServerPort);
            Debug.Log(SendString);
        }


        public void PacketParser(string strPacket)
		{

			JsonData jData = JsonMapper.ToObject(strPacket);

			Debug.Log("Receive :   " + strPacket + " -> " + (PROTOCOL)int.Parse(jData["ID"].ToString()));

			try
			{
				switch ((PROTOCOL)int.Parse(jData["ID"].ToString()))
				{
					/*case PROTOCOL.MSG_NEXT_VIDEO: // 0
						CUIPanelMng.Instance.NextVideo();

						break;
					case PROTOCOL.MSG_IDLE_VIDEO: // 1
						//CUIPanelMng.Instance.IdleVideoPlay();
						break;*/

					default: break;
				}
			}
			catch (Exception e)
			{
				Debug.Log("ErrorMessge" + e);
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
		void ReceiveData(byte[] bytes)
		{
			m_MsgQuee.Enqueue(ByteToString(bytes));
		}

		private string ByteToString(byte[] strByte)
		{
			string str = System.Text.Encoding.Default.GetString(strByte);
			return str;
		}

		private byte[] StringToByte(string str)
		{
			byte[] StrByte = System.Text.Encoding.UTF8.GetBytes(str);
			return StrByte;
		}

		public string GetSendString(string a, int b, string c, float d)
		{
			string strTemp;
			strTemp = "{" +
				"\"" + a + "\"" + ":" + b + "," +
				"\"" + c + "\"" + ":" + d +
				"}";
			return strTemp;
		}

		public string GetSendString(string a, int b, string c, int d)
		{
			string strTemp;
			strTemp = "{" +
				"\"" + a + "\"" + ":" + b + "," +
				"\"" + c + "\"" + ":" + d +
				"}";
			return strTemp;
		}
		public string GetSendString(string a, int b)
		{
			string strTemp;
			strTemp = "{" +
							"\"" + a + "\"" + ":" + b +
							"}";
			return strTemp;

		}
		public string GetSendString(string a, string b)
		{
			string strTemp;
			strTemp = "{" +
							"\"" + a + "\"" + "," + b +
						"}";
			return strTemp;
		}
	}
}