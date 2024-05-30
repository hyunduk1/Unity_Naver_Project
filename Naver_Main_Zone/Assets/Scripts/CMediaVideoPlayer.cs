using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DemolitionStudios.DemolitionMedia
{
    public class CMediaVideoPlayer : MonoBehaviour
    {
        public Media _VideoPlayer;
        public LFOClockNetworkEnetServer _EnetServer;
        public CSound _CSound;

        private bool m_bAutoOncePlay = true;
        private float m_fStopFrameRate = 0.0f;
        // Start is called before the first frame update
        void Start()
        {
            _VideoPlayer.Events.AddListener(OnMediaPlayerEvent);
            if (CUIPanelMng.Instance.m_nCurrentVideo == 1)
            {
                if (CUIPanelMng.Instance.m_bIsIdle == false)
                {
                    CUIPanelMng.Instance.m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
                    
                    CTCPNetWorkMng.Instance.SendingContents(1);
                }
            }
        }
        
        // Update is called once per frame
        void Update()
        {
            if(CConfigMng.Instance._bIsMediaServer == false)
                return;
            NextNumTimeVideo();
            AutoPlayMode();

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                CUIPanelMng.Instance.m_bIsPause = !CUIPanelMng.Instance.m_bIsPause;
                PuaseModeVideo(CUIPanelMng.Instance.m_bIsPause);
            }
        }


        #region AutoMode
        
        void AutoOncePlay()
        {
            m_bAutoOncePlay = true;
        }
        void IdleCapNum()
        {
            CPlugInNetWorkMng.Instance.m_nCurrentCap = 0;
        }
        
        void AutoPlayMode()
        {
            if(CUIPanelMng.Instance.m_MenualMode == false)
            {
                if (CUIPanelMng.Instance.m_bIsIdle == true)
                {
                    CTCPNetWorkMng.Instance.SendingContents(1);
                    CUIPanelMng.Instance.m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
                    _CSound.IdleAudioEvent();
                    CUIPanelMng.Instance.m_bIsIdle = false;
                    Invoke("IdleCapNum", 5.0f);
                }
                else
                {
                    if (_VideoPlayer.VideoNumFrames != 0)
                    {
                        if (_VideoPlayer.VideoCurrentFrame >= _VideoPlayer.VideoNumFrames - 4)
                        {
                            if (m_bAutoOncePlay == true)
                            {
                                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_CAPTURE);
                                CUIPanelMng.Instance.CaptureVideo(false);
                               
                                Debug.Log("오토캡쳐");
                                m_bAutoOncePlay = false;
                                Invoke("AutoOncePlay,", 3.0f);
                            }
                            
                        }
                    }
                }
            }
        }
        #endregion

        #region MenualMode
        //--------------메뉴얼 모드 CUIPanelMng.Instance.m_MenualMode == true 일때만 작용---------------
        void NextNumTimeVideo()
        {
            //메뉴얼 모드일때만
            if (CUIPanelMng.Instance.m_MenualMode == true)
            {
                // 프레임이 0이 아닐때만 유저버그 방지
                if (_VideoPlayer.VideoNumFrames != 0)
                {
                    //메뉴얼 모드일때, 프레임이 0이 아닐때(유저버그 방지), idle 모드일때
                    //idle 영상이 두개의 영상이 합쳐진 영상인데 분리하기 위해 어느 프레임 이상으로 가면
                    // 0프레임으로 돌아가기 위해
                    if(CUIPanelMng.Instance.m_bIsIdle == true)
                    {
                        if (_VideoPlayer.VideoCurrentFrame > CConfigMng.Instance._nIdleLastPlay)
                            _EnetServer.SetTime(0.0f);

                        if (Input.GetKeyDown(KeyCode.RightArrow))
                        {
                            if (transform.GetComponent<iTween>() != null)
                                return;

                            //idle 모드에서 다음영상을 누르면 1번영상으로 시간 변경 + 오디오 객체 생성
                            CUIPanelMng.Instance.m_objCurrentObject.GetComponentInChildren<LFOClockNetworkEnetServer>().SetTime(CConfigMng.Instance._fIdleSecondePlay);
                            CUIPanelMng.Instance.m_objCurrentObject.GetComponentInChildren<CSound>().IdleAudioEvent();
                            CUIPanelMng.Instance.m_bIsIdle = false;
                            CUIPanelMng.Instance.m_bIsPause = false;
                            Invoke("IdleCapNum", 5.0f); 
                        }
                    }
                    else
                    {
                        if (_VideoPlayer.VideoCurrentFrame > _VideoPlayer.VideoNumFrames - 3)
                        {
                            if (_EnetServer != null)
                                _EnetServer.SetTime(_VideoPlayer.VideoNumFrames / 30.3);
                            CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_CAPTURE);
                            CUIPanelMng.Instance.CaptureVideo(true);
                        }
                        else
                        {
                            //그전에 누르면 검은화면에서 다음화면으로 
                            if (Input.GetKeyDown(KeyCode.RightArrow))
                            {
                                if (time == false)
                                {
                                    Invoke("RigitKeyTimeDealy", 2.0f);
                                    CUIPanelMng.Instance.m_bIsIdle = false;
                                    CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentVideo);
                                    time = true;
                                }
                                
                            }
                        }
                    }
                }
            }
        }
        bool time = false;
        void RigitKeyTimeDealy()
        {
            time = false;
            Debug.Log("2초지남 플레이가능");
        }
        #endregion

        #region PauseMode
        //-----------------------------PauseMode------------------------------------


        public void PuaseModeVideo(bool bPause)
        {
            _CSound.AudioPause(bPause);
            _EnetServer.Pause = bPause;
            if (bPause == true)
            {
                CUIPanelMng.Instance.PauseScreenShotCapture();
                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_PAUSE_CAPTURE);
            }
            else
            {
                CUIPanelMng.Instance.DestoryCapture();
                CPlugInNetWorkMng.Instance.Send(PROTOCOL.MSG_CLIENT_DESTROY_CAPTURE);
            }
                
        }

        #endregion

        #region VideoPlayerEvent
        public void OnMediaPlayerEvent(Media source, MediaEvent.Type type, MediaError error)
        {
            //Debug.Log("[들어온이벤트]   :   " + type.ToString());
            switch (type)
            {
                case MediaEvent.Type.Closed:
                    //CUIPanelMng.Instance.FrameCaptureImage();
                    break;

                case MediaEvent.Type.OpeningStarted: break;
                case MediaEvent.Type.PreloadingToMemoryStarted: break;
                case MediaEvent.Type.PreloadingToMemoryFinished: break;
                case MediaEvent.Type.Opened:
                    break;
                case MediaEvent.Type.OpenFailed: break;
                case MediaEvent.Type.VideoRenderTextureCreated: break;
                case MediaEvent.Type.PlaybackStarted: break;
                case MediaEvent.Type.PlaybackStopped:
                    break;
                case MediaEvent.Type.PlaybackEndReached: break;
                case MediaEvent.Type.PlaybackSuspended: break;
                case MediaEvent.Type.PlaybackResumed: break;
                case MediaEvent.Type.PlaybackNewLoop: 

                    break;
                case MediaEvent.Type.PlaybackErrorOccured: break;
            }
        }
        #endregion
    }
}
