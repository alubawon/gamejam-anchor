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
    /// <para>支持卡牌效果子流程：Boat 选目标牌、Cloud 选交换、Snake 选对手、Tree 选目标场地。</para>
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

        [Header("子流程选择按钮")]
        [SerializeField] private GameObject _choicePanel;
        [SerializeField] private Button _choiceYesButton;
        [SerializeField] private Button _choiceNoButton;
        [SerializeField] private TextMeshProUGUI _choicePromptText;

        [Header("气泡与提示")]
        [SerializeField] private DialogueBubbleUI _bubbleUI;

        [Header("当前回合指示")]
        [SerializeField] private TextMeshProUGUI _currentTurnText;

        private GameManager _gameManager;
        private TurnManager _turnManager;
        private InGameScenario _scenario;

        private readonly List<CardUI> _handCardUIs = new();
        private CardUI _selectedCard;

        // ── 出牌子流程状态 ────────────────────────────────────

        /// <summary>当前子流程阶段。</summary>
        private enum SubProcessPhase
        {
            None,               // 无子流程
            SelectBoardTarget,  // 选择目标场地（普通牌+Tree）
            SelectBoatCard,     // Boat：选择对手场地的牌
            SelectSnakeTarget,  // Snake：选择交换对手
            CloudChoice,        // Cloud：选择是否交换
        }

        private SubProcessPhase _phase = SubProcessPhase.None;

        // ── 初始化 ──────────────────────────────────────────

        public void Initialize(GameManager gameManager, InGameScenario scenario)
        {
            _gameManager = gameManager;
            _turnManager = gameManager.TurnManager;
            _scenario = scenario;

            _playerBoardArea.Setup(0, "你");
            _cpu1BoardArea.Setup(1, "瘦子");
            _cpu2BoardArea.Setup(2, "胖子");

            _turnManager.OnCardDrawn += OnCardDrawn;
            _turnManager.OnCardPlayed += OnCardPlayed;
            _turnManager.OnTurnStart += OnTurnStart;
            _turnManager.OnGameWon += OnGameWon;

            if (_scenario != null)
                _bubbleUI.OnInterruptContinue += _scenario.ContinueInterrupt;

            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
                _playButton.gameObject.SetActive(false);
            }

            // 子流程选择按钮
            if (_choiceYesButton != null)
                _choiceYesButton.onClick.AddListener(() => OnCloudChoice(true));
            if (_choiceNoButton != null)
                _choiceNoButton.onClick.AddListener(() => OnCloudChoice(false));
            if (_choicePanel != null)
                _choicePanel.SetActive(false);

            // 绑定出牌区 hover + 点击
            BindBoardAreaEvents(_playerBoardArea);
            BindBoardAreaEvents(_cpu1BoardArea);
            BindBoardAreaEvents(_cpu2BoardArea);

            RefreshAll();
        }

        private void BindBoardAreaEvents(BoardAreaUI area)
        {
            if (area == null) return;
            area.OnBoardCardHovered += OnBoardCardHovered;
            area.OnBoardCardUnhovered += OnBoardCardUnhovered;
        }

        // ── 事件处理 ──────────────────────────────────────────

        private void OnTurnStart(int playerId)
        {
            if (_currentTurnText != null)
            {
                _currentTurnText.text = playerId switch { 0 => "你的回合", 1 => "瘦子的回合", 2 => "胖子的回合", _ => "回合" };
            }
            _playerBoardArea.SetHighlight(playerId == 0);
            _cpu1BoardArea.SetHighlight(playerId == 1);
            _cpu2BoardArea.SetHighlight(playerId == 2);
            RefreshAll();
        }

        private void OnCardDrawn(int playerId, CardBase card)
        {
            if (playerId == 0) RefreshHand();
            RefreshDeckCount();
        }

        private void OnCardPlayed(int playerId, CardBase card, int targetBoardId)
        {
            if (playerId == 0) RefreshHand();
            var boardArea = GetBoardArea(targetBoardId);
            boardArea?.AddCard(card);
            RefreshDeckCount();
        }

        private void OnGameWon(int winnerId)
        {
            if (_currentTurnText != null)
                _currentTurnText.text = winnerId switch { 0 => "你赢了！", 1 => "瘦子赢了", 2 => "胖子赢了", _ => "游戏结束" };
            if (_playButton != null) _playButton.gameObject.SetActive(false);
        }

        // ── 手牌操作 ──────────────────────────────────────────

        private void RefreshHand()
        {
            foreach (var cardUI in _handCardUIs)
                if (cardUI != null) Destroy(cardUI.gameObject);
            _handCardUIs.Clear();
            _selectedCard = null;
            _phase = SubProcessPhase.None;

            if (_gameManager == null) return;
            var player = _gameManager.GetPlayer(0);
            if (player == null) return;

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
            _phase = SubProcessPhase.None;

            // 检查自己场地是否已有同名牌
            bool selfHasCard = _gameManager.Board.HasCardWithId(0, cardUI.Card.Id);

            // 棺材只能打自己场地
            if (cardUI.Card.Id == 8)
            {
                _phase = SubProcessPhase.None; // 直接打到自己场地
                UpdatePlayButton();
                return;
            }

            if (selfHasCard)
            {
                // 自己场地已有 → 需要选择目标场地
                _phase = SubProcessPhase.SelectBoardTarget;
                ShowTopPrompt("选择要将牌打入的出牌区");
                HighlightValidTargets(cardUI.Card.Id);
            }
            else
            {
                // 可以打自己场地
                _phase = SubProcessPhase.None;
            }

            UpdatePlayButton();
        }

        private void OnCardHovered(CardUI cardUI)
        {
            if (cardUI?.Card == null) return;
            ShowTopPrompt($"{cardUI.Card.Name}：{cardUI.Card.Description}");
        }

        private void OnCardUnhovered(CardUI cardUI) => HideTopPrompt();

        private void OnBoardCardHovered(BoardAreaUI area, CardUI cardUI)
        {
            if (cardUI?.Card == null) return;
            ShowTopPrompt($"{cardUI.Card.Name}：{cardUI.Card.Description}");
        }

        private void OnBoardCardUnhovered(BoardAreaUI area, CardUI cardUI) => HideTopPrompt();

        // ── 出牌 ──────────────────────────────────────────────

        private void OnPlayButtonClicked()
        {
            if (_selectedCard == null || _gameManager == null) return;

            int cardId = _selectedCard.Card.Id;

            // 根据卡牌类型进入子流程
            switch (cardId)
            {
                case 3: // Boat — 选择对手场地的一张牌
                    _phase = SubProcessPhase.SelectBoatCard;
                    ShowTopPrompt("点击选择对手出牌区的一张牌，将其移回牌堆底");
                    EnableBoardCardSelection(true);
                    UpdatePlayButton();
                    break;

                case 6: // Cloud — 选择是否交换
                    _phase = SubProcessPhase.CloudChoice;
                    ShowTopPrompt("选择是否交换牌堆顶和牌堆底的牌");
                    ShowChoicePanel("是否交换牌堆顶和牌堆底？");
                    UpdatePlayButton();
                    break;

                case 7: // Snake — 选择交换对手
                    _phase = SubProcessPhase.SelectSnakeTarget;
                    ShowTopPrompt("选择交换手牌的对手");
                    EnableBoardSelection(true);
                    UpdatePlayButton();
                    break;

                case 5: // Tree — 选择放置翻开牌的目标场地
                    _phase = SubProcessPhase.SelectBoardTarget;
                    ShowTopPrompt("选择将翻开的牌放入哪个出牌区");
                    HighlightValidTargets(cardId);
                    UpdatePlayButton();
                    break;

                default:
                    // 普通出牌
                    TrySubmitPlay(0, null);
                    break;
            }
        }

        // ── 子流程：选择场地 ──────────────────────────────────

        /// <summary>Update 中检测点击场地。</summary>
        private void Update()
        {
            if (_scenario == null || _bubbleUI == null) return;

            // 气泡
            if (_scenario.IsBubbleActive)
            {
                var bubble = _scenario.CurrentBubble;
                _bubbleUI.ShowBubble(bubble.speaker, bubble.text, bubble.duration);
                float bubbleX = bubble.speaker switch { "瘦子" => 350f, "胖子" => -350f, _ => 0f };
                _bubbleUI.SetBubblePosition(bubbleX);
            }
            else
            {
                _bubbleUI.HideBubble();
            }

            // 上方提示
            if (!_scenario.CurrentTopPrompt.Equals(default(DialogueLine)))
                _bubbleUI.ShowTopPrompt(_scenario.CurrentTopPrompt.text);

            // 打断模式
            if (_scenario.IsInterruptActive)
                _bubbleUI.ShowInterrupt(_scenario.CurrentTopPrompt.text);
            else
                _bubbleUI.HideInterrupt();
        }

        // ── 子流程交互 ────────────────────────────────────────

        /// <summary>处理场地点击（由 BoardAreaUI 的按钮或自定义输入触发）。</summary>
        public void OnBoardAreaClicked(BoardAreaUI area)
        {
            if (_phase == SubProcessPhase.SelectBoardTarget)
            {
                // 普通牌/Tree 选目标场地
                if (area == null) return;

                // 检查目标场地是否合法
                int cardId = _selectedCard.Card.Id;
                if (_gameManager.Board.HasCardWithId(area.PlayerId, cardId))
                {
                    ShowTopPrompt("该出牌区已存在同名牌！");
                    return;
                }

                if (_selectedCard.Card.Id == 5)
                {
                    // Tree — 效果目标设为 TargetBoardId
                    TrySubmitPlay(area.PlayerId, new PlayTarget { TargetBoardId = area.PlayerId });
                }
                else
                {
                    // 普通牌 — 打到选中场地
                    TrySubmitPlay(area.PlayerId, null);
                }

                ClearSelectionMode();
            }
            else if (_phase == SubProcessPhase.SelectSnakeTarget)
            {
                // Snake 选交换对手
                if (area == null || area.PlayerId == 0) return;
                var targetPlayer = _gameManager.GetPlayer(area.PlayerId);
                if (targetPlayer != null && targetPlayer.IsImmune)
                {
                    ShowTopPrompt($"{targetPlayer.Name} 免疫了影响！");
                    return;
                }
                TrySubmitPlay(0, new PlayTarget { TargetPlayerId = area.PlayerId });
                ClearSelectionMode();
            }
        }

        /// <summary>处理场地上的卡牌点击（Boat 选目标牌）。</summary>
        public void OnBoardCardClicked(BoardAreaUI area, CardUI cardUI)
        {
            if (_phase != SubProcessPhase.SelectBoatCard) return;
            if (area == null || area.PlayerId == 0) return; // 不能选自己场地

            var targetPlayer = _gameManager.GetPlayer(area.PlayerId);
            if (targetPlayer != null && targetPlayer.IsImmune)
            {
                ShowTopPrompt($"{targetPlayer.Name} 免疫了影响！");
                return;
            }

            // 获取卡牌在场地中的索引
            var board = _gameManager.Board.GetPlayerBoard(area.PlayerId);
            int cardIndex = -1;
            for (int i = 0; i < board.Count; i++)
            {
                if (board[i] == cardUI.Card)
                {
                    cardIndex = i;
                    break;
                }
            }

            if (cardIndex >= 0)
            {
                // Boat 放置目标场地：优先自己
                int boatTarget = 0;
                if (_gameManager.Board.HasCardWithId(0, 3))
                {
                    // 自己已有船 → 选其他合法场地
                    var valid = _turnManager.GetValidTargets(_gameManager, 3);
                    if (valid.Count > 0) boatTarget = valid[0];
                }

                TrySubmitPlay(boatTarget, new PlayTarget
                {
                    TargetPlayerId = area.PlayerId,
                    TargetCardIndex = cardIndex,
                });
                ClearSelectionMode();
            }
        }

        // ── Cloud 选择 ────────────────────────────────────────

        private void OnCloudChoice(bool choice)
        {
            if (_phase != SubProcessPhase.CloudChoice) return;
            HideChoicePanel();
            TrySubmitPlay(0, new PlayTarget { Choice = choice });
        }

        // ── 提交出牌 ─────────────────────────────────────────

        private void TrySubmitPlay(int targetBoardId, PlayTarget effectTarget)
        {
            if (_selectedCard == null) return;

            // 棺材强制打自己
            if (_selectedCard.Card.Id == 8)
                targetBoardId = 0;

            var action = new PlayAction(_selectedCard.Card, targetBoardId, effectTarget);
            _gameManager.SubmitHumanPlay(action);
            _selectedCard = null;
            _phase = SubProcessPhase.None;
            HideTopPrompt();
            UpdatePlayButton();
        }

        // ── UI 辅助 ───────────────────────────────────────────

        private void UpdatePlayButton()
        {
            if (_playButton == null) return;

            bool canPlay = _gameManager != null && _gameManager.IsHumanTurn && _selectedCard != null
                && _phase == SubProcessPhase.None;

            _playButton.gameObject.SetActive(canPlay);

            if (canPlay && _playButtonText != null)
                _playButtonText.text = $"打出 {_selectedCard.Card.Name}";
        }

        private void HighlightValidTargets(int cardId)
        {
            _playerBoardArea.SetSelectionMode(!_gameManager.Board.HasCardWithId(0, cardId));
            _cpu1BoardArea.SetSelectionMode(!_gameManager.Board.HasCardWithId(1, cardId));
            _cpu2BoardArea.SetSelectionMode(!_gameManager.Board.HasCardWithId(2, cardId));
        }

        private void EnableBoardSelection(bool enable)
        {
            _cpu1BoardArea.SetSelectionMode(enable);
            _cpu2BoardArea.SetSelectionMode(enable);
        }

        private void EnableBoardCardSelection(bool enable)
        {
            _cpu1BoardArea.SetSelectionMode(enable);
            _cpu2BoardArea.SetSelectionMode(enable);
        }

        private void ClearSelectionMode()
        {
            _playerBoardArea.SetSelectionMode(false);
            _cpu1BoardArea.SetSelectionMode(false);
            _cpu2BoardArea.SetSelectionMode(false);
            HideTopPrompt();
        }

        private void ShowTopPrompt(string text)
        {
            if (_bubbleUI != null) _bubbleUI.ShowTopPrompt(text);
        }

        private void HideTopPrompt()
        {
            if (_bubbleUI != null) _bubbleUI.HideTopPrompt();
        }

        private void ShowChoicePanel(string prompt)
        {
            if (_choicePanel != null) _choicePanel.SetActive(true);
            if (_choicePromptText != null) _choicePromptText.text = prompt;
        }

        private void HideChoicePanel()
        {
            if (_choicePanel != null) _choicePanel.SetActive(false);
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
