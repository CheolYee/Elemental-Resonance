using UnityEngine;

namespace TMProEffect
{
    public class TMPEffect : TMPEffectBase
    {
        #region TMP Shader Property ID
        private static readonly string OUTLINE_ON = "OUTLINE_ON";
        private static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly string OUTLINE_ON_2 = "OUTLINE_ON_2";
        private static readonly int _OutlineColor2 = Shader.PropertyToID("_OutlineColor2");
        private static readonly int _OutlineWidth2 = Shader.PropertyToID("_OutlineWidth2");
        private static readonly string UNDERLAY_ON = "UNDERLAY_ON";
        private static readonly int _UnderlayColor = Shader.PropertyToID("_UnderlayColor");
        private static readonly int _UnderlayOffsetX = Shader.PropertyToID("_UnderlayOffsetX");
        private static readonly int _UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");
        private static readonly int _UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");
        private static readonly string USE_VERTEX_COLOR = "USE_VERTEX_COLOR";
        #endregion

        [Header("Base")]
        [Tooltip("vertex color use in outline and underlay")]
        public bool useVertexColor = false;

        [Header("Outline 1")]
        public bool outline = false;
        public Color outlineColor = Color.black;
        [Min(0)]
        public float outlineWidth = 0f;

        [Header("Outline 2")]
        public bool outline2 = false;
        public Color outlineColor2 = Color.black;
        [Min(0)]
        public float outlineWidth2 = 0f;
        private float m_outlineWidth = 0f;
        private float m_outlineWidth2 = 0f;

        [Header("Underlay")]
        public bool underlay = false;
        public Color underlayColor = Color.black;
        [Range(-180, 180)]
        public float shadowAngle = 90f;
        [Min(0)]
        public float shadowDis = 0f;
        [Range(-1, 1)]
        private float m_underlayOffsetX = 0f;
        [Range(-1, 1)]
        private float m_underlayOffsetY = 0f;
        [Min(0)]
        public float shadowBlur = 0f;
        [Range(0, 1)]
        private float m_underlaySoftness = 0f;

        protected override void DoSetMaterial(Material mat)
        {
            float scale = text.font.faceInfo.pointSize / text.fontSize / text.font.atlasPadding;
            //转换效果参数
            if (outline)
            {
                m_outlineWidth = Mathf.Clamp01(outlineWidth * scale);
                if (outline2)
                {
                    m_outlineWidth2 = Mathf.Clamp01(outlineWidth2 * scale);
                }
            }
            else
            {
                outline2 = false;
            }
            if (underlay)
            {
                m_underlaySoftness = Mathf.Clamp01(shadowBlur * scale);
                float dis = Mathf.Clamp01(shadowDis * scale);
                m_underlayOffsetX = -Mathf.Max(Mathf.Min(dis * Mathf.Cos(Mathf.Deg2Rad * shadowAngle), 1), -1);
                m_underlayOffsetY = -Mathf.Max(Mathf.Min(dis * Mathf.Sin(Mathf.Deg2Rad * shadowAngle), 1), -1);
            }

            if (useVertexColor)
            {
                mat.EnableKeyword(USE_VERTEX_COLOR);
            }
            else
            {
                mat.DisableKeyword(USE_VERTEX_COLOR);
            }
            if (outline)
            {
                mat.EnableKeyword(OUTLINE_ON);
                mat.SetColor(_OutlineColor, outlineColor);
                mat.SetFloat(_OutlineWidth, m_outlineWidth);
            }
            else
            {
                mat.DisableKeyword(OUTLINE_ON);
                mat.SetColor(_OutlineColor, Color.black);
                mat.SetFloat(_OutlineWidth, 0);
            }
            if (outline2)
            {
                mat.EnableKeyword(OUTLINE_ON_2);
                mat.SetColor(_OutlineColor2, outlineColor2);
                mat.SetFloat(_OutlineWidth2, m_outlineWidth2);
            }
            else
            {
                mat.DisableKeyword(OUTLINE_ON_2);
                mat.SetColor(_OutlineColor2, Color.black);
                mat.SetFloat(_OutlineWidth2, 0);
            }
            if (underlay)
            {
                mat.EnableKeyword(UNDERLAY_ON);
                mat.SetColor(_UnderlayColor, underlayColor);
                mat.SetFloat(_UnderlaySoftness, m_underlaySoftness);
                mat.SetFloat(_UnderlayOffsetX, m_underlayOffsetX);
                mat.SetFloat(_UnderlayOffsetY, m_underlayOffsetY);
            }
            else
            {
                mat.DisableKeyword(UNDERLAY_ON);
                mat.SetColor(_UnderlayColor, Color.black);
                mat.SetFloat(_UnderlaySoftness, 0);
                mat.SetFloat(_UnderlayOffsetX, 0);
                mat.SetFloat(_UnderlayOffsetY, 0);
            }
        }
    }
}
