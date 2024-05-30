using UnityEngine;
using System.Collections;
namespace DemolitionStudios.DemolitionMedia
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
            FrameRect = new Rect(0, 50, w, h * 4 / 100);
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 4 / 100;
            style.normal.textColor = Color.red;

            style2 = new GUIStyle();
            style2.alignment = TextAnchor.UpperRight;
            style2.fontSize = h * 4 / 100;
            style2.normal.textColor = Color.green;

            StartCoroutine("worstReset");
        }

        IEnumerator worstReset() //코루틴으로 15초 간격으로 최저 프레임 리셋해줌.
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

        void OnGUI()//소스로 GUI 표시.
        {
            if (CConfigMng.Instance._bFpsToString == true)
            {
                msec = deltaTime * 1000.0f;
                fps = 1.0f / deltaTime;  //초당 프레임 - 1초에

                if (fps < worstFps)  //새로운 최저 fps가 나왔다면 worstFps 바꿔줌.
                    worstFps = fps;

                text = msec.ToString("F1") + "ms (" + fps.ToString("F1") + ") //worst : " + worstFps.ToString("F1");
                GUI.Label(rect, text, style);
                text2 = CUIPanelMng.Instance.CurrentVideoFrame(0).ToString() + "  /  " + CUIPanelMng.Instance.CurrentVideoFrame(1).ToString();
                if (CConfigMng.Instance._bIsMediaServer == true)
                {
                    GUI.Label(rect, text2, style2);
                }
                else
                {
                    style.normal.textColor = Color.green;
                    GUI.Label(FrameRect, text2, style);
                }
                

            }
            else
            {

            }
        }

    }
}