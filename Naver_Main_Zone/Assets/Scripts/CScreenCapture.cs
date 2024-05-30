using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class CScreenCapture : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void ScreenShotImage()
    {
        string filePath = Path.Combine(Application.dataPath, CConfigMng.Instance._StrScreenCapture);
        ScreenCapture.CaptureScreenshot(filePath);
    }
    public void LoadImage(RawImage rawImage)
    {
        string screenshotPath = CConfigMng.Instance._StrScreenCapture;
        Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height);

        byte[] imageBytes = System.IO.File.ReadAllBytes(screenshotPath);
        screenshotTexture.LoadImage(imageBytes);

        // Raw 이미지에 스크린샷 설정
        rawImage.texture = screenshotTexture;
    }


}
