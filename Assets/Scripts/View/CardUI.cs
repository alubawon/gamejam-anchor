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
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI _cardIdText;
        [SerializeField] private TextMeshProUGUI _cardNameText;
        [SerializeField] private Image _background;
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>卡牌 ID → 显示名称 + 背景色 映射。</summary>
        private static readonly Dictionary<int, (string name, Color color)> CardVisuals = new()
        {
            { 2, ("幸运草", new Color(0.4f, 0.8f, 0.3f)) },
            { 3, ("船",     new Color(0.3f, 0.5f, 0.8f)) },
            { 4, ("房子",   new Color(0.8f, 0.6f, 0.3f)) },
            { 5, ("树",     new Color(0.2f, 0.6f, 0.2f)) },
            { 6, ("云",     new Color(0.7f, 0.8f, 0.9f)) },
            { 7, ("蛇",     new Color(0.5f, 0.3f, 0.5f)) },
            { 8, ("棺材",   new Color(0.2f, 0.2f, 0.25f)) },
        };

        public CardBase Card { get; private set; }
        public bool IsSelected { get; private set; }

        /// <summary>卡牌被点击。</summary>
        public event Action<CardUI> OnCardClicked;

        /// <summary>鼠标进入卡牌（用于显示牌面效果提示）。</summary>
        public event Action<CardUI> OnCardHovered;

        /// <summary>鼠标离开卡牌。</summary>
        public event Action<CardUI> OnCardUnhovered;

        public void Setup(CardBase card)
        {
            Card = card;
            if (CardVisuals.TryGetValue(card.Id, out var visual))
            {
                if (_cardIdText != null) _cardIdText.text = card.Id.ToString();
                if (_cardNameText != null) _cardNameText.text = visual.name;
                if (_background != null) _background.color = visual.color;
            }
            SetSelected(false);
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

        // ── 指针事件 ──────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
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
