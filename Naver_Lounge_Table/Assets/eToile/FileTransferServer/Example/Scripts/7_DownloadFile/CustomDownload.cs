using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Simple example of requesting a file download using a custom name.
 * Please note that the resources must be into the shared folder in source devices.
 */

public class CustomDownload : MonoBehaviour
{
    FileTransferServer fts;
    FTSCore.FileRequest fileRequest;

    Dropdown validServerList;
    InputField fileNameInput;
    Image bar;
    Text percent;
    Text stats;

    // Use this for initialization
    void Start ()
    {
        fts = GameObject.Find("FileTransferServer").GetComponent<FileTransferServer>();
        fileNameInput = transform.Find("InputField").GetComponent<InputField>();
        validServerList = transform.Find("DropdownDevices").GetComponent<Dropdown>();

        Transform palenSend = transform.parent.Find("PanelSend");
        bar = palenSend.Find("DownloadBar").Find("Bar").GetComponent<Image>();
        bar.fillAmount = 0f;
        percent = palenSend.Find("DownloadBar").Find("Text").GetComponent<Text>();
        percent.text = "0%";
        stats = palenSend.Find("Stats").GetComponent<Text>();
        stats.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        // Update the progress bar:
        if (fileRequest != null)
        {
            bar.fillAmount = fileRequest.GetProgress();
            percent.text = Mathf.FloorToInt(bar.fillAmount * 100f) + "%";
        }
    }

    // Request the download:
    public void DownloadFile()
    {
        fileRequest = fts.RequestFile(validServerList.value, fileNameInput.text);
    }

    // Clear view and abort current downloads (UI ButtonDelete):
    public void Delete()
    {
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
            bar.fillAmount = 1f;
            percent.text = "100%";
            // Converto file size from bytes to Mega bytes, show download time and transfer rate:
            stats.text = "File: " + fileRequest._sourceName + " (" + (fileRequest._size / 1048576f).ToString("0.00") + "MB) Time: " + fileRequest._elapsedTime.ToString() + "s. TransferRate: " + fileRequest._transferRate.ToString() + "MB/s. ChunkSize: " + request._chunkSize.ToString() + "b. Retries: " + request._retries.ToString();
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
