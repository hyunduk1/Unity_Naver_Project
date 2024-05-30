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
        public bool m_bLangeMode = true; // TRUE = ����, FALSE = �ѹ� 

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
            //�����÷��̾ ���̿��� ������� ��Ŷ�� �޾Ƽ� �����ϴµ� �������� �װ� �ƴϴ� ������Ʈ�� ���̸� �����϶�
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
        //���� ���� ����
        void timedelay()
        {
            time = false;
            Debug.Log("1������ �÷��̰���");
        }

        //------------------------IDLE ���-------------------------------------
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

        //���̵� �Ѿ�ٰ� 37.2�� ���̵鿵��� ������ 1�������� Ʋ������
        public void FirstenetTime()
        {
            m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
            m_objCurrentObject.GetComponentInChildren<CSound>().IdleAudioEvent();

        }

        //-----------------------------------------------------------------

        //Ŭ���̾�Ʈ �÷��̾� ����---------------------------------------------------
        public void ClientCapTure()
        {
            ScreenShotImage();
            Debug.Log("-----Ŭ���̾�Ʈ ĸ��-----");
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
                        Debug.Log("ĸ��");
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
            Debug.Log("------------Puase ������ ���� ĸ��------------");
        }

        public void ClientDestroyCapture()
        {
            Debug.Log("-------------ĸ�� ����--------------");
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


        //-------------------------pause ���-----------------------------------

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
        //------------------------ĸ��--------------------------------------


        bool m_bCapDelay = true;
        void CapDelay()
        {
            m_bCapDelay = true;
        }
        // true �޴��� ���, false ������
        //true ĸ���ϰ� �������� �ȶ�� , false ĸ���ϰ� �������� �����
        public void CaptureVideo(bool IsMenualMode)
        {
            if (IsMenualMode == true)
            {
                if (m_bCapDelay == true)
                {
                    if (m_objCurrentObject != null)
                        m_objCurrentObject.GetComponent<CUIPanel>().FadeOutWindow();
                    Debug.Log("[������ �̹��� �ε�]");
                    FrameCaptureImage();
                    m_bCapDelay = false;
                }
                Invoke("CapDelay", 1.5f);
            }
            else
            {
                if (m_bCapDelay == true)
                {
                    Debug.Log("[������ �̹��� �ε�]");
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


                    //1�������� ���̵��̶� �̾��� �ִ�. ���Լ��� 0���� ȣ���ϸ� ������ FirstenetTime��
                    //�ð��ʷ� ������ �ȴ�. ���̵��� ����ϰ� ������ IdleVideoPlay() �Լ��� ȣ�� ȣ�� ������
                    // �κ�ũ�� 1.5�� ���̵� ���� true


                    //CPlugInNetWorkMng.Instance.Send() �� �����ũ�� ���� ���α׷��� �ٸ� ��ũ ���α׷����� �����ϱ� ����
                    //��ε� ĳ���ÿ�

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
                Debug.Log("���� �ε� ����");
            }
            screenshotTexture.LoadImage(imageBytes);

            // Raw �̹����� ��ũ���� ����
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
                Debug.Log("���� �ε� ����");
            }
            screenshotTexture.LoadImage(imageBytes);

            // Raw �̹����� ��ũ���� ����
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

            // CConfigMng.Instance._bIsMediaServer true = ����, false = Ŭ���̾�Ʈ 
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

            //������ �ð��� ���߸� Ŭ���̾�Ʈ�鵵 �ڵ������� �������� �����
            if (CConfigMng.Instance._bIsMediaServer == true)
            {
                tempWindow.GetComponentInChildren<CSound>().PlayAudio(SoundNum);
                //�����̸��� IDLE(1������) �϶�
                if (FilePath == CConfigMng.Instance._StrMediaIdle)
                {
                    //������ �ε巴�� �÷��̸� ���� HAP�ڵ� ������� ���� ���� ������ �ð���������
                    //��⿵��(m_bIsIdle) -> IDLE(1�� ����) �����ε� �ΰ��� ������ ���� �����Ϸ� ����
                    //IDLE ����(1������)�� �ٳ����� 1�������� �ð��ʼ��� ���� �������� ó��  
                    if (m_bIsIdle != true)
                        tempWindow.GetComponent<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
                }
            }

            m_objCurrentObject = tempWindow;
        }


        /// <summary>
        /// ������������ ĸó�� ���� �Լ���
        /// 
        ///  HAP�ڵ��� ����ϴµ� �������� �̵�� ���� LOOPING�� �Ƚ�Ű�� ����� ���� ������ �������̸� STOP ��Ű��
        ///  ������ �������� ĸ���ϰ� �̹����� ������ �̹��� �ε��ؼ� ������ ��ü ���� ���� ����� �� ������ ��ü�� ������
        ///  ���� : ������ ��ũ�� ���ߴµ� Enet�� ����ؼ� server�� �����÷��̾ ������ �ɶ� TCP�� ���� ��Ʈ��ũ�� ������
        ///      �¾, �׷��� ������ Destroy�� �Ǿ�� ���������� �����Ҽ� �ִ�. ����� fade In, Out ȿ���� ������ �ʱ� ������
        ///      �̾����� ȿ���� ����,  ������ �������� ĸ���ϰ� -> �ε��ϰ� -> ����ü�� �����ϰ� -> ���� ��ȣ�� ������ ->
        ///      ������ �����Ǹ鼭 ĸ���̹����� ��ü�� �����ϴ� ������� ����
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
                Debug.Log("���� ĸ�� �ѹ� : " + (m_nCurrentVideo - 1));
            }

            else
            {
                LoadImage(tempWindow.GetComponent<RawImage>(), m_nCurrentVideo - 1);
                Debug.Log("Ŭ���̾�Ʈ ĸ�� �ѹ� : " + (m_nCurrentVideo - 1));
            }
            m_objScreenShotObject = tempWindow;

        }

            //-------------------���� �Լ�-------------------------
            void CaptureLoadImage(RawImage rawImage)
            {
                string screenshotPath = CConfigMng.Instance._StrScreenCapture;
                Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height);

                byte[] imageBytes = System.IO.File.ReadAllBytes(screenshotPath);
                if (imageBytes == null)
                {
                    Debug.Log("���� �ε� ����");
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
