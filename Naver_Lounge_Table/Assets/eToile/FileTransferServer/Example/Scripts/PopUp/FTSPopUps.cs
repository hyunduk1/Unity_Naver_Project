using UnityEngine;

public class FTSPopUps : MonoBehaviour
{
    public GameObject popupFtsPrefab;   // Drag the PopUpFTS prefab here (in editor).
    // Generic error message:
    public void Error_PopUp(int code, string message)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        popup.GetComponent<PopUpFTS>().SetMessage("FTS Error (" + code.ToString() + "): " + message, transform);
    }
    
    // Generic download event information:
    public void RX_PopUp(FTSCore.FileRequest request)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        popup.GetComponent<PopUpFTS>().SetMessage("Received: " + request._sourceName + " (" + (request._size / 1048576f).ToString("0.00") + "MB t:" + request._elapsedTime.ToString("0.00") + "s tr:" + request._transferRate.ToString("0.00") + "MB/s) r:" + request._retries.ToString(), transform);
    }
    // Generic Forced downlaod begin information:
    public void ForcedDownload_PopUp(FTSCore.FileRequest request)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        if(request.GetStatus() == FTSCore.FileStatus.inactive)
            popup.GetComponent<PopUpFTS>().SetMessage("Forced download requested (requires confirmation): " + request._sourceName, transform, 0, request);
        else
            popup.GetComponent<PopUpFTS>().SetMessage("Download started: " + request._sourceName, transform, 0, request);
    }
    // Generic timedout upload message:
    public void RxTimeout_PopUp(FTSCore.FileRequest request)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        popup.GetComponent<PopUpFTS>().SetMessage("Download timeout: " + request._sourceName, transform);
    }
    // Generic download event information:
    public void FNF_PopUp(FTSCore.FileRequest request)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        popup.GetComponent<PopUpFTS>().SetMessage("File not found: " + request._sourceName, transform);
    }

    // Generic upload started message:
    public void TxBegin_PopUp(FTSCore.FileUpload upload)
    {
        if(upload != null)
        {
            // Instantiate the message:
            GameObject popup = GameObject.Instantiate(popupFtsPrefab);
            popup.transform.SetParent(transform.root, false);
            // Show the name of the downloaded file:
            popup.GetComponent<PopUpFTS>().SetMessage("Upload started: " + upload.GetName(), transform, 0, null, upload);
        }
    }
    // Generic finished upload message:
    public void TX_PopUp(FTSCore.FileUpload upload)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        popup.GetComponent<PopUpFTS>().SetMessage("Upload finished: " + upload.GetName(), transform);
    }
    // Generic timedout upload message:
    public void TxTimeout_PopUp(FTSCore.FileUpload upload)
    {
        // Instantiate the message:
        GameObject popup = GameObject.Instantiate(popupFtsPrefab);
        popup.transform.SetParent(transform.root, false);
        // Show the name of the downloaded file:
        popup.GetComponent<PopUpFTS>().SetMessage("Upload timeout: " + upload.GetName(), transform);
    }
}
