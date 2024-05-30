using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CUIPanel : MonoBehaviour
    {
        private CanvasGroup m_CanvasGroup;
        private AudioSource m_AudioSource;
        public bool _bAnalyzingPanel = false;
        public bool _bCheckAutoIdleMode = true;
        private GameObject _EventMotion;
        private RectTransform m_rectTransform;
        // Start is called before the first frame update
        void Start()
        {
            m_CanvasGroup = transform.GetComponent<CanvasGroup>();
            if(transform.GetComponent<AudioSource>() != null)
                m_AudioSource = transform.GetComponent<AudioSource>();

        }
        private void Update()
        {
                
        }

        public void FadeInWindow()
        {

            ItweenEventStart("EventMoveUpdate", "FadeInComplete", 0.0f, 1.0f, CConfigMng.Instance._fTrasionsSpeed, 0.0f, iTween.EaseType.easeOutExpo);
        }

        public void FadeOutWindow()
        {
            ItweenEventStart("EventMoveUpdate", "FadeOutComplete", 1.0f, 0.0f, CConfigMng.Instance._fTrasionsSpeed, 0.0f, iTween.EaseType.easeOutExpo);
        }

        public void EventMoveUpdate(float fValue)
        {
            m_CanvasGroup.alpha = fValue;
            if(m_AudioSource != null)
            {
                m_AudioSource.volume = fValue;
            }
            
        }
        public void FadeInComplete()
        {

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