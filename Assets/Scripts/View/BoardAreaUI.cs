using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 玩家场地区域 UI — 显示该玩家已打出的卡牌。
    /// <para>实现 IPointerClickHandler 以接收场地点击（选目标场地 / Snake 选对手）。</para>
    /// </summary>
    public class BoardAreaUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardUIPrefab;
        [SerializeField] private Image _panelBackground;
        [SerializeField] private Image _statusIcon;

        private int _playerId;
        private readonly List<CardUI> _cardUIs = new();

        public int PlayerId => _playerId;

        /// <summary>场地被点击（用于选目标场地 / Snake 选对手）。</summary>
        public event Action<BoardAreaUI> OnBoardClicked;

        /// <summary>场地上的卡牌被点击（用于 Boat 选目标牌）。</summary>
        public event Action<BoardAreaUI, CardUI> OnBoardCardClicked;

        /// <summary>场地上的卡牌被 hover。</summary>
        public event Action<BoardAreaUI, CardUI> OnBoardCardHovered;

        /// <summary>场地上的卡牌取消 hover。</summary>
        public event Action<BoardAreaUI, CardUI> OnBoardCardUnhovered;

        public void Setup(int playerId, string displayName)
        {
            _playerId = playerId;
            if (_playerNameText != null)
                _playerNameText.text = displayName;
            if (_statusIcon != null)
                _statusIcon.gameObject.SetActive(false);
        }

        /// <summary>添加一张卡牌到场地显示。</summary>
        public CardUI AddCard(CardBase card)
        {
            if (_cardUIPrefab == null || _cardContainer == null) return null;
            var go = Instantiate(_cardUIPrefab, _cardContainer);
            var cardUI = go.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.Setup(card);
                cardUI.OnCardHovered += c => OnBoardCardHovered?.Invoke(this, c);
                cardUI.OnCardUnhovered += c => OnBoardCardUnhovered?.Invoke(this, c);
            }
            _cardUIs.Add(cardUI);
            return cardUI;
        }

        /// <summary>移除指定卡牌的 UI。</summary>
        public void RemoveCard(CardBase card)
        {
            for (int i = _cardUIs.Count - 1; i >= 0; i--)
            {
                if (_cardUIs[i].Card == card)
                {
                    Destroy(_cardUIs[i].gameObject);
                    _cardUIs.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>清空场地。</summary>
        public void Clear()
        {
            foreach (var cardUI in _cardUIs)
                if (cardUI != null) Destroy(cardUI.gameObject);
            _cardUIs.Clear();
        }

        public IReadOnlyList<CardUI> GetCardUIs() => _cardUIs.AsReadOnly();

        public void ShowStatusIcon(bool show, Sprite icon = null)
        {
            if (_statusIcon != null)
            {
                _statusIcon.gameObject.SetActive(show);
                if (show && icon != null)
                    _statusIcon.sprite = icon;
            }
        }

        public void SetHighlight(bool highlight)
        {
            if (_panelBackground != null)
            {
                var color = _panelBackground.color;
                color.a = highlight ? 0.8f : 0.4f;
                _panelBackground.color = color;
            }
        }

        /// <summary>设置选择模式高亮（用于 Boat/Snake/Tree 选目标）。</summary>
        public void SetSelectionMode(bool active)
        {
            if (_panelBackground != null)
            {
                var color = _panelBackground.color;
                color.a = active ? 1f : 0.4f;
                _panelBackground.color = color;
            }
        }

        /// <summary>由 CardUI.OnPointerClick 转发调用（点击场地上的卡牌）。</summary>
        public void OnBoardCardClick(CardUI cardUI)
        {
            OnBoardCardClicked?.Invoke(this, cardUI);
        }

        /// <summary>由 CardUI.OnPointerClick 转发调用（点击卡牌时也触发场地点击）。</summary>
        public void OnBoardAreaClick()
        {
            OnBoardClicked?.Invoke(this);
        }

        // ── IPointerClickHandler ──────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            // 点击的是空白场地（非卡牌）
            // 卡牌点击由 CardUI.OnPointerClick → OnBoardCardClick 处理
            OnBoardClicked?.Invoke(this);
        }
    }
}
