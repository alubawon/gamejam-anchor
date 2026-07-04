using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 玩家场地区域 UI — 显示该玩家已打出的卡牌。
    /// </summary>
    public class BoardAreaUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardUIPrefab;
        [SerializeField] private Image _panelBackground;
        [SerializeField] private Image _statusIcon; // 免疫盾牌/棺材骷髅

        private int _playerId;
        private readonly List<CardUI> _cardUIs = new();

        public int PlayerId => _playerId;

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
                cardUI.Setup(card);
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

        /// <summary>显示/隐藏状态图标（免疫/棺材）。</summary>
        public void ShowStatusIcon(bool show, Sprite icon = null)
        {
            if (_statusIcon != null)
            {
                _statusIcon.gameObject.SetActive(show);
                if (show && icon != null)
                    _statusIcon.sprite = icon;
            }
        }

        /// <summary>高亮显示（当前回合玩家）。</summary>
        public void SetHighlight(bool highlight)
        {
            if (_panelBackground != null)
            {
                var color = _panelBackground.color;
                color.a = highlight ? 0.8f : 0.4f;
                _panelBackground.color = color;
            }
        }
    }
}
