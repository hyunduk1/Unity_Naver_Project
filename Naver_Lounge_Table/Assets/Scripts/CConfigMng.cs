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



    private float m_fTrasionsSpeed; public float _fTrasionsSpeed { get { return m_fTrasionsSpeed; } set { m_fTrasionsSpeed = value; } }

    private bool m_bFpsToString; public bool _bFpsToString { get { return m_bFpsToString; } set { m_bFpsToString = value; } }
    private bool m_bCursorEnable; public bool _bCursorEnable { get { return m_bCursorEnable; } set { m_bCursorEnable = value; } }
    private int m_nFrame; public int _nFrame { get { return m_nFrame; } set { m_nFrame = value; } }
    private int m_nDisplayNum; public int _nDisplayNum { get { return m_nDisplayNum; } set { m_nDisplayNum = value; } }
    private int m_nVideoNum; public int _nVideoNum { get { return m_nVideoNum; } set { m_nVideoNum = value; } }

    private string m_str_VideoFormat; public string _strVideoFormat { get { return m_str_VideoFormat; } set { m_str_VideoFormat = value; } }
    //--------------------------------------------------------------------------------------------------------------

    private string m_strKR_IDLE; public string _strKR_IDLE { get { return m_strKR_IDLE; } set { m_strKR_IDLE = value; } }
    private string m_strKR_Content1; public string _strKR_Content1 { get { return m_strKR_Content1; } set { m_strKR_Content1 = value; } }
    private string m_strKR_Content2; public string _strKR_Content2 { get { return m_strKR_Content2; } set { m_strKR_Content2 = value; } }
    private string m_strKR_Content3; public string _strKR_Content3 { get { return m_strKR_Content3; } set { m_strKR_Content3 = value; } }
    private string m_strKR_Content4; public string _strKR_Content4 { get { return m_strKR_Content4; } set { m_strKR_Content4 = value; } }
    private string m_strKR_Content5; public string _strKR_Content5 { get { return m_strKR_Content5; } set { m_strKR_Content5 = value; } }
    private string m_strKR_Content6; public string _strKR_Content6 { get { return m_strKR_Content6; } set { m_strKR_Content6 = value; } }
    private string m_strKR_Content7; public string _strKR_Content7 { get { return m_strKR_Content7; } set { m_strKR_Content7 = value; } }
    private string m_strKR_Content8; public string _strKR_Content8 { get { return m_strKR_Content8; } set { m_strKR_Content8 = value; } }
    private string m_strKR_Content9; public string _strKR_Content9 { get { return m_strKR_Content9; } set { m_strKR_Content9 = value; } }
    private string m_strKR_Content10; public string _strKR_Content10 { get { return m_strKR_Content10; } set { m_strKR_Content10 = value; } }
    private string m_strKR_Content11; public string _strKR_Content11 { get { return m_strKR_Content11; } set { m_strKR_Content11 = value; } }
    private string m_strKR_Content12; public string _strKR_Content12 { get { return m_strKR_Content12; } set { m_strKR_Content12 = value; } }
    private string m_strKR_Content13; public string _strKR_Content13 { get { return m_strKR_Content13; } set { m_strKR_Content13 = value; } }
    private string m_strKR_Content14; public string _strKR_Content14 { get { return m_strKR_Content14; } set { m_strKR_Content14 = value; } }
    //--------------------------------------------------------------------------------------------------------------------------

    private string m_strEN_IDLE; public string _strEN_IDLE { get { return m_strEN_IDLE; } set { m_strEN_IDLE = value; } }
    private string m_strEN_Content1; public string _strEN_Content1 { get { return m_strEN_Content1; } set { m_strEN_Content1 = value; } }
    private string m_strEN_Content2; public string _strEN_Content2 { get { return m_strEN_Content2; } set { m_strEN_Content2 = value; } }
    private string m_strEN_Content3; public string _strEN_Content3 { get { return m_strEN_Content3; } set { m_strEN_Content3 = value; } }
    private string m_strEN_Content4; public string _strEN_Content4 { get { return m_strEN_Content4; } set { m_strEN_Content4 = value; } }
    private string m_strEN_Content5; public string _strEN_Content5 { get { return m_strEN_Content5; } set { m_strEN_Content5 = value; } }
    private string m_strEN_Content6; public string _strEN_Content6 { get { return m_strEN_Content6; } set { m_strEN_Content6 = value; } }
    private string m_strEN_Content7; public string _strEN_Content7 { get { return m_strEN_Content7; } set { m_strEN_Content7 = value; } }
    private string m_strEN_Content8; public string _strEN_Content8 { get { return m_strEN_Content8; } set { m_strEN_Content8 = value; } }
    private string m_strEN_Content9; public string _strEN_Content9 { get { return m_strEN_Content9; } set { m_strEN_Content9 = value; } }
    private string m_strEN_Content10; public string _strEN_Content10 { get { return m_strEN_Content10; } set { m_strEN_Content10 = value; } }
    private string m_strEN_Content11; public string _strEN_Content11 { get { return m_strEN_Content11; } set { m_strEN_Content11 = value; } }
    private string m_strEN_Content12; public string _strEN_Content12 { get { return m_strEN_Content12; } set { m_strEN_Content12 = value; } }
    private string m_strEN_Content13; public string _strEN_Content13 { get { return m_strEN_Content13; } set { m_strEN_Content13 = value; } }
    private string m_strEN_Content14; public string _strEN_Content14 { get { return m_strEN_Content14; } set { m_strEN_Content14 = value; } }
    //------------------------------------------------------------------------------------------------------------
    private string m_strSound01; public string _StrSound01 { get { return m_strSound01; } set { m_strSound01 = value; } }
    private string m_strSound02; public string _StrSound02 { get { return m_strSound02; } set { m_strSound02 = value; } }
    private string m_strSound03; public string _StrSound03 { get { return m_strSound03; } set { m_strSound03 = value; } }
    private string m_strSound04; public string _StrSound04 { get { return m_strSound04; } set { m_strSound04 = value; } }
    private string m_strSound05; public string _StrSound05 { get { return m_strSound05; } set { m_strSound05 = value; } }
    private string m_strSound06; public string _StrSound06 { get { return m_strSound06; } set { m_strSound06 = value; } }
    private string m_strSound07; public string _StrSound07 { get { return m_strSound07; } set { m_strSound07 = value; } }
    private string m_strSound08; public string _StrSound08 { get { return m_strSound08; } set { m_strSound08 = value; } }
    private string m_strSound09; public string _StrSound09 { get { return m_strSound09; } set { m_strSound09 = value; } }
    private string m_strSound10; public string _StrSound10 { get { return m_strSound10; } set { m_strSound10 = value; } }
    private string m_strSound11; public string _StrSound11 { get { return m_strSound11; } set { m_strSound11 = value; } }
    private string m_strSound12; public string _StrSound12 { get { return m_strSound12; } set { m_strSound12 = value; } }
    private string m_strSound13; public string _StrSound13 { get { return m_strSound13; } set { m_strSound13 = value; } }
    private string m_strSound14; public string _StrSound14 { get { return m_strSound14; } set { m_strSound14 = value; } }
    private string m_strSound15; public string _StrSound15 { get { return m_strSound15; } set { m_strSound15 = value; } }

    //------------------------------------------------------------------------------------------------------------
    private int m_nBottomScreen1; public int _nBottomScreen1 { get { return m_nBottomScreen1; } set { m_nBottomScreen1 = value; } }
    private int m_nBottomScreen2; public int _nBottomScreen2 { get { return m_nBottomScreen2; } set { m_nBottomScreen2 = value; } }
    private int m_nBottomScreen3; public int _nBottomScreen3 { get { return m_nBottomScreen3; } set { m_nBottomScreen3 = value; } }
    private int m_nBottomScreen4; public int _nBottomScreen4 { get { return m_nBottomScreen4; } set { m_nBottomScreen4 = value; } }

    private int m_nTopScreen1; public int _nTopScreen1 { get { return m_nTopScreen1; } set { m_nTopScreen1 = value; } }
    private int m_nTopScreen2; public int _nTopScreen2 { get { return m_nTopScreen2; } set { m_nTopScreen2 = value; } }

    private string m_strReceiveIp; public string _strReceiveIp { get { return m_strReceiveIp; } set { m_strReceiveIp = value; } }
    private string m_strSendIp; public string _strSendIp { get { return m_strSendIp; } set { m_strSendIp = value; } }
    private int m_nReceivePort; public int _nReceivePort { get { return m_nReceivePort; } set { m_nReceivePort = value; } }
    private int m_nSendPort; public int _nSendPort { get { return m_nSendPort; } set { m_nSendPort = value; } }
    private float m_fSoundDelay; public float _fSoundDelay { get { return m_fSoundDelay; } set { m_fSoundDelay = value; } }
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

    
        m_bCursorEnable     = IniReadValuebool("SET_VALUE", "IS_CURSOR_ENABLE"); 
        m_bFpsToString = IniReadValuebool("SET_VALUE", "FPS_TOSTRING");
        m_nFrame = IniReadValueInt("SET_VALUE", "FPS");
        m_nDisplayNum = IniReadValueInt("SET_VALUE", "DISPLAY_NUM");
        m_nVideoNum = IniReadValueInt("SET_VALUE", "VIDEO_NUM");

        m_str_VideoFormat = IniReadValue("SET_VALUE", "VIDEO_FORMAT");

        m_strKR_IDLE = IniReadValue("SET_VALUE", "KR_VIDEO_IDLES");
        m_strKR_Content1 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_01");
        m_strKR_Content2 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_02");
        m_strKR_Content3 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_03");
        m_strKR_Content4 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_04");
        m_strKR_Content5 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_05");
        m_strKR_Content6 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_06");
        m_strKR_Content7 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_07");
        m_strKR_Content8 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_08");
        m_strKR_Content9 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_09");
        m_strKR_Content10 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_10");
        m_strKR_Content11 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_11");
        m_strKR_Content12 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_12");
        m_strKR_Content13 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_13");
        m_strKR_Content14 = IniReadValue("SET_VALUE", "KR_VIDEO_CONTENT_14");

        m_strEN_IDLE = IniReadValue("SET_VALUE", "EN_VIDEO_IDLES");
        m_strEN_Content1 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_01");
        m_strEN_Content2 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_02");
        m_strEN_Content3 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_03");
        m_strEN_Content4 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_04");
        m_strEN_Content5 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_05");
        m_strEN_Content6 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_06");
        m_strEN_Content7 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_07");
        m_strEN_Content8 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_08");
        m_strEN_Content9 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_09");
        m_strEN_Content10 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_10");
        m_strEN_Content11 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_11");
        m_strEN_Content12 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_12");
        m_strEN_Content13 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_13");
        m_strEN_Content14 = IniReadValue("SET_VALUE", "EN_VIDEO_CONTENT_14");

        m_strSound01 = IniReadValue("SET_VALUE", "SOUND_01");
        m_strSound02 = IniReadValue("SET_VALUE", "SOUND_02");
        m_strSound03 = IniReadValue("SET_VALUE", "SOUND_03");
        m_strSound04 = IniReadValue("SET_VALUE", "SOUND_04");
        m_strSound05 = IniReadValue("SET_VALUE", "SOUND_05");
        m_strSound06 = IniReadValue("SET_VALUE", "SOUND_06");
        m_strSound07 = IniReadValue("SET_VALUE", "SOUND_07");
        m_strSound08 = IniReadValue("SET_VALUE", "SOUND_08");
        m_strSound09 = IniReadValue("SET_VALUE", "SOUND_09");
        m_strSound10 = IniReadValue("SET_VALUE", "SOUND_10");
        m_strSound11 = IniReadValue("SET_VALUE", "SOUND_11");
        m_strSound12 = IniReadValue("SET_VALUE", "SOUND_12");
        m_strSound13 = IniReadValue("SET_VALUE", "SOUND_13");
        m_strSound14 = IniReadValue("SET_VALUE", "SOUND_14");
        m_strSound15 = IniReadValue("SET_VALUE", "SOUND_15");


        m_fSoundDelay = IniReadValueFloat("SET_VALUE", "SOUND_DELAY");



        m_nBottomScreen1 = IniReadValueInt("SET_VALUE", "FLOOR_SCREEN_1");
        m_nBottomScreen2 = IniReadValueInt("SET_VALUE", "FLOOR_SCREEN_2");
        m_nBottomScreen3 = IniReadValueInt("SET_VALUE", "FLOOR_SCREEN_3");
        m_nBottomScreen4 = IniReadValueInt("SET_VALUE", "FLOOR_SCREEN_4");

        m_nTopScreen1 = IniReadValueInt("SET_VALUE", "TOP_SCREEN_1");
        m_nTopScreen2 = IniReadValueInt("SET_VALUE", "TOP_SCREEN_2");

        m_strReceiveIp = IniReadValue("NETWORK_VALUE", "RECEIVE_IP");
        m_strSendIp = IniReadValue("NETWORK_VALUE", "SEND_IP");
        m_nReceivePort = IniReadValueInt("NETWORK_VALUE", "RECEIVE_PORT");
        m_nSendPort = IniReadValueInt("NETWORK_VALUE", "SEND_PORT");




#if UNITY_EDITOR
#else
        SetScreenResolution();
#endif

    }
    private void Start()
    {
        Cursor.visible = m_bCursorEnable;
        Application.targetFrameRate = m_nFrame;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void SetScreenResolution()
    {
        //Application.targetFrameRate = 60;
        Application.runInBackground = true;

        //SetWindowPos(GetForegroundWindow(), (IntPtr)HWND_TOPMOST, m_ScreenPosX, m_ScreenPosY, m_ScreenSizeX, m_ScreenSizeY, SWP_SHOWWINDOW);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
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

