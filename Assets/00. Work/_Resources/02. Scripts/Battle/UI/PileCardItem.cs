using Battle.Data;
using Battle.Events;
using Battle.Instances;
using Gamelib.EventSystem;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.UI
{
    public class PileCardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private EventChannelSO battleEventChannel;
        [SerializeField] private Image artworkImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descText;

        [Header("Hover")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.15f;

        private CardInstance _tempInstance;
        private MotionHandle _scaleHandle;

        public void Setup(CardDataSO data)
        {
            _tempInstance = new CardInstance(data);
            if (artworkImage != null) artworkImage.sprite = data.artwork;
            if (nameText != null) nameText.text = data.cardName;
            if (costText != null) costText.text = data.cost.ToString();
            if (descText != null) descText.text = data.description;
        }

        public void Setup(CardInstance instance) => Setup(instance.data);

        public void OnPointerEnter(PointerEventData eventData)
        {
            TweenScale(hoverScale);
            if (_tempInstance != null)
                battleEventChannel.RaiseEvent(new CardHoverEvent(_tempInstance));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TweenScale(1f);
            battleEventChannel.RaiseEvent(new CardHoverExitEvent());
        }

        private void TweenScale(float target)
        {
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();
            _scaleHandle = LMotion.Create(transform.localScale.x, target, hoverDuration)
                .Bind(s => transform.localScale = new Vector3(s, s, 1f));
        }

        private void OnDestroy()
        {
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();
        }
    }
}
