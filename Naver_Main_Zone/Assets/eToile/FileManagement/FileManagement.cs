// Encryption method directive (should be always the first line in this file):
//#define USE_AES

using System.Collections.Generic;
using System.Linq;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

#if UNITY_WEBGL
// Plugin (WebGL):
using System.Runtime.InteropServices;
#endif

#if USE_AES
// AES Encryption (Not compatible with WindowsPhone):
using System.Security.Cryptography;
#endif

/*
 * File Management:
 * ----------------
 * Easy control on files. All data is saved in binary to prevent data loss or corruption.
 * This asset was downloaded from the Unity AssetStore:
 * 
 * https://assetstore.unity.com/packages/slug/67183
 * 
 * V 1.1 Features:
 * - You may chose between Xor or Aes encryption algorythms manually.
 * Define or comment the USE_AES directive to switch encryption methods.
 * (AES encryption is not available on Windows Phone platforms)
 * - Images can be easily encrypted.
 * 
 * V 1.2 Features:
 * - New feature: Unity types Vector2, Vector3, Vector4, Quaternion, Rect, Color and Color32 now supported for save and load.
 * - New feature: Load AudioClip from file (WebGL not supported yet).
 * - New feature: Increased WebGL save size (Now images can be saved in web browser too!!!).
 * - New feature: Save and load Arrays or Lists in a single line of code.
 * - New feature: Automated virtual StreamingAssets file/directory index. Now StreamingAssets can be accessed on Android and WebGL as real folders!!!
 * - New feature: Directory tools: Exists, Create, Delete and Empty content (StreamingAssets is read only).
 * - New feature: Get lists of files or directories from a given path.
 * - New feature: Get a List<byte[]> with the whole content of a given folder (matching the file name list ListFiles()).
 * - BugFix: File existance detection wasn't working on Android/StreamingAssets.
 * - BugFix: Optimized reading very big strings.
 * 
 * V 1.3 Features:
 * - New feature: Add raw data to an existing file with AddRawData().
 * - New feature: You can drag and resize the BrowserWindow.
 * - New feature: Added several configurations that can be combined to achieve different Browser behaviours.
 * - New feature: Added NormalizePath(), GetExtension() and GetFileExtension() methods (check documentation).
 * - New feature: Added CopyFile(), CopyDirectory(), Move() and Rename() (fully compatible with files and folders).
 * - New feature: Added Cut, Copy and Paste buttons to the FileBrowser.
 * - New feature: Added Delete, Rename and New folder buttons to the FileBrowser.
 * - New feature: Creates the requested path when writing to disk.
 * - New feature: WAV files import now supported in WebGL (uses a custom wrapper).
 * - BugFix: Important improvements in path interpretation (OSX bugfixes).
 * 
 * V 1.4 Features:
 * - New Feature: ReadList and ReadArray overflow allowing multiple separatos. (Useful for CSV parsing)
 * - New Feature: ReadAllLines from any platform using multiple line separators.
 * - New Feature: Navigate back and fwd in FileBrowser window.
 * - BugFix: Extended WAV files compatibility.
 * - BugFix: Importing audio in Android.
 * - BugFix: Not copying empty folders.
 * - BugFix: Retrieving wring files from StreamingAssets index.
 * 
 * V 1.5 Features:
 * - New Feature: Filters the ListFiles view by multiple file extensions.
 * - New Feature: File list filtering is integrated to the FileBrowser prefab.
 * - New Feature: Dropdown with the desired file extensions from the filter.
 * - BugFix: Thread safe.
 * 
 * v1.6 Features:
 * - New Feature: FileBrowser admits a default file to be selected or saved at start.
 * - New Feature: The OpenWavParser is integrated within FileManagement.
 * - New Feature: AudioClips can be saved into wav files using OpenWavParser.
 * - New Feature: FileBrowser includes a caption icon (customisable).
 * - New Feature: Caption text customizable in FileBrowser.
 * - New Feature: Added new GetFileNameWithoutExtension() method.
 * - New Feature: AudioClip name is set with the source file automatically (ImportAudio).
 * 
 * v1.7 Features:
 * - New Feature: Parses .ini files into a complete utility class (FM_IniFile).
 * - New Feature: Run .apk file installation in Android devices (User action required to grant permission).
 * - New Feature: Allows string encoding for all system supported formats.
 * - New Feature: The ListFiles method (with extension filter) now allows the scan of subfolders.
 * - BugFix: Parse error when reading an empty file in ReadList()
 * - BugFix: Corrected audio import for Unity 2017 and further.
 * 
 * v1.8 Features:
 * - BugFix: System.Text.Encoding modes not supported on WSA 8.1 (Skipped to allow only "Fast" 8bit mode).
 * - BugFix: FileBrowser was throwing an error in Unity 2019 (UI issue).
 * - BugFix: Mono in Unity 2018 uses localization culture in float values (failing interpretation of dots and commas).
 * - BugFix: Not performing a folder cut/paste correctly.
 * - BugFix: WWW not available since Unity 2019.
 * - New Feature: AddLine method allows to add text lines to an existing file.
 * - New Feature: Reading StreamingAssets in Android is thread safe (not using WWW nor UnityWebRequest).
 * - New Feature: Adapted to work on non-Unity applications.
 * - New Feature: FileBrowser now is fully 3D/VR compatible.
 * 
 * v1.9 Features:
 * - BugFix: The Delete() method crashed when tried to delete an open/read_only file instead of continue.
 * - BugFix: Compilation error under WebGL platform.
 * 
 * v2.0 Feature:
 * - New feature: Allows default filter selection in file browser.
 * - BugFix: Setting the CultureInfo failing on WSA exports.
 * 
 * v2.1 Features:
 * - BugFix: Not reading files from disk with fullPath.
 * - BugFix: Using Combine in restricted paths leaded to avoid restriction (accessing different paths than expected).
 * - BugFix: StreamingAssetsIndexer failing when the project is placed in a drive different than Unity is.
 * - BugFix: Failing to create containing nested folders when saving files with fullPath.
 * - BugFix: Normalize() didn't deleted slashes at the start of the paths (Causing FileExists to fail).
 * - BugFix: SaveArray() performing poorly.
 * - New feature: Standard read-only embedded resources integration (similar to StreamingAssets).
 * - New feature: The copy/cut custom clipboard is shared between all FileBrowser instances.
 * - New feature: CustomParser returns default values if null or empty strings are provided.
 * - New feature: CustomParser can parse comma separated values into arrays of supported types.
 * - New feature: CustomParser includes FM_IniFile as supported type.
 * - New feature: INI files can also store arrays in keys automatically.
 * - New feature: INI files can be manipulated from/to string.
 * - New feature: A new FileBrowser wrapper allows to attach a script to a GameObject to manage it in editor.
 * - New feature: Added two new color profiles for the FileBrowser (dark and light).
 * 
 * v2.2 Features:
 * - BugFix: Indexer fails when creating StreamingAssets folder automatically.
 * - New feature: Lock the access to files in log lines.
 * 
 * v2.3 Features:
 * - BugFix: Empty INI section must go always first, so sections are forced to be sorted when calling ToString().
 * - BugFix: Disable the FileBrowser's filter DropDown if there are not filters to select.
 * - BugFix: NormalizePath() method fixed for Linux and OSX platforms.
 * - BugFix: CustomParser() didn't used the provided separator to restore FM_IniFile objects.
 * - BugFix: Improved FileBrowser handling (drag and resize).
 * - BugFix: The list of logical drives now returns normalized.
 * - BugFix: StreamingAssets navigation fixed in OSX.
 * - New Feature: FM_IniFile.AddKey() allows to add an FM_IniFile object directly (providing a custom separator).
 * - New Feature: FM_IniFile.CopySection() allows to copy a complete section from another FM_IniFile object.
 * - New Feature: Creates the PersistentDataPath automatically if not exists (for non-Unity exports).
 * - New Feature: FM_IniFile constructor getting content from another FM_IniFile.
 * - New Feature: GetSectionList() in FM_IniFile filters the list with "StartsWith" string.
 * - New Feature: New Clear() method in FM_IniFile.
 * - New Feature: In FileBrowser the content list is updated progressively to make the application responsive.
 * - New Feature: New file icon added to FileBrowser: "Settings" (for ini and xml files).
 * - New Feature: New alternative color schemes for FileBrowser (light and dark).
 * 
 * v2.4 Features:
 * - BugFix: Damaged INI files are imported as null using ImportIniFile.
 * - Improvement: FM_IniFile.Merge() is null protected.
 * - Improvement: GetAppName() is thread-safe.
 * - New Feature: New helper class for dynamic average computation: FM_Average.
 * - New Feature: The ImportAudio() method includes a "stream" parameter for streamed audio files.
 * 
 * v2.5 Features (unreleased):
 * - BugFix: NormalizePath() not working properly on parallel threads under Unity engine.
 * - BugFix: Windows not sorting correctly when clicking the headers.
 * - BugFix: Save INI files sorting keys.
 * - BugFix: CustomParser<>() converts to byte[] without text coding, parsing separated values into bytes.
 * - Improvement: FM_IniFile.GetKeyList() allows filtering by key header (startsWith parameter).
 * - Improvement: FM_IniFile.ToString() uses StringBuilder to accelerate the conversion of large INI files.
 * - New feature: VectorX and Quaternion types are converted without losing resolution (even arrays).
 * - New feature: Sections in INI files can be renamed.
 */

/*
 * Embedded resources:
 * -------------------
 * The embedded resources are files embedded within the final binary.
 * This feature is available for environments other than Unity (as Mono or Visual Studio).
 * Unity uses the StreamingAssets folder to achieve the same result, but those assets are not
 * embedded within the final binary, they are added to the final export in folder named "StreamingAssets".
 * Embedded resources and StreamingAssets files are managed as read only files, and are accessed in the
 * same way for the main application. FileManagement choses the correct method automaticaly.
 * 
 * How to embed resources:
 * -----------------------
 * Add the desired files to your project (Add existing element) to be able to embed them.
 * In Visual Studio or Monodevelop, select the files(s) to be embedded, then go to the "Properties" tab.
 * Set the "Build action" setting to "Embedded resource" in Visual Studio or "Embed as resource" for Mono.
 * The embedded resources can be aranged in folder and subfolders. Those folders can be navigated using the
 * common methods provided by FileManagement as if were real folders.
 */

/// <summary>String conversion available modes</summary>
public enum FM_StringMode
{
    UTF8,
    Fast,               // 8bit chars only.
    ASCII,
    BigEndianUnicode,
    Unicode,
    UTF32,
    UTF7
}

public static class FileManagement
{
    // For AES encryption, 'key' must be 16, 24 or 32 bytes length.
    // Xor encryption uses any length.
    // IMPORTANT: DON'T FORGET TO SET YOUR OWN KEY (Keys must never be written in plain text)
    readonly static byte[] key = { 217, 134, 151, 168, 185, 202, 129, 135, 150, 130, 141, 201, 210, 167, 198, 169 };
    // The current string conversion method:
    public static FM_StringMode stringConversion = FM_StringMode.UTF8;
    // Automatic paths (they are saved to prevent access error in a Thread):
    public static string persistentDataPath = "";

