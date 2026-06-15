using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace Battle.UI
{
    public class DamageTextSpawner : MonoBehaviour
    {
        [SerializeField] private PoolManagerSo poolManager;
        [SerializeField] private PoolItemSo damageTextItem;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private RectTransform uiParent;

        public void Spawn(Vector3 worldPos, int damageToHp, int damageToBlock)
        {
            if (damageToHp <= 0 && damageToBlock <= 0) return;

            var view = poolManager.Pop<DamageTextView>(damageTextItem);
            if (view == null) return;

            view.transform.SetParent(uiParent, false);

            Vector2 screenPos = battleCamera.WorldToScreenPoint(worldPos);
            view.Setup(poolManager);
            view.PlayDamage(screenPos, damageToHp, damageToBlock);
        }
    }
}
