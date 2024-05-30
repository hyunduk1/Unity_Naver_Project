// on OpenGL ES there is no way to query texture extents from native texture id
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
    #define UNITY_GLES_RENDERER
#endif

using System;
using System.Collections;
using System.Runtime.InteropServices;


namespace DemolitionStudios.DemolitionMedia
{
    internal partial class NativeDll
    {
#if UNITY_IPHONE && !UNITY_EDITOR
		private const string _dllName = "__Internal";
#else
        private const string _dllName = "AudioPluginDemolitionMedia";
#endif

        [DllImport(_dllName)]
        public static extern IntPtr GetRenderEventFunc();
    }
}