    // Lock objects:
    static readonly object logLock = new object();

#if UNITY_5_3_OR_NEWER
    public static string browserPath;                                                   // [FileBrowser]: Source file/folder to be copied or cutted.
    public static string browserType;                                                   // [FileBrowser]: The type of the selected source: "F" or "D".
    public static bool browserMove;                                                     // [FileBrowser]: Move or copy the sourcePath?.
    public static bool browserIsFullPath;                                               // [FileBrowser]: Indicates if the path is absolute or relative.
    public static Canvas activeBrowser;                                                 // [UIWindowDrag]: Current FileBrowser displayed on top.
    public static int frontSortOrder = 1;                                               // [UIWindowDrag]: The number to be used as front order (assuming all other Canvas are 0).
    public static string streamingAssetsPath = "";
    static string appName = "";
    static string[] blocks;                                                             // This is the StreamingAssets index for Android and WebGL.
    // Android object used to access Compressed StreamingAssets folder:
    static AndroidJavaObject aManager;
    /// <summary>Constructor to initialize the automatic paths for further use</summary>
    static FileManagement()
    {
        appName = Application.productName;
        persistentDataPath = Application.persistentDataPath;
        streamingAssetsPath = Application.streamingAssetsPath;
        // Force invariant culture:
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
    }
#else
    // Integrated resources parameters:
    static System.Reflection.Assembly _assembly;
    static string _className = Ini_FileManagement();                                    // Resource container class name.
    static string[] _resources;                                                         // Detected resources complete list.

    static string Ini_FileManagement()
    {
        _assembly = System.Reflection.Assembly.GetExecutingAssembly();
        // Force invariant culture:
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        // Application path:
        persistentDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        persistentDataPath = Combine(persistentDataPath, _assembly.GetName().Name);
        // Create default folder if not exists:
        if (!DirectoryExists(persistentDataPath, false, true))
            CreateDirectory(persistentDataPath, true);
        // Get resources list and class name:
        _resources = _assembly.GetManifestResourceNames();
        if (_resources.Length > 0)
            return _resources[0].Substring(0, _resources[0].IndexOf('.'));
        return "";
    }

#endif

#if UNITY_WINRT
    /// <summary>Saves a new file (overwrites if exists an older one)</summary>
    public static void SaveRawFile(string name, byte[] content, bool enc = false, bool fullPath = false)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);
            if (content != null)
            {
                CreateDirectory(GetParentDirectory(name), true);                    // Reached here, is always fullPath.
                if (enc)
                    UnityEngine.Windows.File.WriteAllBytes(name, Encrypt(content, key));
                else
                    UnityEngine.Windows.File.WriteAllBytes(name, content);
            }
            else
                WriteLine("[FileManagement.SaveRawFile] Can't save null data.", true);
        }
        else
            WriteLine("[FileManagement.SaveRawFile] Can't save an unnamed file.", true);
    }

    /// <summary>Returns the byte[] content of a file</summary>
    public static byte[] ReadRawFile(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        byte[] contentArray = { };
        string path = name;

        if (!string.IsNullOrEmpty(name))
        {
            // First checks if file exists no matter where:
            if (FileExists(path, checkSA, fullPath))
            {
                if (!fullPath)
                {
                    // Checks if file exists in PersistentDataPath only:
                    if (FileExists(name, false))
                        path = NormalizePath(persistentDataPath + "/" + name);
                    else if (checkSA)    // Then checks StreamingAssets if desired.
                        path = NormalizePath(streamingAssetsPath + "/" + name);
                }
                // Read content normally:
                contentArray = UnityEngine.Windows.File.ReadAllBytes(path);
            }
            else
                WriteLine("[FileManagement.ReadRawFile] File not found: " + path);

            // Decryption:
            if (contentArray.Length > 0 && enc)
                contentArray = Decrypt(contentArray, key);
        }
        else
            WriteLine("[FileManagement.ReadRawFile] Can't read an unnamed file.", true);
        return contentArray;
    }

    /// <summary>Deletes a file</summary>
    public static void DeleteFile(string name, bool fullPath = false)
    {
        if (FileExists(name, false, fullPath))
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);
            UnityEngine.Windows.File.Delete(name);
        }
        else
            WriteLine("[FileManagement.DeleteFile] File not found: " + name);
    }
#elif UNITY_WEBGL
    [DllImport("__Internal")] private static extern void SyncFiles();
    [DllImport("__Internal")] private static extern int ReadFileLen(string url);    // Downloads a file and returns the length.
    [DllImport("__Internal")] private static extern System.IntPtr ReadData();       // Pointer to the downloaded data.
    [DllImport("__Internal")] public static extern void ShowMessage(string msg);    // Shows messages in browser for debugging.

    // Legacy interfaces (for cookies):
    [DllImport("__Internal")] private static extern void WriteCookie(string name, string value, string expires = null, bool sec = false, string path = null, string domain = null);
    [DllImport("__Internal")] private static extern void DeleteCookie(string name, string path = null, string domain = null);
    [DllImport("__Internal")] private static extern int ReadCookieLen(string name);
    [DllImport("__Internal")] private static extern void DeleteAllCookies();

    /// <summary>Saves a new file (overwrites if exists an older one)</summary>
    public static void SaveRawFile(string name, byte[] content, bool enc = false, bool fullPath = false)
    {
        if (name != "")
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);

            if (content != null)
            {
                CreateDirectory(GetParentDirectory(name), true);                    // Reached here, is always fullPath.
                if (enc)
                    System.IO.File.WriteAllBytes(name, Encrypt(content, key));
                else
                    System.IO.File.WriteAllBytes(name, content);

                if (Application.platform == RuntimePlatform.WebGLPlayer)
                    SyncFiles();
            }
            else
            {
                WriteLine("[FileManagement.SaveRawFile] Can't save null data.", true);
            }
        }
        else
        {
            WriteLine("[FileManagement.SaveRawFile] Can't save an unnamed file.", true);
        }
    }

    /// <summary>Returns the byte[] content of a file</summary>
    public static byte[] ReadRawFile(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        byte[] contentArray = { };
        string path = name;
        if (name != "")
        {
            // First checks if file exists no matter where:
            if (FileExists(path, checkSA, fullPath))
            {
                if (!fullPath)
                {
                    // Checks if file exists in PersistentDataPath only:
                    if (FileExists(name, false))
                        path = NormalizePath(persistentDataPath + "/" + name);
                    else if (checkSA)    // Then checks StreamingAssets if desired.
                        path = NormalizePath(streamingAssetsPath + "/" + name);
                }
                // Chose correct method to read:
                if (path.Contains("://") && Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // Read content from StreamingAssets:
                    int contentLen = ReadFileLen(path);     // Get file length.
                    if (contentLen > 0)
                    {
                        contentArray = new byte[contentLen];
                        Marshal.Copy(ReadData(), contentArray, 0, contentLen);
                    }
                    else
                    {
                        WriteLine("[FileManagement.ReadRawFile] File was deleted in server side: " + path);
                    }
                }
                else
                {
                    // Read content normally:
                    contentArray = System.IO.File.ReadAllBytes(path);
                }
            }
            else
            {
                WriteLine("[FileManagement.ReadRawFile] File not found: " + path);
            }

            // Decryption:
            if (contentArray.Length > 0 && enc)
                contentArray = Decrypt(contentArray, key);
        }
        else
        {
            WriteLine("[FileManagement.ReadRawFile] Can't read an unnamed file.", true);
        }
        return contentArray;
    }

    /// <summary>Deletes a file</summary>
    public static void DeleteFile(string name, bool fullPath = false)
    {
        if (FileExists(name, false, fullPath))
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);
            System.IO.File.Delete(name);

            if (Application.platform == RuntimePlatform.WebGLPlayer)
                SyncFiles();
        }
        else
        {
            WriteLine("[FileManagement.DeleteFile] File not found: " + name);
        }
    }

    /// <summary>Checks a name into the automatic StreamingAssets index file (WebGL)</summary>
    static bool CheckNameOnIndex(string name, string type)
    {
        // First block is StreamingAssets, then there are every subfolders:
        if(blocks == null)
        {
            // Load blocks first time:
            string indexPath = streamingAssetsPath + "/FMSA_Index";
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                int contentLen = ReadFileLen(indexPath);// Get file length.
                if (contentLen > 0)
                {
                    // Get index directly:
                    byte[] contentArray = new byte[contentLen];
                    Marshal.Copy(ReadData(), contentArray, 0, contentLen);
                    string content = ByteArrayToString(contentArray);
                    // Get blocks:
                    blocks = content.Split('|');
                }
                else
                {
                    blocks = new string[0];
                    WriteLine("[FileManagement.CheckNameOnIndex] Index file not found: " + indexPath);
                }
            }
            else
            {
                // To allow testing in editor:
                byte[] contentArray = { };
                if (System.IO.File.Exists(indexPath))
                    contentArray = System.IO.File.ReadAllBytes(indexPath);
                string content = ByteArrayToString(contentArray);
                // Get blocks:
                blocks = content.Split('|');
            }
        }
        // Checks name existance:
        name = name.Replace('\\', '/');                 // Normalize path format with index.
        for (int b = 0; b < blocks.Length; b++)
        {
            // Search every folder and subfodler separatelly:
            string[] entries = blocks[b].Split(';');
            for (int e = 0; e < entries.Length; e++)
            {
                string[] data = entries[e].Split(',');  // [0] file path and name, [1] type ("F" or "D").
                data[0] = data[0].Replace('\\', '/');
                if (name == data[0] && data[1] == type)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Checks a virtual path into the index and returns the names matching (WebGL)</summary>
    static string[] GetNamesOnIndex(string name, string type)
    {
        if (blocks == null)
            CheckNameOnIndex("", "");
        List<string> list = new List<string>();
        name = name.Replace('\\', '/');                 // Normalize path format with index.
        name = name.Replace("//", "/");                 // Allow root folder (empty path).
        // Collect name occurrencies:
        bool exit = false;
        for (int b = 0; b < blocks.Length; b++)
        {
            // Search every folder and subfodler separatelly:
            string[] entries = blocks[b].Split(';');
            for (int e = 0; e < entries.Length; e++)
            {
                string[] data = entries[e].Split(',');  // [0] file path and name, [1] type ("F" or "D").
                data[0] = data[0].Replace('\\', '/');
                if (data[0].Contains(name) && type == data[1])
                {
                    list.Add(data[0]);
                    exit = true;
                }
            }
            if (exit)
                break;
        }
        return list.ToArray();
    }
#else
    /// <summary>Saves a new file (overwrites if exists an older one)</summary>
    public static void SaveRawFile(string name, byte[] content, bool enc = false, bool fullPath = false)
    {
        if (!string.IsNullOrEmpty(name))
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);

            if (content != null)
            {
                CreateDirectory(GetParentDirectory(name), true);                    // Reached here, is always fullPath.
                if (content.Length > 0)
                {
                    if (enc) content = Encrypt(content, key);
                    System.IO.File.WriteAllBytes(name, content);
                }
                else
                {
                    System.IO.File.Create(name).Dispose();                          // Create an empty file.
                }
            }
            else
                WriteLine("[FileManagement.SaveRawFile] Can't save null data.", true);
        }
        else
            WriteLine("[FileManagemnt.SaveRawFile] Can't save an unnamed file.", true);
    }

    /// <summary>Returns the byte[] content of a file</summary>
    public static byte[] ReadRawFile(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        byte[] contentArray = { };
        string path = name;
        if (path != "")
        {
            // First checks if file exists no matter where:
            if (FileExists(path, checkSA, fullPath))
            {
                if (!fullPath)
                {
                    if (FileExists(path, false))
                    {
                        path = NormalizePath(persistentDataPath + "/" + path);      // File exists in persistent data only, so get its full path.
                        contentArray = System.IO.File.ReadAllBytes(path);           // Read content.
                    }
                    else if (checkSA)
                    {
#if UNITY_5_3_OR_NEWER
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            contentArray = GetFileFromStreamingAsets(name);         // Get file from StreamingAssets in Android.
                        }
                        else
                        {
                            path = NormalizePath(streamingAssetsPath + "/" + path); // Get the full path to StreamingAssets.
                            contentArray = System.IO.File.ReadAllBytes(path);       // Read content.
                        }
#else
                        // Format the filename to compare:
                        name = name.Replace('\\', '.');
                        name = name.Replace('/', '.');
                        name = _className + "." + name;
                        // Read the embedded file content:
                        System.IO.Stream stream = _assembly.GetManifestResourceStream(name);
                        System.IO.MemoryStream mStream = new System.IO.MemoryStream();
                        stream.CopyTo(mStream);
                        contentArray = mStream.ToArray();
#endif
                    }
                }
                else
                {
                    contentArray = System.IO.File.ReadAllBytes(path);               // Read content directly.
                }
            }
            else
            {
                WriteLine("[FileManagement.ReadRawFile] File not found: " + path);
            }
            // Decryption:
            if (contentArray.Length > 0 && enc)
                contentArray = Decrypt(contentArray, key);
        }
        else
        {
            WriteLine("[FileManagement.ReadRawFile] Can't read an unnamed file.", true);
        }
        return contentArray;
    }

    /// <summary>Deletes a file</summary>
    public static void DeleteFile(string name, bool fullPath = false)
    {
        if (FileExists(name, false, fullPath))
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);
            try
            {
                System.IO.File.Delete(name);
            }
            catch (System.Exception e)
            {
                WriteLine("[FileManagement.DeleteFile] File " + name + " couldn't be deleted: " + e.Message);
            }
        }
        else
            WriteLine("[FileManagement.DeleteFile] File not found: " + name);
    }

