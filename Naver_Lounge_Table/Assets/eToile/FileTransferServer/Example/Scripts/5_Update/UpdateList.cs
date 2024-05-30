using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/*
 * Simultaneous downloads.
 */

public class UpdateList : MonoBehaviour {

    FileTransferServer fts;

    Dropdown validServerList;
    FTSCore.FileRequest fileRequest1;
    FTSCore.FileRequest fileRequest2;
    Transform statusPanel;
    // First item:
    RawImage rImage1;
    Image bar1;
    Text percent1;
    Text stats1;
    // Second item:
    RawImage rImage2;
    Image bar2;
    Text percent2;
    Text stats2;

    // Use this for initialization
    void Start ()
    {
        fts = GameObject.Find("FileTransferServer").GetComponent<FileTransferServer>();
        validServerList = transform.Find("DropdownServers").GetComponent<Dropdown>();
        statusPanel = transform.parent.Find("PanelStatus");
        // First item group:
        rImage1 = statusPanel.Find("RawImage1").GetComponent<RawImage>();
        bar1 = transform.Find("DownloadBar1").Find("Bar").GetComponent<Image>();
        percent1 = transform.Find("DownloadBar1").Find("Text").GetComponent<Text>();
        bar1.fillAmount = 0f;
        percent1.text = "0%";
        stats1 = transform.Find("Stats1").GetComponent<Text>();
        stats1.text = "";
        stats2 = transform.Find("Stats2").GetComponent<Text>();
        stats2.text = "";
        // Second item group:
        rImage2 = statusPanel.Find("RawImage2").GetComponent<RawImage>();
        bar2 = transform.Find("DownloadBar2").Find("Bar").GetComponent<Image>();
        percent2 = transform.Find("DownloadBar2").Find("Text").GetComponent<Text>();
        bar2.fillAmount = 0f;
        percent2.text = "0%";
    }

    // Update is called once per frame
    void Update ()
    {
        // Update the progress bar 1:
        if (fileRequest1 != null)
        {
            bar1.fillAmount = fileRequest1.GetProgress();
            percent1.text = Mathf.FloorToInt(bar1.fillAmount * 100f) + "%";
        }
        // Update the progress bar 2:
        if (fileRequest2 != null)
        {
            bar2.fillAmount = fileRequest2.GetProgress();
            percent2.text = Mathf.FloorToInt(bar2.fillAmount * 100f) + "%";
        }
    }

    // Request simultaneous downloads:
    public void StartUpdate()
    {
        fileRequest1 = fts.RequestFile(validServerList.value, "example.png");
        fileRequest2 = fts.RequestFile(validServerList.value, "capture.jpg");
    }

    // Clear the view window:
    public void Delete()
    {
        rImage1.texture = null;
        bar1.fillAmount = 0f;
        percent1.text = "";
        stats1.text = "";
        if(fileRequest1 != null)
        {
            fileRequest1.Abort();
            fileRequest1 = null;
        }

        rImage2.texture = null;
        bar2.fillAmount = 0f;
        percent2.text = "";
        stats2.text = "";
        if (fileRequest2 != null)
        {
            fileRequest2.Abort();
            fileRequest2 = null;
        }
    }

    // FTS event: On Devices List Update ()
    public void UpdateDevicesList(List<FTSCore.RemoteDevice> devices)
    {
        validServerList.ClearOptions();
        validServerList.AddOptions(fts.GetDeviceIPList());
    }

    public void ShowDownload(FTSCore.FileRequest request)
    {
        // This function is called from the "OnFileDownload" event:
        if (fileRequest1 != null && fileRequest1.IsThis(request))
        {
            rImage1.texture = FileManagement.ImportTexture(fileRequest1._saveName);
            bar1.fillAmount = 1f;
            percent1.text = "100%";
            // Converto file size from bytes to Mega bytes, show download time and transfer rate:
            stats1.text = (fileRequest1._size / 1048576f).ToString("0.00") + "MB t:" + fileRequest1._elapsedTime.ToString() + "s tr:" + fileRequest1._transferRate.ToString() + "MB/s";
            fileRequest1 = null;
        }
        if (fileRequest2 != null && fileRequest2.IsThis(request))
        {
            rImage2.texture = FileManagement.ImportTexture(fileRequest2._saveName);
            bar2.fillAmount = 1f;
            percent2.text = "100%";
            // Converto file size from bytes to Mega bytes, show download time and transfer rate:
            stats2.text = (fileRequest2._size / 1048576f).ToString("0.00") + "MB t:" + fileRequest2._elapsedTime.ToString() + "s tr:" + fileRequest2._transferRate.ToString() + "MB/s";
            fileRequest2 = null;
        }
    }

    public void FileNotFound(FTSCore.FileRequest request)
    {
        // This function is called from the "OnFileDownload" event:
        if (fileRequest1 != null && fileRequest1.IsThis(request))
        {
            percent1.text = "File not found";
            rImage1.texture = null;
            fileRequest1 = null;
        }
        if (fileRequest2 != null && fileRequest2.IsThis(request))
        {
            percent2.text = "File not found";
            rImage2.texture = null;
            fileRequest2 = null;
        }
    }

    public void FileTimeout(FTSCore.FileRequest request)
    {
        // This function is called from the "OnFileDownload" event:
        if (fileRequest1 != null && fileRequest1.IsThis(request))
        {
            percent1.text = "File timeout";
            rImage1.texture = null;
            fileRequest1 = null;
        }
        if (fileRequest2 != null && fileRequest2.IsThis(request))
        {
            percent2.text = "File timeout";
            rImage2.texture = null;
            fileRequest2 = null;
        }
    }
}
