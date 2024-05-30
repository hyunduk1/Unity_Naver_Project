using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.Audio;


namespace DemolitionStudios.DemolitionMedia
{
    public class CUIPanelMng : MonoBehaviour
    {
        private static CUIPanelMng _instance;
        public static CUIPanelMng Instance { get { return _instance; } }

        private List<string> m_strPanelName = null;
        private Dictionary<string, GameObject> m_ListPrefabs;

        public GameObject _VideoNode;
        public GameObject _FrameCaptureNode;
        [NonSerialized]
        public GameObject m_objCurrentObject;
        private GameObject m_objScreenShotObject;


        public short m_nCurrentVideo = 0;

        public bool m_MenualMode = true;


        public bool m_bIsIdle = true;
        public bool m_bIsPause = false;
        public bool m_bClientPause = false;
        public bool m_bAutoMode = false;
        public bool m_bEndVideo = false;
        public bool m_bLangeMode = true; // TRUE = 영문, FALSE = 한문 

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

            m_ListPrefabs = new Dictionary<string, GameObject>();
            m_strPanelName = new List<string>();
            m_objCurrentObject = null;
            LoadPrefabs("", "00_PanelVideoServer");
            LoadPrefabs("", "01_PanelVideoClient");
            LoadPrefabs("", "02_ScreenShot");
        }

