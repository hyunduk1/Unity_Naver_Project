using UnityEngine;
using System.Collections;
namespace DemolitionStudios.DemolitionMedia.Examples
{
    public class CFPSDisplay : MonoBehaviour
    {
        float deltaTime = 0.0f;


        GUIStyle style;
        GUIStyle style2;
        Rect rect;
        Rect FrameRect;
        float msec;
        float fps;
        float worstFps = 100f;
        string text;
        string text2;

        void Awake()
        {

            int w = Screen.width, h = Screen.height;
            rect = new Rect(0, 0, w, h * 4 / 100);
            FrameRect = new Rect(0, 100, w, h * 4 / 100);
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 4 / 100;
            style.normal.textColor = Color.red;

            style2 = new GUIStyle();
            style2.alignment = TextAnchor.UpperLeft;
            style2.fontSize = h * 4 / 100;
            style2.normal.textColor = Color.red;

            StartCoroutine("worstReset");
        }

        IEnumerator worstReset() //�ڷ�ƾ���� 15�� �������� ���� ������ ��������.
        {
            while (true)
            {
                yield return new WaitForSeconds(15f);
                worstFps = 100f;
            }
        }


        void Update()
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()//�ҽ��� GUI ǥ��.
        {
            
            if (CConfigMng.Instance._bFpsToString == true)
                return;
            msec = deltaTime * 1000.0f;
            fps = 1.0f / deltaTime;  //�ʴ� ������ - 1�ʿ�

            if (fps < worstFps)  //���ο� ���� fps�� ���Դٸ� worstFps �ٲ���.
                worstFps = fps;

            text = msec.ToString("F1") + "ms (" + fps.ToString("F1") + ") //worst : " + worstFps.ToString("F1");
            if(CUIPanelMng.Instance.m_objBottomLeftDisplay_00 != null)
            {
                text2 = CUIPanelMng.Instance.m_objBottomLeftDisplay_00.GetComponentInChildren<Media>().VideoCurrentFrame + " / " + CUIPanelMng.Instance.m_objBottomLeftDisplay_00.GetComponentInChildren<Media>().VideoNumFrames;
            }
            
            GUI.Label(rect, text, style);
            
            GUI.Label(FrameRect, text2, style2);
            if (CConfigMng.Instance._bFpsToString == true)
            {
                
                /*if (CConfigMng.Instance._bIsMediaServer == true)
                {
                    
                }
                else
                {
                    style.normal.textColor = Color.green;
                    GUI.Label(FrameRect, text2, style);
                }*/
                

            }
            else
            {

            }
        }

    }
}