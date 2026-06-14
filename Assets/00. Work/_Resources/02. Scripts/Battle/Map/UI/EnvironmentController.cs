using UnityEngine;

namespace Battle.Map.UI
{
    public class EnvironmentController : MonoBehaviour
    {
        [SerializeField] private Material _defaultSkybox;
        [SerializeField] private Material _nightSkybox;
        [SerializeField] private Light _dayLight;
        [SerializeField] private Light _nightLight;

        public void SetNight()
        {
            if (_nightSkybox == null) { Debug.LogWarning("[EnvironmentController] _nightSkybox is null"); return; }
            RenderSettings.skybox = _nightSkybox;
            if (_dayLight != null)  _dayLight.gameObject.SetActive(false);
            if (_nightLight != null) _nightLight.gameObject.SetActive(true);
            DynamicGI.UpdateEnvironment();
        }

        public void SetDay()
        {
            if (_defaultSkybox == null) { Debug.LogWarning("[EnvironmentController] _defaultSkybox is null"); return; }
            RenderSettings.skybox = _defaultSkybox;
            if (_nightLight != null) _nightLight.gameObject.SetActive(false);
            if (_dayLight != null)  _dayLight.gameObject.SetActive(true);
            DynamicGI.UpdateEnvironment();
        }
    }
}
