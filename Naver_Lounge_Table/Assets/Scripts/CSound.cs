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
                case 1://부지
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound01, false));
                    Debug.Log("[1번파일 : 부지선정 요건 사운드]");
                    break;
                case 2://부지
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound02, false));
                    Debug.Log("[2번파일 : 데이터 부지 사운드]");
                    break;
                case 3://부지
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound03, false));
                    Debug.Log("[3번파일 : 남관/ 북관 사운드]");
                    break;

                case 4://O
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound04, false));
                    Debug.Log("[4번 파일 : 지반안정성 사운드]");
                    break;
                case 5://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound05, false));
                    Debug.Log("[5번 파일 : 내진설계 사운드]");
                    break;
                case 6://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound06, false));
                    Debug.Log("[6번 파일 : 소방 사운드]");
                    break;
                case 7://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound07, false));
                    Debug.Log("[7번 파일 : 전력수급 사운드]");
                    break;
                case 8://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound08, false));
                    Debug.Log("[8번파일 : 풍향 사운드]");
                    break;
                case 9://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound09, false));
                    Debug.Log("[8번파일 : 폐열활용 사운드]");
                    break;
                case 10://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound10, false));
                    Debug.Log("[8번파일 : 태양광 사운드]");
                    break;

                case 11://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound11, false));
                    Debug.Log("[11번파일 : 지열 에너지 사운드]");
                    break;
                case 12://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound12, false));
                    Debug.Log("[12번파일 : 재생 에너지 사운드]");
                    break;
                case 13://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound13, false));
                    Debug.Log("[13번파일 : 우수와 중수 사운드]");
                    break;
                case 14://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound14, false));
                    Debug.Log("[14번파일 : LEED 사운드]");
                    break;
                case 15://o
                    StartCoroutine(LoadAudioClipFromFile(CConfigMng.Instance._StrSound15, false));
                    Debug.Log("[15번파일 : Out 사운드]");
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