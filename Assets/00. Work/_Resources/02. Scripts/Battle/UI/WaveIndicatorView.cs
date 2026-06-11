using Battle.Events;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;

namespace Battle.UI
{
    public class WaveIndicatorView : MonoBehaviour
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Popup")]
        [SerializeField] private float popDuration = 0.25f;
        [SerializeField] private float fadeDuration = 0.2f;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            battleEventChannel.AddListener<WaveStartEvent>(OnWaveStart);
            battleEventChannel.AddListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.AddListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnDisable()
        {
            battleEventChannel.RemoveListener<WaveStartEvent>(OnWaveStart);
            battleEventChannel.RemoveListener<BattleVictoryEvent>(OnBattleEnded);
            battleEventChannel.RemoveListener<BattleDefeatEvent>(OnBattleEnded);
        }

        private void OnWaveStart(WaveStartEvent evt)
        {
            waveText.text = $"{evt.WaveIndex + 1}/{evt.TotalWaves} 웨이브";
            PopIn();
        }

        private void OnBattleEnded(BattleVictoryEvent _) => FadeOut();
        private void OnBattleEnded(BattleDefeatEvent _) => FadeOut();

        private void PopIn()
        {
            transform.localScale = Vector3.one * 0.8f;
            canvasGroup.alpha = 1f;
            LMotion.Create(Vector3.one * 0.8f, Vector3.one, popDuration)
                .WithEase(Ease.OutBack)
                .Bind(s => transform.localScale = s);
        }

        private void FadeOut()
        {
            LMotion.Create(canvasGroup.alpha, 0f, fadeDuration)
                .Bind(a => canvasGroup.alpha = a);
        }
    }
}