        public void Start()
        {
            InsertIdleMoviePanel(CConfigMng.Instance._StrMediaIdle, 0);
            m_nCurrentVideo++;
            m_bIsIdle = true;
            CTCPNetWorkMng.Instance.SendingContents(0);

        }
        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                IdleVideoPlay();
            }
            //비디어플레이어가 널이여도 모바일은 패킷을 받아서 실행하는데 리모컨은 그게 아니니 오브젝트가 널이면 실행하라
            if (m_objCurrentObject == null)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    if (time == false)
                    {
                        InsertMovie(m_nCurrentVideo);
                        time = true;
                        Invoke("timedelay", 2.0f);
                    }
                }
            }
        }

        private bool time = false;
        //유저 버그 방지
        void timedelay()
        {
            time = false;
            Debug.Log("1초지남 플레이가능");
        }

        //------------------------IDLE 기능-------------------------------------
        public void IdleVideoPlay()
        {
            if (m_bIsIdle == true)
                return;
            m_bIsPause = false;
            m_MenualMode = true;
            m_bIsIdle = true;
            CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_DESTROY_CAPTURE);
            Destroy(m_objScreenShotObject);

            m_nCurrentVideo = 0;
            InsertMovie(0);
            CTCPNetWorkMng.Instance.SendingContents(0);

            Invoke("SetTimeIdle", 2.0f);

        }
        public void SetTimeIdle()
        {
            if (m_objCurrentObject != null)
            {
                m_bIsIdle = true;
                m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(0);
                CTCPNetWorkMng.Instance.SendingContents(0);
            }
        }

        //아이들 넘어뛰고 37.2초 아이들영상과 합쳐진 1번영상을 틀기위해
        public void FirstenetTime()
        {
            m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
            m_objCurrentObject.GetComponentInChildren<CSound>().IdleAudioEvent();

        }

        //-----------------------------------------------------------------

        //클라이언트 플레이어 전용---------------------------------------------------
        public void ClientCapTure()
        {
            ScreenShotImage();
            Debug.Log("-----클라이언트 캡쳐-----");
            Invoke("FrameCaptureImage", CConfigMng.Instance._fFrameCapture);
        }

        public void ClientPauseCapture(bool bIsClientPause)
        {
            if (CConfigMng.Instance._bIsMediaServer == false)
            {
                if (m_bClientPause == true)
                {
                    if (bIsClientPause == true)
                    {
                        ScreenShotImage();
                        Debug.Log("캡쳐");
                        Invoke("FrameCaptureImage", CConfigMng.Instance._fFrameCapture);
                    }
                    else
                    {
                        Destroy(m_objScreenShotObject);
                    }

                    m_bClientPause = false;
                }
            }
        }

        public void PauseScreenShotCapture()
        {
            ScreenShotImage();
            Invoke("ScreenShotFrameCaptureImage", CConfigMng.Instance._fFrameCapture);
            Debug.Log("------------Puase 실행중 영상 캡쳐------------");
        }

        public void ClientDestroyCapture()
        {
            Debug.Log("-------------캡쳐 삭제--------------");
            Destroy(m_objScreenShotObject);
        }
        //----------------------------------------------------------------------
        public void LangeModeEN()
        {
            m_bLangeMode = true;
        }
        public void LangeModeKR()
        {
            m_bLangeMode = false;
        }


        //-------------------------pause 기능-----------------------------------

        public void NetWorkPauseEvent(bool bPause)
        {
            if (m_objCurrentObject != null)
                m_objCurrentObject.GetComponent<CMediaVideoPlayer>().PuaseModeVideo(bPause);

        }
        public void StopVideo()
        {
            if (m_objCurrentObject.GetComponent<iTween>() != null)
                return;
            if (m_bIsIdle == true)
                return;
            m_bIsPause = !m_bIsPause;
            Debug.Log("Pause MODE === " + m_bIsPause);

            m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().Pause = m_bIsPause;

            m_objCurrentObject.GetComponentInChildren<CSound>().AudioPause(m_bIsPause);

            if (m_bIsPause == true)
            {
                ScreenShotImage();
                Invoke("ClientFrameCaptureImage", CConfigMng.Instance._fFrameCapture);
                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_PAUSE_CAPTURE);
            }
            else
            {
                Destroy(m_objScreenShotObject);
                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_DESTROY_CAPTURE);
            }
        }
        //------------------------캡쳐--------------------------------------


        bool m_bCapDelay = true;
        void CapDelay()
        {
            m_bCapDelay = true;
        }
        // true 메뉴얼 모드, false 오토모드
        //true 캡쳐하고 다음영상 안띄움 , false 캡쳐하고 다음영상 띄워줌
        public void CaptureVideo(bool IsMenualMode)
        {
            if (IsMenualMode == true)
            {
                if (m_bCapDelay == true)
                {
                    if (m_objCurrentObject != null)
                        m_objCurrentObject.GetComponent<CUIPanel>().FadeOutWindow();
                    Debug.Log("[마지막 이미지 로드]");
                    FrameCaptureImage();
                    m_bCapDelay = false;
                }
                Invoke("CapDelay", 1.5f);
            }
            else
            {
                if (m_bCapDelay == true)
                {
                    Debug.Log("[마지막 이미지 로드]");
                    FrameCaptureImage();
                    InsertMovie(m_nCurrentVideo);
                    m_bCapDelay = false;
                    Invoke("CapDelay", 2.0f);
                }

            }
        }

        //------------------------------------------------------------------
        public float CurrentVideoFrame(short Num)
        {
            float[] Frame = new float[2];
            if (m_objCurrentObject != null)
            {
                Frame[0] = m_objCurrentObject.transform.GetChild(0).GetComponent<Media>().VideoCurrentFrame;
                Frame[1] = m_objCurrentObject.transform.GetChild(0).GetComponent<Media>().VideoNumFrames;
            }
            return Frame[Num];
        }


        public void LoadPrefabs(string strFolderName, string strFileName)
        {
            GameObject tempObject = Resources.Load("Prefabs/" + strFolderName + strFileName) as GameObject;

            if (tempObject != null)
            {
                m_ListPrefabs.Add(strFileName, tempObject);
                m_strPanelName.Add(strFileName);
            }
            else
            {

            }
        }



        public void InsertMovie(short VideoNum = 0)
        {
            if (m_objCurrentObject != null)
            {
                m_objCurrentObject.GetComponent<CUIPanel>().FadeOutWindow();
            }


            if (m_nCurrentVideo > CConfigMng.Instance._nVideoNum)
            {
                m_nCurrentVideo = 0;
                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_00);
                return;
            }
            m_nCurrentVideo = VideoNum;

            switch (VideoNum)
            {
                case 0:


                    //1번영상은 아이들이랑 이어져 있다. 이함수의 0번을 호출하면 무조건 FirstenetTime의
                    //시간초로 시작이 된다. 아이들을 사용하고 싶으면 IdleVideoPlay() 함수를 호출 호출 딜레이
                    // 인보크로 1.5초 아이들 상태 true


                    //CPlugInNetWorkMng.Instance.Send() 는 영상싱크의 서버 프로그램이 다른 싱크 프로그램에게 전달하기 위해
                    //브로드 캐스팅용

                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._StrMediaIdle, 1));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._StrKRMediaIdle, 1));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_00);
                    m_nCurrentVideo = 0;
                    CPlugInNetWorkMng.Instance.m_nCurrentCap = 1;

                    Invoke("FirstenetTime", 1.0f);

                    m_bIsIdle = false;

                    break;

                case 1:

                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMediaInTro, 2));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMediaInTro, 2));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_01);
                    m_nCurrentVideo = 1;
                    break;

                case 2:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia02, 3));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia02, 3));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_02);
                    m_nCurrentVideo = 2;
                    break;

                case 3:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia03, 4));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia03, 4));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_03);
                    m_nCurrentVideo = 3;
                    break;
                case 4:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia04, 5));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia04, 5));
                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_04);
                    m_nCurrentVideo = 4;
                    break;

                case 5:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia05, 6));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia05, 6));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_05);
                    m_nCurrentVideo = 5;
                    break;

                case 6:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia06, 7));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia06, 7));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_06);
                    m_nCurrentVideo = 6;
                    break;

                case 7:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia07, 8));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia07, 8));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_07);
                    m_nCurrentVideo = 7;
                    break;

                case 8:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia08, 9));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia08, 9));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_08);
                    m_nCurrentVideo = 8;
                    break;
                case 9:
                    if (m_bLangeMode == true)
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strMedia09, 10));
                    else
                        StartCoroutine(CurrentVideo(CConfigMng.Instance._strKRMedia09, 10));

                    CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_VIDEO_09);
                    m_nCurrentVideo = 9;
                    break;
            }
            m_nCurrentVideo++;
        }

        #region ScreenShot

        void ClientCaptureLoad(RawImage rawImage)
        {
            string screenshotPath = CConfigMng.Instance._StrScreenCapture;

            Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height);
            byte[] imageBytes = System.IO.File.ReadAllBytes(screenshotPath);
            if (imageBytes == null)
            {
                Debug.Log("파일 로드 실패");
            }
            screenshotTexture.LoadImage(imageBytes);

            // Raw 이미지에 스크린샷 설정
            rawImage.texture = screenshotTexture;
        }
        void LoadImage(RawImage rawImage, int num)
        {
            if (num < 0)
                num = 0;
            if (num > CConfigMng.Instance._nVideoNum)
            {
                num = 0;
            }


            string screenshotPath = "";
            switch (num)
            {
                case 0:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure0;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure0;
                    break;
                case 1:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure1;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure1;
                    break;
                case 2:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure2;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure2;
                    break;
                case 3:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure3;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure3;
                    break;
                case 4:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure4;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure4;
                    break;
                case 5:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure5;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure5;
                    break;
                case 6:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure6;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure6;
                    break;
                case 7:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure7;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure7;
                    break;
                case 8:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure8;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure8;
                    break;
                case 9:
                    if (m_bLangeMode == true)
                        screenshotPath = CConfigMng.Instance._strCapTure9;
                    else
                        screenshotPath = CConfigMng.Instance._strKRCapTure9;
                    break;
            }

            Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height);
            byte[] imageBytes = System.IO.File.ReadAllBytes(screenshotPath);
            if (imageBytes == null)
            {
                Debug.Log("파일 로드 실패");
            }
            screenshotTexture.LoadImage(imageBytes);

            // Raw 이미지에 스크린샷 설정
            rawImage.texture = screenshotTexture;
        }
        #endregion

        #region CurrentPlayVideo
        public void NetWorkSendCurrentVideo(string FileName, short Num = 1)
        {
            if (m_objCurrentObject != null)
                m_objCurrentObject.GetComponent<CUIPanel>().FadeOutWindow();
            StartCoroutine(CurrentVideo(FileName, Num));

        }
        //--------------------------------------------------------------------------------------------
        IEnumerator CurrentVideo(string CurrentFileName, short AudioNum = 1)
        {
            if (AudioNum == 0)
                m_bIsIdle = true;
            else
                m_bIsIdle = false;

            yield return new WaitForSeconds(CConfigMng.Instance._fItweenDelay + CConfigMng.Instance._fTrasionsSpeed + 0.1f);
            InsertIdleMoviePanel(CurrentFileName, AudioNum);

        }


        public void DestoryCapture()
        {
            if (m_objScreenShotObject != null)
            {
                Destroy(m_objScreenShotObject);
            }
        }
        #endregion

        #region Instantiate Prefabs

        public void InsertIdleMoviePanel(string FilePath, short SoundNum)
        {
            if (m_objCurrentObject != null)
            {
                m_objCurrentObject.GetComponent<CUIPanel>().FadeOutWindow();
            }
            GameObject tempWindow;

            // CConfigMng.Instance._bIsMediaServer true = 서버, false = 클라이언트 
            if (CConfigMng.Instance._bIsMediaServer)
            {
                tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["00_PanelVideoServer"]) as GameObject;
                tempWindow.GetComponentInChildren<LFOClockNetworkEnetServer>().Speed = CConfigMng.Instance._fServerVideoSpeed;
            }
            else
            {
                tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["01_PanelVideoClient"]) as GameObject;
            }

            tempWindow.transform.SetParent(_VideoNode.transform);

            tempWindow.GetComponentInChildren<Media>().mediaUrl = FilePath;
            RectTransform rectTransform = tempWindow.transform.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(CConfigMng.Instance._ScreenSizeX, CConfigMng.Instance._ScreenSizeY);
            tempWindow.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(CConfigMng.Instance._ScreenSizeX, CConfigMng.Instance._ScreenSizeY);
            rectTransform.anchoredPosition = new Vector2(0, 0.0f);
            rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
            tempWindow.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            tempWindow.GetComponent<CUIPanel>().FadeInWindow();

            //서버가 시간을 맞추면 클라이언트들도 자동적으로 프레임이 맞춰옴
            if (CConfigMng.Instance._bIsMediaServer == true)
            {
                tempWindow.GetComponentInChildren<CSound>().PlayAudio(SoundNum);
                //파일이름이 IDLE(1번영상) 일때
                if (FilePath == CConfigMng.Instance._StrMediaIdle)
                {
                    //영상이 부드럽게 플레이를 위해 HAP코덱 사용으로 인해 영상 렌더링 시간부족으로
                    //대기영상(m_bIsIdle) -> IDLE(1번 파일) 영상인데 두개의 영상을 합쳐 한파일로 받음
                    //IDLE 영상(1번영상)이 다끝나고 1번파일의 시간초수로 돌아 가기위한 처리  
                    if (m_bIsIdle != true)
                        tempWindow.GetComponent<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
                }
            }

            m_objCurrentObject = tempWindow;
        }


        /// <summary>
        /// 마지막프레임 캡처를 위한 함수들
        /// 
        ///  HAP코덱을 사용하는데 데몰리션 미디어 에선 LOOPING을 안시키는 기능이 없어 영상이 끝프레이면 STOP 시키고
        ///  마지막 프레임을 캡쳐하고 이미지로 저장후 이미지 로드해서 영상의 객체 위에 덮어 씌우고 그 영상의 객체는 삭제함
        ///  이유 : 영상의 싱크를 맞추는데 Enet을 사용해서 server의 영상플레이어가 생성이 될때 TCP의 서버 네트워크를 가지고
        ///      태어남, 그러면 영상이 Destroy가 되어야 다음영상을 생성할수 있다. 고객사는 fade In, Out 효과를 원하지 않기 때문에
        ///      이어지는 효과를 위해,  마지막 프레임을 캡쳐하고 -> 로드하고 -> 영상객체를 삭제하고 -> 다음 신호를 받으면 ->
        ///      영상이 생성되면서 캡쳐이미지의 객체를 삭제하는 방식으로 개발
        /// </summary>


        public void FrameCaptureImage()
        {
            if (m_objScreenShotObject != null)
            {
                return;
            }
            if (m_objCurrentObject != null)
            {
                m_objCurrentObject.GetComponent<CUIPanel>().FadeOutWindow();
            }

            GameObject tempWindow;
            tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["02_ScreenShot"]) as GameObject;
            tempWindow.transform.SetParent(_FrameCaptureNode.transform);

            RectTransform rectTransform = tempWindow.transform.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(CConfigMng.Instance._ScreenSizeX, CConfigMng.Instance._ScreenSizeY);
            rectTransform.anchoredPosition = new Vector2(0, 0.0f);
            rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
            tempWindow.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            if (CConfigMng.Instance._bIsMediaServer == true)
            {
                LoadImage(tempWindow.GetComponent<RawImage>(), m_nCurrentVideo - 1);
                Debug.Log("서버 캡쳐 넘버 : " + (m_nCurrentVideo - 1));
            }

            else
            {
                LoadImage(tempWindow.GetComponent<RawImage>(), m_nCurrentVideo - 1);
                Debug.Log("클라이언트 캡쳐 넘버 : " + (m_nCurrentVideo - 1));
            }
            m_objScreenShotObject = tempWindow;

        }

            //-------------------스샷 함수-------------------------
            void CaptureLoadImage(RawImage rawImage)
            {
                string screenshotPath = CConfigMng.Instance._StrScreenCapture;
                Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height);

                byte[] imageBytes = System.IO.File.ReadAllBytes(screenshotPath);
                if (imageBytes == null)
                {
                    Debug.Log("파일 로드 실패");
                    return;
                }
                screenshotTexture.LoadImage(imageBytes);
                rawImage.texture = screenshotTexture;
            }

            public void ScreenShotImage()
            {
                string filePath = Path.Combine(Application.dataPath, CConfigMng.Instance._StrScreenCapture);
                ScreenCapture.CaptureScreenshot(filePath);
            }

            public void ScreenShotFrameCaptureImage()
            {
                if (m_objScreenShotObject != null)
                {
                    return;
                }
                ScreenShotImage();
                GameObject tempWindow;
                tempWindow = MonoBehaviour.Instantiate(m_ListPrefabs["02_ScreenShot"]) as GameObject;
                tempWindow.transform.SetParent(_FrameCaptureNode.transform);

                RectTransform rectTransform = tempWindow.transform.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(CConfigMng.Instance._ScreenSizeX, CConfigMng.Instance._ScreenSizeY);
                rectTransform.anchoredPosition = new Vector2(0, 0.0f);
                rectTransform.anchoredPosition3D = new Vector3(0.0f, 0.0f, 0.0f);
                tempWindow.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                CaptureLoadImage(tempWindow.GetComponent<RawImage>());
                m_objScreenShotObject = tempWindow;
            }

            #endregion
        }
    }
