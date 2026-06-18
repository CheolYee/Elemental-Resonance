using System;
using Battle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.UI
{
    public class ShopCardSlotView : MonoBehaviour
    {
        [SerializeField] private RewardCardView _cardView;
        [SerializeField] private TMP_Text       _priceText;
        [SerializeField] private Button         _buyButton;
        [SerializeField] private CanvasGroup    _dimGroup;

        [Header("Colors")]
        [SerializeField] private Color _normalPriceColor    = Color.white;
        [SerializeField] private Color _insufficientColor   = new Color(1f, 0.3f, 0.3f);

        public event Action OnBuyClicked;

        private CardDataSO _card;
        private int        _price;
        private bool       _purchased;

        private void Awake()
        {
            _buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke());
        }

        public void Setup(CardDataSO card, int price)
        {
            _card      = card;
            _price     = price;
            _purchased = false;

            _cardView.Setup(card);
            _priceText.text  = price.ToString();
            _priceText.color = _normalPriceColor;
            _buyButton.interactable = true;

            if (_dimGroup != null)
            {
                _dimGroup.alpha          = 0f;
                _dimGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(true);
        }

        public void SetPurchased()
        {
            _purchased = true;
            _buyButton.interactable = false;
            if (_dimGroup != null)
            {
                _dimGroup.alpha          = 1f;
                _dimGroup.blocksRaycasts = true;
            }
        }

        public void SetPriceInsufficient(bool insufficient)
        {
            if (_purchased) return;
            _priceText.color = insufficient ? _insufficientColor : _normalPriceColor;
        }

        public CardDataSO Card               => _card;
        public int        Price             => _price;
        public bool       IsPurchased       => _purchased;
        public Vector2    CardScreenPosition => _cardView != null ? (Vector2)_cardView.transform.position : (Vector2)transform.position;
    }
}
