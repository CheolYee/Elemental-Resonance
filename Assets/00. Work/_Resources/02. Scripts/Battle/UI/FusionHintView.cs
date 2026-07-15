using System;
using System.Collections.Generic;
using Battle.Enums;
using LitMotion;
using TMPro;
using TMProEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public struct FusionHintData
    {
        public ElementType MaterialElement;
        public ElementType ResultElement;
    }

    public class FusionHintView : MonoBehaviour
    {
        [Serializable]
        private struct HintEntry
        {
            public GameObject root;
            public Image      materialIcon;
            public TMP_Text   materialText;
            public TMPEffect  materialTextEffect;
            public Image      resultIcon;
            public TMP_Text   resultText;
            public TMPEffect  resultTextEffect;
        }

        [SerializeField] private CanvasGroup        canvasGroup;
        [SerializeField] private HintEntry[]        entries;          // Inspector에서 2개 설정
        [SerializeField] private ElementIconTableSO elementIconTable;
        [SerializeField] private float              fadeDuration = 0.12f;

        private MotionHandle _fadeHandle;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            foreach (var e in entries)
                if (e.root != null) e.root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        }

        public void Show(List<FusionHintData> hints)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.root == null) continue;

                if (i < hints.Count)
                {
                    var hint = hints[i];
                    entry.root.SetActive(true);

                    if (entry.materialIcon != null)
                    {
                        var icon = elementIconTable != null ? elementIconTable.GetIcon(hint.MaterialElement) : null;
                        entry.materialIcon.sprite  = icon;
                        entry.materialIcon.enabled = icon != null;
                    }
                    if (entry.materialText != null)
                        entry.materialText.text = CardColorUtility.GetElementName(hint.MaterialElement);
                    CardColorUtility.ApplyElementTextEffect(entry.materialTextEffect, hint.MaterialElement);

                    if (entry.resultIcon != null)
                    {
                        var icon = elementIconTable != null ? elementIconTable.GetIcon(hint.ResultElement) : null;
                        entry.resultIcon.sprite  = icon;
                        entry.resultIcon.enabled = icon != null;
                    }
                    if (entry.resultText != null)
                        entry.resultText.text = CardColorUtility.GetElementName(hint.ResultElement);
                    CardColorUtility.ApplyElementTextEffect(entry.resultTextEffect, hint.ResultElement);
                }
                else
                {
                    entry.root.SetActive(false);
                }
            }

            FadeTo(1f);
        }

        public void Hide() => FadeTo(0f);

        private void FadeTo(float target)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(canvasGroup.alpha, target, fadeDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(a => { if (this != null) canvasGroup.alpha = a; });
        }
    }
}