#endif

#if UNITY_5_3_OR_NEWER

#if UNITY_ANDROID
    /// <summary>Checks a name into the automatic StreamingAssets index file (Android)</summary>
    static bool CheckNameOnIndex(string name, string type)
    {
        // First block is StreamingAssets, then there are every subfolders:
        if (blocks == null)
        {
            // Load blocks first time:
            string indexPath = "FMSA_Index";
            byte[] contentArray = { };
            if (Application.platform == RuntimePlatform.Android)
            {
                // Read StreamingAssets in Android:
                contentArray = GetFileFromStreamingAsets(indexPath);
            }
            else
            {
                // Read content normally (It allows testing in Editor for Android):
                indexPath = NormalizePath(streamingAssetsPath + "/" + indexPath);
                if (System.IO.File.Exists(indexPath))
                    contentArray = System.IO.File.ReadAllBytes(indexPath);
                else
                    contentArray = new byte[0];
            }
            // If file was successfully found:
            if (contentArray != null)
            {
                string content = ByteArrayToString(contentArray);
                blocks = content.Split('|');
            }
            else
            {
                WriteLine("[FileManagement.CheckNameOnIndex] Index file not found: " + indexPath);
            }
        }
        // Checks name existence:
        name = name.Replace('\\', '/');      // Normalize path format with index.
        for (int b = 0; b < blocks.Length; b++)
        {
            // Search every folder and subfodler separatelly:
            string[] entries = blocks[b].Split(';');
            for (int e = 0; e < entries.Length; e++)
            {
                string[] data = entries[e].Split(',');  // [0] file path and name, [1] type ("F" or "D").
                data[0] = data[0].Replace('\\', '/');
                if (name == data[0] && type == data[1])
                    return true;
            }
        }
        return false;
    }

    /// <summary>Checks a virtual path into the index and returns the names matching (Android)</summary>
    static string[] GetNamesOnIndex(string name, string type)
    {
        if (blocks == null)
            CheckNameOnIndex("", "");   // Forces load the index (if not already loaded)
        List<string> list = new List<string>();
        name = name.Replace("//", "/");                 // Allow root folder (empty path).
        name = name.Replace('\\', '/');                 // Normalize path format with index.
        // Collect name occurrencies:
        bool exit = false;
        for (int b = 0; b < blocks.Length; b++)
        {
            // Search every folder and subfodler separatelly:
            string[] entries = blocks[b].Split(';');
            for (int e = 0; e < entries.Length; e++)
            {
                string[] data = entries[e].Split(',');  // [0] file path and name, [1] type ("F" or "D").
                data[0] = NormalizePath(data[0]);
                if (data[0].StartsWith(name) && type == data[1])
                {
                    list.Add(data[0]);
                    exit = true;
                }
            }
            if (exit)
                break;
        }
        return list.ToArray();
    }
#endif

    /// <summary>Read files from the installed APK in "assets" folder (Android only)</summary>
    static byte[] GetFileFromStreamingAsets(string name)
    {
        // This function reads a file placed into the "assets" folder inside the compressed APK.
        // The method is not using any Unity class so it's completely thread safe.
        byte[] contentArray = { };
        // Read StreamingAssets in Android ("assets):
        if (aManager == null)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            aManager = context.Call<AndroidJavaObject>("getAssets");                                    // AssetManager
        }
        // Get content:
        AndroidJavaObject iStream = aManager.Call<AndroidJavaObject>("open", new object[] { name });    // InputStream
        contentArray = new byte[iStream.Call<int>("available")];
        for (int i = 0; i < contentArray.Length; i++)
        {
            contentArray[i] = (byte)iStream.Call<int>("read");
        }
        iStream.Call("close");
        return contentArray;
    }

    /// <summary>Load an audio file into AudioClip</summary>
    public static AudioClip ImportAudio(string file, bool enc = false, bool checkSA = true, bool fullPath = false, bool stream = false)
    {
        byte[] audioFile = ReadRawFile(file, enc, checkSA, fullPath);
        if (OpenWavParser.IsWAVFile(audioFile))
        {
            AudioClip clip = OpenWavParser.ByteArrayToAudioClip(audioFile, GetFileNameWithoutExtension(file), stream);
            return clip;
        }
        else
        {
#if UNITY_2019_1_OR_NEWER
            WriteLine("[FileManagement.ImportAudio] " + FileManagement.GetFileExtension(file) + " format not supported.", true);
            return null;
#else
            if (!fullPath)
            {
                // Checks if file exists in PersistentDataPath only:
                if (FileExists(file, false))
                {
                    file = NormalizePath(persistentDataPath + "/" + file);
                    file = "file:///" + file;
                }
                else if (checkSA)    // Then checks StreamingAssets if desired.
                {
                    file = NormalizePath(streamingAssetsPath + "/" + file);
                    if (Application.platform != RuntimePlatform.Android)
                        file = "file:///" + file;
                }
            }
            else
            {
                file = "file:///" + file;
            }

            // Imports audio clip:
            WWW www = new WWW(file);
            while (!www.isDone) { };    // Simulates synchronous reading.

            if (string.IsNullOrEmpty(www.error))
            {
                AudioClip clip = www.GetAudioClip(true, stream);
                if (clip != null)
                {
                    clip.name = GetFileNameWithoutExtension(file);
                    return clip;
                }
                return null;
            }

            WriteLine("[FileManagement.ImportAudio] WWW error: " + www.error);
            return null;
#endif
        }
    }

    /// <summary>Save AudioClip into a WAV file</summary>
    public static void SaveAudio(string name, AudioClip clip, bool enc = false, bool fullPath = false, OpenWavParser.Resolution res = OpenWavParser.Resolution._16bit)
    {
        if (!name.ToLower().EndsWith(".wav"))
            name += ".wav";
        SaveRawFile(name, OpenWavParser.AudioClipToByteArray(clip, res), enc, fullPath);
    }

    /// <summary>Loads a JPG/PNG image from file and returns a Texture2D</summary>
    public static Texture2D ImportTexture(string file, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        byte[] image = ReadRawFile(file, enc, checkSA, fullPath);
        Texture2D texture = new Texture2D(2, 2); // Assigns minimum size
        texture.LoadImage(image);
        texture.Apply();
        return texture;
    }

    /// <summary>Loads a JPG/PNG image from file and returns a Sprite</summary>
    public static Sprite ImportSprite(string file, bool enc = false, bool checkSA = true, bool fullPath = false, Vector2 pivot = new Vector2())
    {
        Sprite icon = null;
        Texture2D texture = ImportTexture(file, enc, checkSA, fullPath);
        texture.Apply();
        if (texture.width >= 32 && texture.height >= 32)  // Minimum valid size
            icon = Sprite.Create(texture, new Rect(new Vector2(0f, 0f), new Vector2(texture.width, texture.height)), pivot);
        return icon;
    }

    /// <summary>Saves a texture to file formated as JPG (StreamingAssets is read only)</summary>
    public static void SaveJpgTexture(string name, Texture texture, int quality = 75, bool enc = false, bool fullPath = false)
    {
        Texture2D image = (Texture2D)texture;
        SaveRawFile(name, image.EncodeToJPG(quality), enc, fullPath);
    }

    /// <summary>Saves a texture to file formated as PNG (StreamingAssets is read only)</summary>
    public static void SavePngTexture(string name, Texture texture, bool enc = false, bool fullPath = false)
    {
        Texture2D image = (Texture2D)texture;
        SaveRawFile(name, image.EncodeToPNG(), enc, fullPath);
    }

