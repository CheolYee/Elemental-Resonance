using _00._Work._Resources._02._Scripts.Agents.Enemies;
using _00._Work._Resources._02._Scripts.Modules;
using Agents;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class EnemyHealthBarView : AgentHealthBarView
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private float fadeInDuration = 0.4f;

        private AgentTrigger _agentTrigger;
        private MotionHandle _fadeHandle;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _agentTrigger = owner.GetModule<AgentTrigger>();
            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd += HandleEntryEnd;

            if (owner is AbstractEnemy enemy)
                nameText.text = enemy.EnemyData.enemyName;

            canvasGroup.alpha = 0f;
        }

        private void HandleEntryEnd()
        {
            _agentTrigger.OnAnimationEnd -= HandleEntryEnd;

            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(0f, 1f, fadeInDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => canvasGroup.alpha = a);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_agentTrigger != null)
                _agentTrigger.OnAnimationEnd -= HandleEntryEnd;
        }
    }
}
