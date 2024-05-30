using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
public class CScreenShot : MonoBehaviour
{
    // Start is called before the first frame update
    string aa;
    void Start()
    {
    } 

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))
        {
            ScreenShotImage();
        }
    }
    void ScreenShotImage()
    {
        string filePath = Path.Combine(Application.dataPath, CConfigMng.Instance._strVideoFormat);
        ScreenCapture.CaptureScreenshot(filePath);
    }
}