#endif

    /// <summary>Checks file existence (checks PersistentData, then StreamingAssets)</summary>
    public static bool FileExists(string name, bool checkSA = true, bool fullPath = false)
    {
        // Check existance:
        bool result = false;
        if (!fullPath)
        {
            // Check PersistentData path first:
            string path = NormalizePath(persistentDataPath + "/" + name);
            result = System.IO.File.Exists(path);
            if (!result && checkSA)
            {
                // Then check StreamingAssets path:
#if UNITY_ANDROID || UNITY_WEBGL
                result = CheckNameOnIndex("StreamingAssets/" + name, "F");
#elif UNITY_5_3_OR_NEWER
                path = NormalizePath(streamingAssetsPath + "/" + name);
                result = System.IO.File.Exists(path);
#else
                if (_resources.Length > 0)
                {
                    // Format the filename to compare:
                    name = name.Replace('\\', '.');
                    name = name.Replace('/', '.');
                    name = _className + "." + name;
                    // Check for the requested file in the list:
                    for (int i = 0; i < _resources.Length; i++)
                    {
                        if (_resources[i] == name)
                        {
                            result = true;
                            break;
                        }
                    }
                }
#endif
            }
        }
        else
        {
            // Direct check:
            result = System.IO.File.Exists(name);
        }
        return result;
    }

    /// <summary>Save file with type convertion (StreamingAssets is read only)</summary>
    public static void SaveFile<T>(string name, T content, char separator, bool enc = false, bool fullPath = false)
    {
        // Force invariant culture:
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        // Convert string to byte array:
        byte[] contentArray = StringToByteArray(CustomToString(content, separator));
        SaveRawFile(name, contentArray, enc, fullPath);
    }
    /// <summary>Save file with type convertion (StreamingAssets is read only)</summary>
    public static void SaveFile<T>(string name, T content, bool enc = false, bool fullPath = false)
    {
        SaveFile(name, content, (char)0x09, enc, fullPath);
    }
    
    /// <summary>Read file with type conversion</summary>
    public static T ReadFile<T>(string name, char separator, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        byte[] contentArray = { };
        string content = "";
        T val = default(T);   // Return value.
        contentArray = ReadRawFile(name, enc, checkSA, fullPath);

        // Restore the string to parse:
        if (contentArray.Length > 0)
            content = ByteArrayToString(contentArray);
        else
            return default(T);  // There is no string, so return the apropriate null value.

        try
        {
            val = CustomParser<T>(content, separator);
        }
        catch (System.FormatException)
        {
            WriteLine("[FileManagement.ReadFile] Exception: FormatException - Trying to read data in the wrong format. (" + typeof(T) + "): " + name, true);
        }

        return val;
    }
    /// <summary>Read file with type conversion</summary>
    public static T ReadFile<T>(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        return ReadFile<T>(name, (char)0x09, enc, checkSA, fullPath);
    }

    /// <summary>Saves arrays of one dimension</summary>
    public static void SaveArray<T>(string name, T[] content, char separator = (char)0x09, bool enc = false, bool fullPath = false)
    {
        if (content.Length > 0)
            SaveFile(name, content, separator, enc, fullPath);
        else
            WriteLine("[FileManagement.SaveArray] Trying to save empty array: " + name, true);
    }
    /// <summary>Saves lists of one dimension</summary>
    public static void SaveList<T>(string name, List<T> content, char separator = (char)0x09, bool enc = false, bool fullPath = false)
    {
        if (content.Count > 0)
            SaveFile(name, content.ToArray(), separator, enc, fullPath);
        else
            WriteLine("[FileManagement.SaveList] Trying to save empty List<>: " + name, true);
    }

    /// <summary>Reads a one dimension Array from file</summary>
    public static T[] ReadArray<T>(string name, char separator = (char)0x09, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        return ReadFile<T[]>(name, separator, enc, checkSA, fullPath);
    }
    /// <summary>Reads a one dimension Array from file</summary>
    public static T[] ReadArray<T>(string name, string[] separator, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        return ReadList<T>(name, separator, enc, checkSA, fullPath).ToArray();
    }

    /// <summary>Reads a List from file</summary>
    public static List<T> ReadList<T>(string name, char separator = (char)0x09, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        T[] array = ReadFile<T[]>(name, separator, enc, checkSA, fullPath);
        if (array != null)
            return new List<T>(array);
        return new List<T>();
    }
    /// <summary>Reads a List from file (applies several separators)</summary>
    public static List<T> ReadList<T>(string name, string[] separator, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        List<T> list = new List<T>();
        string file = ReadFile<string>(name, enc, checkSA, fullPath);
        if (!string.IsNullOrEmpty(file))
        {
            string[] content = file.Split(separator, System.StringSplitOptions.None);
            for (int i = 0; i < content.Length; i++)
            {
                list.Add(CustomParser<T>(content[i]));
            }
        }
        return list;
    }

    /// <summary>Reads all text lines from a file</summary>
    public static string[] ReadAllLines(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        string[] eol = { "\r\n", "\n", "\r" };
        string file = ReadFile<string>(name, (char)0x09, enc, checkSA, fullPath);
        if (!string.IsNullOrEmpty(file))
            return file.Split(eol, System.StringSplitOptions.None);
        else
            return new string[] { };
    }

    /// <summary>Imports a .ini file into a FM_IniFile</summary>
    public static FM_IniFile ImportIniFile(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        FM_IniFile ini = new FM_IniFile();
        if (ini.LoadNewIniFile(name, enc, checkSA, fullPath))
            return ini;
        else
            return null;
    }

    /// <summary>Imports a .csv file into a two-dimensional string array</summary>
    public static string[][] ImportCsvFile(string name, char separator = (char)0x09, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        string[] file = ReadAllLines(name, enc, checkSA, fullPath);
        string[][] result = new string[file.Length][];
        for (int i = 0; i < file.Length; i++)
        {
            result[i] = file[i].Split(separator);
        }
        return result;
    }

    /// <summary>Saves a two-dimensional string array into a .csv file</summary>
    public static void SaveCsvFile(string name, string[][] content, char separator = (char)0x09, bool enc = false, bool fullPath = false)
    {
        SaveFile(name, "", enc, fullPath);
        for (int i = 0; i < content.GetLength(0); i++)
        {
            AddLine(name, string.Join(separator.ToString(), content[i]), enc, fullPath);
        }
    }

    /// <summary>Adds text lines to an existing file</summary>
    public static void AddLine(string name, string content, bool enc = false, bool fullPath = false)
    {
        lock (logLock)
        {
#if UNITY_WINRT
        // StreamingAssets is read-only, so avoid checking it:
        string savedContent = ReadFile<string>(name, enc, false, fullPath);
        if (string.IsNullOrEmpty(savedContent))
            savedContent = content;
        else
            savedContent += System.Environment.NewLine + content;
        // Save new content:
        SaveFile(name, savedContent, enc, fullPath);
#else
            if (FileExists(name, false, fullPath))
            {
                if (!fullPath)
                    name = NormalizePath(persistentDataPath + "/" + name);
                System.IO.File.AppendAllText(name, content + System.Environment.NewLine);
            }
            else
            {
                SaveFile(name, content + System.Environment.NewLine, enc, fullPath);
            }
#endif
        }
    }

    /// <summary>Adds text lines to an existing file (adds a date stamp header)</summary>
    public static void AddLogLine(string name, string content, bool enc = false, bool fullPath = false)
    {
        AddLine(name, System.DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss:fff") + content);
    }

    /// <summary>Adds raw data to an existing file (StreamingAssets is read only)</summary>
    public static void AddRawData(string name, byte[] content, bool enc = false, bool fullPath = false)
    {
        byte[] file = ReadRawFile(name, enc, true, fullPath);
        byte[] data = new byte[file.Length + content.Length];                   // New file size.
        System.Buffer.BlockCopy(file, 0, data, 0, file.Length);                 // Copy file first.
        System.Buffer.BlockCopy(content, 0, data, file.Length, content.Length); // Add raw data.
        SaveRawFile(name, data, enc, fullPath);
    }

    /// <summary>Checks directory existence (checks PersistentData, then StreamingAssets)</summary>
    public static bool DirectoryExists(string name, bool checkSA = true, bool fullPath = false)
    {
        // Check existance:
        bool result = false;
        if (!fullPath)
        {
            // Check PersistentData path first:
            string path = NormalizePath(persistentDataPath + "/" + name);
            result = System.IO.Directory.Exists(path);
            if (!result && checkSA)
            {
                // Then check StreamingAssets path:
#if UNITY_ANDROID || UNITY_WEBGL
                result = CheckNameOnIndex("StreamingAssets/" + name, "D");
#elif UNITY_5_3_OR_NEWER
                path = NormalizePath(streamingAssetsPath + "/" + name);
                result = System.IO.Directory.Exists(path);
#else
                // Format the folder name to compare:
                name = name.Replace('\\', '.');
                name = name.Replace('/', '.');
                name = _className + "." + name;
                // Check for the requested file in the list:
                for (int i = 0; i < _resources.Length; i++)
                {
                    if (_resources[i].StartsWith(name))
                    {
                        // Differentiate files and folders:
                        if (_resources[i].Remove(0, name.Length + 1).Split('.').Length > 1)
                        {
                            result = true;
                            break;
                        }
                    }
                }
#endif
            }
        }
        else
        {
            // Direct check:
            result = System.IO.Directory.Exists(name);
        }
        return result;
    }

    /// <summary>Create directory (StreamingAssets is read only)</summary>
    public static void CreateDirectory(string name, bool fullPath = false)
    {
        if (!fullPath)
            name = NormalizePath(persistentDataPath + "/" + name);
        if (!DirectoryExists(name, false, fullPath))
        {
            System.IO.Directory.CreateDirectory(name);
#if UNITY_WEBGL
            if(Application.platform == RuntimePlatform.WebGLPlayer)
                SyncFiles();
#endif
        }
    }

    /// <summary>Delete directory and its content (StreamingAssets is read only)</summary>
    public static void DeleteDirectory(string name, bool fullPath = false)
    {
        if (DirectoryExists(name, false, fullPath))
        {
            if (!fullPath)
                name = NormalizePath(persistentDataPath + "/" + name);
            System.IO.Directory.Delete(name, true);
#if UNITY_WEBGL
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                SyncFiles();
#endif
        }
        else
        {
            WriteLine("[FileManagement.DeleteDirectory] Can't delete, directory not found: " + name);
        }
    }

    /// <summary>Delete directory content (StreamingAssets is read only)</summary>
    public static void EmptyDirectory(string name = "", bool filesOnly = true, bool fullPath = false)
    {
        if (DirectoryExists(name, false, fullPath))
        {
            // Delete all files:
            string[] files = ListFiles(name, false, fullPath);
            for (int i = 0; i < files.Length; i++)
            {
                string path = NormalizePath(name + "/" + files[i]);
                DeleteFile(path, fullPath);
            }
            // Delete all fodlers also (if requested):
            if (!filesOnly)
            {
                string[] folders = ListDirectories(name, false, fullPath);
                for (int i = 0; i < folders.Length; i++)
                {
                    string path = NormalizePath(name + "/" + folders[i]);
                    DeleteDirectory(path, fullPath);
                }
            }

#if UNITY_WEBGL
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                SyncFiles();
#endif
        }
        else
        {
            WriteLine("[FileManagement.EmptyDirectory] Can't delete content, directory not found: " + name);
        }
    }

    /// <summary>List directory content (files only)</summary>
    public static string[] ListFiles(string name, bool checkSA = true, bool fullPath = false)
    {
        // Check existance:
        string[] result = null;
        try
        {
            if (!fullPath)
            {
                // Check PersistentData path first:
                string path = NormalizePath(persistentDataPath + "/" + name);
                path = NormalizePath(path);
                if (DirectoryExists(path, false, true))
                {
                    result = System.IO.Directory.GetFiles(path);
                    result = FilterPathNames(result);
                }
                // Then check StreamingAssets path:
                if (checkSA)
                {
#if UNITY_ANDROID || UNITY_WEBGL
                    if (DirectoryExists(name))
                    {
                        string[] temp = GetNamesOnIndex("StreamingAssets/" + name + "/", "F");
                        temp = FilterPathNames(temp);
                        if (result != null)
                            result = result.Union(temp).ToArray();
                        else
                            result = temp;
                    }
#elif UNITY_5_3_OR_NEWER
                    path = NormalizePath(streamingAssetsPath + "/" + name);
                    if (DirectoryExists(path, false, true))
                    {
                        string[] temp = System.IO.Directory.GetFiles(path);
                        temp = FilterPathNames(temp);
                        if (result != null)
                            result = result.Union(temp).ToArray();
                        else
                            result = temp;
                    }
#else
                    if (_resources.Length > 0)
                    {
                        // Format the folder name to compare:
                        name = name.Replace('\\', '.');
                        name = name.Replace('/', '.');
                        name = _className + "." + name;
                        if (name[name.Length - 1] != '.') name += ".";
                        // Check all resources:
                        List<string> files = new List<string>();
                        for (int i = 0; i < _resources.Length; i++)
                        {
                            if (_resources[i].StartsWith(name))
                            {
                                string[] names = _resources[i].Remove(0, name.Length).Split('.');
                                if (names.Length == 2)
                                    files.Add(names[0] + "." + names[1]);
                            }
                        }
                        if (result != null)
                            result = result.Union(files).ToArray();
                        else
                            result = files.ToArray();
                    }
#endif
                }
            }
            else
            {
                // Checks directly:
                if (DirectoryExists(name, false, true))
                {
                    result = System.IO.Directory.GetFiles(name);
                    result = FilterPathNames(result);
                }
            }
            SortPathNames(result);

            // Error message:
            if (result == null)
                WriteLine("[FileManagement.ListFiles] Can't read folder content, folder not found: " + name);

            return result;
        }
        catch (System.Exception e)
        {
            WriteLine("[FileManagement.ListFiles] Error: " + e.Message, true);
            return result;
        }
    }
    /// <summary>List directory content (files only) filtering by extension</summary>
    public static string[] ListFiles(string name, string[] filter, bool checkSA = true, bool fullPath = false, bool subdirectories = false)
    {
        // The filtered result list:
        List<string> result = new List<string>();

        // Get complete file list:
        string[] files = ListFiles(name, checkSA, fullPath);
        // Extension file filter:
        if (filter.Length > 0 && files != null)
        {
            // Check every file in the list:
            for (int i = 0; i < files.Length; i++)
            {
                // Check file extensions:
                for (int f = 0; f < filter.Length; f++)
                {
                    if (filter[f] == "" || filter[f].ToLower() == GetFileExtension(files[i]).ToLower())
                    {
                        result.Add(files[i]);
                        break;
                    }
                }
            }
        }

        // Search subfolders using the same settings:
        if (subdirectories)
        {
            string[] folders = ListDirectories(name, checkSA, fullPath);
            if (folders != null && folders.Length > 0)
            {
                for (int i = 0; i < folders.Length; i++)
                {
                    string subfolder = NormalizePath(name + "/" + folders[i]);
                    string[] tempFiles = ListFiles(subfolder, filter, checkSA, fullPath, subdirectories);
                    if (tempFiles != null)
                    {
                        for (int f = 0; f < tempFiles.Length; f++)
                        {
                            result.Add(NormalizePath(subfolder + "/" + tempFiles[f]));
                        }
                    }
                }
            }
        }

        return result.ToArray();        // Return file list.
    }

    /// <summary>List directory content (folders only)</summary>
    public static string[] ListDirectories(string name, bool checkSA = true, bool fullPath = false)
    {
        string[] result = null;
        try
        {
            if (!fullPath)
            {
                // Check PersistentData path first:
                string path = NormalizePath(persistentDataPath + "/" + name);
                if (DirectoryExists(path, false, true))
                {
                    result = System.IO.Directory.GetDirectories(path);
                    result = FilterPathNames(result);
                }
                // Then check StreamingAssets path:
                if (checkSA)
                {
#if UNITY_ANDROID || UNITY_WEBGL
                    if (DirectoryExists(name))
                    {
                        string[] temp = GetNamesOnIndex("StreamingAssets/" + name + "/", "D");    // Search for subfolders.
                        temp = FilterPathNames(temp);
                        if (result != null)
                            result = result.Union(temp).ToArray();
                        else
                            result = temp;
                    }
#elif UNITY_5_3_OR_NEWER
                    path = NormalizePath(streamingAssetsPath + "/" + name);
                    if (DirectoryExists(path, false, true))
                    {
                        string[] temp = System.IO.Directory.GetDirectories(path);
                        temp = FilterPathNames(temp);
                        if (result != null)
                            result = result.Union(temp).ToArray();
                        else
                            result = temp;
                    }
#else
                    if (_resources.Length > 0)
                    {
                        // Format the folder name to compare:
                        name = name.Replace('\\', '.');
                        name = name.Replace('/', '.');
                        name = _className + "." + name;
                        if (name[name.Length - 1] != '.') name += ".";
                        // Check all resources:
                        List<string> folders = new List<string>();
                        for (int i = 0; i < _resources.Length; i++)
                        {
                            if (_resources[i].StartsWith(name))
                            {
                                string[] names = _resources[i].Remove(0, name.Length).Split('.');
                                if (names.Length > 2)
                                {
                                    if (!folders.Contains(names[0]))
                                        folders.Add(names[0]);
                                }
                            }
                        }
                        if (result != null)
                            result = result.Union(folders).ToArray();
                        else
                            result = folders.ToArray();
                    }
#endif
                }
            }
            else
            {
                // Checks directly:
                if (DirectoryExists(name, false, true))
                {
                    result = System.IO.Directory.GetDirectories(name);
                    result = FilterPathNames(result);
                }
            }
            SortPathNames(result);
            // Error message:
            if (result == null)
                WriteLine("[FileManagement.ListDirectories] Can't read folder content, folder not found: " + name);
            return result;
        }
        catch (System.Exception e)
        {
            WriteLine("[FileManagement.ListDirectories] Error: " + e.Message, true);
            return result;
        }
    }

    /// <summary>Gets all files into a directory as a list of byte arrays</summary>
    public static List<byte[]> ReadDirectoryContent(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        List<byte[]> content = null;
        if (DirectoryExists(name, checkSA, fullPath))
        {
            content = new List<byte[]>();
            string[] list = ListFiles(name, checkSA, fullPath);
            for (int l = 0; l < list.Length; l++)
            {
                string path = NormalizePath(name + "/" + list[l]);
                byte[] buffer = ReadRawFile(path, enc, checkSA, fullPath);
                content.Add(buffer);
            }
        }
        else
        {
            WriteLine("[FileManagement.ReadDirectoryContent] Can't read folder content, folder not found: " + name);
        }
        return content;
    }

    /// <summary>Copies a file (not directories)</summary>
    public static void CopyFile(string source, string dest, bool checkSA = true, bool fullPathSource = false, bool fullPathDest = false)
    {
        if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(dest))
        {
            source = NormalizePath(source);
            dest = NormalizePath(dest);
            if (FileExists(source, checkSA, fullPathSource))                            // If source exists on persistent data, full path or StreamingAssets.
            {
                byte[] file = ReadRawFile(source, false, checkSA, fullPathSource);
                SaveRawFile(dest, file, false, fullPathDest);
            }
            else
            {
                WriteLine("[FileManagement.CopyFile] Source file not found: " + source);
            }
        }
    }

    /// <summary>Copies a directory with all its content recursively</summary>
    public static void CopyDirectory(string source, string dest, bool checkSA = true, bool fullPathSource = false, bool fullPathDest = false)
    {
        if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(dest))
        {
            source = NormalizePath(source);
            dest = NormalizePath(dest);
            if (DirectoryExists(source, checkSA, fullPathSource))                       // If source exists on persistent data, full path or StreamingAssets.
            {
                string[] files = ListFiles(source, checkSA, fullPathSource);            // Get the list of files.
                string[] folders = ListDirectories(source, checkSA, fullPathSource);    // Get the list of folders.
                // Copy every file:
                for (int f = 0; f < files.Length; f++)
                {
                    string pathS = NormalizePath(source + "/" + files[f]);              // Source file path
                    string pathD = NormalizePath(dest + "/" + files[f]);                // Destination file path
                    CopyFile(pathS, pathD, checkSA, fullPathSource, fullPathDest);
                }
                // Iterate on every folder:
                for (int f = 0; f < folders.Length; f++)
                {
                    string pathS = NormalizePath(source + "/" + folders[f]);            // Source folder path.
                    string pathD = NormalizePath(dest + "/" + folders[f]);              // Destination folder path.
                    CopyDirectory(pathS, pathD, checkSA, fullPathSource, fullPathDest);
                }
                CreateDirectory(dest, fullPathDest);
            }
            else
            {
                WriteLine("[FileManagement.CopyDirectory] Source path not found: " + source);
            }
        }
    }

    /// <summary>Moves files or directories</summary>
    public static void Move(string source, string dest, bool fullPathSource = false, bool fullPathDest = false)
    {
        if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(dest))
        {
            // If exists in persistent data or fullPath, then moves (StreamingAssets not allowed):
            if (FileExists(source, false, fullPathSource))
            {
                // We are moving a file:
                source = NormalizePath(source);
                dest = NormalizePath(dest);
                CreateDirectory(GetParentDirectory(dest), fullPathDest);
                // Load the source file, deletes the source file and save it with the destination name:
                byte[] data = ReadRawFile(source, false, false, fullPathSource);
                DeleteFile(source, fullPathSource);
                SaveRawFile(dest, data, false, fullPathDest);
            }
            else if (DirectoryExists(source, false, fullPathSource))
            {
                // We are moving a folder:
                CopyDirectory(source, dest, false, fullPathSource, fullPathDest);
                DeleteDirectory(source, fullPathSource);
            }
            else
                WriteLine("[FileManagement.Move] Source file not found: " + source);
        }
    }
    /// <summary>Renames files or directories</summary>
    public static void Rename(string source, string dest, bool fullPathSource = false, bool fullPathDest = false)
    {
        // If exists in persistent data or fullPath, then moves (StreamingAssets not allowed):
        if (FileExists(source, false, fullPathSource) || DirectoryExists(source, false, fullPathSource))
            Move(source, dest, fullPathSource, fullPathDest);
        else
            WriteLine("[FileManagement.Rename] Source file not found: " + source);
    }

    /// <summary>Gets the parent directory of a file/directory</summary>
    public static string GetParentDirectory(string path)
    {
        path = NormalizePath(path);
        int slash = path.LastIndexOf('/');
        if (slash >= 0)
        {
            if (path == System.IO.Path.GetPathRoot(path))
                path = "";
            else
                path = path.Substring(0, slash);
        }
        else
            path = "";
        return NormalizePath(path);
    }

    /// <summary>Combines both paths into a single path correctly</summary>
    public static string Combine(string path1, string path2)
    {
        return System.IO.Path.Combine(path1, path2);
    }

    /// <summary>Normalizes a path name</summary>
    public static string NormalizePath(string path)
    {
        path = path.Replace('\\', '/');                         // Use simple slashes only.
        path = path.Replace("//", "/");                         // Use simple slashes only.
#if UNITY_5_3_OR_NEWER
        if (path.Contains(":/") && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer))
            path = path.Replace(":/", "://");                   // Restore URLs.
