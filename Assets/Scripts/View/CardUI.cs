using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 卡牌 UI 元素 — 代表一张卡牌的视觉表现。
    /// <para>使用 Resources/CardFaces 下的塔罗牌面 Sprite。</para>
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI _cardIdText;
        [SerializeField] private TextMeshProUGUI _cardNameText;
        [SerializeField] private Image _background;
        [SerializeField] private Image _cardFaceImage; // 牌面 Sprite
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>卡牌 ID → Resources 路径（不含扩展名）。</summary>
        private static readonly Dictionary<int, string> CardSpritePaths = new()
        {
            { 2, "CardFaces/clover" },
            { 3, "CardFaces/ship" },
            { 4, "CardFaces/house" },
            { 5, "CardFaces/tree" },
            { 6, "CardFaces/cloud" },
            { 7, "CardFaces/snake" },
            { 8, "CardFaces/coffin" },
        };

        /// <summary>卡牌 ID → 显示名称。</summary>
        private static readonly Dictionary<int, string> CardNames = new()
        {
            { 2, "幸运草" }, { 3, "船" }, { 4, "房子" },
            { 5, "树" }, { 6, "云" }, { 7, "蛇" }, { 8, "棺材" },
        };

        /// <summary>缓存已加载的 Sprite。</summary>
        private static readonly Dictionary<int, Sprite> _spriteCache = new();

        public CardBase Card { get; private set; }
        public bool IsSelected { get; private set; }

        public event Action<CardUI> OnCardClicked;
        public event Action<CardUI> OnCardHovered;
        public event Action<CardUI> OnCardUnhovered;

        public void Setup(CardBase card)
        {
            Card = card;

            if (CardNames.TryGetValue(card.Id, out var name))
            {
                if (_cardIdText != null) _cardIdText.text = card.Id.ToString();
                if (_cardNameText != null) _cardNameText.text = name;
            }

            // 加载并设置牌面 Sprite
            if (_cardFaceImage != null)
            {
                var sprite = GetCardSprite(card.Id);
                if (sprite != null)
                {
                    _cardFaceImage.sprite = sprite;
                    _cardFaceImage.color = Color.white;
                    _cardFaceImage.preserveAspect = true;
                }
            }

            SetSelected(false);
        }

        private static Sprite GetCardSprite(int cardId)
        {
            if (_spriteCache.TryGetValue(cardId, out var cached))
                return cached;

            if (CardSpritePaths.TryGetValue(cardId, out var path))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                    _spriteCache[cardId] = sprite;
                return sprite;
            }
            return null;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            var pos = transform.localPosition;
            pos.y = selected ? 30f : 0f;
            transform.localPosition = pos;
        }

        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = interactable ? 1f : 0.5f;
                _canvasGroup.blocksRaycasts = interactable;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 如果在场地（BoardAreaUI）上，转发给 BoardAreaUI 处理
            var boardArea = GetComponentInParent<BoardAreaUI>();
            if (boardArea != null)
            {
                boardArea.OnBoardCardClick(this);
                boardArea.OnBoardAreaClick();
                return;
            }

            // 在手牌中 → 正常选中逻辑
            SetSelected(!IsSelected);
            OnCardClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnCardHovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnCardUnhovered?.Invoke(this);
        }
    }
}
