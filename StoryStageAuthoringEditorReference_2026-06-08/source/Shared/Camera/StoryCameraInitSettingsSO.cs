using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Camera
{
    [CreateAssetMenu(
        fileName = "StoryCameraInitSettings",
        menuName = "CheolYee/Story/Camera Init Settings")]
    public sealed class StoryCameraInitSettingsSO : ScriptableObject
    {
        [SerializeField] private float baseOrthographicSize = 5f;
        [SerializeField] private float defaultZoom = 1f;
        [SerializeField] private Vector2 defaultStageLocalPosition = Vector2.zero;

        public float BaseOrthographicSize => Mathf.Max(0.1f, baseOrthographicSize);
        public float DefaultZoom => Mathf.Max(0.01f, defaultZoom);
        public Vector2 DefaultStageLocalPosition => defaultStageLocalPosition;

        // cameraWorldWidth = baseOrthographicSize * 2 * storyVisibleAspect (zoom=1 기준)
        public float GetBaseCameraWorldWidth(float storyVisibleAspect) =>
            BaseOrthographicSize * 2f * Mathf.Max(0.0001f, storyVisibleAspect);
    }
}
