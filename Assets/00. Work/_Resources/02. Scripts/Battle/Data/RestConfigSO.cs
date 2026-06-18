using UnityEngine;

namespace Battle.Data
{
    [CreateAssetMenu(fileName = "RestConfig", menuName = "Battle/Rest Config")]
    public class RestConfigSO : ScriptableObject
    {
        [Range(0f, 1f)] public float minHealPercent = 0.2f;
        [Range(0f, 1f)] public float maxHealPercent = 0.4f;
    }
}
