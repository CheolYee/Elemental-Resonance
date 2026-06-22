using System.Collections.Generic;
using System.Linq;
using Battle.Data;
using Battle.Map.UI;
using DeckBuilding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battle.UI
{
    public class SaveController : MonoBehaviour
    {
        private const string KeySaveData    = "Run_SaveData";
        private const string KeyPendingLoad = "Run_PendingLoad";

        [SerializeField] private PlayerRunStateSO  _playerRunState;
        [SerializeField] private MapFlowController _mapFlowController;
        [SerializeField] private CardDatabaseSO    _cardDatabase;
        [SerializeField] private string            _titleSceneName = "Title";

        // ── 정적 유틸 (시작화면에서도 호출 가능) ─────────────────

        public static bool HasSaveData() =>
            !string.IsNullOrEmpty(PlayerPrefs.GetString(KeySaveData, ""));

        public static void SetPendingLoad()
        {
            PlayerPrefs.SetInt(KeyPendingLoad, 1);
            PlayerPrefs.Save();
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(KeySaveData);
            PlayerPrefs.DeleteKey(KeyPendingLoad);
            PlayerPrefs.Save();
        }

        // ── Main 씬 전용 ─────────────────────────────────────────

        public void Save()
        {
            var mapState = _mapFlowController?.CurrentMapState;
            var data = new RunSaveData
            {
                gold            = _playerRunState.Gold,
                currentHp       = _playerRunState.CurrentHp,
                maxHp           = _playerRunState.MaxHp,
                floorIndex      = _playerRunState.CurrentFloorIndex,
                cardIds         = _playerRunState.CurrentPile.Select(c => c.cardId).ToList(),
                graphId         = mapState?.graphId         ?? "",
                currentNodeId   = mapState?.currentNodeId   ?? "",
                visitedNodeIds  = mapState?.visitedNodeIds  ?? new List<string>(),
                resolvedNodeIds = mapState?.resolvedNodeIds ?? new List<string>(),
            };

            PlayerPrefs.SetString(KeySaveData, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public bool TryRestoreIfPending()
        {
            if (!HasSaveData()) return false;
            if (PlayerPrefs.GetInt(KeyPendingLoad, 0) == 0) return false;

            PlayerPrefs.SetInt(KeyPendingLoad, 0);
            PlayerPrefs.Save();

            string json = PlayerPrefs.GetString(KeySaveData, "");
            var data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null) return false;

            var cards = data.cardIds
                .Select(id => _cardDatabase.allCards.FirstOrDefault(c => c.cardId == id))
                .Where(c => c != null)
                .ToList();

            _playerRunState.LoadFromSave(data.gold, data.currentHp, data.maxHp, data.floorIndex, cards);
            _mapFlowController?.RestoreMapState(data);

            return true;
        }

        public void SaveAndReturnToTitle()
        {
            Save();
            SceneManager.LoadScene(_titleSceneName);
        }
    }
}
