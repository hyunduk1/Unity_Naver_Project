using UnityEngine;

namespace DemolitionStudios.DemolitionMedia
{
    class Utils
    {
        public static void HandleKeyboardVsyncAndScale()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                QualitySettings.vSyncCount = 1;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 120;
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                ToggleVideoScale();
            }
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleBorderlessFullscreen();
            }
        }

        static void ToggleBorderlessFullscreen(int deltaX = 9, int deltaY = -9)
        {
            if (BorderlessWindow.framed)
            {
                BorderlessWindow.MaximizeWindow();
                BorderlessWindow.SetFramelessWindow();
                BorderlessWindow.MoveWindowPos(new Vector2Int(deltaX, deltaY),
                                               Screen.currentResolution.width,
                                               Screen.currentResolution.height);
            }       
            else
            {
                BorderlessWindow.SetFramedWindow();
                BorderlessWindow.MaximizeWindow();
            }
        }

        static void ToggleVideoScale()
        {
            RenderToIMGUI render = Camera.main.GetComponent(typeof(RenderToIMGUI)) as RenderToIMGUI;
            if (render.size.x < 0.9)
            {
                render.position = new Vector2(0f, 0f);
                render.size = new Vector2(1f, 1f);
            }
            else
            {
                render.position = new Vector2(0.3f, 0.3f);
                render.size = new Vector2(0.7f, 0.7f);
            }
        }
    }
}
