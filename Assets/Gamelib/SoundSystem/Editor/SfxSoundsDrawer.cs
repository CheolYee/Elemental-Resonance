using UnityEditor;

namespace Gamelib.SoundSystem.Editor
{
    [CustomPropertyDrawer(typeof(SfxSounds))]
    public sealed class SfxSoundsDrawer : SoundEnumDrawer<SfxSounds> { }
}
