using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay.Cards
{
    public class Card : MonoBehaviour
    {
        public Transform cardHolder;
        public Image image;
        public Image backImage;
        public Button button;

        public CardData cardData;
        
        private Vector3 _originalPosition;
        private Tween _interactTween;

        public CardState state = CardState.InDeck;
        public bool isFaceUp;

        public Action OnCardClicked; // ✅ Step 1: Custom click event
        public RectTransform rectTransform;

        private void Start()
        {
            _originalPosition = cardHolder.localPosition;
        }

        public void SetCardData(CardData updatedCardData)
        {
            Init(updatedCardData);
        }

        public void Init(CardData data)
        {
            cardData = data;
            isFaceUp = data.isFaceUp;
            
            image.sprite = cardData.sprite;
            image.gameObject.SetActive(isFaceUp);
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                OnCardClicked?.Invoke();
            });
        }

        public void SetFaceUp(bool showFaceUp)
        {
            if (isFaceUp == showFaceUp) return; // Already in desired state

            isFaceUp = showFaceUp;

            // transform.DOKill(); // Stop any existing tweens
            cardHolder.DOLocalRotate(new Vector3(0, 90, 0), 0.15f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // Swap card face/back
                    image.gameObject.SetActive(isFaceUp);
                    backImage.gameObject.SetActive(!isFaceUp);

                    // Continue rotation to 0°
                    cardHolder.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.OutQuad);
                });
        }
        
        public void SetInteractable(bool isInteractable)
        {
            // Kill any previous animation
            _interactTween?.Kill();
            
            if (isInteractable)
            {
                // Move up by 20 units smoothly
                _interactTween = cardHolder.DOLocalMove(_originalPosition + Vector3.up * 50f, 0.25f)
                    .SetEase(Ease.OutQuad);
            }
            else
            {
                // Move back to original position
                _interactTween = cardHolder.DOLocalMove(_originalPosition, 0.25f)
                    .SetEase(Ease.OutQuad);
            }
            // button.interactable = isInteractable;
        }
        
        public int GetCardPower(Suit trumpSuit, Suit leadSuit)
        {
            IsTrump(trumpSuit);
            var isLeftBower = IsLeftBower(trumpSuit);
            var isRightBower = IsRightBower(trumpSuit);

            if (isRightBower) return 100; // Highest
            if (isLeftBower) return 99;   // Second highest

            if (cardData.suit == trumpSuit) return 90 + (int)cardData.rank;
            if (cardData.suit == leadSuit) return 50 + (int)cardData.rank;

            return 10 + (int)cardData.rank; // Non-trump, non-lead suit
        }

        private bool IsRightBower(Suit trumpSuit)
        {
            return cardData.rank == Rank.Jack && cardData.suit == trumpSuit;
        }

        private bool IsLeftBower(Suit trumpSuit)
        {
            if (cardData.rank != Rank.Jack) return false;

            return (trumpSuit == Suit.Spades && cardData.suit == Suit.Clubs) ||
                   (trumpSuit == Suit.Clubs && cardData.suit == Suit.Spades) ||
                   (trumpSuit == Suit.Hearts && cardData.suit == Suit.Diamonds) ||
                   (trumpSuit == Suit.Diamonds && cardData.suit == Suit.Hearts);
        }

        public bool IsTrump(Suit trumpSuit)
        {
            return cardData.suit == trumpSuit || IsLeftBower(trumpSuit);
        }
        
        public Suit GetEffectiveSuit(Suit trumpSuit)
        {
            // Left bower is treated as trump suit for follow-suit purposes
            if (IsLeftBower(trumpSuit))
                return trumpSuit;
            return cardData.suit;
        }
    }
}