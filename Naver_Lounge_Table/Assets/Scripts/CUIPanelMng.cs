using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CUIPanelMng : MonoBehaviour
    {
        private static CUIPanelMng _instance;
        public static CUIPanelMng Instance { get { return _instance; } }

        private List<string> m_strPanelName = null;
        private Dictionary<string, GameObject> m_ListPrefabs;

        [HideInInspector]
        public GameObject m_objBottomLeftDisplay_00;

        [HideInInspector]
        public GameObject m_objBottomLeftDisplay_01;

        [HideInInspector]
        public GameObject m_objTopRightDisplay;

        [HideInInspector]
        public GameObject m_objProjectionSource_00;

        [HideInInspector]
        public GameObject m_objProjectionSource_01;

        private GameObject m_objSound;

        public short m_nCurrentCountVideo = 0;
        public short m_nCurrentNum = 0;

        private string m_strFileNameReturn;
        private bool m_bIsUvMap = false;
        public bool m_bMenualMode = true;

        private bool m_bIdleMode = true;

        public bool m_bPause = false;

        public GameObject _UvMAP;

        private bool m_bIsAutoOncePlay = true;


        public GameObject _VideoNode;
        public GameObject _RenderTexturNode;

        public RenderTexture[] _renderTextures;
        public bool m_bIsLanguage = true;
        private bool m_bIsOncePlay = true;

        bool m_bTsetImage = false;

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
            m_objBottomLeftDisplay_00 = null;
            m_objBottomLeftDisplay_01 = null;
            m_objTopRightDisplay = null;
            m_objProjectionSource_00 = null;
            m_objProjectionSource_01 = null;


            m_ListPrefabs = new Dictionary<string, GameObject>();
            m_strPanelName = new List<string>();

            LoadPrefabs("", "00_Uvmap");

            LoadPrefabs("Video_Prefabs/", "00_Display_VideoPlayer");
            LoadPrefabs("Video_Prefabs/", "01_Sound");

            LoadPrefabs("RenderTexture_Prefabs/", "00_Render_Texture");
        }
        // Start is called before the first frame update
        void Start()
        {
            for (int i = 0; i < CConfigMng.Instance._nDisplayNum; i++)
            {
                Display.displays[i].Activate();
            }

            for (short i = 0; i < 6; i++)
                InsertRenderTexturPanel(i);

            m_nCurrentNum++;

            Debug.Log(m_nCurrentNum);
        }

        public void AutoDelay()
        {
            m_bIsAutoOncePlay = true;
            m_bMenualMode = false;
        }
        // Update is called once per frame
        void Update()
        {

            if (m_objBottomLeftDisplay_00 == null)
            {
                if(m_bMenualMode == false)
                {
                    if (m_bIsAutoOncePlay == true)
                    {
                        InsertMovie(m_nCurrentNum);
                        Invoke("AutoDelay", 1.0f);
                        m_bIsAutoOncePlay = false;
                    }
                }
                
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                m_bPause = !m_bPause;
                PauseRecive(m_bPause);
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                CTCPNetWorkMng.Instance.SendDataPackt(PROTOCOL.MSG_LAUNG_NEXT_PAGE);
                PauseRecive(false);
                m_bPause = false;
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                CTCPNetWorkMng.Instance.SendDataPackt(PROTOCOL.MSG_LAUNG_TABLE_AUTO_START);
            }
            if(Input.GetKeyDown(KeyCode.H))
            {
                CTCPNetWorkMng.Instance.SendDataPackt(PROTOCOL.MSG_LAUNG_TABLE_MANUAL_START);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                m_bIsUvMap = !m_bIsUvMap;
                _UvMAP.SetActive(m_bIsUvMap);
            }
            if(Input.GetMouseButtonDown(0))
            {

                m_nCurrentNum = 0;
                InsertMovie(m_nCurrentNum);
            }

        }

        #region NetWorkSend


        public void PauseRecive(bool Pause) // true 멈춤, false 플레이
        {
            if (m_objSound && m_objBottomLeftDisplay_00 && m_objBottomLeftDisplay_01 && m_objTopRightDisplay && m_objProjectionSource_00 && m_objProjectionSource_01 != null)
            {
                m_objBottomLeftDisplay_00.GetComponentInChildren<CVideoPlayer>().VideoPause(Pause);
                m_objBottomLeftDisplay_01.GetComponentInChildren<CVideoPlayer>().VideoPause(Pause);
                m_objTopRightDisplay.GetComponentInChildren<CVideoPlayer>().VideoPause(Pause);
                m_objProjectionSource_00.GetComponentInChildren<CVideoPlayer>().VideoPause(Pause);
                m_objProjectionSource_01.GetComponentInChildren<CVideoPlayer>().VideoPause(Pause);
                if(m_objSound !=null)
                    m_objSound.GetComponent<CSound>().AudioPause(Pause);
            }
        }

        
        #endregion
        public void IdleMovie()
        {
            if (m_objBottomLeftDisplay_00 != null)
            {
                m_objBottomLeftDisplay_01.GetComponent<CUIPanel>().FadeOutWindow();
                m_objTopRightDisplay.GetComponent<CUIPanel>().FadeOutWindow();
                m_objProjectionSource_00.GetComponent<CUIPanel>().FadeOutWindow();
                m_objProjectionSource_01.GetComponent<CUIPanel>().FadeOutWindow();
                m_objBottomLeftDisplay_00.GetComponent<CUIPanel>().FadeOutWindow();
                m_objSound.GetComponent<CUIPanel>().FadeOutWindow();
                m_bMenualMode = true;
            }
        }

        #region InsertVideo
        void FileLoad(string FileName, short num)
        {
            for (short i = 0; i < 5; i++)
            {
                InsertMoviePanel(FileName + i.ToString() + CConfigMng.Instance._strVideoFormat, i, false);
            }
            m_nCurrentNum = num;
        }

        public void InsertMovie(short nVideoNum)
        {
            if (_VideoNode.GetComponentInChildren<iTween>() != null)
                return;
            if (m_nCurrentNum > CConfigMng.Instance._nVideoNum)
            {
                
                IdleMovie();
                m_nCurrentNum = 0;
                if(m_bMenualMode == false)
                {
                    CTCPNetWorkMng.Instance.SendData(CTCPNetWorkMng.Instance.GetPacketProtocol("id", (int)PROTOCOL.MSG_LAUNGE_AUTO_FINISH));
                }
            }
            /*if (nVideoNum != 0)
                m_bMenualMode = true;*/

            switch (nVideoNum)
            {
                case 0:
                    IdleMovie();
                   
                    break;

                case 1:

                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_IDLE, 1);
                    else
                        FileLoad(CConfigMng.Instance._strEN_IDLE, 1);
                    Debug.Log("부지 선정요건");
                    InsertSoundPanel(m_nCurrentNum);
                    break;

                case 2:

                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content1, 2);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content1, 2);
                    InsertSoundPanel(m_nCurrentNum);

                    Debug.Log("데이터 센터 부지");

                    break;

                case 3:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content2, 3);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content2, 3);
                    InsertSoundPanel(m_nCurrentNum);
                    Debug.Log("남관/ 북관");
                    break;

                case 4:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content3, 4);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content3, 4);
                    Debug.Log("지반 안정성");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                    //-----------------------------------
                case 5:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content4, 5);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content4, 5);
                    Debug.Log("내진설계");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                case 6:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content5, 6);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content5, 6);
                    Debug.Log("소방");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                case 7:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content6, 7);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content6, 7);
                    Debug.Log("전력수급");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                case 8:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content7, 8);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content7, 8);
                    Debug.Log("풍향");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                    //------------------------------------
                case 9:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content8, 9);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content8, 9);
                    InsertSoundPanel(m_nCurrentNum);
                    Debug.Log("폐열활용");
                    break;
                case 10:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content9, 10);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content9, 10);
                    InsertSoundPanel(m_nCurrentNum);
                    Debug.Log("태양광");
                    break;
                case 11:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content10, 11);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content10, 11);
                    Debug.Log("지열");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                case 12:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content11, 12);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content11, 12);
                    Debug.Log("재생");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                case 13:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content12, 13);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content12, 13);
                    Debug.Log("우수와 중수");
                    InsertSoundPanel(m_nCurrentNum);
                    break;
                case 14:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content13, 14);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content13, 14);
                    Debug.Log("LEED");
                    InsertSoundPanel(m_nCurrentNum);
                    break;

                case 15:
                    if (m_bIsLanguage == true)
                        FileLoad(CConfigMng.Instance._strKR_Content14, 15);
                    else
                        FileLoad(CConfigMng.Instance._strEN_Content14, 15);
                    InsertSoundPanel(m_nCurrentNum);
                    Debug.Log("Outro");
                    break;
            }
            
            Debug.Log(m_nCurrentNum + " 번 영상");
            CTCPNetWorkMng.Instance.SendingContents(m_nCurrentNum);
            m_nCurrentNum++;
        }

        #endregion

        #region Instantiate Prefabs
        public void LoadPrefabs(string strFolderName, string strFileName)
        {
            GameObject tempObject = Resources.Load("Prefabs/" + strFolderName + strFileName) as GameObject;

            if (tempObject != null)
            {
                m_ListPrefabs.Add(strFileName, tempObject);
                m_strPanelName.Add(strFileName);
            }
        }

        private void InsertRenderTexturPanel(short Num)
        {
            GameObject tempWindow;
            tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["00_Render_Texture"]) as GameObject;
            tempWindow.transform.SetParent(_RenderTexturNode.transform);
            RectTransform rectTransform = tempWindow.transform.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1920, 1080);
            switch (Num)
            {
                case 0:
                    tempWindow.GetComponent<RawImage>().texture = _renderTextures[CConfigMng.Instance._nBottomScreen1];
                    rectTransform.position = new Vector2(-6720.0f, 540.0f);
                    break;
                case 1:
                    tempWindow.GetComponent<RawImage>().texture = _renderTextures[CConfigMng.Instance._nBottomScreen2];
                    rectTransform.position = new Vector2(-6720.0f, -540.0f);

                    break;
                case 2:
                    tempWindow.GetComponent<RawImage>().texture = _renderTextures[CConfigMng.Instance._nBottomScreen3];
                    rectTransform.position = new Vector2(-4800.0f, -540.0f);

                    break;
                case 3:
                    tempWindow.GetComponent<RawImage>().texture = _renderTextures[CConfigMng.Instance._nBottomScreen4];
                    rectTransform.position = new Vector2(-4800.0f, 540.0f);
                    break;
                case 4:
                    tempWindow.GetComponent<RawImage>().texture = _renderTextures[CConfigMng.Instance._nTopScreen1];
                    rectTransform.position = new Vector2(-2880.0f, 540.0f);
                    break;
                case 5:
                    tempWindow.GetComponent<RawImage>().texture = _renderTextures[CConfigMng.Instance._nTopScreen2];
                    rectTransform.position = new Vector2(-2880.0f, -540.0f);
                    break;
            }
            tempWindow.GetComponent<CUIPanel>().FadeInWindow();
        }

        public void InsertSoundPanel(short Num)
        {
            if( m_objSound != null)
            {
                m_objSound.GetComponent<CUIPanel>().FadeOutWindow();
            }
            GameObject tempWindow;
            tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["01_Sound"]) as GameObject;
            tempWindow.GetComponent<CSound>().PlayAudio(Num);
            tempWindow.transform.SetParent(_VideoNode.transform);
            tempWindow.GetComponent<CUIPanel>().FadeInWindow();
            m_objSound = tempWindow;
        }

        public void InsertMoviePanel(string FilePath, short Num, bool Loop)
        {
            /*if (m_strFileNameReturn == FilePath)
                return;

            m_strFileNameReturn = FilePath; */
            switch (Num)
            {
                case 0: // 하단 디스플레이 1
                    if (m_objBottomLeftDisplay_00 != null)
                    {
                        if (m_objBottomLeftDisplay_00.GetComponent<iTween>() != null)
                            return;
                        m_objBottomLeftDisplay_00.GetComponent<CUIPanel>().FadeOutWindow();
                    }
                    break;
                case 1: // 하단 디스플레이 2
                    if (m_objBottomLeftDisplay_01 != null)
                    {
                        if (m_objBottomLeftDisplay_01.GetComponent<iTween>() != null)
                            return;
                        m_objBottomLeftDisplay_01.GetComponent<CUIPanel>().FadeOutWindow();
                    }
                    break;

                case 2: // 상단 디스플레이
                    if (m_objTopRightDisplay != null)
                    {
                        if (m_objTopRightDisplay.GetComponent<iTween>() != null)
                            return;
                        m_objTopRightDisplay.GetComponent<CUIPanel>().FadeOutWindow();
                    }

                    break;
                case 3: // 프로젝션 디스플레이1
                    if (m_objProjectionSource_00 != null)
                    {
                        if (m_objProjectionSource_00.GetComponent<iTween>() != null)
                            return;
                        m_objProjectionSource_00.GetComponent<CUIPanel>().FadeOutWindow();
                    }

                    break;
                case 4: // 프로젝션 디스플레이2
                    if (m_objProjectionSource_01 != null)
                    {
                        if (m_objProjectionSource_01.GetComponent<iTween>() != null)
                            return;
                        m_objProjectionSource_01.GetComponent<CUIPanel>().FadeOutWindow();
                    }
                    break;
            }

            GameObject tempWindow;
            tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["00_Display_VideoPlayer"]) as GameObject;

            tempWindow.GetComponentInChildren<CVideoPlayer>().InitiallizeContents(FilePath, Loop);
            tempWindow.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            tempWindow.transform.SetParent(_VideoNode.transform);
            RectTransform rectTransform = tempWindow.transform.GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
            rectTransform.anchoredPosition = new Vector2(0.0f, 0.0f);
            

            switch (Num)
            {
                case 0: // 하단 디스플레이 1
                    rectTransform.sizeDelta = new Vector2(3840, 2160);
                    tempWindow.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(3840, 2160);
                    rectTransform.position = new Vector2(-5760.0f, 2180.0f);
                    m_objBottomLeftDisplay_00 = tempWindow;
                    
                    break;
                case 1: // 하단 디스플레이 2
                    rectTransform.sizeDelta = new Vector2(1920, 1080);
                    tempWindow.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
                    rectTransform.position = new Vector2(-5760.0f, 3900.0f);
                    m_objBottomLeftDisplay_01 = tempWindow;
                    break;
                case 2: // 상단 디스플레이
                    rectTransform.sizeDelta = new Vector2(3840, 2160);
                    tempWindow.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(3840, 2160);
                    rectTransform.position = new Vector2(-1920.0f, 2180.0f);
                    m_objTopRightDisplay = tempWindow;
                    break;
                case 3: // 프로젝션 디스플레이1
                    rectTransform.sizeDelta = new Vector2(3840, 2160);
                    tempWindow.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(3840.0f, 2160.0f);
                    tempWindow.transform.GetChild(0).GetComponent<RectTransform>().position = new Vector2(0.0f, 0.0f);
                    rectTransform.position = new Vector2(1920.0f, 0.0f);
                    m_objProjectionSource_00 = tempWindow;
                    break;
                case 4: // 프로젝션 디스플레이2
                    rectTransform.sizeDelta = new Vector2(3840, 2160);
                    tempWindow.transform.GetChild(0).GetComponent<RectTransform>().position = new Vector2(0.0f, 0.0f);
                    rectTransform.position = new Vector2(5760.0f, 0.0f);
                    m_objProjectionSource_01 = tempWindow;
                    break;
            }

            tempWindow.GetComponent<CUIPanel>().FadeInWindow();

        }
        #endregion
    }
}