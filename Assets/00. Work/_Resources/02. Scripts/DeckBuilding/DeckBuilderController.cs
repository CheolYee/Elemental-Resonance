using System.Collections.Generic;
using Battle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeckBuilding
{
    public class DeckBuilderController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CardDatabaseSO _cardDatabase;
        [SerializeField] private TempStartCardSO _deckProvider;

        [Header("카드 목록 (상단)")]
        [SerializeField] private Transform _cardGridRoot;
        [SerializeField] private DeckBuilderCardItemView _cardItemPrefab;

        [Header("덱 구성 (하단)")]
        [SerializeField] private Transform _deckSlotRoot;
        [SerializeField] private DeckBuilderCardItemView _deckCardItemPrefab;
        [SerializeField] private TextMeshProUGUI _deckCountText;

        [Header("시작")]
        [SerializeField] private Button _startButton;
        [SerializeField] private string _mainSceneName = "Main";

        private readonly List<CardDataSO> _selectedCards = new();
        private readonly List<DeckBuilderCardItemView> _deckSlotViews = new();

        private void Start()
        {
            BuildCardGrid();
            _startButton.onClick.AddListener(OnStartClicked);
            RefreshDeckUI();
        }

        private void BuildCardGrid()
        {
            foreach (var cardData in _cardDatabase.allCards)
            {
                var item = Instantiate(_cardItemPrefab, _cardGridRoot);
                item.Setup(cardData);
                item.OnClicked += OnCardClicked;
            }
        }

        private void OnCardClicked(CardDataSO data)
        {
            if (_selectedCards.Count >= _cardDatabase.maxDeckSize) return;
            _selectedCards.Add(data);
            RefreshDeckUI();
        }

        private void OnDeckCardClicked(CardDataSO data)
        {
            _selectedCards.Remove(data);
            RefreshDeckUI();
        }

        private void RefreshDeckUI()
        {
            foreach (var view in _deckSlotViews)
                Destroy(view.gameObject);
            _deckSlotViews.Clear();

            foreach (var cardData in _selectedCards)
            {
                var item = Instantiate(_deckCardItemPrefab, _deckSlotRoot);
                item.Setup(cardData);
                item.OnClicked += OnDeckCardClicked;
                _deckSlotViews.Add(item);
            }

            _deckCountText.text = $"{_selectedCards.Count} / {_cardDatabase.maxDeckSize}";
            _startButton.interactable = _selectedCards.Count > 0;
        }

        private void OnStartClicked()
        {
            _deckProvider.SetDeck(_selectedCards);
            SceneManager.LoadScene(_mainSceneName);
        }
    }
}
