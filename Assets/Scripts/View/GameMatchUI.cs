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

        [Header("牌堆顶底预览（Cloud 用）")]
        [SerializeField] private GameObject _deckPreviewPanel;
        [SerializeField] private Image _deckTopImage;
        [SerializeField] private Image _deckBottomImage;
        [SerializeField] private TextMeshProUGUI _deckTopLabel;
        [SerializeField] private TextMeshProUGUI _deckBottomLabel;

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
            SelectBoardTarget,  // 选择目标场地（普通牌）
            SelectBoatCard,     // Boat 旧阶段（废弃）
            BoatSelectBoard,    // Boat 步骤1：选择船放入的场地
            BoatSelectTarget,   // Boat 步骤2：选择要拆的对手场地
            SelectSnakeTarget,  // Snake：选择交换对手
            CloudPreview,       // Cloud：查看牌堆顶底
            CloudChoice,        // Cloud：选择是否交换
            TreeSelectBoard,    // Tree 步骤1：选择树牌放入的场地
            TreePreview,        // Tree 步骤2：展示翻开的牌
            TreeSelectTarget,   // Tree 步骤3：选择翻开的牌放入的场地
        }

        private SubProcessPhase _phase = SubProcessPhase.None;
        private int _treeBoardId = -1; // Tree 步骤1 选中的树牌放置场地
        private int _boatBoardId = -1; // Boat 步骤1 选中的船牌放置场地
        private int _boatTargetPlayerId = -1; // Boat 步骤2 选中的对手
        private int _pendingBoardId = -1; // 当自己场地有同名牌时，先选目标场地存这里

        private bool _initialized = false;

        // ── 初始化 ──────────────────────────────────────────

        public void Initialize(GameManager gameManager, InGameScenario scenario)
        {
            // 如果 TurnManager 变了（新一局），需要重新绑定
            var newTm = gameManager.TurnManager;
            if (_initialized && _turnManager == newTm)
            {
                // 同一局，只需刷新
                RefreshAll();
                return;
            }

            // 如果有旧的 TurnManager，先取消订阅
            if (_turnManager != null)
            {
                _turnManager.OnCardDrawn -= OnCardDrawn;
                _turnManager.OnCardPlayed -= OnCardPlayed;
                _turnManager.OnPlayCompleted -= OnPlayCompleted;
                _turnManager.OnTurnStart -= OnTurnStart;
                _turnManager.OnGameWon -= OnGameWon;
            }

            _gameManager = gameManager;
            _turnManager = newTm;
            _scenario = scenario;

            _playerBoardArea.Setup(0, "你");
            _cpu1BoardArea.Setup(1, "瘦子");
            _cpu2BoardArea.Setup(2, "胖子");

            _turnManager.OnCardDrawn += OnCardDrawn;
            _turnManager.OnCardPlayed += OnCardPlayed;
            _turnManager.OnPlayCompleted += OnPlayCompleted;
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

            _initialized = true;
            RefreshAll();
        }

        private void BindBoardAreaEvents(BoardAreaUI area)
        {
            if (area == null) return;
            area.OnBoardClicked += OnBoardAreaClicked;
            area.OnBoardCardClicked += OnBoardCardClicked;
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
            // 不在此处刷新手牌 — OnPlay 可能改变手牌（如 Clover 抽牌换牌）
            // 手牌刷新延迟到 OnPlayCompleted / OnEffectResolved 之后
            RefreshBoards();
            RefreshDeckCount();
        }

        private void OnPlayCompleted(int playerId, CardBase card, int targetBoardId, PlayTarget effectTarget)
        {
            // 卡牌效果可能改变手牌（如 Clover 抽牌换牌）或移除场上牌（如 Boat）
            if (playerId == 0) RefreshHand();
            RefreshBoards();
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
            _treeBoardId = -1;
            _boatBoardId = -1;
            _boatTargetPlayerId = -1;
            _pendingBoardId = -1;

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
            // 取消之前选中的牌
            if (_selectedCard != null && _selectedCard != cardUI)
                _selectedCard.SetSelected(false);

            _selectedCard = cardUI;
            _phase = SubProcessPhase.None;
            _pendingBoardId = -1;
            _boatBoardId = -1;
            _boatTargetPlayerId = -1;
            _treeBoardId = -1;

            // 棺材只能打自己场地，不需要选目标
            if (cardUI.Card.Id == 8)
            {
                _phase = SubProcessPhase.None;
                UpdatePlayButton();
                return;
            }

            // 检查自己场地是否已有同名牌
            bool selfHasCard = _gameManager.Board.HasCardWithId(0, cardUI.Card.Id);

            // 需要子流程的牌（Boat/Cloud/Snake/Tree）始终显示出牌按钮，
            // 即使自己场地已有同名牌，子流程中 TrySubmitPlay 会找合法场地
            bool needsSubProcess = cardUI.Card.Id == 3 || cardUI.Card.Id == 5
                || cardUI.Card.Id == 6 || cardUI.Card.Id == 7;

            if (selfHasCard && !needsSubProcess)
            {
                // 普通牌自己场地已有 → 直接进入选目标场地模式
                _phase = SubProcessPhase.SelectBoardTarget;
                ShowTopPrompt($"你的场地已有{cardUI.Card.Name}，选择将牌打入的出牌区");
                HighlightValidTargets(cardUI.Card.Id);
            }
            else
            {
                // 可以打自己场地 或 需要子流程 → 显示出牌按钮
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

            // 检查自己场地是否已有同名牌
            bool selfHasCard = _gameManager.Board.HasCardWithId(0, cardId);

            // 如果自己场地有同名牌，需要先选目标场地
            if (selfHasCard)
            {
                _phase = SubProcessPhase.SelectBoardTarget;
                ShowTopPrompt($"你的场地已有{_selectedCard.Card.Name}，选择将牌打入的出牌区");
                HighlightValidTargets(cardId);
                UpdatePlayButton();
                return;
            }

            // 自己场地没有同名牌 → 默认打入自己场地
            _pendingBoardId = 0;

            // 根据卡牌类型进入子流程
            switch (cardId)
            {
                case 3: // Boat — 进入选对手拆牌
                    _boatBoardId = 0;
                    _phase = SubProcessPhase.BoatSelectTarget;
                    ShowTopPrompt("选择要拆除其场地牌的对手");
                    EnableBoardSelection(true);
                    UpdatePlayButton();
                    break;

                case 6: // Cloud — 展示牌堆顶底 + 选择是否交换
                    _phase = SubProcessPhase.CloudPreview;
                    ShowDeckPreview();
                    ShowChoicePanel("是否交换牌堆顶和牌堆底？");
                    UpdatePlayButton();
                    break;

                case 7: // Snake — 选择交换对手
                    _phase = SubProcessPhase.SelectSnakeTarget;
                    ShowTopPrompt("选择交换手牌的对手");
                    EnableBoardSelection(true);
                    UpdatePlayButton();
                    break;

                case 5: // Tree — 自己场地没有 → 检查牌堆是否有牌
                    if (_gameManager.Deck.Count <= 0)
                    {
                        // 牌堆为空 → 树作为普通牌打出，不翻牌
                        TrySubmitPlay(_pendingBoardId, null);
                        _pendingBoardId = -1;
                        break;
                    }
                    _treeBoardId = 0;
                    _phase = SubProcessPhase.TreePreview;
                    ShowTreePreview();
                    ShowTopPrompt("选择将翻开的牌放入哪个出牌区");
                    HighlightValidTargetsForDeckTop();
                    UpdatePlayButton();
                    break;

                default:
                    // 普通出牌
                    TrySubmitPlay(_pendingBoardId, null);
                    _pendingBoardId = -1;
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
                // 选中目标场地 — 根据牌类型进入各自子流程或直接打出
                if (area == null) return;
                int cardId = _selectedCard.Card.Id;
                if (_gameManager.Board.HasCardWithId(area.PlayerId, cardId))
                {
                    ShowTopPrompt("该出牌区已存在同名牌！");
                    return;
                }
                _pendingBoardId = area.PlayerId;
                ClearSelectionMode();

                // 根据牌类型进入子流程
                switch (cardId)
                {
                    case 3: // Boat
                        _boatBoardId = _pendingBoardId;
                        _phase = SubProcessPhase.BoatSelectTarget;
                        ShowTopPrompt("选择要拆除其场地牌的对手");
                        EnableBoardSelection(true);
                        break;
                    case 6: // Cloud
                        _phase = SubProcessPhase.CloudPreview;
                        ShowDeckPreview();
                        ShowChoicePanel("是否交换牌堆顶和牌堆底？");
                        break;
                    case 7: // Snake
                        _phase = SubProcessPhase.SelectSnakeTarget;
                        ShowTopPrompt("选择交换手牌的对手");
                        EnableBoardSelection(true);
                        break;
                    case 5: // Tree
                        if (_gameManager.Deck.Count <= 0)
                        {
                            // 牌堆为空 → 树作为普通牌打出
                            TrySubmitPlay(_pendingBoardId, null);
                            _pendingBoardId = -1;
                            break;
                        }
                        _treeBoardId = _pendingBoardId;
                        _phase = SubProcessPhase.TreePreview;
                        ShowTreePreview();
                        ShowTopPrompt("选择将翻开的牌放入哪个出牌区");
                        HighlightValidTargetsForDeckTop();
                        break;
                    default:
                        // 普通牌 — 直接打出
                        TrySubmitPlay(_pendingBoardId, null);
                        _pendingBoardId = -1;
                        break;
                }
            }
            else if (_phase == SubProcessPhase.BoatSelectBoard)
            {
                // Boat 步骤1：选择船放入的场地
                if (area == null) return;
                int cardId = _selectedCard.Card.Id;
                if (_gameManager.Board.HasCardWithId(area.PlayerId, cardId))
                {
                    ShowTopPrompt("该出牌区已存在同名牌！");
                    return;
                }
                _boatBoardId = area.PlayerId;
                ClearSelectionMode();

                // 进入步骤2：选择要拆的对手场地
                _phase = SubProcessPhase.BoatSelectTarget;
                ShowTopPrompt("选择要拆除其场地牌的对手");
                EnableBoardSelection(true);
            }
            else if (_phase == SubProcessPhase.BoatSelectTarget)
            {
                // Boat 步骤2：选择了对手场地 → 检查是否可拆
                if (area == null || area.PlayerId == 0)
                {
                    // 点击自己场地或空 → 直接结束船的结算（不拆牌）
                    FinishBoatNoTarget();
                    return;
                }

                var targetPlayer = _gameManager.GetPlayer(area.PlayerId);
                if (targetPlayer == null) return;

                if (targetPlayer.IsImmune)
                {
                    ShowTopPrompt($"{targetPlayer.Name} 免疫了影响！无牌可拆");
                    FinishBoatNoTarget();
                    return;
                }

                var targetBoard = _gameManager.Board.GetPlayerBoard(area.PlayerId);
                if (targetBoard.Count == 0)
                {
                    ShowTopPrompt($"{targetPlayer.Name} 场地没有牌！无牌可拆");
                    FinishBoatNoTarget();
                    return;
                }

                // 有牌可拆 → 进入步骤3：高亮该场地上的牌供选择
                ShowTopPrompt($"点击 {targetPlayer.Name} 场地中的一张牌将其移回牌堆底");
                ClearSelectionMode();
                // 高亮对手场地的牌
                area.SetSelectionMode(true);
                _phase = SubProcessPhase.SelectBoatCard; // 复用旧阶段名给步骤3
                _boatTargetPlayerId = area.PlayerId;
            }
            else if (_phase == SubProcessPhase.TreeSelectBoard)
            {
                // Tree 步骤1：选择树牌放入的场地
                if (area == null) return;
                int cardId = _selectedCard.Card.Id;
                if (_gameManager.Board.HasCardWithId(area.PlayerId, cardId))
                {
                    ShowTopPrompt("该出牌区已存在同名牌！");
                    return;
                }
                _treeBoardId = area.PlayerId;
                ClearSelectionMode();

                // 进入步骤2：展示翻开的牌
                _phase = SubProcessPhase.TreePreview;
                ShowTreePreview();
                ShowTopPrompt("选择将翻开的牌放入哪个出牌区");
                HighlightValidTargetsForDeckTop();
            }
            else if (_phase == SubProcessPhase.TreeSelectTarget)
            {
                // Tree 步骤3：选择翻开的牌放入的场地
                if (area == null) return;
                var topCard = _gameManager.Deck.PeekTop();
                if (topCard == null) return;
                if (_gameManager.Board.HasCardWithId(area.PlayerId, topCard.Id))
                {
                    ShowTopPrompt("该出牌区已存在同名牌！");
                    return;
                }
                // 翻出来的是树(5) → 自动放入第一个不拥有树的其他玩家场地
                if (topCard.Id == 5 && area.PlayerId == 0)
                {
                    // 找一个没有树的其他玩家场地
                    int autoTarget = -1;
                    foreach (var p in _gameManager.Players)
                    {
                        if (p.Id != 0 && !_gameManager.Board.HasCardWithId(p.Id, 5))
                        {
                            autoTarget = p.Id;
                            break;
                        }
                    }
                    if (autoTarget >= 0)
                    {
                        TrySubmitPlay(_treeBoardId, new PlayTarget { TargetBoardId = autoTarget });
                        _treeBoardId = -1;
                        ClearSelectionMode();
                    }
                    else
                    {
                        ShowTopPrompt("所有玩家场地都有树，无法放入！");
                    }
                    return;
                }
                // 如果自己场地没有这张牌，只能放入自己场地
                if (!_gameManager.Board.HasCardWithId(0, topCard.Id) && area.PlayerId != 0)
                {
                    ShowTopPrompt("自己场地没有此牌，翻开的牌只能放入自己场地！");
                    return;
                }
                TrySubmitPlay(_treeBoardId, new PlayTarget { TargetBoardId = area.PlayerId });
                _treeBoardId = -1;
                ClearSelectionMode();
            }
            else if (_phase == SubProcessPhase.SelectSnakeTarget)
            {
                // Snake 选交换对手
                if (area == null) return;
                
                // 点击自己场地 → 检查是否所有对手都免疫，是则不交换只打出
                if (area.PlayerId == 0)
                {
                    bool allImmune = true;
                    foreach (var p in _gameManager.Players)
                    {
                        if (p.Id != 0 && !p.IsImmune) { allImmune = false; break; }
                    }
                    if (allImmune)
                    {
                        // 所有对手都免疫 → 不交换手牌，蛇正常打出
                        TrySubmitPlay(_pendingBoardId >= 0 ? _pendingBoardId : 0, null);
                        _pendingBoardId = -1;
                        ClearSelectionMode();
                    }
                    return;
                }

                var targetPlayer = _gameManager.GetPlayer(area.PlayerId);
                if (targetPlayer != null && targetPlayer.IsImmune)
                {
                    ShowTopPrompt($"{targetPlayer.Name} 免疫了影响！");
                    return;
                }
                TrySubmitPlay(_pendingBoardId >= 0 ? _pendingBoardId : 0, new PlayTarget { TargetPlayerId = area.PlayerId });
                _pendingBoardId = -1;
                ClearSelectionMode();
            }
        }

        /// <summary>船无牌可拆时结束结算，不设效果目标。</summary>
        private void FinishBoatNoTarget()
        {
            TrySubmitPlay(_boatBoardId, null);
            _boatBoardId = -1;
            _boatTargetPlayerId = -1;
            ClearSelectionMode();
        }

        /// <summary>处理场地上的卡牌点击（Boat 步骤3：选要拆的牌）。</summary>
        public void OnBoardCardClicked(BoardAreaUI area, CardUI cardUI)
        {
            if (_phase != SubProcessPhase.SelectBoatCard) return;
            if (area == null || cardUI == null) return;
            if (area.PlayerId != _boatTargetPlayerId) return;

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
                TrySubmitPlay(_boatBoardId, new PlayTarget
                {
                    TargetPlayerId = area.PlayerId,
                    TargetCardIndex = cardIndex,
                });
                _boatBoardId = -1;
                _boatTargetPlayerId = -1;
                ClearSelectionMode();
            }
        }

        // ── Cloud 选择 ────────────────────────────────────────

        private void OnCloudChoice(bool choice)
        {
            if (_phase != SubProcessPhase.CloudPreview) return;
            HideChoicePanel();
            HideDeckPreview();
            TrySubmitPlay(_pendingBoardId >= 0 ? _pendingBoardId : 0, new PlayTarget { Choice = choice });
            _pendingBoardId = -1;
        }

        // ── 提交出牌 ─────────────────────────────────────────

        private void TrySubmitPlay(int targetBoardId, PlayTarget effectTarget)
        {
            if (_selectedCard == null) return;

            // 棺材强制打自己
            if (_selectedCard.Card.Id == 8)
                targetBoardId = 0;

            // 确保目标场地合法（没有同名牌）
            if (_selectedCard.Card.GoesToBoardAfterPlay &&
                _gameManager.Board.HasCardWithId(targetBoardId, _selectedCard.Card.Id))
            {
                // 自己场地有同名牌 → 找其他合法场地
                var validTargets = _turnManager.GetValidTargets(_gameManager, _selectedCard.Card.Id);
                if (validTargets.Count > 0)
                    targetBoardId = validTargets[0];
                else
                {
                    ShowTopPrompt("没有合法的出牌区！");
                    return;
                }
            }

            var action = new PlayAction(_selectedCard.Card, targetBoardId, effectTarget);
            if (!_gameManager.SubmitHumanPlay(action))
            {
                // 出牌失败 — 显示原因，不清理选中状态让玩家重试
                ShowTopPrompt(_gameManager.LastPlayError);
                return;
            }
            _selectedCard = null;
            _phase = SubProcessPhase.None;
            HideTopPrompt();
            HideDeckPreview();
            ClearSelectionMode();
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

        /// <summary>高亮牌堆顶牌可放入的场地（排除已有同名牌的场地）。</summary>
        private void HighlightValidTargetsForDeckTop()
        {
            var topCard = _gameManager.Deck.PeekTop();
            if (topCard == null) return;
            _playerBoardArea.SetSelectionMode(!_gameManager.Board.HasCardWithId(0, topCard.Id));
            _cpu1BoardArea.SetSelectionMode(!_gameManager.Board.HasCardWithId(1, topCard.Id));
            _cpu2BoardArea.SetSelectionMode(!_gameManager.Board.HasCardWithId(2, topCard.Id));
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

        // ── 牌堆顶底预览 ─────────────────────────────────────

        private void ShowDeckPreview()
        {
            if (_deckPreviewPanel != null) _deckPreviewPanel.SetActive(true);

            var topCard = _gameManager.Deck.PeekTop();
            var bottomCard = _gameManager.Deck.PeekBottom();

            if (_deckTopImage != null)
            {
                var sprite = GetCardSpriteForPreview(topCard);
                if (sprite != null) { _deckTopImage.sprite = sprite; _deckTopImage.color = Color.white; }
                else _deckTopImage.color = new Color(0.1f, 0.2f, 0.4f, 1f);
            }
            if (_deckBottomImage != null)
            {
                var sprite = GetCardSpriteForPreview(bottomCard);
                if (sprite != null) { _deckBottomImage.sprite = sprite; _deckBottomImage.color = Color.white; }
                else _deckBottomImage.color = new Color(0.1f, 0.2f, 0.4f, 1f);
            }
            if (_deckTopLabel != null)
                _deckTopLabel.text = topCard != null ? $"牌堆顶：{topCard.Name}" : "牌堆顶：空";
            if (_deckBottomLabel != null)
                _deckBottomLabel.text = bottomCard != null ? $"牌堆底：{bottomCard.Name}" : "牌堆底：空";
        }

        private void HideDeckPreview()
        {
            if (_deckPreviewPanel != null) _deckPreviewPanel.SetActive(false);
        }

        // ── Tree 牌堆顶预览 ───────────────────────────────────

        private void ShowTreePreview()
        {
            if (_deckPreviewPanel != null) _deckPreviewPanel.SetActive(true);

            var topCard = _gameManager.Deck.PeekTop();

            if (_deckTopImage != null)
            {
                var sprite = GetCardSpriteForPreview(topCard);
                if (sprite != null) { _deckTopImage.sprite = sprite; _deckTopImage.color = Color.white; }
                else _deckTopImage.color = new Color(0.1f, 0.2f, 0.4f, 1f);
            }
            if (_deckBottomImage != null)
            {
                _deckBottomImage.color = new Color(0f, 0f, 0f, 0f); // 隐藏底牌
            }
            if (_deckTopLabel != null)
                _deckTopLabel.text = topCard != null ? $"牌堆顶（即将翻开）：{topCard.Name}" : "牌堆顶：空";
            if (_deckBottomLabel != null)
                _deckBottomLabel.text = "选择放入的出牌区";

            // 展示后立即进入步骤3：选择翻开的牌放入的场地
            _phase = SubProcessPhase.TreeSelectTarget;
            HighlightValidTargetsForDeckTop();
        }

        private Sprite GetCardSpriteForPreview(CardBase card)
        {
            if (card == null) return null;
            int id = card.Id;
            string path = id switch
            {
                2 => "CardFaces/clover",
                3 => "CardFaces/ship",
                4 => "CardFaces/house",
                5 => "CardFaces/tree",
                6 => "CardFaces/cloud",
                7 => "CardFaces/snake",
                8 => "CardFaces/coffin",
                _ => null,
            };
            if (path == null) return null;
            return Resources.Load<Sprite>(path);
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
            _turnManager.OnPlayCompleted -= OnPlayCompleted;
            _turnManager.OnTurnStart -= OnTurnStart;
            _turnManager.OnGameWon -= OnGameWon;
        }
    }
}
