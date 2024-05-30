// Comment the line below to update texture pointers in a coroutine instead of Update 
#define DLT_UPDATE_TEXTURE_IN_UPDATE

// Uncomment the line below to move all the texture creation work to the Unity side instead of native C++ plugin side
// It's somehow safer (but less performant) and also works across all the Unity render backends (OpenGL, etc)
// Default for Unity 2022 and above and for non-DX11 gfx devices
//#define DLT_CREATE_TEXTURE_IN_UNITY

using System;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace DemolitionStudios.DemolitionMedia
{
    [AddComponentMenu("Demolition Media/Media")]
    public partial class Media : MonoBehaviour
    {
    #region fields
        /// Possible types of media url
		public enum UrlType
        {
            Absolute,
            RelativeToProjectPath,
            RelativeToStreamingAssetsPath,
            RelativeToDataPath,
            RelativeToPeristentPath,
        }
        /// Current mediaUrl type
        public UrlType urlType = UrlType.Absolute;

        /// Native textures list
		private List<Texture2D> _nativeTextures = new List<Texture2D>();
        /// Output texture for rendering the video stream frames
        public Texture VideoRenderTexture
        {
            get
            {
                if (_colorConversionMaterial != null)
                {
                    // Could be null in the beginning
                    return _colorConversionRenderTexture;
                }
                if (_nativeTextures.Count == 0)
                    return null;
                // Assuming _nativeTextures.Count == 1
                return _nativeTextures[0];
            }
        }

        /// Unity audio source
        private AudioSource _audioSource;
        /// Unity audio muxer
        private AudioMixer _audioMixer;

        // Render texture for video color conversion.
        // Being used if material is not null
        private RenderTexture _colorConversionRenderTexture = null;

        // Hap Q shader (YCoCg -> RGB)
        private static Shader _shaderHapQ;
        // Hap Q Alpha shader ((YCoCg, A) -> RGBA)
        private static Shader _shaderHapQAlpha;

        /// Material used for video color conversion
        private Material _colorConversionMaterial = null;

        private int _last_displayed_frame_index = -1;
        public enum CreateTexturesResult
        {
            Fail,
            SuccessOldTexture,
            SuccessNewTexture,
        };

#if UNITY_EDITOR
        /// Whether the media was playing before the in-editor pause
        bool _wasPlayingBeforeEditorPause = false;
#endif
    #endregion

    #region monobehavior impl
        public virtual void Start()
        {
#if UNITY_EDITOR
            // Subscribe for the editor playmode state changed events (pause/play)
    #if UNITY_2018_1_OR_NEWER
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
    #else
            EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
    #endif
#endif

            // Check whether MediaManager comonnent presents in the scene
            //Debug.Assert(MediaManager.Instance != null,
            //             "[DemolitionMedia] Please add DemolitionMediaManager component to your scene. " +
            //             "You can simply attach it to an empty actor. See the documentation pdf file for more details.");

            // Set the global audio params anyways
            var audioConfiguration = AudioSettings.GetConfiguration();
            var bufferSize = audioConfiguration.dspBufferSize; // AudioSettings.GetDSPBufferSize();
            var sampleRate = audioConfiguration.sampleRate;
            // The actual channel number used for playback can differ depending on the audio card capabilities.
            // But it's not a problem, as we will get the actual channel number in the audio callback and handle the change
            var channels = GetAudioChannelCount(audioConfiguration.speakerMode);
            NativeDll.SetAudioParams(SampleFormat.Float, sampleRate, bufferSize, channels);
            // Audio can be enabled in run-time, so init the audio mixer regradless of the enableAudio flag
            initAudioMixer();

            // Load the global shaders
            if (_shaderHapQ == null)
            {
                _shaderHapQ = Shader.Find("DemolitionMedia/HapQ");
                if (_shaderHapQ == null)
                {
                    Utilities.LogError("[DemolitionMedia] unable to load \"DemolitionMedia/HapQ\" shader from resources. Check the plugin installation completeness.");
                }
            }
            if (_shaderHapQAlpha == null)
            {
                _shaderHapQAlpha = Shader.Find("DemolitionMedia/HapQAlpha");
                if (_shaderHapQAlpha == null)
                {
                    Utilities.LogError("[DemolitionMedia] unable to load \"DemolitionMedia/HapQAlpha\" shader from resources. Check the plugin installation completeness.");
                }
            }

            // Open on start if needed
            if (openOnStart)
            {
                Open(mediaUrl);
            }

#if !DLT_UPDATE_TEXTURE_IN_UPDATE
            // We're using per Media instance render coroutine
            if (passRawTextureDataOutside)
            {
                StartCoroutine("UpdateUnityTexturesAtEndOfFrames");
            }
            else 
            {
                StartCoroutine("UpdateNativeTexturesAtEndOfFrames");
            }
#endif
        }

#if !DLT_UPDATE_TEXTURE_IN_UPDATE
        private System.Collections.IEnumerator UpdateUnityTexturesAtEndOfFrames()
        {
            YieldInstruction waitForEndOfFrame = new WaitForEndOfFrame();

            while (Application.isPlaying)
            {
                yield return waitForEndOfFrame; // after rendering done

                NativeDll.UpdateDisplayedFrameIndex(_mediaId);
                var result = UpdateUnityTexturesFromData();

                if (result == CreateTexturesResult.SuccessNewTexture)
                    UpdateRendererCompTexture();
            }
        }
        private System.Collections.IEnumerator UpdateNativeTexturesAtEndOfFrames()
        {
            YieldInstruction waitForEndOfFrame = new WaitForEndOfFrame();

            while (Application.isPlaying)
            {
                yield return waitForEndOfFrame; // after rendering done

                NativeDll.UpdateDisplayedFrameIndex(_mediaId);
                var result = CreateExternalTextures();

                if (result == CreateTexturesResult.SuccessNewTexture)
                    UpdateRendererCompTexture();
            }
        }
#endif

#if DLT_UPDATE_TEXTURE_IN_UPDATE
        public virtual void Update()
        {
            // Note: you can also update textures in LateUpdate, but it will break uGUI/Material renderers: some additional work is needed
            NativeDll.UpdateDisplayedFrameIndex(_mediaId);
            var result = CreateTexturesResult.Fail;
            switch (VideoTextureType)
            {
                case VideoTextureType.Immutable:
                case VideoTextureType.Dynamic:
                    result = CreateExternalTextures();
                    break;
                case VideoTextureType.External:
                    result = UpdateUnityTexturesFromData();
                    break;
            }
            if (result == CreateTexturesResult.SuccessNewTexture)
                UpdateRendererCompTexture();
        }
#endif

        private void UpdateRendererCompTexture()
        {
            if (VideoRenderTexture == null)
                return;

            // Map the video texture to the Renderer component material
            var rendererComp = GetComponent<Renderer>();
            if (rendererComp != null)
                rendererComp.material.mainTexture = VideoRenderTexture;
        }

        private void LateUpdate()
        {
            // Get events & errors 
            PopulateEvents();
            PopulateErrors();

            if (_mediaId >= 0)
            {
                _videoDecodeFramerateTime += Time.deltaTime;

                if (_endFrame > _startFrame && _endFrame - _startFrame < 2.0 * VideoFramerate)
                {
                    // Make it constant, since can't measure
                    _videoDecodeFramerate = VideoFramerate;
                }
                else if (_videoDecodeFramerateTime >= _videoDecodeFramerateInterval)
                {
                    var currentFrame = VideoCurrentFrame;
                    var decodedFrames = Math.Abs(currentFrame - _videoDecodeFramerateFrameCount);
                    _videoDecodeFramerate = decodedFrames / _videoDecodeFramerateTime;
                    _videoDecodeFramerate = Mathf.Max(_videoDecodeFramerate, 0.0f);
                    _videoDecodeFramerateFrameCount = currentFrame;
                    _videoDecodeFramerateTime = 0.0f;
                }
            }
        }

#if UNITY_EDITOR
    #if UNITY_2018_1_OR_NEWER
        void HandleOnPlayModeChanged(PlayModeStateChange state)
    #else
        void HandleOnPlayModeChanged()
    #endif
        {
            // This method is run whenever the playmode state is changed.
            if (EditorApplication.isPaused)
            {
                if (IsPlaying)
                {
                    _wasPlayingBeforeEditorPause = true;
                    Pause();
                }
            }
            else
            {
                if (_wasPlayingBeforeEditorPause)
                {
                    Play();
                }
                _wasPlayingBeforeEditorPause = false;
            }
        }
#endif


        public void OnDestroy()
        {
            Close();
            NativeDll.DestroyMediaId(_mediaId);
        }
    #endregion

    #region internal
        private UnityEngine.TextureFormat TextureFormatNativeToUnity(NativeDll.NativeTextureFormat format)
        {
            UnityEngine.TextureFormat result;
            switch (format)
            {
                case NativeDll.NativeTextureFormat.RGBAu8: result = TextureFormat.RGBA32; break;
                case NativeDll.NativeTextureFormat.Ru8: result = TextureFormat.Alpha8; break;
                case NativeDll.NativeTextureFormat.RGu8: result = TextureFormat.RGHalf; break;

                case NativeDll.NativeTextureFormat.Ru16: result = TextureFormat.R16; break;

                case NativeDll.NativeTextureFormat.DXT1: result = TextureFormat.DXT1; break;
                case NativeDll.NativeTextureFormat.DXT5: result = TextureFormat.DXT5; break;

                case NativeDll.NativeTextureFormat.RGTC1: result = TextureFormat.BC4; break;

                // FIXME: signed/unsigned?
                // https://docs.unity3d.com/ScriptReference/TextureFormat.BC6H.html
                // https://forum.unity.com/threads/rgba-half-textures-clamping-negative-numbers.534797/#post-3522488
                case NativeDll.NativeTextureFormat.BPTCfu:
                case NativeDll.NativeTextureFormat.BPTCfs:
                    result = TextureFormat.BC6H;
                    break;
                
                case NativeDll.NativeTextureFormat.BPTC: result = TextureFormat.BC7; break;

                default:
                    Utilities.LogError("[DemolitionMedia] unknown texture format: " + format.ToString());
                    // Try to fallback to rgba texture format
                    result = TextureFormat.RGBA32;
                    break;
            }


            return result;
        }

        public CreateTexturesResult CreateExternalTextures()
        {
            // Check whether the native texture(s) created already
            if (!NativeDll.AreNativeTexturesCreated(_mediaId))
                return CreateTexturesResult.Fail;

            // Update (replace) old textures or create for the first time
            bool updateTextures = _nativeTextures.Count > 0;
            int currentFrame = NativeDll.LockCurrentFrameNativeTextures(MediaId);
            if (updateTextures)
            {
                if (_last_displayed_frame_index == currentFrame)
                {
                    NativeDll.UnlockNativeTextures(MediaId, currentFrame);
                    return CreateTexturesResult.SuccessOldTexture;
                }
            }

            // Get the textures count (1 for Hap/Hap Alpha/Hap Q/Hap R, 2 for Hap Q Alpha)
            var texturesCount = NativeDll.GetNativeTexturesCount(_mediaId, currentFrame);
            if (texturesCount == 0)
            {
                NativeDll.UnlockNativeTextures(MediaId, currentFrame);
                return CreateTexturesResult.Fail;
            }

            for (int idx = 0; idx < texturesCount; ++idx)
            {
                int width, height;
                NativeDll.NativeTextureFormat nativeFormat;
                IntPtr nativeTexture, shaderResourceView;

                bool ok = NativeDll.GetNativeTexturePtrByIndex(
                    _mediaId, FrameType.Current, idx, out nativeTexture, out shaderResourceView, out width, out height, out nativeFormat);

                // From Unity docs: For Direct3D-like devices, the nativeTex parameter is a pointer to the underlying Direct3D base type, from which a texture can be created. They can be:
                // D3D11: ID3D11ShaderResourceView* or ID3D11Texture2D*
                var texPtr = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ? shaderResourceView : nativeTexture;
                var format = TextureFormatNativeToUnity(nativeFormat);

                if (!ok || nativeTexture == IntPtr.Zero || nativeTexture.ToInt32() == 0 || texPtr.ToInt32() == 0 ||
                    width <= 0 || height <= 0 || nativeFormat == NativeDll.NativeTextureFormat.Unknown)
                {
                    Utilities.LogError("[DemolitionMedia] native texture is invalid");
                    Utilities.LogError($"[DemolitionMedia] texture index: {idx}, result: {ok}, " +
                                   $"nativeTexture: {nativeTexture}, width: {width}, height: {height}, " +
                                   $"nativeFormat: {nativeFormat}");
                    // Clear all the textures, so we create next time from scratch
                    ClearNativeTextures();
                    NativeDll.UnlockNativeTextures(MediaId, currentFrame);
                    return CreateTexturesResult.Fail;
                }
                if (_nativeTextures.Count == 0 && nativeFormat == NativeDll.NativeTextureFormat.BPTCfs)
                {
                    Utilities.LogWarning("The BC6H Signed texture format may be not working correctly. Using Unsigned format is recommended");
                }

                if (VideoTextureType == VideoTextureType.Immutable)
                {
                    var texture = Texture2D.CreateExternalTexture(width, height, format, false, false, texPtr);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.filterMode = FilterMode.Bilinear;

                    if (updateTextures)
                    {
                        var oldTexture = _nativeTextures[idx];
                        _nativeTextures[idx] = texture;
                        Destroy(oldTexture);

                    }
                    else
                    {
                        // Create for the first time
                        _nativeTextures.Add(texture);
                    }
                }
                else if (VideoTextureType == VideoTextureType.Dynamic)
                {
                    if (!updateTextures)
                    {
                        // Create for the first time
                        var texture = Texture2D.CreateExternalTexture(width, height, format, false, false, texPtr);
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.filterMode = FilterMode.Bilinear;
                        _nativeTextures.Add(texture);
                    }
                    else
                    {
                        _nativeTextures[idx].UpdateExternalTexture(texPtr);
                    }
                }
                else
                {
                    Utilities.LogError("Shouldn't be called. VideoTextureType is " + VideoTextureType);
                }
            }

            PerformColorConversionIfNeeded();

            NativeDll.UnlockNativeTextures(MediaId, _last_displayed_frame_index);
            _last_displayed_frame_index = currentFrame;
            return CreateTexturesResult.SuccessOldTexture;
        }

        public CreateTexturesResult UpdateUnityTexturesFromData()
        {
            // Check whether the native texture(s) created already
            if (!NativeDll.AreNativeTexturesCreated(_mediaId))
                return CreateTexturesResult.Fail;

            // Update (replace) old textures or create for the first time
            bool updateTextures = _nativeTextures.Count > 0;
            int currentFrame = NativeDll.LockCurrentFrameNativeTextures(MediaId);
            if (updateTextures)
            {
                if (_last_displayed_frame_index == currentFrame)
                {
                    NativeDll.UnlockNativeTextures(MediaId, currentFrame);
                    return CreateTexturesResult.SuccessOldTexture;
                }
            }

            // Get the textures count (1 for Hap/Hap Alpha/Hap Q/Hap R, 2 for Hap Q Alpha)
            var texturesCount = NativeDll.GetNativeTexturesCount(_mediaId, currentFrame);
            if (texturesCount == 0)
            {
                NativeDll.UnlockNativeTextures(MediaId, currentFrame);
                return CreateTexturesResult.Fail;
            }

            for (int idx = 0; idx < texturesCount; ++idx)
            {
                int width, height;
                NativeDll.NativeTextureFormat nativeFormat;
                IntPtr textureData;
                int textureDataSize;

                bool ok = NativeDll.GetNativeTextureDataByIndex(
                    _mediaId, idx, out textureData, out textureDataSize, out width, out height, out nativeFormat);

                var format = TextureFormatNativeToUnity(nativeFormat);

                if (!ok || textureData == IntPtr.Zero || textureData.ToInt32() == 0 ||
                    width <= 0 || height <= 0 || nativeFormat == NativeDll.NativeTextureFormat.Unknown)
                {
                    Utilities.LogError("[DemolitionMedia] textureData is invalid");
                    Utilities.LogError($"[DemolitionMedia] texture index: {idx}, result: {ok}, " +
                                   $"nativeTexture: {textureData}, width: {width}, height: {height}, " +
                                   $"nativeFormat: {nativeFormat}");
                    // Clear all the textures, so we create next time from scratch
                    ClearNativeTextures();
                    NativeDll.UnlockNativeTextures(MediaId, currentFrame);
                    return CreateTexturesResult.Fail;
                }

                if (!updateTextures)
                {
                    if (nativeFormat == NativeDll.NativeTextureFormat.BPTCfs)
                    {
                        Utilities.LogWarning("The BC6H Signed texture format may be not working correctly. Using Unsigned format is recommended");
                    }

                    // Create for the first time
                    var linear = QualitySettings.activeColorSpace == ColorSpace.Linear && RequiresColorConversion;
                    _nativeTextures.Add(new Texture2D(width, height, format, mipChain: false, linear: linear));
                    _nativeTextures[idx].wrapMode = TextureWrapMode.Clamp;
                    _nativeTextures[idx].filterMode = FilterMode.Bilinear;
                }
                _nativeTextures[idx].LoadRawTextureData(textureData, textureDataSize);
                _nativeTextures[idx].Apply(); // Call Apply() so it's actually upopened to the GPU
            }

            PerformColorConversionIfNeeded();

            NativeDll.UnlockNativeTextures(MediaId, _last_displayed_frame_index);
            _last_displayed_frame_index = currentFrame;
            return CreateTexturesResult.SuccessOldTexture;
        }

        public bool TexturesCreated()
        {
            return _nativeTextures.Count > 0;
        }

        public void PerformColorConversionIfNeeded()
        {
            if (_nativeTextures.Count == 0)
                return;

            // Get the current material shader
            Shader shaderCur = null;
            if (_colorConversionMaterial != null)
            {
                shaderCur = _colorConversionMaterial.shader;
            }
            Shader shaderNew = GetShader();

            // Check if we need to do something about the material
            if (shaderNew != shaderCur)
            {
                // Destroy old
                DestroyColorConversionMaterialIfNeeded();
                // Create new
                if (shaderNew != null)
                {
                    _colorConversionMaterial = new Material(shaderNew);
                }
                else
                {
                    DestroyColorConversionRenderTextureIfNeeded();
                }
            }

            // Check if the color conversion is needed
            if (_colorConversionMaterial == null)
                return;

            // Create the render texture
            var srcTexture = _nativeTextures[0];
            if (_colorConversionRenderTexture == null || 
                _colorConversionRenderTexture.width != srcTexture.width ||
                _colorConversionRenderTexture.height != srcTexture.height)
            {
                DestroyColorConversionRenderTextureIfNeeded();

                _colorConversionRenderTexture = new RenderTexture(srcTexture.width, srcTexture.height, depth: 0,
                                                                  RenderTextureFormat.ARGB32);
                _colorConversionRenderTexture.Create();
            }

            // Stop Unity complaining about:
            // "Tiled GPU perf. warning: RenderTexture color surface was not cleared/discarded"
            // https://forum.unity3d.com/threads/4-2-any-way-to-turn-off-the-tiled-gpu-perf-warning.191906/
            //_colorConversionRenderTexture.MarkRestoreExpected();
            _colorConversionRenderTexture.DiscardContents();

            if (_colorConversionMaterial.shader == _shaderHapQAlpha)
            {
                _colorConversionMaterial.SetTexture("_AlphaTex", _nativeTextures[1]);
            }

            // Perform the conversion
            Graphics.Blit(srcTexture, _colorConversionRenderTexture, _colorConversionMaterial);
            // Restore the default render target (screen)
            Graphics.SetRenderTarget(null);
        }

        delegate Texture2D CreateTextureDelegate(int width, int height, TextureFormat format);

        public static string GetUrlPrefix(UrlType urlType)
        {
            switch (urlType)
            {
                case UrlType.Absolute:
                    return string.Empty;

                case UrlType.RelativeToDataPath:
                    return Application.dataPath;

                case UrlType.RelativeToPeristentPath:
                    return Application.persistentDataPath;

                case UrlType.RelativeToProjectPath:
#if !UNITY_WINRT_8_1
                    string parentPath = "..";
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX
                    parentPath += "/..";
#endif // UNITY_STANDALONE_OSX && !UNITY_EDITOR_OSX
                    return System.IO.Path.GetFullPath(System.IO.Path.Combine(
                        Application.dataPath, parentPath)).Replace('\\', '/');
#else
                    return string.Empty;
#endif // UNITY_WINRT_8_1

                case UrlType.RelativeToStreamingAssetsPath:
                    return Application.streamingAssetsPath;

                default:
                    return string.Empty;
            }
        }

        public static string GetOpeningUrl(string path, UrlType urlType)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            string pathNew;
            switch (urlType)
            {
                case UrlType.Absolute:
                    pathNew = System.Environment.ExpandEnvironmentVariables(path);
                    return path;

                case UrlType.RelativeToDataPath:
                case UrlType.RelativeToPeristentPath:
                case UrlType.RelativeToProjectPath:
                case UrlType.RelativeToStreamingAssetsPath:
                    pathNew = System.IO.Path.Combine(GetUrlPrefix(urlType), path);
                    return pathNew;

                default:
                    return path;
            }
        }

        private string PreOpenImpl(string url)
        {
            if (_audioMixer == null)
                openWithAudio = false;

            // Comment this block to enable preload to memory on 32-bit architecture.
            // Note that with large files it will crash Unity (total process memory > 2Gb)
#if !(UNITY_64 || UNITY_EDITOR_64)
            if (preloadToMemory)
            {
                preloadToMemory = false;
                Utilities.LogWarning("[DemolitionMedia] disabling preload to memory due to 32-bit architecture");
                Utilities.LogWarning("[DemolitionMedia] you can enable it (if you know what you're doing!) by commenting the corresponding code block in Media.cs");
            }
#endif

            var openingUrl = GetOpeningUrl(url, urlType);
            return openingUrl;
        }

        static public void GetGraphicsParams(out bool fallbackToDynamicTexture, out bool passRawTextureDataOutside, out bool useSrgbNativeTextures)
        {
            useSrgbNativeTextures = QualitySettings.activeColorSpace == ColorSpace.Linear;

            var gfxDevice = SystemInfo.graphicsDeviceType;
            if (gfxDevice != GraphicsDeviceType.Direct3D11)
            {
                Utilities.Log("[DemolitionMedia] switching to Unity texture creation method for graphics device " + gfxDevice);
                fallbackToDynamicTexture = false;
                passRawTextureDataOutside = true;
                return;
            }

#if UNITY_2022_1_OR_NEWER
            Utilities.Log("Switching to Unity texture for Unity 2022+");
            fallbackToDynamicTexture = false;
            passRawTextureDataOutside = true;
#elif DLT_CREATE_TEXTURE_IN_UNITY
            fallbackToDynamicTexture = false;
            passRawTextureDataOutside = true;
#else
            fallbackToDynamicTexture = false;
            passRawTextureDataOutside = false;
#endif
        }

        private bool initAudioMixer()
        {
            //Utilities.LogWarning("initAudioMixer");

            var audioSourceComponents = GetComponentsInChildren<AudioSource>();
            if (audioSourceComponents.Length == 0)
            {
                // No children AudioMixer component attached: no audio 
                Utilities.LogWarning("[DemolitionMedia] No children AudioSource component attached, audio will be disabled");
                return false;
            }
            if (audioSourceComponents.Length > 1)
            {
                // More than one children AudioMixer component attached
                Utilities.Log("[DemolitionMedia] More than one children AudioSource component attached, audio will be disabled");
                return false;
            }

            // A single children AudioMixer component attached
            _audioSource = audioSourceComponents[0];
            //Utilities.Log("[DemolitionMedia] Using AudioSource: " + audioSource.name);

            var mixerGroup = _audioSource.outputAudioMixerGroup;
            if (mixerGroup == null)
            {
                Utilities.LogWarning("[DemolitionMedia] No output AudioMixerGroup for AudioSource " + _audioSource.name + ", audio will be disabled");
                return false;
            }

            //Utilities.Log("[DemolitionMedia] Using AudioMixerGroup: " + mixerGroup.name);

            _audioMixer = mixerGroup.audioMixer;
            if (_audioMixer == null)
            {
                Utilities.LogWarning("[DemolitionMedia] No AudioMixer for AudioMixerGroup " + mixerGroup.name + ", audio will be disabled");
                return false;
            }

            //Utilities.Log("[DemolitionMedia] Using AudioMixer: " + _audioMixer.name);
            return true;
        }

        private void InvokeEvent(MediaEvent.Type type, MediaError error)
        {
            _events.Invoke(this, type, error);
        }

        private void InvokeEvent(MediaEvent.Type eventType)
        {
            _events.Invoke(this, eventType, MediaError.NoError);
        }

        private void OnOpenedImpl()
        {
            SyncMode = SyncMode.SyncAudioMaster;

            if (openWithAudio && useNativeAudioPlugin)
            {
                // Check MediaAudioSource script is attached and enabled
                var mediaAudioSourceComponent = GetComponent<MediaAudioSource>();
                var mediaAudioSourceScriptComponentActive = mediaAudioSourceComponent != null &&
                                                            mediaAudioSourceComponent.isActiveAndEnabled &&
                                                            mediaAudioSourceComponent.media != null;
                if (mediaAudioSourceScriptComponentActive)
                {
                    Utilities.LogWarning("[DemolitionMedia] " + name + ": \"Use Native Audio Plugin\" option is enabled, " +
                                     "while MediaAudioSource component is attached and active.\nYou probably want to " +
                                     "disable the \"Use Native Audio Plugin\" option.");
                }

                // Try to set the DemolitionMediaId parameter which is used by the native
                // audio plugin to determine the media sound should played from
                var success = _audioMixer.SetFloat("DemolitionMediaId", MediaId);
                if (success)
                {
                    //Utilities.Log("[DemolitionMedia] Setting DemolitionMediaId to " + MediaId + " for " + _audioMixer.name);
                }
                else
                {
                    // Setting failed, so probably there is no "Demolition Audio Source" 
                    // effect attached and the parameter doesn't exist
                    if (!mediaAudioSourceScriptComponentActive)
                    {
                        Utilities.LogWarning("[DemolitionMedia] " + name + ": \"Demolition Audio Source\" effect isn't " +
                                         "added to the " + _audioMixer.name + " audio mixer, " +
                                         "which is used in the AudioSource");
                        Utilities.LogWarning("[DemolitionMedia] It is recommended adding the " +
                                         "\"Demolition Audio Source\" effect using the Audio Mixer editor " +
                                         "(double-click on the " + _audioMixer.name + " audio mixer asset)");
                        Utilities.LogWarning("[DemolitionMedia] Alternatively you can use the " +
                                         "MediaAudioSource script, which will feed an AudioSource with audio data " +
                                         "of the specified media.\nThis option isn't recommended for performance reasons, " +
                                         "especially if you need fast seeking");
                    }
                }
            }
        }

        private void PrePlayImpl()
        {
            // Hack to overcome Unity bug: re-enable the Audio Source component, so the audio plays every time
            if (useNativeAudioPlugin && _audioSource != null && _audioSource.enabled)
            {
                _audioSource.enabled = false;
                _audioSource.enabled = true;
                //Utilities.LogWarning("Hack to overcome Unity bug: re-enable the Audio Source component, so the audio plays every time");
            }
        }

        private Shader GetShader()
        {
            // Note: VideoPixelFormat isn't initialized here yet
            var videoPixelFormat = NativeDll.GetPixelFormat(_mediaId);
            switch (videoPixelFormat)
            {
                case PixelFormat.YCoCg:
                    return _shaderHapQ;
                case PixelFormat.YCoCgAndAlphaP:
                    return _shaderHapQAlpha;
                default:
                    // No color conversion needed
                    return null;
            }
        }

        private int GetAudioChannelCount(AudioSpeakerMode speakerMode)
        {
            switch (speakerMode)
            {
                case AudioSpeakerMode.Mono:
                    return 1;
                case AudioSpeakerMode.Stereo:
                    return 2;
                case AudioSpeakerMode.Quad:
                    return 4;
                case AudioSpeakerMode.Surround:
                    return 5;
                case AudioSpeakerMode.Mode5point1:
                    return 6;
                case AudioSpeakerMode.Mode7point1:
                    return 8;

                default:
                    Utilities.LogError("[DemolitionMedia] " + "AudioSpeakerMode." + speakerMode.ToString() + " is unsupported");
                    return 0;
            }
        }

        private void ClearNativeTextures()
        {
            foreach (Texture2D tex in _nativeTextures)
                Destroy(tex);
            _nativeTextures.Clear();
        }

        private void DestroyColorConversionRenderTextureIfNeeded()
        {
            if (_colorConversionRenderTexture != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_colorConversionRenderTexture);
#else
                Destroy(_colorConversionRenderTexture);
#endif
                _colorConversionRenderTexture = null;
            }
        }

        private void DestroyColorConversionMaterialIfNeeded()
        {
            if (_colorConversionMaterial != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_colorConversionMaterial);
#else
                Destroy(_colorConversionMaterial);
#endif
                _colorConversionMaterial = null;
            }
        }
    #endregion

    #region overloaded_methods

        private void CloseImpl()
        {
            // Destroy native render textures
            ClearNativeTextures();
        }

        private void DisposeImpl()
        {
            // Destroy color conversion render texture and material if they exist
            DestroyColorConversionRenderTextureIfNeeded();
            DestroyColorConversionMaterialIfNeeded();
        }
    #endregion
    }
}