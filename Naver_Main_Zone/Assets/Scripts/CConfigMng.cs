using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System.IO;

public class CConfigMng : MonoBehaviour
{
    private static CConfigMng _instance;
    public static CConfigMng Instance { get { return _instance; } }

    const int HWND_TOPMOST = -2;
    const uint SWP_HIDEWINDOW = 0x0080;
    const uint SWP_SHOWWINDOW = 0x0040;

    private static string strPath;
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


    private float m_fFrameCapture; public float _fFrameCapture { get { return m_fFrameCapture; } set { m_fFrameCapture = value; } }
    private float m_fTrasionsSpeed; public float _fTrasionsSpeed { get { return m_fTrasionsSpeed; } set { m_fTrasionsSpeed = value; } }
    private float m_fDelayTime;     public float _fDelayTime { get { return m_fDelayTime; } set { m_fDelayTime = value; } }
    private float m_fItweenDelay; public float _fItweenDelay { get { return m_fItweenDelay; } set { m_fItweenDelay = value; } }
    private bool m_isFullScreen;    public bool _isFullScreen { get { return m_isFullScreen; } set { m_isFullScreen = value; } }
    private int m_ScreenSizeX;      public int _ScreenSizeX { get { return m_ScreenSizeX; } set { m_ScreenSizeX = value; } }
    private int m_ScreenSizeY;      public int _ScreenSizeY { get { return m_ScreenSizeY; } set { m_ScreenSizeY = value; } }
    private int m_ScreenPosX;       public int _ScreenPosX { get { return m_ScreenPosX; } set { m_ScreenPosX = value; } }
    private int m_ScreenPosY;       public int _ScreenPosY { get { return m_ScreenPosY; } set { m_ScreenPosY = value; } }
    private int m_nClientCaptureCurrent; public int _nClientCaptureCurrent { get { return m_nClientCaptureCurrent; } set { m_nClientCaptureCurrent = value; } }
    private string m_strMediaServerIP; public string _strMediaServerIP { get { return m_strMediaServerIP; } set { m_strMediaServerIP = value; } }
    private string m_StrSendIP; public string _StrSendIP { get { return m_StrSendIP; } set { m_StrSendIP = value; } }
    private string m_strUdpReciveIp; public string _strUdpRececiveIp { get { return m_strUdpReciveIp; } set { m_strUdpReciveIp = value; } }
    private int m_nMediaServerPort;     public int _nMediaServerPort { get { return m_nMediaServerPort; } set { m_nMediaServerPort = value; } }
    private int m_nEnetPort; public int _nEnetPort { get { return m_nEnetPort; } set { m_nEnetPort = value; } }
    private int m_nReceivePort; public int _nReceivePort { get { return m_nReceivePort; } set { m_nReceivePort = value; } }

    private bool m_bIsMediaServer;      public bool _bIsMediaServer { get { return m_bIsMediaServer; } set { m_bIsMediaServer = value; } }
    private float m_fServerVideoSpeed; public float _fServerVideoSpeed { get { return m_fServerVideoSpeed; } set { m_fServerVideoSpeed = value; } }

    private int m_nVideoNum;    public int _nVideoNum { get { return m_nVideoNum; } set { m_nVideoNum = value; } }
    private float m_fIdleSecondePlay; public float _fIdleSecondePlay { get { return m_fIdleSecondePlay; } set { m_fIdleSecondePlay = value; } }
    private int m_nIdleLastPlay; public int _nIdleLastPlay { get { return m_nIdleLastPlay; } set { m_nIdleLastPlay = value; } }

    //-----------------------------------------------

    private string m_strMediaIdle; public string _StrMediaIdle { get { return m_strMediaIdle; } set { m_strMediaIdle = value; } }
    private string m_strMediaInTro; public string _strMediaInTro { get { return m_strMediaInTro; } set { m_strMediaInTro = value; } }
    private string m_strMedia02; public string _strMedia02 { get { return m_strMedia02; } set { m_strMedia02 = value; } }
    private string m_strMedia03; public string _strMedia03 { get { return m_strMedia03; } set { m_strMedia03 = value; } }
    private string m_strMedia04; public string _strMedia04 { get { return m_strMedia04; } set { m_strMedia04 = value; } }
    private string m_strMedia05; public string _strMedia05 { get { return m_strMedia05; } set { m_strMedia05 = value; } }
    private string m_strMedia06; public string _strMedia06 { get { return m_strMedia06; } set { m_strMedia06 = value; } }
    private string m_strMedia07; public string _strMedia07 { get { return m_strMedia07; } set { m_strMedia07 = value; } }
    private string m_strMedia08; public string _strMedia08 { get { return m_strMedia08; } set { m_strMedia08 = value; } }
    private string m_strMedia09; public string _strMedia09 { get { return m_strMedia09; } set { m_strMedia09 = value; } }
    private string m_strMedia10; public string _strMedia10 { get { return m_strMedia10; } set { m_strMedia10 = value; } }
    //--------------------------------------------------------------------------------------------

