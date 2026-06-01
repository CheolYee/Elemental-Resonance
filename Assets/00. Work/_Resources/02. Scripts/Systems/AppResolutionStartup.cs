using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Systems
{
    public static class AppResolutionStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnforceResolution()
        {
#if !UNITY_EDITOR
        if (Display.main.systemWidth < Display.main.systemHeight)
            return;
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
#endif
        }
    }
}
