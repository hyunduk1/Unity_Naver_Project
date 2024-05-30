using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/*
 * This script controls the FileBrowser window behaviour, it shows folder content and
 * also admits navigating through the unified virtual drive PersistentData + StreamingAssets.
 * The copy/cut parameters are saved in the FileManagement class, so they can be shared between all FileBrowser instances.
 * 
 * Parameters of SetBrowserWindow():
 * - selectionReturn: Funtion to be called when navigation/selection ends, and returns the selected item/path.
 * - iniPath: Path where to start browsing.
 * - fullPath: Determines if browsing in restricted (false) or unrestricted (true) mode.
 * - selectionMode: Determines teh type of item to be selected ("F" for files, "D" for directories).
 * - save: enables the "save" mode, so the file name can be written instead of selected.
 * - lockPath: The minimum fixed path where the browser has access.
 * - defaultSelection: Selects some existing item automatically or sets the name of the item to be saved.
 * - thumbnails: Enables the presentation of thumbnails for supported image files.
 * 
 * For 3D environments:
 * --------------------
 * 1 - Duplicate the original "FileBrowser" prefab and name it as you wish (In this example "NewFileBrowser").
 * 2 - Set the Canvas element in "NewFileBrowser" prefab as "World space".
 * 3 - Drag the "NewFileBrowser" into your scene.
 * 4 - Adjust position, size and scale of the "NewFileBrowser" Canvas in editor (Do NOT scale the "BrowserWindow" child gameObject).
 * 5 - Place the "NewFileBrowser" gameObject in Hierarchy as desired, even as a child of other 3D models.
 * 6 - Save or Apply the "NewFileBrowser".
 */

public class FileBrowser : MonoBehaviour
{
    // UI elements:
    RectTransform _canvas;                          // Necessary to make some screen calculations.
    Transform _browserUI;                           // The browser window itself.
    InputField _currentPath;                        // Input field with the displayed path.
    Transform _content;                             // List of items (ContentItem items).
    InputField _inputSelection;                     // Selected visible item name (not including path).
    Button _selectionButton;                        // Used to enable or disable the selection button.
    Text _selectionButtonLabel;                     // Used to set the button label "Open" or "Select".
    Dropdown _filterDropdown;                       // The list of available file extension filters.
    Slider _sizeSlider;                             // Slider to set the size of the items.
    ScrollRect _contentWindow;                      // Here is where all the folder content is shown.
    Text _caption;                                  // The window caption.
    Image _captionIcon;                             // The icon in the window caption.
    // Selection path control:
    public delegate void OnPathSelected(string path);
    OnPathSelected _return;                         // "Function" called to pass the selected path relative to the pase folder, when selection ends.
    List<string> _navHistory = new List<string>();
    bool _browsingHistory = false;                  // Activated while the user browses back the navHistory.
    int _navIndex = 0;
    // Settings:
    public GameObject _contentItem;                             // Prefab representing files or folders.
    [SerializeField] float _defaultItemSize = 0.05f;            // Percentage of the canvas height (the height of the screen in canvas units)
    [SerializeField] List<string> _filter = new List<string>(); // File extensions to filter rendered list.
    [SerializeField] bool _fullPath;                            // Starts as protected "safe mode" by default.
    [SerializeField] string _lockPath;                          // The path to where the browser is limited to access (can't go outside this folder).
    [SerializeField] string _selectionMode = "F";               // Sets the type of items allowed to be selected ("F" or "D").
    [SerializeField] bool _save;                                // Activates the "save" mode.
    [SerializeField] bool _thumbnails;                          // Enables the presentation of thumbnails for image files.
    // Icons to identify the items:
    public Sprite[] _icons;
    // Internal status:
    string _selectedItem = "";                      // Last valid item selection.
    string _selectionType;                          // Remembers type of last item selection.
    bool _open = false;                             // Determines if action button should select the item or attempt to open as folder.
    // Confirmation window:
    GameObject _confirmation;                       // Confirmation window (enable/disable).
    Text _confirmLabel;                             // Label of the confirmation window.
    // New name window:
    GameObject _newNameWindow;                      // NewName window (enable/disable).
    Text _newNameLabel;                             // Label of the "new name" window.
    InputField _inputNewName;                       // InputField with the "new name".
    // Error message window:
    GameObject _errorMessage;                       // Error message window (enable/disable).
    Text _errorMsgLabel;                            // Label of the confirmation window.
    
