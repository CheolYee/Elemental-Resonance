using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Modules;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class PlayerHealthBarView : MonoBehaviour, IModule
    {
        [SerializeField] private TMP_Text hpText;

        private HealthModule _health;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
            _health.OnHpChanged += UpdateDisplay;
        }

        private void Start()
        {
            UpdateDisplay(_health.CurrentHp, _health.MaxHp);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHpChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(int currentHp, int maxHp)
        {
            hpText.text = $"{currentHp}/{maxHp}";
        }
    }
}