    private string m_strKRMediaIdle; public string _StrKRMediaIdle { get { return m_strKRMediaIdle; } set { m_strKRMediaIdle = value; } }
    private string m_strKRMediaInTro; public string _strKRMediaInTro { get { return m_strKRMediaInTro; } set { m_strKRMediaInTro = value; } }
    private string m_strKRMedia02; public string _strKRMedia02 { get { return m_strKRMedia02; } set { m_strKRMedia02 = value; } }
    private string m_strKRMedia03; public string _strKRMedia03 { get { return m_strKRMedia03; } set { m_strKRMedia03 = value; } }
    private string m_strKRMedia04; public string _strKRMedia04 { get { return m_strKRMedia04; } set { m_strKRMedia04 = value; } }
    private string m_strKRMedia05; public string _strKRMedia05 { get { return m_strKRMedia05; } set { m_strKRMedia05 = value; } }
    private string m_strKRMedia06; public string _strKRMedia06 { get { return m_strKRMedia06; } set { m_strKRMedia06 = value; } }
    private string m_strKRMedia07; public string _strKRMedia07 { get { return m_strKRMedia07; } set { m_strKRMedia07 = value; } }
    private string m_strKRMedia08; public string _strKRMedia08 { get { return m_strKRMedia08; } set { m_strKRMedia08 = value; } }
    private string m_strKRMedia09; public string _strKRMedia09 { get { return m_strKRMedia09; } set { m_strKRMedia09 = value; } }
    private string m_strKRMedia10; public string _strKRMedia10 { get { return m_strKRMedia10; } set { m_strKRMedia10 = value; } }

    private int m_nCilentCaptureNum; public int _nClientCaptureNum { get { return m_nCilentCaptureNum; } set { m_nCilentCaptureNum = value; } }
    //--------------------------------------------------------------------------------------------


    private string m_strScreenCapture; public string _StrScreenCapture { get { return m_strScreenCapture; } set { m_strScreenCapture = value; } }

    //--------------------------------------------------------------------------------------------


    private bool m_bFpsToString; public bool _bFpsToString { get { return m_bFpsToString; } set { m_bFpsToString = value; } }

    private bool m_bCursorEnable; public bool _bCursorEnable { get { return m_bCursorEnable; } set { m_bCursorEnable = value; } }
    private int m_nFrame; public int _nFrame { get { return m_nFrame; } set { m_nFrame = value; } }

    private string m_strSoundFolder; public string _strSoundFolder { get { return m_strSoundFolder;  } set { m_strSoundFolder = value; } }
    private string m_strSoundPath00; public string _strSoundPath00 { get { return m_strSoundPath00; } set { m_strSoundPath00 = value; } }
    private string m_strSoundPath01; public string _strSoundPath01 { get { return m_strSoundPath01; } set { m_strSoundPath01 = value; } }
    private string m_strSoundPath02; public string _strSoundPath02 { get { return m_strSoundPath02; } set { m_strSoundPath02 = value; } }
    private string m_strSoundPath03; public string _strSoundPath03 { get { return m_strSoundPath03; } set { m_strSoundPath03 = value; } }
    private string m_strSoundPath04; public string _strSoundPath04 { get { return m_strSoundPath04; } set { m_strSoundPath04 = value; } }
    private string m_strSoundPath05; public string _strSoundPath05 { get { return m_strSoundPath05; } set { m_strSoundPath05 = value; } }
    private string m_strSoundPath06; public string _strSoundPath06 { get { return m_strSoundPath06; } set { m_strSoundPath06 = value; } }
    private string m_strSoundPath07; public string _strSoundPath07 { get { return m_strSoundPath07; } set { m_strSoundPath07 = value; } }
    private string m_strSoundPath08; public string _strSoundPath08 { get { return m_strSoundPath08; } set { m_strSoundPath08 = value; } }
    private string m_strSoundPath09; public string _strSoundPath09 { get { return m_strSoundPath09; } set { m_strSoundPath09 = value; } }
    private string m_strSoundPath10; public string _strSoundPath10 { get { return m_strSoundPath10; } set { m_strSoundPath10 = value; } }
    //----------------------------------------------------------------------------------------------------------------------------