    // Confirmation window control:
    delegate void ConfirmationAction();
    ConfirmationAction _action;

    // Use this for initialization
    private void Awake()
    {
        // Connects every UI element in the window:
        _canvas = gameObject.GetComponent<RectTransform>();
        _browserUI = transform.Find("BrowserWindow");
        _currentPath = _browserUI.Find("InputCurrentPath").GetComponent<InputField>();
        _content = _browserUI.Find("ContentWindow").Find("Viewport").Find("Content");
        _contentWindow = _browserUI.Find("ContentWindow").GetComponent<ScrollRect>();
        _inputSelection = _browserUI.Find("InputSelection").GetComponent<InputField>();
        _selectionButton = _browserUI.Find("ButtonSelect").GetComponent<Button>();
        _selectionButtonLabel = _selectionButton.transform.Find("Text").GetComponent<Text>();
        _caption = _browserUI.Find("Caption").GetComponent<Text>();
        // Confirmation window:
        _confirmation = _browserUI.Find("Confirmation").gameObject;
        _confirmLabel = _confirmation.transform.Find("Label").GetComponent<Text>();
        _confirmation.SetActive(false);
        // NewName window:
        _newNameWindow = _browserUI.Find("NewName").gameObject;
        _newNameLabel = _newNameWindow.transform.Find("Label").GetComponent<Text>();
        _inputNewName = _newNameWindow.transform.Find("InputNewName").GetComponent<InputField>();
        _newNameWindow.SetActive(false);
        // Error message window:_inputNewName
        _errorMessage = _browserUI.Find("ErrorMessage").gameObject;
        _errorMsgLabel = _errorMessage.transform.Find("Label").GetComponent<Text>();
        _errorMessage.SetActive(false);
        // Optional UI components:
        Transform uiItem;
        if (uiItem = _browserUI.Find("Icon"))
            _captionIcon = uiItem.GetComponent<Image>();
        if (uiItem = _browserUI.Find("SizeSlider"))
            _sizeSlider = uiItem.GetComponent<Slider>();
        if (uiItem = _browserUI.Find("FilterDropdown"))
            _filterDropdown = uiItem.GetComponent<Dropdown>();
        // Show default content:
        SetBrowserWindow(_return, "", _fullPath, _selectionMode, _save, _lockPath, "", _thumbnails);
    }

    /// <summary>Set browser return event, first path to show, access mode (full path or override), selection mode (file/folder), work mode (load/save), fixed path and defaut selection.</summary>
    public void SetBrowserWindow(OnPathSelected selectionReturn, string iniPath = "", bool fullPath = false, string selectionMode = "F", bool save = false, string lockPath = "", string defaultSelection = "", bool thumbnails = false)
    {
        _return = selectionReturn;                                      // Saves the return method.
        _fullPath = fullPath;                                           // Remembers access mode.
        _selectionMode = selectionMode;                                 // The type of item to be selected ("F" or "D")
        _save = save;                                                   // Set "save" mode.
        _inputSelection.interactable = _save;                           // Enables/disables the file input field.
        _thumbnails = thumbnails;                                       // Enables/disables the presentation of thumbnails.
        // Show default content:
        _lockPath = FileManagement.NormalizePath(lockPath);             // The browser will access to this directory and subdirectories only.
        _currentPath.text = FileManagement.NormalizePath(_lockPath + "/" + iniPath);
        _navHistory.Clear();
        RememberPath();
        UpdateView(defaultSelection);                                   // Shows the content selecting the default file.
        if (_save)
            _inputSelection.text = defaultSelection;                    // In save mode sets the default name even without selection.
    }

    /// <summary>Sets the caption text and color.</summary>
    public void SetCaption(string title)
    {
        _caption.text = title;
    }
    /// <summary>Sets the caption text and color.</summary>
    public void SetCaption(string title, Color32 colour)
    {
        _caption.text = title;
        _caption.color = colour;
    }
    /// <summary>Sets the caption icon and color.</summary>
    public void SetCaptionIcon(Sprite icon)
    {
        if(_captionIcon != null)
            _captionIcon.sprite = icon;
    }
    /// <summary>Sets the caption icon and color.</summary>
    public void SetCaptionIcon(Sprite icon, Color32 colour)
    {
        if (_captionIcon != null)
        {
            _captionIcon.sprite = icon;
            _captionIcon.color = colour;
        }
    }