#endif
        if (path.Length >= 1 && path[path.Length - 1] == '/')   // Delete the last slash (to prevent DirectoryExists from failure)
            path = path.Substring(0, path.Length - 1);
        // Windows only:
        if (System.Environment.OSVersion.ToString().Contains("Windows") && path.Length >= 1 && path[0] == '/')
            path = path.Substring(1);
        path = path.Replace("..", "");                          // Delete double dot to avoid "out of bounds" navigation.
        path = path.Replace("/.", "/");                         // Delete slash dot to avoid "out of bounds" navigation.
        if (path.Length == 2 && path[path.Length - 1] == ':')   // To prevent failure for "C:" like paths.
            path += '/';
        return path;
    }

    /// <summary>Retrieves a file/directory name from a path</summary>
    public static string GetFileName(string path)
    {
        path = NormalizePath(path);
        return System.IO.Path.GetFileName(path);
    }

    /// <summary>Retrieves a file/directory name from a path</summary>
    public static string GetFileNameWithoutExtension(string path)
    {
        path = NormalizePath(path);
        string file = System.IO.Path.GetFileName(path);
        int point = file.LastIndexOf('.');
        if (point > 0)
            return file.Substring(0, point);
        else
            return file;
    }

    /// <summary>Gets the file extension (even from a path)</summary>
    public static string GetFileExtension(string path)
    {
        int point = path.LastIndexOf('.');
        string ext = "";
        if (point > 0)
            ext = path.Substring(point);
        return ext;
    }

