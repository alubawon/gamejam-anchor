using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 对局主界面 — 手牌区、牌堆区、三个玩家场地、上方提示、气泡。
    /// <para>监听 TurnManager 事件刷新 UI，通过 GameManager 获取游戏状态。</para>
    /// </summary>
    public class GameMatchUI : MonoBehaviour
    {
        [Header("玩家手牌区")]
        [SerializeField] private Transform _handContainer;
        [SerializeField] private GameObject _cardUIPrefab;

        [Header("场地区域")]
        [SerializeField] private BoardAreaUI _playerBoardArea;
        [SerializeField] private BoardAreaUI _cpu1BoardArea;
        [SerializeField] private BoardAreaUI _cpu2BoardArea;

        [Header("牌堆区")]
        [SerializeField] private TextMeshProUGUI _deckCountText;
        [SerializeField] private Image _deckImage;

        [Header("出牌控制")]
        [SerializeField] private Button _playButton;
        [SerializeField] private TextMeshProUGUI _playButtonText;

        [Header("气泡与提示")]
        [SerializeField] private DialogueBubbleUI _bubbleUI;

        [Header("当前回合指示")]
        [SerializeField] private TextMeshProUGUI _currentTurnText;

        private GameManager _gameManager;
        private TurnManager _turnManager;
        private InGameScenario _scenario;

        private readonly List<CardUI> _handCardUIs = new();
        private CardUI _selectedCard;
        private int _selectedTargetBoardId = -1;

        // ── 初始化 ──────────────────────────────────────────

        /// <summary>初始化并绑定 GameManager。</summary>
        public void Initialize(GameManager gameManager, InGameScenario scenario)
        {
            _gameManager = gameManager;
            _turnManager = gameManager.TurnManager;
            _scenario = scenario;

            // 绑定场地
            _playerBoardArea.Setup(0, "你");
            _cpu1BoardArea.Setup(1, "瘦子");
            _cpu2BoardArea.Setup(2, "胖子");

            // 绑定事件
            _turnManager.OnCardDrawn += OnCardDrawn;
            _turnManager.OnCardPlayed += OnCardPlayed;
            _turnManager.OnTurnStart += OnTurnStart;
            _turnManager.OnGameWon += OnGameWon;

            // 气泡更新
            if (_scenario != null)
            {
                _bubbleUI.OnInterruptContinue += _scenario.ContinueInterrupt;
            }

            // 出牌按钮
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
                _playButton.gameObject.SetActive(false);
            }

            // 初始刷新
            RefreshAll();
        }

        // ── 事件处理 ──────────────────────────────────────────

        private void OnTurnStart(int playerId)
        {
            if (_currentTurnText != null)
            {
                string name = playerId switch { 0 => "你的回合", 1 => "瘦子的回合", 2 => "胖子的回合", _ => "回合" };
                _currentTurnText.text = name;
            }

            // 高亮当前玩家场地
            _playerBoardArea.SetHighlight(playerId == 0);
            _cpu1BoardArea.SetHighlight(playerId == 1);
            _cpu2BoardArea.SetHighlight(playerId == 2);

            RefreshAll();
        }

        private void OnCardDrawn(int playerId, CardBase card)
        {
            if (playerId == 0)
                RefreshHand();
            RefreshDeckCount();
        }

        private void OnCardPlayed(int playerId, CardBase card, int targetBoardId)
        {
            // 刷新手牌（出牌后手牌减少）
            if (playerId == 0)
                RefreshHand();

            // 在目标场地添加卡牌 UI
            var boardArea = GetBoardArea(targetBoardId);
            boardArea?.AddCard(card);

            RefreshDeckCount();
        }

        private void OnGameWon(int winnerId)
        {
            if (_currentTurnText != null)
            {
                string name = winnerId switch { 0 => "你赢了！", 1 => "瘦子赢了", 2 => "胖子赢了", _ => "游戏结束" };
                _currentTurnText.text = name;
            }
            if (_playButton != null)
                _playButton.gameObject.SetActive(false);
        }

        // ── 手牌操作 ──────────────────────────────────────────

        private void RefreshHand()
        {
            // 清除旧 UI
            foreach (var cardUI in _handCardUIs)
                if (cardUI != null) Destroy(cardUI.gameObject);
            _handCardUIs.Clear();
            _selectedCard = null;

            if (_gameManager == null) return;
            var player = _gameManager.GetPlayer(0);
            if (player == null) return;

            // 创建新手牌 UI
            foreach (var card in player.Hand)
            {
                if (_cardUIPrefab == null || _handContainer == null) break;
                var go = Instantiate(_cardUIPrefab, _handContainer);
                var cardUI = go.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUI.Setup(card);
                    cardUI.OnCardClicked += OnHandCardSelected;
                    cardUI.OnCardHovered += OnCardHovered;
                    cardUI.OnCardUnhovered += OnCardUnhovered;
                }
                _handCardUIs.Add(cardUI);
            }

            UpdatePlayButton();
        }

        private void OnHandCardSelected(CardUI cardUI)
        {
            _selectedCard = cardUI;
            UpdatePlayButton();
        }

        private void OnCardHovered(CardUI cardUI)
        {
            if (cardUI?.Card == null) return;
            // CSV #3: 鼠标指向卡牌 → 上方提示牌面效果
            if (_bubbleUI != null)
                _bubbleUI.ShowTopPrompt($"{cardUI.Card.Name}：{cardUI.Card.Description}");
        }

        private void OnCardUnhovered(CardUI cardUI)
        {
            // 恢复默认提示
            if (_bubbleUI != null)
                _bubbleUI.HideTopPrompt();
        }

        // ── 出牌 ──────────────────────────────────────────────

        private void OnPlayButtonClicked()
        {
            if (_selectedCard == null || _gameManager == null) return;

            // 默认打到自己场地（目标选择 UI 后续扩展）
            int targetBoardId = 0;

            // 棺材只能打自己
            if (_selectedCard.Card.Id == 8)
                targetBoardId = 0;

            // 检查自己场地是否已有同名牌
            if (_gameManager.Board.HasCardWithId(0, _selectedCard.Card.Id))
            {
                // 已有 → 需要选择其他场地，暂默认打到第一个合法场地
                var validTargets = _turnManager.GetValidTargets(_gameManager, _selectedCard.Card.Id);
                if (validTargets.Count > 0)
                    targetBoardId = validTargets[0];
                else
                    return; // 无合法目标
            }

            var action = new PlayAction(_selectedCard.Card, targetBoardId);
            _gameManager.SubmitHumanPlay(action);
            _selectedCard = null;
        }

        private void UpdatePlayButton()
        {
            if (_playButton == null) return;

            bool canPlay = _gameManager != null && _gameManager.IsHumanTurn && _selectedCard != null;
            _playButton.gameObject.SetActive(canPlay);

            if (canPlay && _playButtonText != null)
                _playButtonText.text = $"打出 {_selectedCard.Card.Name}";
        }

        // ── 刷新 ──────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshHand();
            RefreshDeckCount();
            RefreshBoards();
        }

        private void RefreshDeckCount()
        {
            if (_deckCountText != null && _gameManager != null)
                _deckCountText.text = $"剩余 {_gameManager.Deck.Count} 张";
        }

        private void RefreshBoards()
        {
            if (_gameManager == null) return;

            _playerBoardArea.Clear();
            _cpu1BoardArea.Clear();
            _cpu2BoardArea.Clear();

            foreach (var player in _gameManager.Players)
            {
                var board = _gameManager.Board.GetPlayerBoard(player.Id);
                var area = GetBoardArea(player.Id);
                if (area == null) continue;
                foreach (var card in board)
                    area.AddCard(card);
            }
        }

        private BoardAreaUI GetBoardArea(int playerId)
        {
            return playerId switch
            {
                0 => _playerBoardArea,
                1 => _cpu1BoardArea,
                2 => _cpu2BoardArea,
                _ => null,
            };
        }

        // ── 气泡更新（每帧检查 InGameScenario 状态）────────────

        private void Update()
        {
            if (_scenario == null || _bubbleUI == null) return;

            // 气泡
            if (_scenario.IsBubbleActive)
            {
                var bubble = _scenario.CurrentBubble;
                _bubbleUI.ShowBubble(bubble.speaker, bubble.text, bubble.duration);
            }
            else
            {
                _bubbleUI.HideBubble();
            }

            // 上方提示
            if (!_scenario.CurrentTopPrompt.Equals(default(DialogueLine)))
            {
                _bubbleUI.ShowTopPrompt(_scenario.CurrentTopPrompt.text);
            }

            // 打断模式
            if (_scenario.IsInterruptActive)
            {
                _bubbleUI.ShowInterrupt(_scenario.CurrentTopPrompt.text);
            }
            else
            {
                _bubbleUI.HideInterrupt();
            }
        }

        // ── 清理 ──────────────────────────────────────────────

        private void OnDestroy()
        {
            if (_turnManager == null) return;
            _turnManager.OnCardDrawn -= OnCardDrawn;
            _turnManager.OnCardPlayed -= OnCardPlayed;
            _turnManager.OnTurnStart -= OnTurnStart;
            _turnManager.OnGameWon -= OnGameWon;
        }
    }
}