    /// <summary>Set file filters (using a List of strings)</summary>
    public void SetBrowserWindowFilter(List<string> newFilter, int set = 0)
    {
        // File extensions:
        _filter = newFilter;
        for (int i = 0; i < _filter.Count; i++)
        {
            if (!_filter[i].StartsWith("."))
                _filter[i] = "." + _filter[i];
        }
        // First item (all extensions at once):
        if (_filter.Count > 1)
        {
            string allFilters = string.Join(";", newFilter.ToArray());
            _filter.Insert(0, allFilters);
        }
        // Update the Dropdown:
        _filterDropdown.ClearOptions();
        _filterDropdown.AddOptions(_filter);
        if(set < _filterDropdown.options.Count)
            _filterDropdown.value = set;
        // Enable the DropDown if there are filters to be shown:
        _filterDropdown.interactable = _filterDropdown.options.Count > 0;
        // Refresh view correcting the input path (just in case):
        CorrectInputPath();
    }
    /// <summary>Set file filters (using a string array)</summary>
    public void SetBrowserWindowFilter(string[] newFilter, int set = 0)
    {
        SetBrowserWindowFilter(new List<string>(newFilter), set);
    }
    /// <summary>Set file filters (semicolon separated arguments)</summary>
    public void SetBrowserWindowFilter(string newFilter, int set = 0)
    {
        SetBrowserWindowFilter(newFilter.Split(';'), set);
    }

    /// <summary>Closes returning the selected file (Also called by ContentItem when DoubleClick)</summary>
    public void ReturnSelectedFile()
    {
        CorrectInputPath();
        _inputSelection.text = FileManagement.NormalizePath(_inputSelection.text);
        if (!_open && _inputSelection.text != "" && (_selectionType == _selectionMode || _save))
        {
            if (_return != null)
            {
                string path = _currentPath.text + "/" + _inputSelection.text;
                _return(FileManagement.NormalizePath(path));
            }
            CloseFileBrowser();
        }
        else
        {
            GoToNextFolder();
        }
    }
    /// <summary>Closes the browser window</summary>
    public void CloseFileBrowser()
    {
        GameObject.Destroy(gameObject);
    }

