using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Gamelib.SoundSystem.Editor
{
    public static class SoundEditorUtils
    {
        private static readonly System.Collections.Generic.Dictionary<string, SoundListSo> _listCache = new();

        public static SoundListSo FindSoundList<T>() where T : Enum => FindSoundList(typeof(T).Name);

        public static SoundListSo FindSoundList(string enumName)
        {
            if (_listCache.TryGetValue(enumName, out var cached) && cached != null)
                return cached;
            foreach (var guid in AssetDatabase.FindAssets("t:SoundListSo"))
            {
                var list = AssetDatabase.LoadAssetAtPath<SoundListSo>(AssetDatabase.GUIDToAssetPath(guid));
                if (list != null && list.enumName == enumName)
                {
                    _listCache[enumName] = list;
                    return list;
                }
            }
            return null;
        }

        public static string GetDisplayName<T>(T value, SoundListSo list) where T : Enum
        {
            if (list?.sounds != null)
            {
                int idx = Convert.ToInt32(value);
                if (idx >= 0 && idx < list.sounds.Length && list.sounds[idx] != null)
                {
                    string note = list.sounds[idx].note?.Trim();
                    if (!string.IsNullOrEmpty(note)) return note;
                }
            }
            return value.ToString();
        }

        public static string[] BuildDisplayNames<T>(T[] values, SoundListSo list) where T : Enum
        {
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                names[i] = GetDisplayName(values[i], list);
            return names;
        }

        public static AudioClip FindClip<T>(T value) where T : Enum
        {
            var list = FindSoundList<T>();
            if (list?.sounds == null) return null;
            int index = Convert.ToInt32(value);
            return index >= 0 && index < list.sounds.Length && list.sounds[index] != null
                ? list.sounds[index].clip
                : null;
        }

        // ── 하위 호환 (SfxSounds 전용 별칭) ─────────────────────────────────────
        public static SoundListSo FindSfxSoundList()                             => FindSoundList<SfxSounds>();
        public static string[]    BuildDisplayNames(SfxSounds[] v, SoundListSo l) => BuildDisplayNames<SfxSounds>(v, l);
        public static AudioClip   FindSfxClip(SfxSounds sfx)                     => FindClip(sfx);
        public static AudioClip   FindBgmClip(BgmSounds bgm)                     => FindClip(bgm);

        // ── 미리듣기 ─────────────────────────────────────────────────────────────
        public static void PlayPreview(SfxSounds sfx) { var c = FindSfxClip(sfx); if (c != null) PlayEditorClip(c); }
        public static void PlayPreview(BgmSounds bgm)  { var c = FindBgmClip(bgm);  if (c != null) PlayEditorClip(c); }

        public static void PlayEditorClip(AudioClip clip) => PlayEditorClipFromSample(clip, 0);

        public static void PlayEditorClipFromSample(AudioClip clip, int startSample)
        {
            var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtil?.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            method?.Invoke(null, new object[] { clip, startSample, false });
        }

        public static void StopEditorClip()
        {
            var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtil?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, null);
        }

        public static bool IsPreviewPlaying()
        {
            var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtil?.GetMethod("IsPreviewClipPlaying", BindingFlags.Static | BindingFlags.Public);
            return method != null && (bool)method.Invoke(null, null);
        }

        public static int GetPreviewSamplePosition()
        {
            var audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            var method = audioUtil?.GetMethod("GetPreviewClipSamplePosition", BindingFlags.Static | BindingFlags.Public);
            return method != null ? (int)method.Invoke(null, null) : 0;
        }
    }
}
