using UnityEngine;

namespace Gamelib.SoundSystem
{
    [CreateAssetMenu(fileName = "Sound List", menuName = "Sound/Sound List", order = 15)]
    public class SoundListSo : ScriptableObject
    {
        public string enumName;
        public SoundClipSo[] sounds;
    }
}