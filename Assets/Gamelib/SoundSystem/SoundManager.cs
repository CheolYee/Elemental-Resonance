using System.Collections.Generic;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;
using UnityEngine.Audio;

namespace Gamelib.SoundSystem
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundListSo sfxSoundList;
        [SerializeField] private SoundListSo bgmSoundList;

        [SerializeField] private PoolManagerSo poolManager;
        [SerializeField] private PoolItemSo soundItem;

        [field: SerializeField] public EventChannelSO SoundChannel { get; private set; }

        [Header("Volume Init")]
        [SerializeField] private AudioMixer _audioMixer;

        private readonly Dictionary<SoundChannelId, SoundPlayer> _channelPlayers = new();
        private readonly Dictionary<SoundChannelId, HashSet<SoundPlayer>> _groupPlayers = new();

        private void Awake()
        {
            var existing = FindObjectsByType<SoundManager>(FindObjectsSortMode.None);
            if (existing.Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);

            SoundChannel.AddListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundChannel.AddListener<StopSoundEvent>(HandleStopSoundEvent);
            SoundChannel.AddListener<PlayManagedSoundEvent>(HandlePlayManagedSoundEvent);
            SoundChannel.AddListener<StopManagedSoundEvent>(HandleStopManagedSoundEvent);
            SoundChannel.AddListener<PauseSoundEvent>(HandlePauseSoundEvent);
            SoundChannel.AddListener<ResumeSoundEvent>(HandleResumeSoundEvent);
        }

        private void Start()
        {
            ApplyVolumeFromPrefs();
        }

        private void OnDestroy()
        {
            SoundChannel.RemoveListener<PlaySoundEvent>(HandlePlaySoundEvent);
            SoundChannel.RemoveListener<StopSoundEvent>(HandleStopSoundEvent);
            SoundChannel.RemoveListener<PlayManagedSoundEvent>(HandlePlayManagedSoundEvent);
            SoundChannel.RemoveListener<StopManagedSoundEvent>(HandleStopManagedSoundEvent);
            SoundChannel.RemoveListener<PauseSoundEvent>(HandlePauseSoundEvent);
            SoundChannel.RemoveListener<ResumeSoundEvent>(HandleResumeSoundEvent);
        }

        private void ApplyVolumeFromPrefs()
        {
            if (_audioMixer == null) return;
            SetMixerVolume("MasterVolume", PlayerPrefs.GetFloat("Setting_MasterVolume", 1f));
            SetMixerVolume("BGMVolume",    PlayerPrefs.GetFloat("Setting_BgmVolume",    1f));
            SetMixerVolume("SFXVolume",    PlayerPrefs.GetFloat("Setting_SfxVolume",    1f));
        }

        private void SetMixerVolume(string parameter, float linear)
        {
            float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
            _audioMixer.SetFloat(parameter, db);
        }

        private void HandlePlaySoundEvent(PlaySoundEvent evt)
        {
            SoundClipSo clipData = ResolveClip(evt.Sound);
            if (clipData == null)
            {
                Debug.LogWarning($"사운드를 찾을 수 없습니다. Type={evt.Sound.AudioType}, Index={evt.Sound.Index}");
                return;
            }
            if (clipData.loop && evt.ChannelId != SoundChannelId.None)
            {
                StopChannel(evt.ChannelId);
            }
            else if (clipData.loop && evt.ChannelId == SoundChannelId.None)
            {
                Debug.LogWarning($"루프 사운드는 채널 지정이 필요합니다. : {clipData.name}");
            }

            SoundPlayer player = CreatePlayer(evt.Position);
            player.PlaySound(clipData);

            if (clipData.loop && evt.ChannelId != SoundChannelId.None)
            {
                _channelPlayers[evt.ChannelId] = player;
            }
        }

        private void HandlePlayManagedSoundEvent(PlayManagedSoundEvent evt)
        {
            SoundClipSo clipData = ResolveClip(evt.Sound);
            if (clipData == null)
            {
                Debug.LogWarning($"사운드를 찾을 수 없습니다. Type={evt.Sound.AudioType}, Index={evt.Sound.Index}");
                return;
            }

            if (evt.ChannelId == SoundChannelId.StorySfx)
            {
                PlayGroupedManagedSound(evt, clipData);
                return;
            }

            PlaySingleManagedSound(evt, clipData);
        }

        private void PlayGroupedManagedSound(PlayManagedSoundEvent evt, SoundClipSo clipData)
        {
            SoundPlayer player = CreatePlayer(evt.Position);
            player.PlaySoundWithEnvelope(clipData, evt.FadeInDuration, evt.FadeOutDuration);
            GetOrCreateGroup(evt.ChannelId).Add(player);
        }

        private void PlaySingleManagedSound(PlayManagedSoundEvent evt, SoundClipSo clipData)
        {
            if (_channelPlayers.TryGetValue(evt.ChannelId, out SoundPlayer existingPlayer))
            {
                _channelPlayers.Remove(evt.ChannelId);

                if (existingPlayer != null)
                {
                    if (evt.CrossfadeExisting)
                    {
                        existingPlayer.FadeOutAndStop(evt.FadeOutDuration);
                    }
                    else
                    {
                        existingPlayer.OnSoundFinished -= HandleSoundFinish;
                        existingPlayer.ForceStopSound();
                        poolManager.Push(existingPlayer);
                    }
                }
            }

            SoundPlayer player = CreatePlayer(evt.Position);
            player.PlaySoundWithEnvelope(clipData, evt.FadeInDuration, clipData.loop ? 0f : evt.FadeOutDuration);
            _channelPlayers[evt.ChannelId] = player;
        }

        private SoundClipSo ResolveClip(SoundRef soundRef)
        {
            SoundListSo targetList = soundRef.AudioType switch
            {
                AudioTypes.SFX => sfxSoundList,
                AudioTypes.MUSIC => bgmSoundList,
                _ => null
            };

            if (targetList == null || targetList.sounds == null)
                return null;

            if (soundRef.Index < 0 || soundRef.Index >= targetList.sounds.Length)
                return null;

            return targetList.sounds[soundRef.Index];
        }

        private void HandleStopSoundEvent(StopSoundEvent evt)
        {
            StopChannel(evt.ChannelId);
        }

        private void HandleStopManagedSoundEvent(StopManagedSoundEvent evt)
        {
            if (_channelPlayers.Remove(evt.ChannelId, out SoundPlayer singlePlayer))
            {
                if (evt.FadeOutDuration > 0f)
                    singlePlayer.FadeOutAndStop(evt.FadeOutDuration);
                else
                {
                    singlePlayer.OnSoundFinished -= HandleSoundFinish;
                    singlePlayer.ForceStopSound();
                    poolManager.Push(singlePlayer);
                }
            }

            if (_groupPlayers.TryGetValue(evt.ChannelId, out HashSet<SoundPlayer> groupPlayers))
            {
                foreach (SoundPlayer player in new List<SoundPlayer>(groupPlayers))
                {
                    if (player == null)
                        continue;

                    if (evt.FadeOutDuration > 0f)
                        player.FadeOutAndStop(evt.FadeOutDuration);
                    else
                    {
                        player.OnSoundFinished -= HandleSoundFinish;
                        player.ForceStopSound();
                        poolManager.Push(player);
                    }
                }

                groupPlayers.Clear();
            }
        }

        private void HandlePauseSoundEvent(PauseSoundEvent evt)
        {
            if (_channelPlayers.TryGetValue(evt.ChannelId, out SoundPlayer player))
                player.Pause();
        }

        private void HandleResumeSoundEvent(ResumeSoundEvent evt)
        {
            if (_channelPlayers.TryGetValue(evt.ChannelId, out SoundPlayer player))
                player.Resume();
        }

        private void StopChannel(SoundChannelId channelId)
        {
            if (_channelPlayers.Remove(channelId, out SoundPlayer player)) 
            {
                player.OnSoundFinished -= HandleSoundFinish;
                player.ForceStopSound();
                poolManager.Push(player);
            }
        }

        private void HandleSoundFinish(SoundPlayer player)
        {
            player.OnSoundFinished -= HandleSoundFinish;
            RemovePlayerFromTracking(player);
            poolManager.Push(player);
        }

        private SoundPlayer CreatePlayer(Vector3 position)
        {
            SoundPlayer player = poolManager.Pop<SoundPlayer>(soundItem);
            player.transform.SetParent(transform);
            player.transform.position = position;
            player.OnSoundFinished -= HandleSoundFinish;
            player.OnSoundFinished += HandleSoundFinish;
            return player;
        }

        private HashSet<SoundPlayer> GetOrCreateGroup(SoundChannelId channelId)
        {
            if (_groupPlayers.TryGetValue(channelId, out HashSet<SoundPlayer> players))
                return players;

            players = new HashSet<SoundPlayer>();
            _groupPlayers[channelId] = players;
            return players;
        }

        private void RemovePlayerFromTracking(SoundPlayer player)
        {
            SoundChannelId foundChannel = SoundChannelId.None;
            bool singleFound = false;
            foreach (var pair in _channelPlayers)
            {
                if (pair.Value == player)
                {
                    foundChannel = pair.Key;
                    singleFound = true;
                    break;
                }
            }

            if (singleFound)
                _channelPlayers.Remove(foundChannel);

            foreach (var group in _groupPlayers.Values)
                group.Remove(player);
        }
    }
}
