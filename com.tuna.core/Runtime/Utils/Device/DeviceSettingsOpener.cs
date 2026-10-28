using UnityEngine;

namespace Core
{
    public static class DeviceSettingsOpener
    {
        /// <summary>
        /// Mở màn hình cài đặt Wi-Fi (Android) / mở Settings của app (iOS)
        /// </summary>
        public static void OpenWifiSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                // Wi-Fi settings screen
                intent.Call<AndroidJavaObject>("setAction", "android.settings.WIFI_SETTINGS");
                activity.Call("startActivity", intent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("OpenWifiSettings failed: " + e.Message);
        }

#elif UNITY_IOS && !UNITY_EDITOR
        // iOS không cho mở thẳng Wi-Fi settings.
        // Cách hợp lệ: mở Settings của app để user tự bật quyền / xem hướng dẫn.
        Application.OpenURL("app-settings:");

#else
            Debug.Log("OpenWifiSettings: only works on device (Android/iOS).");
#endif
        }

        /// <summary>
        /// (Android) Mở Wireless Settings chung (Wi-Fi + Mobile Network)
        /// </summary>
        public static void OpenWirelessSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setAction", "android.settings.WIRELESS_SETTINGS");
                activity.Call("startActivity", intent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("OpenWirelessSettings failed: " + e.Message);
        }
#else
            OpenWifiSettings();
#endif
        }
    }
}