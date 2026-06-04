using System.Collections.Generic;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using _00._Work._Resources._02._Scripts.Agents.Players;
using Battle.Data;
using Battle.Events;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace Battle.UI
{
    public class StageBootstrapper : MonoBehaviour
    {
        [SerializeField] private BattleStageSO stageData;
        [SerializeField] private Transform[] slotPositions;
        [SerializeField] private Player player;
        [SerializeField] private RuntimeEnemyRegistrySO enemyRegistry;
        [SerializeField] private EventChannelSO battleEventChannel;

        private async UniTaskVoid Start()
        {
            await UniTask.Yield(); // 모든 Start() 완료 대기

            SpawnWave(0);

            // 씬에 존재하는 모든 AbstractEnemy(씬 배치 + 동적 스폰)를 수집
            // lazy TCS 덕분에 InitializeEntry() 호출 전에 수집해도 안전
            var tasks = new List<UniTask> { player.WaitForEntryComplete() };
            foreach (var enemy in FindObjectsByType<AbstractEnemy>(FindObjectsSortMode.None))
                tasks.Add(enemy.WaitForEntryComplete());

            await UniTask.WhenAll(tasks);
            battleEventChannel.RaiseEvent(new BattleReadyEvent());
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
                var enemy = go.GetComponent<AbstractEnemy>();
                enemyRegistry.Register(enemy);
            }
            battleEventChannel.RaiseEvent(new EnemiesUpdatedEvent());
        }
    }
}
