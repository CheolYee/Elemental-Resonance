using System;
using Battle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckBuilding
{
    public class DeckBuilderCardItemView : MonoBehaviour
    {
        [SerializeField] private Image _artworkImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private Button _button;

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
        }
    }
}
