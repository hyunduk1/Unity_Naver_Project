using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DownloadText : MonoBehaviour {

    FileTransferServer fts;

    Dropdown validServerList;
    FTSCore.FileRequest fileRequest;
    Text input;
    Image bar;
    Text percent;
    Text stats;

    // Use this for initialization
    void Start ()
    {
        fts = GameObject.Find("FileTransferServer").GetComponent<FileTransferServer>();
        validServerList = transform.Find("DropdownServers").GetComponent<Dropdown>();
        input = transform.Find("ReceivedText").Find("Text").GetComponent<Text>();

        bar = transform.Find("DownloadBar").Find("Bar").GetComponent<Image>();
        bar.fillAmount = 0f;
        percent = transform.Find("DownloadBar").Find("Text").GetComponent<Text>();
        percent.text = "0%";
        stats = transform.Find("Stats").GetComponent<Text>();
        stats.text = "";
    }

    // Update is called once per frame
    void Update ()
    {
        // Update the progress bar:
        if (fileRequest != null)
        {
            bar.fillAmount = fileRequest.GetProgress();
            percent.text = Mathf.FloorToInt(bar.fillAmount * 100f) + "%";
        }
    }

    // Request the file download (UI ButtonDownload):
    public void DownloadTextFile()
    {
        fileRequest = fts.RequestFile(validServerList.value, "text.txt");
    }

    // Clear view and abort current downloads (UI ButtonDelete):
    public void Delete()
    {
        input.text = "";
        bar.fillAmount = 0f;
        percent.text = "";
        stats.text = "";
        // Abort current download:
        if (fileRequest != null)
        {
            fileRequest.Abort();
            fileRequest = null;
        }
    }

    // FTS event: On Devices List Update ()
    public void UpdateDevicesList(List<FTSCore.RemoteDevice> devices)
    {
        validServerList.ClearOptions();
        validServerList.AddOptions(fts.GetDeviceIPList());
    }

    // FTS event: On File Download (FileRequest)
    public void ShowDownload(FTSCore.FileRequest request)
    {
        if (fileRequest != null && fileRequest.IsThis(request))
        {
            input.text = FileManagement.ReadFile<string>("text.txt");
            bar.fillAmount = 1f;
            percent.text = "100%";
            // Converto file size from bytes to Mega bytes, show download time and transfer rate:
            stats.text = (fileRequest._size / 1048576f).ToString("0.00") + "MB t:" + fileRequest._elapsedTime.ToString() + "s tr:" + fileRequest._transferRate.ToString() + "MB/s";
            fileRequest = null;
        }
    }

    // FTS event: On File Not Found ()
    public void FileNotFound(FTSCore.FileRequest request)
    {
        if (fileRequest != null && fileRequest.IsThis(request))
        {
            Delete();
            percent.text = "File not found";
        }
    }

    // FTS event: On File Timeout ()
    public void FileTimeout(FTSCore.FileRequest request)
    {
        if (fileRequest != null && fileRequest.IsThis(request))
        {
            Delete();
            percent.text = "File timeout";
        }
    }
}
