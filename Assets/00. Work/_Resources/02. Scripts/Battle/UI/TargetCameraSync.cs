using UnityEngine;
using UnityEngine.Rendering;

namespace Battle.UI
{
    // CharacterOverlayCamera에 부착.
    // Cinemachine은 PostLateUpdate에서 물리 카메라를 구동하므로 LateUpdate로는 1프레임 지연 발생.
    // beginCameraRendering 콜백에서 소스 카메라 렌더 직전에 투영행렬·위치·회전을 복사한다.
    [RequireComponent(typeof(Camera))]
    public class TargetCameraSync : MonoBehaviour
    {
        [SerializeField] private Camera sourceCamera;

        private Camera _camera;

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != sourceCamera || sourceCamera == null) return;

            // URP는 오버레이 카메라의 projectionMatrix를 무시하고 fieldOfView로 재계산하므로
            // FOV·clip을 직접 복사한다.
            _camera.fieldOfView   = sourceCamera.fieldOfView;
            _camera.nearClipPlane = sourceCamera.nearClipPlane;
            _camera.farClipPlane  = sourceCamera.farClipPlane;

            // local offset으로 인한 ±1 포지션 어긋남 방지
            transform.SetPositionAndRotation(
                sourceCamera.transform.position,
                sourceCamera.transform.rotation);
        }
    }
}
