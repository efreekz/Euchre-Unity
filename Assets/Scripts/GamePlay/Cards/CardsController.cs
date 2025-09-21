using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ui.GamePlayScreens;
using UIArchitecture;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GamePlay.Cards
{
    public class CardsController : MonoBehaviour
    {
        public List<CardData> cards = new List<CardData>();
        public SuitData[] suits;
        public Card cardPrefab;

        private readonly List<Card> _deck = new List<Card>();
        
        private GamePlayScreen _gamePlayScreen;
        private GamePlayScreen GamePlayScreen {
            get
            {
                if (_gamePlayScreen == null)
                    _gamePlayScreen = UiManager.Instance.GetUiView(UiScreenName.GamePlayScreens) as GamePlayScreen;
                return _gamePlayScreen;
            }
            set => _gamePlayScreen = value;
        }

        public void Initialize(GamePlayScreen gamePlayScreen)
        {
            GamePlayScreen = gamePlayScreen;
        }

        private void InitCards()
        {
            foreach (var cardData in cards)
            {
                var card = Instantiate(cardPrefab, GamePlayScreen.GetDeck());
                card.Init(cardData);
                _deck.Add(card);
            }
        }

        public List<Card> GetShuffledDeck()
        {
            InitCards();
            for (var i = 0; i < _deck.Count; i++)
            {
                var randomIndex = Random.Range(i, _deck.Count);
                (_deck[i], _deck[randomIndex]) = (_deck[randomIndex], _deck[i]);
            }
            return _deck;
        }

        public SuitData GetSuitData(Suit suit)
        {
            return Array.Find(suits, s => s.suit == suit);
        }

        public void Reset()
        {
            foreach (var card in _deck)
            {
                Destroy(card.gameObject);
            }
            _deck.Clear();
        }

        public CardData ToCardData(CardDataDto dto)
        {
            return cards.FirstOrDefault(cardData => cardData.rank == dto.rank && cardData.suit == dto.suit);
        }
    }

}