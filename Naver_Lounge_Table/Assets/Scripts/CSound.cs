using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CSound : MonoBehaviour
    {
        public AudioSource _AudioSource;
        // Start is called before the first frame update
        void Start()
        {
            Invoke("SoundDealyStart", CConfigMng.Instance._fSoundDelay);
        }
        void SoundDealyStart()
        {
            _AudioSource.Play();
        }
        // Update is called once per frame
        void Update()
        {
        }

        public void PlayAudio(short nInsertAudio)
        {
            switch (nInsertAudio)
            {
                case 1://����
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound01, false));
                    Debug.Log("[1������ : �������� ��� ����]");
                    break;
                case 2://����
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound02, false));
                    Debug.Log("[2������ : ������ ���� ����]");
                    break;
                case 3://����
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound03, false));
                    Debug.Log("[3������ : ����/ �ϰ� ����]");
                    break;

                case 4://O
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound04, false));
                    Debug.Log("[4�� ���� : ���ݾ����� ����]");
                    break;
                case 5://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound05, false));
                    Debug.Log("[5�� ���� : �������� ����]");
                    break;
                case 6://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound06, false));
                    Debug.Log("[6�� ���� : �ҹ� ����]");
                    break;
                case 7://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound07, false));
                    Debug.Log("[7�� ���� : ���¼��� ����]");
                    break;
                case 8://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound08, false));
                    Debug.Log("[8������ : ǳ�� ����]");
                    break;
                case 9://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound09, false));
                    Debug.Log("[8������ : ��Ȱ�� ����]");
                    break;
                case 10://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound10, false));
                    Debug.Log("[8������ : �¾籤 ����]");
                    break;

                case 11://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound11, false));
                    Debug.Log("[11������ : ���� ������ ����]");
                    break;
                case 12://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound12, false));
                    Debug.Log("[12������ : ��� ������ ����]");
                    break;
                case 13://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound13, false));
                    Debug.Log("[13������ : ����� �߼� ����]");
                    break;
                case 14://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound14, false));
                    Debug.Log("[14������ : LEED ����]");
                    break;
                case 15://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound15, false));
                    Debug.Log("[15������ : Out ����]");
                    break;

            }
        }

        private IEnumerator LoadAudioClipFromFile(string FolderPath, bool Loop)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(FolderPath, AudioType.WAV))
            {
                yield return www.SendWebRequest();
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                _AudioSource.clip = audioClip;
                _AudioSource.loop = false;
            }
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
    }
}