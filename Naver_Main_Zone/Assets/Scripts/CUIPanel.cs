using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DemolitionStudios.DemolitionMedia
{
    public class CUIPanel : MonoBehaviour
    {
        private CanvasGroup m_CanvasGroup;
        private AudioSource m_AudioSource;
        public bool _bAnalyzingPanel = false;
        public bool _bCheckAutoIdleMode = true;
        private GameObject _EventMotion;

        // Start is called before the first frame update
        void Start()
        {
            m_CanvasGroup = transform.GetComponent<CanvasGroup>();
            m_AudioSource = transform.GetComponentInChildren<AudioSource>();
        }
        public void FadeInWindow()
        {

            ItweenEventStart("EventMoveUpdate", "FadeInComplete", 0.0f, 1.0f, CConfigMng.Instance._fTrasionsSpeed, CConfigMng.Instance._fDelayTime, iTween.EaseType.easeOutExpo);
        }

        public void FadeOutWindow()
        {
            ItweenEventStart("EventMoveUpdate", "FadeOutComplete", 1.0f, 0.0f, CConfigMng.Instance._fTrasionsSpeed, 0.0f, iTween.EaseType.easeOutExpo);
        }

        public void EventMoveUpdate(float fValue)
        {
            m_CanvasGroup.alpha = fValue;
            if(CConfigMng.Instance._bIsMediaServer == true)
                m_AudioSource.volume = fValue;
        }
        public void FadeInComplete()
        {
            CUIPanelMng.Instance.DestoryCapture();
        }

        public void FadeOutComplete()
        {
            Destroy(gameObject);
        }
        public void ItweenEventStart(string strUpdetName, string strCompleteName, float fValueA, float fValueB, float fSpeed, float fDelay, iTween.EaseType easyType)
        {
            iTween.ValueTo(gameObject, iTween.Hash("from", fValueA, "to", fValueB, "time", fSpeed, "delay", fDelay, "easetype", easyType.ToString(),
            "onUpdate", strUpdetName, "oncomplete", strCompleteName));
        }

    }
}
