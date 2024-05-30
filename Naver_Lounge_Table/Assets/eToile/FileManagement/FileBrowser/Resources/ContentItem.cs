using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/*
 * This script controls every item displayed in the browser window.
 * Items represents files and folders contained in the displayed path.
 * 
 * Parameters of SetItem():
 * - parent: the transform if the list container, will parent automatically.
 * - name: the name of the item (file or folder name)
 * - type: The item may represent three types of item (F: file, D: directory or folder, I: information).
 * - thumb: Attempts to load a thumbnail from the file (images only).
 */

public class ContentItem : MonoBehaviour
{
    Toggle _toggle;
    Text _nameLabel;
    FileBrowser _browser;
    string _type;           // Remembers what kind of item is representing ("F" or "D").

    // Double click recognition:
    float doubleClickTimeLimit = 0.3f;
    bool clickedOnce = false;
    float count = 0f;

    /// <summary>Set Item properties (called from outside)</summary>
    public void SetItem(FileBrowser browser, Transform parent, string name, string type, bool thumb = false)
    {
        _type = type;
        transform.SetParent(parent, false);
        _toggle = gameObject.GetComponent<Toggle>();
        _toggle.group = parent.GetComponent<ToggleGroup>();
        _nameLabel = transform.Find("Label").GetComponent<Text>();
        _nameLabel.text = name;
        Image icon = transform.Find("Icon").GetComponent<Image>();
        _browser = browser;
        // The _icons[] array is by default like this:
        // [0]: 0_info
        // [1]: 1_folder
        // [2]: 2_file
        // [3]: 3_text_file
        // [4]: 4_image_file
        // [5]: 5_audio_file
        // [6]: 6_settings_file
        // All icons are placed in: Assets/eToile/FileManagement/FileBrowser/Resources/Icons
        if (type == "I")
            icon.sprite = _browser._icons[0];
        else if (type == "D")
            icon.sprite = _browser._icons[1];
        else if (type == "F")
        {
            // Set icon depending on file extension:
            switch (FileManagement.GetFileExtension(name).ToLower())
            {
                // Text files:
                case ".txt":
                case ".doc":
                    icon.sprite = _browser._icons[3];
                    break;
                // Image files:
                case ".bmp":
                    icon.sprite = _browser._icons[4];
                    break;
                case ".jpg":
                case ".png":
                    if(thumb)
                    {
                        string path = FileManagement.Combine(parent.root.GetComponent<FileBrowser>().GetCurrentPath(), name);
                        icon.sprite = FileManagement.ImportSprite(path);    // Thumbnail.
                    }
                    else
                    {
                        icon.sprite = _browser._icons[4];
                    }
                    break;
                // Audio files:
                case ".wav":
                case ".mp3":
                case ".ogg":
                    icon.sprite = _browser._icons[5];
                    break;
                // Settings:
                case ".ini":
                case ".xml":
                    icon.sprite = _browser._icons[6];
                    break;
                default:
                    icon.sprite = _browser._icons[2];
                    break;
            }
        }
    }

    /// <summary>Set itself as selected item and starts detecting double click gesture</summary>
    public void SetSelectedItem()
    {
        if (_toggle != null && _toggle.isOn && _browser != null)
        {
            _browser.UpdateSelectedItem(_nameLabel.text, _type);
            StartCoroutine(ClickEvent());
        }
    }

    /// <summary>Deletes itself</summary>
    public void Delete()
    {
        _toggle.group.UnregisterToggle(_toggle);
        _toggle.group = null;
        transform.SetParent(null);
        GameObject.Destroy(gameObject);
    }

    /// <summary>Gesture recognition to execute selection on double click</summary>
    public IEnumerator ClickEvent()
    {
        if (!clickedOnce && count < doubleClickTimeLimit)
        {
            clickedOnce = true;
        }
        else
        {
            clickedOnce = false;
            yield break;  //If the button is pressed twice, don't allow the second function call to fully execute.
        }
        yield return new WaitForEndOfFrame();

        while (count < doubleClickTimeLimit)
        {
            if (!clickedOnce)
            {
                count = 0f;
                DoubleClick();
                clickedOnce = false;
                yield break;
            }
            count += Time.deltaTime;// increment counter by change in time between frames
            yield return null; // wait for the next frame
        }
        count = 0f;
        clickedOnce = false;
    }
    /// <summary>Double clic event</summary>
    void DoubleClick()
    {
        if (_type == "D")
            _browser.GoToNextFolder();       // If it's a directory, navigates.
        else
            _browser.ReturnSelectedFile();   // Otherwise atempts to select and close.
    }
}