    /// <summary>Set the size of the items and the list accordingly</summary>
    public void SetContentSize()
    {
        // Set list size:
        float dynamicHeight = _canvas.rect.height * _defaultItemSize;
        if(_sizeSlider != null)
            dynamicHeight = _canvas.rect.height * _sizeSlider.value;
        _content.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _content.childCount * dynamicHeight);
    }

    /// <summary>Set the current path from the InputField</summary>
    public void SetCurrentPath()
    {
        CorrectInputPath();
        RememberPath();
        UpdateView(_selectedItem);
    }

    /// <summary>Update the content view</summary>
    IEnumerator ShowFolderContent(string defaultSelection = "")
    {
        // Reset selection (Because showing a new folder):
        _selectedItem = "";
        EnableSelectButton();
        // Delete list content:
        while (_content.childCount > 0)
        {
            _content.GetChild(0).GetComponent<ContentItem>().Delete();
        }
        _currentPath.text = FileManagement.NormalizePath(_currentPath.text);
        if (_currentPath.text == "" && _fullPath == true)
        {
            // Show available logical drives instead of content:
            string[] drives = FileManagement.ListLogicalDrives();
            for (int d = 0; d < drives.Length; d++)
            {
                GameObject item = GameObject.Instantiate(_contentItem);
                item.GetComponent<ContentItem>().SetItem(this, _content, drives[d], "D");
                SetContentSize();
                // Apply default selection:
                if (defaultSelection == drives[d]) item.GetComponent<Toggle>().isOn = true;
                yield return null;
            }
        }
        else
        {
            // Get directories:
            string[] directories = FileManagement.ListDirectories(_currentPath.text, true, _fullPath);
            if (directories != null)
            {
                for (int d = 0; d < directories.Length; d++)
                {
                    GameObject item = GameObject.Instantiate(_contentItem);
                    item.GetComponent<ContentItem>().SetItem(this, _content, directories[d], "D");
                    SetContentSize();
                    // Apply default selection:
                    if (defaultSelection == directories[d]) item.GetComponent<Toggle>().isOn = true;
                    yield return null;
                }
            }
            // Get files (Only in file mode):
            string[] filter = _filter.ToArray();
            if (_filterDropdown != null)
                filter = _filterDropdown.captionText.text.Split(';');
            string[] files = FileManagement.ListFiles(_currentPath.text, filter, true, _fullPath);
            if (files != null && _selectionMode == "F")
            {
                for (int f = 0; f < files.Length; f++)
                {
                    GameObject item = GameObject.Instantiate(_contentItem);
                    item.GetComponent<ContentItem>().SetItem(this, _content, files[f], "F", _thumbnails);
                    SetContentSize();
                    // Apply default selection:
                    if (defaultSelection == files[f]) item.GetComponent<Toggle>().isOn = true;
                    yield return null;
                }
            }
            // Exception detection (empty or access denied):
            if (directories == null)
            {
                if (FileManagement.DirectoryExists(_currentPath.text, false, _fullPath))
                {
                    // Access denied:
                    GameObject item = GameObject.Instantiate(_contentItem);
                    item.GetComponent<ContentItem>().SetItem(this, _content, "Access denied", "I");
                }
                else
                {
                    // Folder not exists:
                    GameObject item = GameObject.Instantiate(_contentItem);
                    item.GetComponent<ContentItem>().SetItem(this, _content, "Folder not exists", "I");
                }
            }
            else if (directories.Length == 0 && files.Length == 0)
            {
                // Folder is empty:
                GameObject item = GameObject.Instantiate(_contentItem);
                item.GetComponent<ContentItem>().SetItem(this, _content, "Folder is empty", "I");
            }
        }
        // Set size of rendered elements:
        SetContentSize();
        _contentWindow.verticalNormalizedPosition = 1f;  // Send list to the top.
    }
    /// <summary>Update the content view</summary>
    public void UpdateView()
    {
        StopAllCoroutines();
        StartCoroutine(ShowFolderContent(_selectedItem));
    }
    /// <summary>Update the content view selecting an item</summary>
    public void UpdateView(string defaultSelection)
    {
        StopAllCoroutines();
        StartCoroutine(ShowFolderContent(defaultSelection));
    }
    /// <summary>Ensures a valid path in InputField</summary>
    void CorrectInputPath()
    {
        // Force the LockPath as the minimum accessible path:
        _currentPath.text = FileManagement.NormalizePath(_currentPath.text);
        if (!_currentPath.text.StartsWith(_lockPath))
            _currentPath.text = _lockPath;
    }
    /// <summary>Remember new rendered path</summary>
    void RememberPath()
    {
        if (!_browsingHistory && (FileManagement.DirectoryExists(_currentPath.text, true, _fullPath) || string.IsNullOrEmpty(_currentPath.text)) )
        {
            // Do not remember the path if has not changed:
            if (_navHistory.Count == 0)
            {
                _navHistory.Add(_currentPath.text);
                _navIndex = _navHistory.Count - 1;
            }
            else if (_currentPath.text != _navHistory[_navHistory.Count - 1])
            {
                _navIndex++;
                if(_navIndex < _navHistory.Count)
                {
                    _navHistory.Insert(_navIndex, _currentPath.text);
                    // Update history:
                    _navHistory.RemoveRange(_navIndex + 1, _navHistory.Count - _navIndex - 1);
                }
                else
                {
                    _navHistory.Add(_currentPath.text);
                    _navIndex = _navHistory.Count - 1;
                }
            }
        }
        _browsingHistory = false;
    }
    /// <summary>Returns the path being rendered</summary>
    public string GetCurrentPath()
    {
        _currentPath.text = FileManagement.NormalizePath(_currentPath.text);
        return _currentPath.text;
    }

    /// <summary>Go to parent folder (navigation)</summary>
    public void GoToParentFolder()
    {
        CorrectInputPath();
        string parentPath = FileManagement.GetParentDirectory(_currentPath.text);
        if(_currentPath.text != parentPath)
        {
            _currentPath.text = parentPath;
            CorrectInputPath();
            UpdateView();
        }
        RememberPath();
    }
    /// <summary>Go to next folder (navigation)</summary>
    public void GoToNextFolder()
    {
        _browsingHistory = false;
        string nextPath;
        nextPath = string.IsNullOrEmpty(_currentPath.text) ? _selectedItem : FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem);
        if (_currentPath.text != nextPath)
        {
            _currentPath.text = nextPath;
            UpdateView();
        }
        RememberPath();
    }
    /// <summary>Browse back from history path (navigation)</summary>
    public void BrowseBack()
    {
        // Index calculation:
        _browsingHistory = true;
        if (_navIndex > 0)
        {
            _navIndex--;
            // Set the path:
            _currentPath.text = _navHistory[_navIndex];
            UpdateView();
        }
    }
    /// <summary>Browse forward from history path (navigation):</summary>
    public void BrowseFwd()
    {
        // Index calculation:
        _browsingHistory = true;
        if (_navIndex < _navHistory.Count - 1)
        {
            _navIndex++;
            // Set the path:
            _currentPath.text = _navHistory[_navIndex];
            CorrectInputPath();
            UpdateView();
        }
    }

    /// <summary>Procedure of item selection (called from ContentItem)</summary>
    public void UpdateSelectedItem(string item, string type)
    {
        // TODO: Needs refacroting?
        // The type of item determines the "Select" button behaviour:
        _selectionType = type;
        _inputSelection.text = "";
        if (_selectionType == _selectionMode)
        {
            // If the items matches the selection mode, allows to select and close:
            _inputSelection.text = item;
            _selectedItem = item;
            if (_save)
                _selectionButtonLabel.text = "Save";
            else
                _selectionButtonLabel.text = "Select";
            _selectionButton.interactable = true;
            _open = false;
        }
        else if(_selectionType == "D")
        {
            // When selecting only files, folders shows the "Open" option.
            if(_selectionType == _selectionMode) _inputSelection.text = item;
            _selectedItem = item;
            _selectionButtonLabel.text = "Open";
            _selectionButton.interactable = true;
            _open = true;
        }
        else
        {
            // Reset selection (Because no selection allowed):
            _selectedItem = "";
            _selectionButtonLabel.text = "Select";
            _selectionButton.interactable = false;
            _open = false;
        }
    }
    /// <summary>Enables or disables the action button accordingly</summary>
    public void EnableSelectButton()
    {
        _inputSelection.text = FileManagement.NormalizePath(_inputSelection.text);
        _selectionButton.interactable = false;
        if (_save && _inputSelection.text != "")
        {
            _selectionButtonLabel.text = "Save";
            _selectionButton.interactable = true;
            _open = false;
        }
    }

    /// <summary>Asks for confirmation before deletion (Deletes files or folders)</summary>
    public void PromptDeleteSelection()
    {
        if(_selectedItem != "")
        {
            string path = FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem);
            switch (_selectionType)
            {
                case "F":
                    if (FileManagement.FileExists(path, false, _fullPath))
                    {
                        _confirmLabel.text = "Delete this file permanently? " + _selectedItem;
                        _confirmation.SetActive(true);
                        _action = DeleteFile;       // Set the delegate.
                    }
                    else
                    {
                        PromtErrorMessage("Can't delete. The file is read only (" + _selectedItem + ").");
                    }
                    break;
                case "D":
                    if (FileManagement.DirectoryExists(path, false, _fullPath))
                    {
                        _confirmLabel.text = "Delete this folder and all of its content? " + _selectedItem;
                        _confirmation.SetActive(true);
                        _action = DeleteFolder;     // Set the delegate.
                    }
                    else
                    {
                        PromtErrorMessage("Can't delete. The folder is read only (" + _selectedItem + ").");
                    }
                    break;
            }
        }
    }
    void DeleteFile()
    {
        FileManagement.DeleteFile(FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem), _fullPath);
        Cancel();
    }
    void DeleteFolder()
    {
        FileManagement.DeleteDirectory(FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem), _fullPath);
        Cancel();
    }
    /// <summary>Asks for the name of the new folder</summary>
    public void PromptNewFolderName()
    {
        _newNameWindow.SetActive(true);
        _newNameLabel.text = "Plase write the new folder name:";
        _inputNewName.ActivateInputField();
        _inputNewName.text = "";
        _action = NewFolder;
    }
    void NewFolder()
    {
        if (_inputNewName.text != "")
        {
            // Create the new folder:
            string directory = FileManagement.NormalizePath(_currentPath.text + "/" + _inputNewName.text);
            FileManagement.CreateDirectory(directory, _fullPath);
            _inputNewName.text = "";
            _newNameWindow.SetActive(false);
            Cancel();
        }
    }
    /// <summary>Asks for the new name (renames files and folders)</summary>
    public void PromptForRename()
    {
        if (_selectedItem != "")
        {
            string path = FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem);
            if (!FileManagement.FileExists(path, false, _fullPath) && _selectionType == "F")
            {
                PromtErrorMessage("Can't rename. The file is read only (" + _selectedItem + ").");
            }
            else if (!FileManagement.DirectoryExists(path, false, _fullPath) && _selectionType == "D")
            {
                PromtErrorMessage("Can't rename. The folder is read only (" + _selectedItem + ").");
            }
            else
            {
                _newNameWindow.SetActive(true);
                _newNameLabel.text = "Plase write a new name for: " + _selectedItem;
                _inputNewName.ActivateInputField();
                _inputNewName.text = _selectedItem;
                _action = Rename;
            }
        }
    }
    void Rename()
    {
        if (_inputNewName.text != "")
        {
            // Rename the file or folder:
            string source = FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem);
            string dest = FileManagement.NormalizePath(_currentPath.text + "/" + _inputNewName.text);
            FileManagement.Rename(source, dest, _fullPath, _fullPath);
            Cancel();
        }
    }
    /// <summary>Shows an error message</summary>
    void PromtErrorMessage(string msg)
    {
        _errorMsgLabel.text = msg;
        _errorMessage.SetActive(true);
        _action = Cancel;
    }
    /// <summary>Confirm action</summary>
    public void Confirm()
    {
        _action();              // Execute delegated action.
    }
    /// <summary>Cancel action</summary>
    public void Cancel()
    {
        if (_confirmation.activeInHierarchy)
            _confirmation.SetActive(false);
        if (_newNameWindow.activeInHierarchy)
            _newNameWindow.SetActive(false);
        if (_errorMessage.activeInHierarchy)
            _errorMessage.SetActive(false);
        UpdateView();
    }

    /// <summary>Cut a path (not clipboard)</summary>
    public void Cut()
    {
        if (_selectedItem != "")
        {
            // Can move files or folders (Excepting from StreamingAssets):
            string path = FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem);
            if (!FileManagement.FileExists(path, false, _fullPath) && _selectionType == "F")
            {
                PromtErrorMessage("Can't cut. The file is read only (" + _selectedItem + ").");
            }
            else if (!FileManagement.DirectoryExists(path, false, _fullPath) && _selectionType == "D")
            {
                PromtErrorMessage("Can't cut. The folder is read only (" + _selectedItem + ").");
            }
            else
            {
                FileManagement.browserPath = FileManagement.NormalizePath(path);
                FileManagement.browserType = _selectionType;
                FileManagement.browserMove = true;
                FileManagement.browserIsFullPath = _fullPath;
            }
        }
    }
    /// <summary>Copy a path (not clipboard)</summary>
    public void Copy()
    {
        if(_selectedItem != "")
        {
            FileManagement.browserPath = FileManagement.NormalizePath(_currentPath.text + "/" + _selectedItem);
            FileManagement.browserType = _selectionType;
            FileManagement.browserMove = false;
            FileManagement.browserIsFullPath = _fullPath;
        }
    }
    /// <summary>Paste a path (not clipboard)</summary>
    public void Paste()
    {
        string pastePath = FileManagement.NormalizePath(_currentPath.text + "/" + FileManagement.GetFileName(FileManagement.browserPath));
        if(FileManagement.browserPath != pastePath)
        {
            if (FileManagement.browserMove)
            {
                // Move files or folders:
                FileManagement.Move(FileManagement.browserPath, pastePath, FileManagement.browserIsFullPath, _fullPath);
            }
            else
            {
                // Copy files or folders:
                if (FileManagement.browserType == "F")
                    FileManagement.CopyFile(FileManagement.browserPath, pastePath, true, FileManagement.browserIsFullPath, _fullPath);
                else if (FileManagement.browserType == "D")
                    FileManagement.CopyDirectory(FileManagement.browserPath, pastePath, true, FileManagement.browserIsFullPath, _fullPath);
            }
            UpdateView();
        }
    }
}