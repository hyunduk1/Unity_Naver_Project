using System;


namespace DemolitionStudios.DemolitionMedia
{
    public static class Utilities
    {
        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
		}

        public static bool ApproximatelyEqual(float a, float b)
        {
            const float epsilon = 1.0e-05f;
            if (Math.Abs(a - b) <= epsilon)
                return true;
            return Math.Abs(a - b) <= epsilon * Math.Max(Math.Abs(a), Math.Abs(b));
        }

        public static bool ApproximatelyEqual(double a, double b)
        {
            const float epsilon = 1.0e-05f;
            if (Math.Abs(a - b) <= epsilon)
                return true;
            return Math.Abs(a - b) <= epsilon * Math.Max(Math.Abs(a), Math.Abs(b));
        }

        public static void Log(string logMsg)
        {
#if !DEMOLITION_MEDIA_DISABLE_LOGS && !DEMOLITION_MEDIA_DISABLE_INFO_LOGS
            UnityEngine.Debug.Log(logMsg);
#endif
        }

        public static void LogError(string logMsg)
        {
#if !DEMOLITION_MEDIA_DISABLE_LOGS
            UnityEngine.Debug.LogError(logMsg);
#endif
        }

        public static void LogWarning(string logMsg)
        {
#if !DEMOLITION_MEDIA_DISABLE_LOGS
            UnityEngine.Debug.LogWarning(logMsg);
#endif
        }
    }
}
