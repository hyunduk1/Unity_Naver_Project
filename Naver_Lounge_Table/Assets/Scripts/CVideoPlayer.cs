using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CVideoPlayer : MonoBehaviour
    {
        public Media _videoPlayer;
        public CUIPanel _UIPanel;
        // Start is called before the first frame update
        enum DISPLAY_NUMBER
        {
            DISPLYE_LEFT_01 = 0,
            DISPLYE_LEFT_02 = 1,
            DISPLYE_LEFT_03 = 2,
            DISPLYE_LEFT_04 = 3,

            DISPLYE_RIGHT_01 = 4,
            DISPLYE_RIGHT_02 = 5,
        }

        public int m_nTotalCount = 0;
        public int m_nCurrnetCount = 0;
        private float m_nStepFrame = 5.0f;

        private bool m_bAutoOncePlay = true;

        public bool IsLoop = false;

        void Start()
        {
            _videoPlayer.Events.AddListener(OnMediaPlayerEvent);
            StartCoroutine(DelayStart(1.0f));
            m_bAutoOncePlay = true;
        }
        public IEnumerator DelayStart(float DelayTime)
        {
            yield return new WaitForSeconds(DelayTime);
            _videoPlayer.Play();
        }
        // Update is called once per frame
        void Update()
        {
            IsVideoPlayerLoopMode();
            MenualMode(IsLoop);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                _videoPlayer.SeekToTime(_videoPlayer.CurrentTime - m_nStepFrame);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                _videoPlayer.SeekToTime(_videoPlayer.CurrentTime + m_nStepFrame);
            }
        }
        public void IsVideoPlayerLoopMode()
        {
            if (CUIPanelMng.Instance.m_nCurrentNum == 6 && CUIPanelMng.Instance.m_nCurrentNum == 7 &&
                CUIPanelMng.Instance.m_nCurrentNum == 8 && CUIPanelMng.Instance.m_nCurrentNum == 9)
            {
                IsLoop = true;
            }
            else
                IsLoop = false;
                

        }
        public void IsVideoLoops(bool Loop)
        {
            if(Loop == false)
            {
                if (_videoPlayer.VideoCurrentFrame > _videoPlayer.VideoNumFrames - 2)
                {
                    _videoPlayer.Pause();
                }
            }
            
        }
        public void VideoPause(bool Pause)
        {
            if (Pause == true)
                _videoPlayer.Pause();
            else
                _videoPlayer.Play();
        }

        public void AutoMode()
        {
            if (_videoPlayer.VideoNumFrames != 0)
            {
                if (_videoPlayer.VideoCurrentFrame >= _videoPlayer.VideoNumFrames - 2)
                {
                    Debug.Log("[오토모드 --- 다음영상]");
                    CUIPanelMng.Instance.m_nCurrentNum++;
                    

                    m_bAutoOncePlay = false;
                }
            }
        }

        private void MenualMode(bool IsLoop)
        {
            if (_videoPlayer.VideoNumFrames != 0)
            {
                if (_videoPlayer.VideoCurrentFrame >= _videoPlayer.VideoNumFrames - 2)
                {
                    if (IsLoop == false)
                        _videoPlayer.Pause();

                }
            }
            
        }
        public void InitiallizeContents(string strMovieName, bool IsLoop)
        {
            _videoPlayer.mediaUrl = strMovieName;
            _videoPlayer.openOnStart = true;
            IsVideoLoops(IsLoop);
        }

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
                case MediaEvent.Type.PlaybackSuspended:
                    
                    if (CUIPanelMng.Instance.m_bMenualMode == false)
                    {
                        CUIPanelMng.Instance.InsertMovie(CUIPanelMng.Instance.m_nCurrentNum);
                    }
                    break;
                case MediaEvent.Type.PlaybackResumed: break;
                case MediaEvent.Type.PlaybackNewLoop:

                    break;
                case MediaEvent.Type.PlaybackErrorOccured: break;
            }
        }


    }
}