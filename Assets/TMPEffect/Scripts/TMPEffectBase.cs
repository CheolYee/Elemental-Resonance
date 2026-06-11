using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TMProEffect
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
    public abstract class TMPEffectBase : MonoBehaviour
    {
        private TMP_Text m_text;
        /// <summary>
        /// TMP Component
        /// </summary>
        public TMP_Text text
        {
            get
            {
                if (m_text == null)
                {
                    m_text = GetComponent<TMP_Text>();
                }
                return m_text;
            }
        }
        /// <summary>
        /// using effect material list
        /// </summary>
        private List<TMPEffectMaterial> effectMaterials = new List<TMPEffectMaterial>();

        private void OnEnable()
        {
            SetupEffect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnEditorModeChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnEditorModeChanged;
#endif
            SetupEffect(false);
        }

        private void OnDestroy()
        {
            //ensure release all materials
            ReleaseAllMats();
        }

        /// <summary>
        /// refresh effect
        /// </summary>
        /// <param name="active">true to refresh, false to use default material</param>
        public void SetupEffect(bool active = true)
        {
            ReleaseAllMats();
            DoSetup(active);
        }

        void ReleaseAllMats()
        {
            for (int i = 0; i < effectMaterials.Count; i++)
                TMPEffectMaterial.ReleaseEffectMaterial(effectMaterials[i]);
            effectMaterials.Clear();
        }

        protected virtual void DoSetup(bool active = true)
        {
            text.fontSharedMaterial = text.font.material;
            if (CanRender())
            {
                text.ForceMeshUpdate();

                if (active)
                {
                    var fontMaterial = text.fontSharedMaterial;
                    TMPEffectMaterial effectMaterial;
                    effectMaterial = TMPEffectMaterial.LoadEffectMaterial(fontMaterial, DoSetMaterial);

                    effectMaterials.Add(effectMaterial);
                    text.fontSharedMaterial = effectMaterial.EffectMaterial;
                    text.ForceMeshUpdate();
                }
            }
        }

        abstract protected void DoSetMaterial(Material mat);

        bool CanRender()
        {
            return text != null && text.gameObject.activeInHierarchy && text.enabled == true && text.textInfo != null && text.font != null && text.font.material != null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            SetupEffect(enabled);
        }

        void OnEditorModeChanged(UnityEditor.PlayModeStateChange mode)
        {
            if (mode == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                SetupEffect(enabled);
            }
        }
#endif
    }
}
