using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine.Events;
using System.Timers;
using System.Runtime.InteropServices;


public class CUDPNetWork : MonoBehaviour
{

    private int m_nReceivePort;

    private UdpClient m_SendClinet = null;
    private UdpClient m_ReceiverClient = null;

    private bool m_bFinishFlag = false;
    private bool m_bOnlyFlag = false;
    private int m_SendCount = 0;


    private string m_RecvIp = "";
 //   private int m_nRecvPort = 1820;

    private Action<byte[]> m_ArrayCallBack;

    //private bool m_bSendingEnable = false;
    void Start()
    {
        
    }

    public void OnApplicationQuit()
    {
        Debug.Log("네트워크 삭제________[0]");
        if (m_ReceiverClient != null)
            m_ReceiverClient.Close();
        if (m_SendClinet != null)
            m_SendClinet.Close();
    }

    public void ReleaseNetWorkUdp()
    {
        Debug.Log("네트워크 삭제________[1]");
        m_ReceiverClient.Dispose();
        m_SendClinet.Dispose();

        if (m_ReceiverClient != null)
            m_ReceiverClient.Close();
        if (m_SendClinet != null)
            m_SendClinet.Close();

        Debug.Log("네트워크 삭제________");
        m_ReceiverClient = null;
        m_SendClinet = null;
    }
  
    public string GetReceiveIp()
    {
        return m_RecvIp;
    }
    public bool Initialize(Action<byte[]> callback = null)
    {
        if (callback != null)
            m_ArrayCallBack = callback;

        UdpClient udpClientReceive;
        m_RecvIp = GetLocalIPArray(0);

        Debug.Log("IP SET : " + m_RecvIp);
        m_nReceivePort = CConfigMng.Instance._nReceivePort;

        Debug.Log("Port SET : " + m_nReceivePort);

        udpClientReceive = new UdpClient(new IPEndPoint(IPAddress.Parse(m_RecvIp), m_nReceivePort));
        udpClientReceive.BeginReceive(UDPReceive, udpClientReceive);
        m_ReceiverClient = udpClientReceive;
        m_SendClinet = new UdpClient();

        if (m_RecvIp == CConfigMng.Instance._strMediaServerIP)
        {
            return true;
        }
       
        return false;
    }


    public void Initialize(string strIP, int recvPort, Action<byte[]> callback = null)
    {

        m_RecvIp = strIP;
        m_nReceivePort = recvPort;

        if (callback != null)
            m_ArrayCallBack = callback;

        UdpClient tempUdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, m_nReceivePort));
        tempUdpClient.BeginReceive(UDPReceive, tempUdpClient);
        m_ReceiverClient = tempUdpClient;
        m_SendClinet = new UdpClient();

    }


    private void UDPReceive(IAsyncResult res)
    {
        if (m_bFinishFlag){
            (res.AsyncState as UdpClient).Close();
            return;
        }
       
        UdpClient UdpClient = (UdpClient)res.AsyncState;
        IPEndPoint ipEnd = null;        
        byte[] recvByte;


        try
        {           
            recvByte = UdpClient.EndReceive(res, ref ipEnd);
            if (m_ArrayCallBack != null)                       
                m_ArrayCallBack(recvByte);                    
        }
        catch (SocketException ex)
        {
            Debug.LogError(ex);
            return;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        if (m_bFinishFlag || m_bOnlyFlag){
            UdpClient.Close();
            return;
        }

        UdpClient.BeginReceive(UDPReceive, UdpClient);
    }

    public void Send(byte[] sendByte, string strSendIP, int sendPort, byte retryCount = 0)
    {
        if (m_SendClinet == null)
            return;

      
        m_SendClinet.Send(sendByte, sendByte.Length, strSendIP, sendPort);
    }

    void UDPSender(IAsyncResult res)
    {
        UdpClient udp = (UdpClient)res.AsyncState;
        try{
            udp.EndSend(res);
            Debug.Log("Send");
        }
        catch (SocketException ex){
            Debug.Log("Error" + ex);
            return;
        }
        catch (ObjectDisposedException){
            Debug.Log("Socket Already Closed.");
            return;
        }

        m_SendCount--;
        if (udp != null)
            udp.Close();
    }

    public string GetLocalIPArray(int nIndex)
    {
        IPAddress[] addr_arr = Dns.GetHostAddresses(Dns.GetHostName());
        List<string> list = new List<string>();

        foreach (IPAddress address in addr_arr)
        {
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                list.Add(address.ToString());
            }
        }

        if (list.Count == 0)
            return null;

        return list[nIndex];
    }
}


// m_SendClinet = new UdpClient(new IPEndPoint(IPAddress.Parse(CConfigMng.Instance._strServer), CConfigMng.Instance._strServerPort));