    private string m_StrCapTure0; public string _strCapTure0 { get { return m_StrCapTure0; } set { m_StrCapTure0 = value; } }
    private string m_StrCapTure1; public string _strCapTure1 { get { return m_StrCapTure1; } set { m_StrCapTure1 = value; } }
    private string m_StrCapTure2; public string _strCapTure2 { get { return m_StrCapTure2; } set { m_StrCapTure2 = value; } }
    private string m_StrCapTure3; public string _strCapTure3 { get { return m_StrCapTure3; } set { m_StrCapTure3 = value; } }
    private string m_StrCapTure4; public string _strCapTure4 { get { return m_StrCapTure4; } set { m_StrCapTure4 = value; } }
    private string m_StrCapTure5; public string _strCapTure5 { get { return m_StrCapTure5; } set { m_StrCapTure5 = value; } }
    private string m_StrCapTure6; public string _strCapTure6 { get { return m_StrCapTure6; } set { m_StrCapTure6 = value; } }
    private string m_StrCapTure7; public string _strCapTure7 { get { return m_StrCapTure7; } set { m_StrCapTure7 = value; } }
    private string m_StrCapTure8; public string _strCapTure8 { get { return m_StrCapTure8; } set { m_StrCapTure8 = value; } }
    private string m_StrCapTure9; public string _strCapTure9 { get { return m_StrCapTure9; } set { m_StrCapTure9 = value; } }
    //--------------------------------------------------------------------------------------------------------------------------------
    private string m_StrKRCapTure0; public string _strKRCapTure0 { get { return m_StrKRCapTure0; } set { m_StrKRCapTure0 = value; } }
    private string m_StrKRCapTure1; public string _strKRCapTure1 { get { return m_StrKRCapTure1; } set { m_StrKRCapTure1 = value; } }
    private string m_StrKRCapTure2; public string _strKRCapTure2 { get { return m_StrKRCapTure2; } set { m_StrKRCapTure2 = value; } }
    private string m_StrKRCapTure3; public string _strKRCapTure3 { get { return m_StrKRCapTure3; } set { m_StrKRCapTure3 = value; } }
    private string m_StrKRCapTure4; public string _strKRCapTure4 { get { return m_StrKRCapTure4; } set { m_StrKRCapTure4 = value; } }
    private string m_StrKRCapTure5; public string _strKRCapTure5 { get { return m_StrKRCapTure5; } set { m_StrKRCapTure5 = value; } }
    private string m_StrKRCapTure6; public string _strKRCapTure6 { get { return m_StrKRCapTure6; } set { m_StrKRCapTure6 = value; } }
    private string m_StrKRCapTure7; public string _strKRCapTure7 { get { return m_StrKRCapTure7; } set { m_StrKRCapTure7 = value; } }
    private string m_StrKRCapTure8; public string _strKRCapTure8 { get { return m_StrKRCapTure8; } set { m_StrKRCapTure8 = value; } }
    private string m_StrKRCapTure9; public string _strKRCapTure9 { get { return m_StrKRCapTure9; } set { m_StrKRCapTure9 = value; } }

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
        
        strPath = Application.dataPath + "/StreamingAssets/Config.ini";

        m_fTrasionsSpeed    = IniReadValueFloat("SET_VALUE", "TRANSITION_SPEED");
        m_fDelayTime        = IniReadValueFloat("SET_VALUE", "DELAY_TIME");
        m_nCilentCaptureNum = IniReadValueInt("SET_VALUE", "CAPTURE_NUM");

        m_fFrameCapture = IniReadValueFloat("SET_VALUE", "FRAME_CAPTURE_DELAY");
            m_isFullScreen      = IniReadValuebool("SET_VALUE", "IS_WINDOW_MODE");

        m_ScreenPosX        = IniReadValueInt("SET_VALUE", "SCREEN_POS_X");
        m_ScreenPosY        = IniReadValueInt("SET_VALUE", "SCREEN_POS_Y");

