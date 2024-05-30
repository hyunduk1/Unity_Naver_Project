using UnityEngine;
using UnityEngine.Events;

/*
 * Wrapper of the FileBrowser class intended to allow customize settings from editor.
 * It includes two simple methods to open and force close the browser.
 * The selected path is returned by the custom UnityEvent onPathSelected (assigned in editor).
 * 
 * For 3D environments:
 * --------------------
 * 1 - Duplicate the original "FileBrowser" prefab and name it as you wish (In this example "NewFileBrowser").
 * 2 - Set the Canvas component in your "NewFileBrowser" prefab as "World space".
 * 3 - Drag your "NewFileBrowser" into your scene.
 * 4 - Adjust position, size and scale of the "NewFileBrowser" Canvas in editor (Do NOT scale the "BrowserWindow" child gameObject).
 * 5 - Place the "NewFileBrowser" gameObject in Hierarchy as desired, even as a child of other 3D models.
 * 6 - Save or Apply the "NewFileBrowser".
 * 7 - Drag the "NewFileBrowser" to _fileBrowserPrefab to keep the reference.
 */

public class FileBrowserWrapper : MonoBehaviour
{
    public GameObject _fileBrowserPrefab;                           // This variable MUST be connected to the FileBrowser prefab in editor.
    GameObject _fileBrowserInstance;                                // Here is saved the temporary instance of the current FileBrowser window.
    public Transform _parentTo;                                     // The object to parent to automatically (Set in editor).
    // FileBrowser settings:
    [Header("Behaviour settings")]
    public bool _useFullPaths = false;                              // Allows to use full paths in _lockPath and _startPath.
    public string _lockPath = "";                                   // Fixed path to browse. The user can't browse outside this path.
    public string _startPath = "";                                  // Path to start. If _lockPath is set, _startPath must be relative to it.
    public FileModes _selectionMode;                                // Allows to select files or folders.
    public string _defaultSelected = "";                            // File or folder to select automatically when the window opens.
    public bool _workInSaveMode = false;                            // Sets the browser to help selecting a destination instead of a source.
    public string[] _extensionFilters;                              // File extensions allowed to be shown.
    public int _defaultFilterIndex = 0;                             // Default file extension filter to be activated when window opens.
    public bool _openAtStart = false;                               // Open this file browser automatically when scene loads.
    [Header("Visual settings")]
    public string _title = "File browser";                          // Sets the text in the caption bar.
    public Color32 _titleColor = Color.white;                       // Sets the text color of the caption bar.
    public Sprite _icon;                                            // Sets the icon of the caption bar.
    public Color32 _iconColor = Color.white;                        // Sets the icon color of the caption.
    public bool _thumbnails = false;                                // Enables the presentation of thumbnails for image files.
    // FileBrowser events:
    [System.Serializable]
    public class PathSelectedEvent : UnityEvent<string> { }
    [Header("File Browser events")]
    public volatile PathSelectedEvent _onPathSelected;              // Fired when the user selects a file or folder.
    /*
     * Event method example:
       
        public void FileBrowserEvent(string path)
        {
        }
     */

    public enum FileModes
    {
        Files,
        Directories
    }

    void Start()
    {
        _startPath = FileManagement.NormalizePath(_startPath);
        _lockPath = FileManagement.NormalizePath(_lockPath);
        _defaultSelected = FileManagement.NormalizePath(_defaultSelected);
        if (_openAtStart)
            Open();
    }
	
    /// <summary> Creates an instance of the FileBrowser prefab and returns it </summary>
    public void Open()
    {
        if(_fileBrowserInstance == null)
        {
            _fileBrowserInstance = GameObject.Instantiate(_fileBrowserPrefab);
            // Sets the FileBrowser as a child of the provided Transform:
            if(_parentTo != null)
                _fileBrowserInstance.transform.SetParent(_parentTo, false);
            // Apply settings:
            FileBrowser browser = _fileBrowserInstance.GetComponent<FileBrowser>();
            browser.SetBrowserWindow((string path) => {
                if (_onPathSelected != null) _onPathSelected.Invoke(path);
                _fileBrowserInstance = null;
            }, _startPath, _useFullPaths, (_selectionMode == FileModes.Files) ? "F" : "D", _workInSaveMode, _lockPath, _defaultSelected, _thumbnails);
            browser.SetBrowserWindowFilter(_extensionFilters, _defaultFilterIndex);
            browser.SetCaption(_title, _titleColor);
            browser.SetCaptionIcon(_icon, _iconColor);
        }
    }
    /// <summary> Forces the browser to close without firing the selection event </summary>
    public void ForceClose()
    {
        if(_fileBrowserInstance != null)
        {
            GameObject.Destroy(_fileBrowserInstance);
            _fileBrowserInstance = null;
        }
    }
    /// <summary> Gets the currently displayed FileBrowser instance (if any) </summary>
    public GameObject GetInstance()
    {
        return _fileBrowserInstance;
    }
}
