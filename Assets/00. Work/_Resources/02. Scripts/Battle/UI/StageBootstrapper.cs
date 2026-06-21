using System;
using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using _00._Work._Resources._02._Scripts.Agents.Players;
using Battle.Data;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Reflex.Attributes;
using Reflex.Core;
using Reflex.Injectors;
using UnityEngine;

namespace Battle.UI
{
    public class StageBootstrapper : MonoBehaviour
    {
        [SerializeField] private BattleStageSO stageData;
        [SerializeField] private Transform[] slotPositions;
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private DeckController deckController;
        [SerializeField] private PlayerRunStateSO playerRunState;
        [SerializeField] private float waveTransitionDelay = 1.0f;

        [Inject] private Player _player;
        [Inject] private Container _container;

        private int _currentWaveIndex;
        private bool _battleEnded;

        private void OnEnable()
        {
            battleEventChannel.AddListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<WaveClearEvent>(OnWaveClear);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnBattleEnded(BattleVictoryEvent _) => _battleEnded = true;
        private void OnBattleEnded(BattleDefeatEvent _) => _battleEnded = true;

        public async UniTask BeginStage(BattleStageSO stage)
        {
            foreach (var enemy in enemyRegistry.Enemies)
                if (enemy != null) Destroy(enemy.gameObject);
            enemyRegistry.Clear();

            _battleEnded = false;
            _currentWaveIndex = 0;
            stageData = stage;

            battleEventChannel.RaiseEvent(new BattleSessionStartEvent());

            await UniTask.Yield();

            InitializePlayerHp();
            deckController.Initialize();

            SpawnWave(_currentWaveIndex);
            battleEventChannel.RaiseEvent(new WaveStartEvent(_currentWaveIndex, stageData.waves.Count));

            var tasks = new List<UniTask> { _player.BeginEntryAsync() };
            foreach (var enemy in enemyRegistry.Enemies)
                tasks.Add(enemy.WaitForEntryComplete());

            await UniTask.WhenAll(tasks);
            battleEventChannel.RaiseEvent(new PlayerTurnStartEvent());
        }

        private void InitializePlayerHp()
        {
            if (playerRunState == null) return;

            if (playerRunState.IsHpInitialized)
            {
                _player.Health.InitializeHp(playerRunState.CurrentHp, playerRunState.MaxHp);
            }
            else
            {
                _player.Health.Reinitialize();
                playerRunState.SetHp(_player.Health.CurrentHp, _player.Health.MaxHp);
            }
        }

        [ContextMenu("Debug: Begin Stage")]
        private void DebugBeginStage() => BeginStage(stageData).Forget();

        private void OnWaveClear(WaveClearEvent _)
        {
            if (_battleEnded) return;
            TransitionToNextWaveAsync().Forget();
        }

        private async UniTaskVoid TransitionToNextWaveAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(waveTransitionDelay), cancellationToken: destroyCancellationToken);

            _currentWaveIndex++;

            if (_currentWaveIndex >= stageData.waves.Count)
            {
                _battleEnded = true;
                battleEventChannel.RaiseEvent(new BattleVictoryEvent());
                return;
            }

            SpawnWave(_currentWaveIndex);
            battleEventChannel.RaiseEvent(new WaveStartEvent(_currentWaveIndex, stageData.waves.Count));

            var tasks = new List<UniTask>();
            foreach (var enemy in enemyRegistry.Enemies)
                tasks.Add(enemy.WaitForEntryComplete());

            await UniTask.WhenAll(tasks);
            battleEventChannel.RaiseEvent(new PlayerTurnStartEvent());
        }

        private void SpawnWave(int waveIndex)
        {
            enemyRegistry.Clear();
            var wave = stageData.waves[waveIndex];
            foreach (var entry in wave.enemySpawns)
            {
                int slot = entry.isLargeEnemy ? 1 : entry.slotIndex;
                var pos = slotPositions[slot].position + entry.localOffset;
                var go = Instantiate(entry.enemyData.enemyPrefab, pos, Quaternion.identity);
                GameObjectInjector.InjectRecursive(go, _container);
                var enemy = go.GetComponent<AbstractEnemy>();
                enemyRegistry.Register(enemy);
                enemy.OnDeathStarted += () => enemyRegistry.Unregister(enemy);
            }
            battleEventChannel.RaiseEvent(new EnemiesUpdatedEvent());
        }
    }
}