        m_ScreenSizeX       = IniReadValueInt("SET_VALUE", "SCREEN_SIZE_X");
        m_ScreenSizeY       = IniReadValueInt("SET_VALUE", "SCREEN_SIZE_Y");
        m_bCursorEnable     = IniReadValuebool("SET_VALUE", "IS_CURSOR_ENABLE"); 
        m_bIsMediaServer    = IniReadValuebool("SET_VALUE", "IS_MEDIA_SERVER");
        m_strMediaServerIP  = IniReadValue    ("SET_VALUE", "MEDIA_SERVER_IP");
        m_strUdpReciveIp = IniReadValue("SET_VALUE", "UDP_RECEVIVE_IP");
        m_StrSendIP = IniReadValue("SET_VALUE", "SEND_PROTOCOL_IP");
        m_nMediaServerPort  = IniReadValueInt ("SET_VALUE", "MEDIA_SERVER_PORT");
        m_nEnetPort = IniReadValueInt("SET_VALUE", "ENET_PORT");
        m_nReceivePort = IniReadValueInt("SET_VALUE", "RECEIVE_PORT");
        m_bFpsToString = IniReadValuebool("SET_VALUE", "FPS_TOSTRING");
        m_nVideoNum = IniReadValueInt("SET_VALUE", "VIDEO_NUM");
        m_fItweenDelay = IniReadValueFloat("SET_VALUE", "ITWEEN_DELAY");
        m_nClientCaptureCurrent = IniReadValueInt("SET_VALUE", "CLIENT_CAPTURE_NUM");


        m_nFrame = IniReadValueInt("SET_VALUE", "FRAME");

        m_fIdleSecondePlay = IniReadValueFloat("SET_VALUE", "IDLE_SECONDE_PLAY_TIME");
        m_nIdleLastPlay = IniReadValueInt("SET_VALUE", "IDLE_LAST_PLAY_FRAME");
        

        m_fServerVideoSpeed = IniReadValueFloat("SET_VALUE", "SERVER_TIME_SPEED");
        m_strMediaIdle = IniReadValue("SET_VALUE", "IDLE_FILE");
        m_strMediaInTro = IniReadValue("SET_VALUE", "INTRO_FILE");
        m_strMedia02 = IniReadValue("SET_VALUE", "FILE_02");
        m_strMedia03 = IniReadValue("SET_VALUE", "FILE_03");
        m_strMedia04 = IniReadValue("SET_VALUE", "FILE_04");
        m_strMedia05 = IniReadValue("SET_VALUE", "FILE_05");
        m_strMedia06 = IniReadValue("SET_VALUE", "FILE_06");
        m_strMedia07 = IniReadValue("SET_VALUE", "FILE_07");
        m_strMedia08 = IniReadValue("SET_VALUE", "FILE_08");
        m_strMedia09 = IniReadValue("SET_VALUE", "FILE_09");
        m_strMedia10 = IniReadValue("SET_VALUE", "FILE_10");

        //--------------------------------------------------------------------
        m_strKRMediaIdle = IniReadValue("SET_VALUE", "KR_IDLE_FILE");
        m_strKRMediaInTro = IniReadValue("SET_VALUE", "KR_INTRO_FILE");
        m_strKRMedia02 = IniReadValue("SET_VALUE", "KR_FILE_02");
        m_strKRMedia03 = IniReadValue("SET_VALUE", "KR_FILE_03");
        m_strKRMedia04 = IniReadValue("SET_VALUE", "KR_FILE_04");
        m_strKRMedia05 = IniReadValue("SET_VALUE", "KR_FILE_05");
        m_strKRMedia06 = IniReadValue("SET_VALUE", "KR_FILE_06");
        m_strKRMedia07 = IniReadValue("SET_VALUE", "KR_FILE_07");
        m_strKRMedia08 = IniReadValue("SET_VALUE", "KR_FILE_08");
        m_strKRMedia09 = IniReadValue("SET_VALUE", "KR_FILE_09");
        m_strKRMedia10 = IniReadValue("SET_VALUE", "KR_FILE_10");
        //--------------------------------------------------------------------



        m_strScreenCapture = IniReadValue("SET_VALUE", "SCREEN_SHOT_PATH");

        m_strSoundFolder = IniReadValue("SET_VALUE", "SOUND_FOLDER");

        m_strSoundPath00 = IniReadValue("SET_VALUE", "SOUND_PATH_00");
        m_strSoundPath01 = IniReadValue("SET_VALUE", "SOUND_PATH_01");
        m_strSoundPath02 = IniReadValue("SET_VALUE", "SOUND_PATH_02");
        m_strSoundPath03 = IniReadValue("SET_VALUE", "SOUND_PATH_03");
        m_strSoundPath04 = IniReadValue("SET_VALUE", "SOUND_PATH_04");
        m_strSoundPath05 = IniReadValue("SET_VALUE", "SOUND_PATH_05");
        m_strSoundPath06 = IniReadValue("SET_VALUE", "SOUND_PATH_06");
        m_strSoundPath07 = IniReadValue("SET_VALUE", "SOUND_PATH_07");
        m_strSoundPath08 = IniReadValue("SET_VALUE", "SOUND_PATH_08");
        m_strSoundPath09 = IniReadValue("SET_VALUE", "SOUND_PATH_09");
        m_strSoundPath10 = IniReadValue("SET_VALUE", "SOUND_PATH_10");
        //------------------------------------------------------------------------