#if UNITY_5_3_OR_NEWER
    /// <summary>Parses string to requested types</summary>
    public static T CustomParser<T>(string content, char separator = (char)0x09)
    {
        string[] values = { };                          // Separate values to be parsed.
        // Type information:
        System.Type type = typeof(T);                   // Can be an array.
        System.Type element = type.GetElementType();    // Array element (null if 'type' is not an array).
        if (string.IsNullOrEmpty(content))
        {
            if (typeof(T).ToString() == "System.String")
                return (T)System.Convert.ChangeType("", typeof(T));
            else
                return default(T);
        }
        else if (type.IsArray)
        {
            // Call to CustomParser casted to requested method dynamically:
            System.Reflection.MethodInfo parser = typeof(FileManagement).GetMethod("CustomParser").MakeGenericMethod(element);
            // Parse data to the request data type:
            values = content.Split(separator);
            var array = System.Array.CreateInstance(element, values.Length);
            for (int i = 0; i < array.Length; i++)
            {
                array.SetValue(parser.Invoke(null, new object[] { values[i], separator }), i);
            }
            return (T)System.Convert.ChangeType(array, type);
        }
        else
        {
            switch (type.ToString())
            {
                case "UnityEngine.Vector2":
                    content = content.Substring(1, content.Length - 2);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Vector2(float.Parse(values[0]), float.Parse(values[1])), type);
                case "UnityEngine.Vector3":
                    content = content.Substring(1, content.Length - 2);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2])), type);
                case "UnityEngine.Vector4":
                    content = content.Substring(1, content.Length - 2);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Vector4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])), type);
                case "UnityEngine.Quaternion":
                    content = content.Substring(1, content.Length - 2);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Quaternion(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])), type);
                case "UnityEngine.Rect":
                    content = content.Substring(1, content.Length - 2);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Rect(float.Parse(values[0].Split(':')[1]), float.Parse(values[1].Split(':')[1]), float.Parse(values[2].Split(':')[1]), float.Parse(values[3].Split(':')[1])), type);
                case "UnityEngine.Color":
                    content = content.Substring(5, content.Length - 6);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Color(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3])), type);
                case "UnityEngine.Color32":
                    content = content.Substring(5, content.Length - 6);     // Delete parentheses
                    values = content.Split(',');
                    return (T)System.Convert.ChangeType(new Color32(byte.Parse(values[0]), byte.Parse(values[1]), byte.Parse(values[2]), byte.Parse(values[3])), type);
                case "FM_IniFile":
                    return (T)System.Convert.ChangeType(FM_IniFile.Parse(content, separator), type);   // Discart separator if convertinf to INI file, use always NewLine.
                default:
                    return (T)System.Convert.ChangeType(content, typeof(T));
            }
        }
    }
    /// <summary>Converts to string (preserving float resolution)</summary>
    public static string CustomToString<T>(T content, char separator = (char)0x09)
    {
        // Type information:
        System.Type type = typeof(T);                   // Can be an array.
        System.Type element = type.GetElementType();    // Array element (null if 'type' is not an array).
        if (type.IsArray)
        {
            System.Array array = content as System.Array;
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            System.Reflection.MethodInfo toString = typeof(FileManagement).GetMethod("CustomToString").MakeGenericMethod(element);
            for (int i = 0; i < array.GetLength(0); i++)
            {
                result.Append(toString.Invoke(null, new object[] { array.GetValue(i), separator }));
                if (i < array.GetLength(0) - 1)
                    result.Append(separator);
            }
            return result.ToString();
        }
        else
        {
            switch (type.ToString())
            {
                case "UnityEngine.Vector2":
                    Vector2 v2 = (Vector2)System.Convert.ChangeType(content, type);
                    return "(" + v2.x + "," + v2.y + ")";
                case "UnityEngine.Vector3":
                    Vector3 v3 = (Vector3)System.Convert.ChangeType(content, type);
                    return "(" + v3.x + "," + v3.y + "," + v3.z + ")";
                case "UnityEngine.Vector4":
                    Vector4 v4 = (Vector4)System.Convert.ChangeType(content, type);
                    return "(" + v4.x + "," + v4.y + "," + v4.z + "," + v4.w + ")";
                case "UnityEngine.Quaternion":
                    Quaternion q = (Quaternion)System.Convert.ChangeType(content, type);
                    return "(" + q.x + "," + q.y + "," + q.z + "," + q.w + ")";
                case "FM_IniFile":
                    return (content as FM_IniFile).ToString(separator);
                default:
                    return content.ToString();
            }
        }
    }
#else
    /// <summary>Parse strings to requested types</summary>
    public static T CustomParser<T>(string content, char separator = (char)0x09)
    {
        // Type information:
        System.Type type = typeof(T);                   // Can be an array.
        System.Type element = type.GetElementType();    // Array element (null if type is not an array).
        if (string.IsNullOrEmpty(content))
        {
            if (typeof(T).ToString() == "System.String")
                return (T)System.Convert.ChangeType("", typeof(T));
            else
                return default(T);
        }
        else if (type.IsArray)
        {
            // Call to CustomParser casted to requested method dynamically:
            System.Reflection.MethodInfo parser = typeof(FileManagement).GetMethod("CustomParser").MakeGenericMethod(element);
            // Parse data to the request data type:
            string[] values = content.Split(separator);
            var array = System.Array.CreateInstance(element, values.Length);
            for (int i = 0; i < array.Length; i++)
            {
                array.SetValue(parser.Invoke(null, new object[] { values[i], separator }), i);
            }
            return (T)System.Convert.ChangeType(array, type);
        }
        else
        {
            switch (type.ToString())
            {
                case "FM_IniFile":
                    return (T)System.Convert.ChangeType(FM_IniFile.Parse(content), type);   // Discart separator if convertinf to INI file, use always NewLine.
                default:
                    return (T)System.Convert.ChangeType(content, typeof(T));
            }
        }
    }
    /// <summary>Converts to string (preserving float resolution)</summary>
    public static string CustomToString<T>(T content, char separator = (char)0x09)
    {
        // Type information:
        System.Type type = typeof(T);                   // Can be an array.
        System.Type element = type.GetElementType();    // Array element (null if 'type' is not an array).
        if (type.IsArray)
        {
            System.Array array = content as System.Array;
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            System.Reflection.MethodInfo toString = typeof(FileManagement).GetMethod("CustomToString").MakeGenericMethod(element);
            for (int i = 0; i < array.GetLength(0); i++)
            {
                result.Append(toString.Invoke(null, new object[] { array.GetValue(i), separator }));
                if (i < array.GetLength(0) - 1)
                    result.Append(separator);
            }
            return result.ToString();
        }
        else
        {
            switch (type.ToString())
            {
                case "FM_IniFile":
                    return (content as FM_IniFile).ToString(separator);
                default:
                    return content.ToString();
            }
        }
    }
#endif
    /// <summary>Encryption call</summary>
    public static byte[] Encrypt(byte[] data, byte[] key)
    {
#if USE_AES
        return AesEncrypt(data, key);           // Aes encryption.
#else
        return XorEncryptDecrypt(data, key);    // Xor encryption.
#endif
    }
    /// <summary>Decryption call</summary>
    public static byte[] Decrypt(byte[] data, byte[] key)
    {
#if USE_AES
        return AesDecrypt(data, key);           // Aes decryption.
#else
        return XorEncryptDecrypt(data, key);   // Xor decryption.
#endif
    }

    /// <summary>Custom string conversion</summary>
    public static string ByteArrayToString(byte[] content)
    {
        string result = "";
        switch (stringConversion)
        {
#if !(UNITY_WINRT_8_1 || UNITY_WP_8_1 || UNITY_WSA_8_1)
            case FM_StringMode.ASCII:
                result = System.Text.Encoding.ASCII.GetString(content);
                break;
            case FM_StringMode.UTF32:
                result = System.Text.Encoding.UTF32.GetString(content);
                break;
            case FM_StringMode.UTF7:
                result = System.Text.Encoding.UTF7.GetString(content);
                break;
            case FM_StringMode.BigEndianUnicode:
                result = System.Text.Encoding.BigEndianUnicode.GetString(content);
                break;
            case FM_StringMode.Unicode:
                result = System.Text.Encoding.Unicode.GetString(content);
                break;
            case FM_StringMode.UTF8:
                result = System.Text.Encoding.UTF8.GetString(content);
                break;
#endif
            default:
                char[] chars = new char[content.Length];
                content.CopyTo(chars, 0);
                result = new string(chars);
                break;
        }
        return result;
    }

    /// <summary>Custom string conversion</summary>
    public static byte[] StringToByteArray(string content)
    {
        byte[] result = { };
        switch (stringConversion)
        {
#if !(UNITY_WINRT_8_1 || UNITY_WP_8_1 || UNITY_WSA_8_1)
            case FM_StringMode.ASCII:
                result = System.Text.Encoding.ASCII.GetBytes(content);
                break;
            case FM_StringMode.UTF32:
                result = System.Text.Encoding.UTF32.GetBytes(content);
                break;
            case FM_StringMode.UTF7:
                result = System.Text.Encoding.UTF7.GetBytes(content);
                break;
            case FM_StringMode.BigEndianUnicode:
                result = System.Text.Encoding.BigEndianUnicode.GetBytes(content);
                break;
            case FM_StringMode.Unicode:
                result = System.Text.Encoding.Unicode.GetBytes(content);
                break;
            case FM_StringMode.UTF8:
                result = System.Text.Encoding.UTF8.GetBytes(content);
                break;
#endif
            default:
                result = new byte[content.Length];
                for (int c = 0; c < result.Length; c++) result[c] = (byte)content[c];
                break;
        }
        return result;
    }

    /// <summary>Deletes the path strings and leaves the file/directory names</summary>
    static string[] FilterPathNames(string[] names)
    {
        if (names != null)
        {
            for (int r = 0; r < names.Length; r++)
            {
                names[r] = System.IO.Path.GetFileName(names[r]);
            }
        }
        return names;
    }

    /// <summary>Sorts the names alphabetically</summary>
    static string[] SortPathNames(string[] names)
    {
        if (names != null)
        {
            System.Array.Sort(names);
        }
        return names;
    }

