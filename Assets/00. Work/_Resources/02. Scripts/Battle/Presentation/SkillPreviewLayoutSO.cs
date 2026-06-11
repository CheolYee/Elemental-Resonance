using UnityEngine;

namespace Battle.Presentation
{
    [CreateAssetMenu(menuName = "Battle/Skill Preview Layout")]
    public class SkillPreviewLayoutSO : ScriptableObject
    {
        public GameObject   casterPrefab;
        public GameObject[] targetPrefabs   = new GameObject[3];
        public Vector3      casterPosition;
        public Vector3[]    targetPositions = new Vector3[3];
        public GameObject   environmentPrefab;
        public Vector3      cameraPosition;
        public Vector3      cameraRotationEuler;
        public float        cameraFov = 60f;
    }
}
