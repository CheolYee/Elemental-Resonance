using UnityEditor;

namespace Gamelib.SoundSystem.Editor
{
    [CustomPropertyDrawer(typeof(BgmSounds))]
    public sealed class BgmSoundsDrawer : SoundEnumDrawer<BgmSounds> { }
}
