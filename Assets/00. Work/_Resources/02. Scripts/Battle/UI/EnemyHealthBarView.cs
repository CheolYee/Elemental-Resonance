using _00._Work._Resources._02._Scripts.Agents;
using _00._Work._Resources._02._Scripts.Agents.Enemies;
using _00._Work._Resources._02._Scripts.Modules;
using Agents;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class EnemyHealthBarView : MonoBehaviour, IModule
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Slider redSlider;
        [SerializeField] private Slider graySlider;
        [SerializeField] private float redDuration = 0.15f;
        [SerializeField] private float grayDelay = 0.5f;
        [SerializeField] private float grayDuration = 0.3f;
        [SerializeField] private float fadeInDuration = 0.4f;

        private HealthModule _health;
        private AgentTrigger _agentTrigger;
        private MotionHandle _redHandle;
        private MotionHandle _grayHandle;
        private MotionHandle _fadeHandle;

        public void Initialize(ModuleOwner owner)
        {
            _health = owner.GetModule<HealthModule>();
            _health.OnHpChanged += OnHpChanged;

            _agentTrigger = owner.GetModule<AgentTrigger>();
            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd += HandleEntryEnd;

            if (owner is AbstractEnemy enemy)
                nameText.text = enemy.EnemyData.enemyName;

            canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            redSlider.maxValue = _health.MaxHp;
            redSlider.value = _health.CurrentHp;
            graySlider.maxValue = _health.MaxHp;
            graySlider.value = _health.CurrentHp;
        }

        private void HandleEntryEnd()
        {
            _agentTrigger.OnAnimationEnd -= HandleEntryEnd;

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(0f, 1f, fadeInDuration)
                .Bind(a => canvasGroup.alpha = a);
        }

        private void OnHpChanged(int currentHp, int maxHp)
        {
            if (_redHandle.IsActive()) _redHandle.Cancel();
            _redHandle = LMotion.Create(redSlider.value, (float)currentHp, redDuration)
                .Bind(v => redSlider.value = v);

            if (_grayHandle.IsActive()) _grayHandle.Cancel();
            _grayHandle = LMotion.Create(graySlider.value, (float)currentHp, grayDuration)
                .WithDelay(grayDelay)
                .Bind(v => graySlider.value = v);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHpChanged -= OnHpChanged;
            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd -= HandleEntryEnd;
        }
    }
}