#if USE_AES
    /// <summary>AES initialization</summary>
    static readonly AesManaged aes;
    /// <summary>Class constructor</summary>
    static FileManagement()
    {
        aes = new AesManaged { Key = key };
        aes.BlockSize = key.Length * 8;     // Key size expresed in bits (16 * 8 = 128).
    }
    /// <summary>AES encryption</summary>
    static byte[] AesEncrypt(byte[] data, byte[] key)
    {
        using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, key))
        {
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
    /// <summary>AES decryption</summary>
    static byte[] AesDecrypt(byte[] data, byte[] key)
    {
        using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, key))
        {
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
#else
    /// <summary>Basic xor encryption/decryption algorithm</summary>
    static byte[] XorEncryptDecrypt(byte[] data, byte[] key)
    {
        int d = 0;
        int k = 0;
        byte[] output = new byte[data.Length];

        while (d < data.Length)
        {
            while (k < key.Length && d < data.Length)
            {
                output[d] = (byte)(data[d] ^ key[k]);
                d++;
                k++;
            }
            k = 0;
        }
        return output;
    }
#endif

    /********************************
        PlayerPrefs compatibility:
    ********************************/
    /// <summary>Deletes all files into PersistentData folder</summary>
    public static void DeleteAll()
    {
        EmptyDirectory();
    }
    /// <summary>Deletes a key</summary>
    public static void DeleteKey(string key)
    {
        DeleteFile(key);
    }
    /// <summary>Reads a float value from disk</summary>
    public static float GetFloat(string key, float defaultValue = 0.0F)
    {
        float retVal = defaultValue;
        if (FileExists(key))
            retVal = ReadFile<float>(key);
        return retVal;
    }
    /// <summary>Reads an int value from disk</summary>
    public static int GetInt(string key, int defaultValue = 0)
    {
        int retVal = defaultValue;
        if (FileExists(key))
            retVal = ReadFile<int>(key);
        return retVal;
    }
    /// <summary>Reads a string from disk</summary>
    public static string GetString(string key, string defaultValue = "")
    {
        string retVal = defaultValue;
        if (FileExists(key))
            retVal = ReadFile<string>(key);
        return retVal;
    }
    /// <summary>Checks the existence of a key</summary>
    public static bool HasKey(string key)
    {
        return FileExists(key);
    }
    /// <summary>This has no efect (PlayerPrefs compatibility)</summary>
    public static void Save()
    {
        WriteLine("[FileManagement.Save] This method has no effect, data is already saved.");
    }
    /// <summary>Saves a float value to disk</summary>
    public static void SetFloat(string key, float value)
    {
        SaveFile(key, value);
    }
    /// <summary>Saves an int value to disk</summary>
    public static void SetInt(string key, int value)
    {
        SaveFile(key, value);
    }
    /// <summary>Saves a string to disk</summary>
    public static void SetString(string key, string value)
    {
        SaveFile(key, value);
    }

    // Non standard methods:
    // ---------------------
    /// <summary>Reads a bool value from disk</summary>
    public static bool GetBool(string key, bool defaultValue = false)
    {
        bool retVal = defaultValue;
        if (FileExists(key))
            retVal = ReadFile<bool>(key);
        return retVal;
    }
    /// <summary>Reads a double value from disk</summary>
    public static double GetDouble(string key, double defaultValue = 0)
    {
        double retVal = defaultValue;
        if (FileExists(key))
            retVal = ReadFile<double>(key);
        return retVal;
    }
    /// <summary>Saves a bool value to disk</summary>
    public static void SetBool(string key, bool value)
    {
        SaveFile(key, value);
    }
    /// <summary>Saves a double value to disk</summary>
    public static void SetDouble(string key, double value)
    {
        SaveFile(key, value);
    }

    /********************************
        Experimental functionality:
    ********************************/
    /// <summary> Run a command line including arguments or not (not widely supported)</summary>
#if UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS || UNITY_WINRT
    public static void RunCmd(string path = "", bool fullPath = false)
    {
        WriteLine("[FileManagement.RunCmd] Not supported in this platform.", true);
    }
#else
    public static void RunCmd(string pathOrCmd)
    {
        if (!string.IsNullOrEmpty(pathOrCmd))
        {
            string[] cmd = pathOrCmd.Split(new char[] { ' ' }, 2);
            if (cmd.Length == 1)
                System.Diagnostics.Process.Start(cmd[0]);
            else
                System.Diagnostics.Process.Start(cmd[0], cmd[1]);
        }
        else
        {
            WriteLine("[FileManagement.RunCmd] The requested command was empty.", true);
        }
    }
#endif

#if UNITY_WINRT
    /// <summary>Serialize object (works with default & user defined types only)</summary>
    public static byte[] ObjectToByteArray(object obj)
    {
        WriteLine("[FileManagement.ObjectToByteArray] Not supported in this platform.", true);
        return null;
    }
    /// <summary>Deseerialize object (works with default & user defined types only)</summary>
    public static object ByteArrayToObject(byte[] arrBytes)
    {
        WriteLine("[FileManagement.ByteArrayToObject] Not supported in this platform.", true);
        return null;
    }
#else
    /// <summary>Serialize object (works with default & user defined types only)</summary>
    public static byte[] ObjectToByteArray(object obj)
    {
        if (obj == null) return null;
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        System.IO.MemoryStream ms = new System.IO.MemoryStream();
        bf.Serialize(ms, obj);
        return ms.ToArray();
    }
    /// <summary>Deseerialize object (works with default & user defined types only)</summary>
    public static object ByteArrayToObject(byte[] arrBytes)
    {
        System.IO.MemoryStream memStream = new System.IO.MemoryStream();
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        memStream.Write(arrBytes, 0, arrBytes.Length);
        memStream.Seek(0, System.IO.SeekOrigin.Begin);
        return bf.Deserialize(memStream);
    }
#endif

    /// <summary>List logical drives</summary>
#if UNITY_WINRT
    public static string[] ListLogicalDrives()
    {
        WriteLine("[FileManagement.ListLogicDrives] Not supported in this platform.", true);
        return null;
    }
#else
    public static string[] ListLogicalDrives()
    {
        string[] drives = System.IO.Directory.GetLogicalDrives();
        for (int d = 0; d < drives.Length; d++)
        {
            drives[d] = NormalizePath(drives[d]);
        }
        return drives;
    }
#endif

    /// <summary>Run a local APK file installation in an external process.</summary>
#if UNITY_ANDROID
    public static void InstallLocalApk(string file, bool fullPath = false)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (FileExists(file, true, fullPath))
            {
                // Get the main Activity, Context and classes:
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
                AndroidJavaClass uri = new AndroidJavaClass("android.net.Uri");
                // Evaluate the Andrlid API level:
                AndroidJavaClass build = new AndroidJavaClass("android.os.Build$VERSION");
                int api = build.GetStatic<int>("SDK_INT");
                if (api <= 23)
                {
                    // Create path object to the apk file (Uri):
                    AndroidJavaObject targetUri = uri.CallStatic<AndroidJavaObject>("parse", new object[] { "file://" + file });
                    // Create and set the Intent object to be executed:
                    AndroidJavaClass intent = new AndroidJavaClass("android.content.Intent");
                    AndroidJavaObject promptInstall = new AndroidJavaObject("android.content.Intent", new object[] { intent.GetStatic<string>("ACTION_VIEW") });
                    promptInstall = promptInstall.Call<AndroidJavaObject>("setDataAndType", new object[] { targetUri, "application/vnd.android.package-archive" });
                    promptInstall = promptInstall.Call<AndroidJavaObject>("setFlags", new object[] { intent.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK") });
                    // Call the installation:
                    context.Call("startActivity", new object[] { promptInstall });
                }
                else
                {
                    // Open file content:
                    AndroidJavaObject fileContent = new AndroidJavaObject("java.io.File", new object[] { file });
                    AndroidJavaObject targetUri = uri.CallStatic<AndroidJavaObject>("fromFile", new object[] { fileContent });
                    // Create and set the Intent object to be executed:
                    AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", new object[] { });
                    intent = intent.Call<AndroidJavaObject>("setDataAndType", new object[] { targetUri, "application/vnd.android.package-archive" });
                    intent = intent.Call<AndroidJavaObject>("addFlags", new object[] { intent.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION") });
                    // Run the installation:
                    context.Call("startActivity", new object[] { intent });
                }
            }
            else
            {
                WriteLine("[FileManagement.InstallLocalApk] The requested APK file wasn't found.");
            }
        }
        else
        {
            WriteLine("[FileManagement.InstallLocalApk] Please export to some Android device.");
        }
    }
#else
    public static void InstallLocalApk(string file, bool fullPath = false)
    {
        WriteLine("[FileManagement.InstallLocalApk] Not supported in this platform.");
    }
#endif

    /// <summary> Detects the Mono runtime (Unity, MonoDevelop, Xamarin) </summary>
    public static bool IsRunningOnMono()
    {
        return System.Type.GetType("Mono.Runtime") != null;
    }

    /// <summary> Returns the name of the application (Unity, MonoDevelop, Xamarin) </summary>
    public static string GetAppName()
    {
#if UNITY_5_3_OR_NEWER
        return appName;
#else
        return _assembly.GetName().Name;
#endif
    }

    /// <summary>Writes to the console depending on the platform.</summary>
    static void WriteLine(string line, bool error = false)
    {
#if UNITY_5_3_OR_NEWER
        if (error)
            Debug.LogError(line);
        else
            Debug.Log(line);
#else
        System.Console.WriteLine(line);
#endif
    }
}

/// <summary> Ini file parser </summary>
public class FM_IniFile
{
    internal Dictionary<string, string> _iniFile = new Dictionary<string, string>();
    char _keySeparator = (char)0x01;

    /// <summary> Constructors </summary>
    public FM_IniFile() { }
    public FM_IniFile(FM_IniFile ini)
    {
        _iniFile = new Dictionary<string, string>(ini._iniFile);
    }
    public FM_IniFile(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        LoadNewIniFile(name, enc, checkSA, fullPath);
    }

    /// <summary> String parser (char 0x00 defaults to EndOfLine) </summary>
    public static FM_IniFile Parse(string content, char separator = (char)0x00)
    {
        FM_IniFile ini = new FM_IniFile();
        // Read the lines into the ini file object:
        if (!ini.LoadNewIni(content, separator))
            WriteLine("[FM_IniFile.Parse] Wrong file format.");
        return ini;
    }
    /// <summary> String parser (char 0x00 defaults to EndOfLine) </summary>
    public static FM_IniFile Parse(byte[] content, char separator = (char)0x00)
    {
        FM_IniFile ini = new FM_IniFile();
        // Read the lines into the ini file object:
        if (!ini.LoadNewIni(FileManagement.ByteArrayToString(content), separator))
            WriteLine("[FM_IniFile.Parse] Wrong file format.");
        return ini;
    }

    /// <summary> File parser </summary>
    public bool LoadNewIniFile(string name, bool enc = false, bool checkSA = true, bool fullPath = false)
    {
        // Load the ini file content:
        if (FileManagement.FileExists(name, checkSA, fullPath))
        {
            string[] lines = FileManagement.ReadAllLines(name, enc, checkSA, fullPath);
            return LoadNewIni(lines);
        }
        else
        {
            WriteLine("[FM_IniFile.LoadNewIniFile] File not found.");
            return false;
        }
    }

    /// <summary> String parser (char 0x00 defaults to EndOfLine) </summary>
    public bool LoadNewIni(string content, char separator = (char)0x00)
    {
        string[] eol = { "\r\n", "\n", "\r" };
        // Get all lines from the provided string:
        string[] lines;
        // The char 0x256 doesn't exists, so it defaults to EndOfLine separators:
        if (separator == (char)0x00)
            lines = content.Split(eol, System.StringSplitOptions.RemoveEmptyEntries);
        else
            lines = content.Split(separator);
        // Read the lines into the ini file object:
        return LoadNewIni(lines);
    }
    /// <summary> Parses a string to the INI file class </summary>
    public bool LoadNewIni(string[] content)
    {
        try
        {
            // Analize the file content line by line:
            string section = "";
            for (int i = 0; i < content.Length; i++)
            {
                string current = content[i].Trim();                                     // Delete empty spaces at both sides of the text.
                if (!string.IsNullOrEmpty(current))                                     // Discard empty lines.
                {
                    // Check for section, comment or data:
                    switch (current[0])
                    {
                        // Comments are discarded:
                        case ';':
                            break;
                        // Section tag:
                        case '[':
                            section = current.Trim(new char[] { '[', ']' });
                            break;
                        // Everything else may be some valid data:
                        default:
                            string[] data = current.Split(new char[] { '=' }, 2);       // Splits by the first '=' character only.
                            string key = data[0].Trim();                                // Deletes spaces at begining and end.
                            if (!string.IsNullOrEmpty(section))
                                key = section + _keySeparator + key;
                            string value = data[1].Split(new char[] { ';' }, 2)[0];     // Discard comments in the same line.
                            value = value.Trim().Trim(new char[] { '"' });              // Deletes spaces and quotes at begining and end.
                            if (_iniFile.ContainsKey(key))                              // If key already exists, then it's updated.
                                _iniFile[key] = value;
                            else
                                _iniFile.Add(key, value);
                            break;
                    }
                }
            }
            return true;
        }
        catch (System.Exception e)
        {
            WriteLine("[FM_IniFile.LoadNewIniArray] Wrong file format. (" + e.Message + ")");
            return false;
        }
    }

    /// <summary> Serialized the INI file into a string using the provided separator (char 0x00 defaults to EndOfLine) </summary>
    public string ToString(char separator = (char)0x00, bool sort = false)
    {
        // If no separator is provided, it defaults to NewLine:
        string _separator = char.ToString(separator);
        if (separator == 0x00)
            _separator = System.Environment.NewLine;
        // Data convertion:
        System.Text.StringBuilder iniContent = new System.Text.StringBuilder(); // Here goes the Ini file final content.
        string section = "";
        string[] sections = GetSectionList(true);   // Sections must be sorted (empty section must always go first).
        string[] keys;                              // Parameters without section goes first.
        // Add the keys by sections:
        for (int s = 0; s < sections.Length; s++)
        {
            // Go to the next section:
            section = sections[s];
            keys = GetKeyList(section, "", sort);
            // Add the section:
            if (section != "")
            {
                iniContent.Append('[');
                iniContent.Append(section);
                iniContent.Append(']');
                iniContent.Append(_separator);
            }
            // Add the keys:
            for (int k = 0; k < keys.Length; k++)
            {
                string val = GetKey(keys[k], "", section);
                if (val != "" && (val[0] == ' ' || val[val.Length - 1] == ' '))
                {
                    iniContent.Append(keys[k]);
                    iniContent.Append(" = \"");
                    iniContent.Append(val);
                    iniContent.Append("\"");
                    iniContent.Append(_separator);
                }
                else
                {
                    iniContent.Append(keys[k]);
                    iniContent.Append(" = ");
                    iniContent.Append(val);
                    iniContent.Append(_separator);
                }
            }
        }
        return iniContent.ToString();
    }

    /// <summary> Returns the content of a given key </summary>
    public T GetKey<T>(string key, T defaultValue, string section = "", char separator = (char)0x09)
    {
        // Key:
        if (!string.IsNullOrEmpty(section))
            key = section + _keySeparator + key;
        // Get the value:
        if (_iniFile.ContainsKey(key))
            return FileManagement.CustomParser<T>(_iniFile[key], separator);
        else
            return defaultValue;
    }

    /// <summary> Modifies a key value (if it exists)</summary>
    public void SetKey<T>(string key, T value, string section = "")
    {
        if (KeyExists(key, section))
            AddKey(key, value, section);
        else
            WriteLine("[FM_IniFile.SetKey] Key not found.");
    }

    /// <summary> Add a new key value (Or updates if exists) </summary>
    public void AddKey<T>(string key, T value, string section = "", char separator = (char)0x09)
    {
        // Key:
        if (!string.IsNullOrEmpty(section))
            key = section + _keySeparator + key;
        // Add the value:
        if (!_iniFile.ContainsKey(key))
            _iniFile.Add(key, FileManagement.CustomToString(value, separator));                 // Creates the new value.
        else
            _iniFile[key] = FileManagement.CustomToString(value, separator);                    // Updates the existing value.
    }

    /// <summary> Remove a key value </summary>
    public void RemoveKey(string key, string section = "")
    {
        // Key:
        if (!string.IsNullOrEmpty(section))
            key = section + _keySeparator + key;
        // Remove the value:
        if (_iniFile.ContainsKey(key))
            _iniFile.Remove(key);
        else
            WriteLine("[FM_IniFile.RemoveKey] Key not found.");
    }

    /// <summary> Copies a complete section from another FM_IniFile </summary>
    public void CopySection(FM_IniFile source, string section = "")
    {
        if (source.SectionExists(section))
        {
            string[] rawKeys = source._iniFile.Keys.ToArray();
            for (int i = 0; i < rawKeys.Length; i++)
            {
                int index = rawKeys[i].LastIndexOf(_keySeparator);
                string s = index > 0 ? rawKeys[i].Substring(0, index) : "";
                if (section == s)
                    _iniFile[rawKeys[i]] = source._iniFile[rawKeys[i]];
            }
        }
    }

    /// <summary> Remove a complete section </summary>
    public void RemoveSection(string section = "")
    {
        string[] rawKeys = _iniFile.Keys.ToArray();
        for (int i = 0; i < rawKeys.Length; i++)
        {
            int index = rawKeys[i].LastIndexOf(_keySeparator);
            string s = index > 0 ? rawKeys[i].Substring(0, index) : "";
            if (section == s)
                _iniFile.Remove(rawKeys[i]);
        }
    }

    /// <summary> Rename a section keeping it's content </summary>
    public void RenameSection(string section, string newName)
    {
        if (SectionExists(section))
        {
            string[] keys = GetKeyList(section);
            for (int k = 0; k < keys.Length; k++)
            {
                AddKey(keys[k], GetKey(keys[k], "", section), newName);
                RemoveKey(keys[k], section);
            }
        }
    }

    /// <summary> Checks if a value exists </summary>
    public bool KeyExists(string key, string section = "")
    {
        // Key:
        if (!string.IsNullOrEmpty(section))
            key = section + _keySeparator + key;
        // If the key exists:
        return _iniFile.ContainsKey(key);
    }

    /// <summary> Checks if a section exists </summary>
    public bool SectionExists(string section)
    {
        if (!string.IsNullOrEmpty(section))
        {
            string[] sections = GetSectionList();
            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i] == section)
                    return true;                                                        // Section found.
            }
            return false;                                                               // Section not found.
        }
        return true;                                                                    // The default empty "" section always exists.
    }

    /// <summary> Get the list of the sections </summary>
    public string[] GetSectionList(bool sort = false)
    {
        List<string> sections = new List<string>();
        string[] rawKeys = _iniFile.Keys.ToArray();
        if(rawKeys != null)
        {
            for (int i = 0; i < rawKeys.Length; i++)
            {
                int index = rawKeys[i].LastIndexOf(_keySeparator);
                string s = index > 0 ? rawKeys[i].Substring(0, index) : "";
                if (!sections.Contains(s))
                    sections.Add(s);
            }
            if (sort) sections.Sort();
        }
        return sections.ToArray();
    }
    /// <summary> Get the filtered list of the sections </summary>
    public string[] GetSectionList(string startsWith, bool sort = false)
    {
        List<string> sections = new List<string>();
        string[] rawKeys = _iniFile.Keys.ToArray();
        for (int i = 0; i < rawKeys.Length; i++)
        {
            int index = rawKeys[i].LastIndexOf(_keySeparator);
            string s = index > 0 ? rawKeys[i].Substring(0, index) : "";
            if (!sections.Contains(s) && s.StartsWith(startsWith))
                sections.Add(s);
        }
        if (sort) sections.Sort();
        return sections.ToArray();
    }

    /// <summary> Get filtered list of keys by section </summary>
    public string[] GetKeyList(string section = "", string startsWith = "", bool sort = false)
    {
        List<string> keys = new List<string>();
        string[] rawKeys = _iniFile.Keys.ToArray();
        for (int i = 0; i < rawKeys.Length; i++)
        {
            int index = rawKeys[i].LastIndexOf(_keySeparator);
            string s = index > 0 ? rawKeys[i].Substring(0, index) : "";     // Get section to compare.
            string k = rawKeys[i].Substring(index + 1);                     // Get key to analyze.
            if (section == s && k.StartsWith(startsWith))
                keys.Add(k);
        }
        if (sort) keys.Sort();
        return keys.ToArray();
    }

    /// <summary> Save the content into a file </summary>
    public void Save(string name, bool sort = false, bool enc = false, bool fullPath = false)
    {
        string iniContent = ToString((char)0x00, sort);
        // Save the Ini file:
        FileManagement.SaveFile(name, iniContent, enc, fullPath);
    }

    /// <summary> Merge the source ini file with this ini file </summary>
    public void Merge(FM_IniFile source)
    {
        if (source != null)
        {
            string[] sections = source.GetSectionList();
            for (int s = 0; s < sections.Length; s++)
            {
                string[] keys = source.GetKeyList(sections[s]);
                for (int k = 0; k < keys.Length; k++)
                {
                    string value = source.GetKey(keys[k], "", sections[s]);
                    AddKey(keys[k], value, sections[s]);
                }
            }
        }
    }

    /// <summary> Deletes the whole content of this ini file </summary>
    public void Clear()
    {
        _iniFile.Clear();
    }

    /// <summary> Writes to the console depending on the platform </summary>
    static void WriteLine(string line, bool error = false)
    {
#if UNITY_5_3_OR_NEWER
        if (error)
            Debug.LogError(line);
        else
            Debug.Log(line);
#else
        System.Console.WriteLine(line);
#endif
    }
}

/// <summary> Dynamic average computation </summary>
public class FM_Average
{
    Queue<double> _samples = new Queue<double>();
    uint _size;
    double _accumulator;
    public double _result
    {
        get
        {
            return _accumulator / _samples.Count;
        }
    }

    /// <summary> Constructor </summary>
    public FM_Average(uint size)
    {
        _size = size;
    }

    /// <summary> Add sample and compute average </summary>
    public void AddSample(double newSample)
    {
        _accumulator += newSample;
        _samples.Enqueue(newSample);
        if (_samples.Count > _size)
        {
            _accumulator -= _samples.Dequeue();
        }
    }

    /// <summary> Reset values </summary>
    public void Clear()
    {
        _samples.Clear();
        _accumulator = 0;
    }
}