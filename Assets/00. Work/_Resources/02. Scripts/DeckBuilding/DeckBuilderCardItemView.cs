using System;
using Battle.Data;
using Battle.UI;
using TMPro;
using TMProEffect;
using UnityEngine;
using UnityEngine.UI;

namespace DeckBuilding
{
    public class DeckBuilderCardItemView : MonoBehaviour
    {
        [SerializeField] private Image _artworkImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _labelImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private Button _button;
        [SerializeField] private TMPEffect _nameTextEffect;
        [SerializeField] private TMPEffect _typeTextEffect;

        private CardDataSO _data;

        public event Action<CardDataSO> OnClicked;

        private void Awake()
        {
            _button.onClick.AddListener(() => OnClicked?.Invoke(_data));
        }

        public void Setup(CardDataSO data)
        {
            _data = data;
            _nameText.text = data.cardName;
            _descText.text = data.description;
            _costText.text = data.cost.ToString();
            if (_artworkImage != null && data.artwork != null)
                _artworkImage.sprite = data.artwork;
            if (_typeText != null) _typeText.text = CardColorUtility.GetTypeName(data.cardType);
            CardColorUtility.Apply(_frameImage, _labelImage, data);
            CardColorUtility.ApplyTextEffects(_nameTextEffect, _typeTextEffect, data.grade);
        }
    }
}