        m_StrCapTure0 = IniReadValue("SET_VALUE", "CAPTURE_0");
        m_StrCapTure1 = IniReadValue("SET_VALUE", "CAPTURE_1");
        m_StrCapTure2 = IniReadValue("SET_VALUE", "CAPTURE_2");
        m_StrCapTure3 = IniReadValue("SET_VALUE", "CAPTURE_3");
        m_StrCapTure4 = IniReadValue("SET_VALUE", "CAPTURE_4");
        m_StrCapTure5 = IniReadValue("SET_VALUE", "CAPTURE_5");
        m_StrCapTure6 = IniReadValue("SET_VALUE", "CAPTURE_6");
        m_StrCapTure7 = IniReadValue("SET_VALUE", "CAPTURE_7");
        m_StrCapTure8 = IniReadValue("SET_VALUE", "CAPTURE_8");
        m_StrCapTure9 = IniReadValue("SET_VALUE", "CAPTURE_9");
        //--------------------------------------------------------------------------
        m_StrKRCapTure0 = IniReadValue("SET_VALUE", "KR_CAPTURE_0");
        m_StrKRCapTure1 = IniReadValue("SET_VALUE", "KR_CAPTURE_1");
        m_StrKRCapTure2 = IniReadValue("SET_VALUE", "KR_CAPTURE_2");
        m_StrKRCapTure3 = IniReadValue("SET_VALUE", "KR_CAPTURE_3");
        m_StrKRCapTure4 = IniReadValue("SET_VALUE", "KR_CAPTURE_4");
        m_StrKRCapTure5 = IniReadValue("SET_VALUE", "KR_CAPTURE_5");
        m_StrKRCapTure6 = IniReadValue("SET_VALUE", "KR_CAPTURE_6");
        m_StrKRCapTure7 = IniReadValue("SET_VALUE", "KR_CAPTURE_7");
        m_StrKRCapTure8 = IniReadValue("SET_VALUE", "KR_CAPTURE_8");
        m_StrKRCapTure9 = IniReadValue("SET_VALUE", "KR_CAPTURE_9");
#if UNITY_EDITOR
#else
        SetScreenResolution();
#endif

    }
    private void Start()
    {
        
        Cursor.visible = m_bCursorEnable;
        Application.targetFrameRate = m_nFrame;
        Debug.Log("ServerMode    -----------    " + m_bIsMediaServer);
    }
    public void SetScreenResolution()
    {
        Application.targetFrameRate = 60;
        Cursor.visible = m_bCursorEnable;
        Application.runInBackground = true;
        Screen.SetResolution((int)m_ScreenSizeX, (int)m_ScreenSizeY, false);

        SetWindowPos(GetForegroundWindow(), (IntPtr)HWND_TOPMOST, m_ScreenPosX, m_ScreenPosY, m_ScreenSizeX, m_ScreenSizeY, SWP_SHOWWINDOW);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        //Cursor.lockState = CursorLockMode.Locked;
    }
    public static string IniReadValue(string Section, string Key)
    {
        StringBuilder temp = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", temp, 255, strPath);
        return temp.ToString();
    }


    public static float IniReadValueFloat(string Section, string Key)
    {
        StringBuilder temp = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", temp, 255, strPath);
        float result = 0.0f;
        float.TryParse(temp.ToString(), out result);
        return result;
    }

    public static bool IniReadValuebool(string Section, string Key)
    {
        StringBuilder temp = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", temp, 255, strPath);
        int result = 0;
        int.TryParse(temp.ToString(), out result);
        if (result == 1)
        {
            return true;
        }
        return false;
    }

    public static int IniReadValueInt(string Section, string Key)
    {
        StringBuilder temp = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", temp, 255, strPath);
        int result = 0;
        int.TryParse(temp.ToString(), out result);
        return result;
    }

    public static int IniReadValueIntTimeData(string Section, string Key, string strDataPath)
    {
        StringBuilder temp = new StringBuilder(255);
        GetPrivateProfileString(Section, Key, "", temp, 255, strDataPath);
        int result = 0;
        int.TryParse(temp.ToString(), out result);
        return result;
    }
}

