using System;
using System.Threading;
using Battle.Data;
using Battle.Enums;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using LitMotion;
using TMPro;
using TMProEffect;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.UI
{
    public class RewardCardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image     artworkImage;
        [SerializeField] private Image     frameImage;
        [SerializeField] private Image     labelImage;
        [SerializeField] private Image     gradeGlowImage;
        [SerializeField] private Image     fusionGlowImage;
        [SerializeField] private Image     costImage;
        [SerializeField] private ElementIconTableSO elementIconTable;
        [SerializeField] private TMP_Text  nameText;
        [SerializeField] private TMP_Text  typeText;
        [SerializeField] private TMP_Text  costText;
        [SerializeField] private TMP_Text  descText;
        [SerializeField] private TMPEffect nameTextEffect;
        [SerializeField] private TMPEffect typeTextEffect;

        [Header("Sound")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SfxSounds      hoverSound;

        [Header("Hover")]
        [SerializeField] private float hoverScale         = 1.06f;
        [SerializeField] private float hoverScaleDuration = 0.15f;
        [SerializeField] private float hoverGlowAlpha     = 0.55f;
        [SerializeField] private float hoverGlowDuration  = 0.2f;
        [SerializeField] private Color hoverGlowColor     = Color.white;

        public Action OnClicked;

        private bool _interactable = true;

        private MotionHandle _hoverScaleHandle;
        private MotionHandle _hoverGlowHandle;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (fusionGlowImage != null)
                fusionGlowImage.color = new Color(hoverGlowColor.r, hoverGlowColor.g, hoverGlowColor.b, 0f);
        }

        private void OnDestroy()
        {
            if (_hoverScaleHandle.IsActive()) _hoverScaleHandle.Cancel();
            if (_hoverGlowHandle.IsActive())  _hoverGlowHandle.Cancel();
        }

        public void Setup(CardDataSO data)
        {
            _interactable = true;
            transform.localScale = _baseScale;
            if (fusionGlowImage != null)
                fusionGlowImage.color = new Color(hoverGlowColor.r, hoverGlowColor.g, hoverGlowColor.b, 0f);

            if (artworkImage != null) artworkImage.sprite = data.artwork;
            if (nameText != null)     nameText.text        = data.cardName;
            if (descText != null)     descText.text        = data.description;
            if (costText != null)     costText.text        = data.cost.ToString();
            if (typeText != null)     typeText.text        = CardColorUtility.GetTypeName(data.cardType);

            CardColorUtility.Apply(frameImage, labelImage, data);
            CardColorUtility.ApplyTextEffects(nameTextEffect, typeTextEffect, data.grade);
            CardColorUtility.ApplyCostImage(costImage, elementIconTable, data.elementType);
            ApplyGradeGlow(data.grade);
        }

        private void ApplyGradeGlow(CardGrade grade)
        {
            if (gradeGlowImage == null) return;
            if (!CardColorUtility.GradeColors.TryGetValue(grade, out var color)) return;

            float alpha = grade switch
            {
                CardGrade.Normal    => 0f,
                CardGrade.Rare      => 0.25f,
                CardGrade.Epic      => 0.45f,
                CardGrade.Legendary => 0.65f,
                _                   => 0f,
            };
            gradeGlowImage.color = new Color(color.r, color.g, color.b, alpha);
        }

        public void SetInteractable(bool interactable) => _interactable = interactable;

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_interactable) return;
            soundChannel?.RaiseEvent(new PlaySoundEvent(hoverSound, Vector3.zero));
            TweenHover(true);
        }

        public void OnPointerExit(PointerEventData _)
        {
            TweenHover(false);
        }

        private void TweenHover(bool enter)
        {
            float baseUniform = _baseScale.x;
            float targetScale  = enter ? baseUniform * hoverScale : baseUniform;
            float currentScale = transform.localScale.x;
            if (_hoverScaleHandle.IsActive()) _hoverScaleHandle.Cancel();
            _hoverScaleHandle = LMotion.Create(currentScale, targetScale, hoverScaleDuration)
                .WithEase(enter ? Ease.OutBack : Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => { if (this != null) transform.localScale = Vector3.one * s; });

            if (fusionGlowImage != null)
            {
                float currentAlpha = fusionGlowImage.color.a;
                float targetAlpha  = enter ? hoverGlowAlpha : 0f;
                if (_hoverGlowHandle.IsActive()) _hoverGlowHandle.Cancel();
                _hoverGlowHandle = LMotion.Create(currentAlpha, targetAlpha, hoverGlowDuration)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(a => { if (fusionGlowImage != null) fusionGlowImage.color = new Color(hoverGlowColor.r, hoverGlowColor.g, hoverGlowColor.b, a); });
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable) return;
            OnClicked?.Invoke();
        }

        public async UniTask PlaySelectAsync(CancellationToken ct)
        {
            _interactable = false;
            float start = transform.localScale.x;

            await LMotion.Create(start, start * 1.25f, 0.1f)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f))
                .ToUniTask(cancellationToken: ct);

            await LMotion.Create(transform.localScale.x, 0f, 0.15f)
                .WithEase(Ease.InBack)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f))
                .ToUniTask(cancellationToken: ct);
        }
    }
}
