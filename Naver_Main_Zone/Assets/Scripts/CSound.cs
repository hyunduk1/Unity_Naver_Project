using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

namespace DemolitionStudios.DemolitionMedia
{
    public class CSound : MonoBehaviour
    {
        // Start is called before the first frame update
        public AudioSource _AudioSource;
        bool m_bPauseAudio = true;
        short CurrentNum;
        private void Awake()
        {
        }
        void Start()
        {
            CurrentNum = CUIPanelMng.Instance.m_nCurrentVideo;
            PlayAudio(CurrentNum - 1);
        }

        // Update is called once per frame
        void Update()
        {
            
        }
        public void IdleAudioEvent()
        {
            StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath00));
            _AudioSource.Play();
        }
        public void AudioPause(bool Pause)
        {
            if (Pause == true)
            {
                _AudioSource.Pause();
            }
            else
            {
                _AudioSource.Play();
            }
        }

        public void PlayAudio(int nInsertAudio)
        {
            if (nInsertAudio > CConfigMng.Instance._nVideoNum)
                nInsertAudio = 0;
            switch (nInsertAudio)
            {
                case 0:
                    if (CTCPNetWorkMng.Instance.m_bMbileIdleTrow == true)
                    {
                        IdleAudioEvent();
                        CTCPNetWorkMng.Instance.m_bMbileIdleTrow = false;
                    }
                        
                    break;
                case 1:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath01));
                    break;
                case 2:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath02));
                    break;
                case 3:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath03));
                    break;
                case 4:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath04));
                    break;
                case 5:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath05));
                    break;
                case 6:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath06));
                    break;
                case 7:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath07));
                    break;
                case 8:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath08));
                    break;
                case 9:
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._strSoundFolder, CConfigMng.Instance._strSoundPath09));
                    break;


            }
            
        }
        IEnumerator LoadAudioClipFromFile(string FolderPath, string filePath)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(FolderPath + filePath, AudioType.WAV))
            {
                yield return www.SendWebRequest();
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                _AudioSource.clip = audioClip;
                _AudioSource.Play();
                _AudioSource.loop = false;
            }
        }
    }
}
