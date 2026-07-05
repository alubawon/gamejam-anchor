using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 单局对局管理器 — 实现 IGameContext，管理一局完整对局流程。
    /// <para>职责：初始化玩家/牌堆/CPU → 驱动回合流程 → 对局结束产出 MatchResult。</para>
    /// <para>由 SystemManager 在 Match 阶段启动。</para>
    /// </summary>
    public class GameManager : MonoBehaviour, IGameContext
    {
        [Header("对局配置")]
        [Tooltip("每种卡牌的份数（×7 = 总牌数）")]
        [SerializeField] private int _copiesPerCard = 3;

        [Tooltip("CPU 思考延迟（秒）")]
        [SerializeField] private float _cpuThinkDelay = 1.5f;

        [Tooltip("被锁定（棺材）时的跳过延迟（秒）")]
        [SerializeField] private float _skipDelay = 0.5f;

        // ── 核心组件 ──────────────────────────────────────────

        private DeckManager _deck = new DeckManager();
        private BoardManager _board = new BoardManager();
        private readonly List<PlayerData> _players = new List<PlayerData>();
        private TurnManager _turnManager;
        private readonly List<CPUBehavior> _cpuBehaviors = new List<CPUBehavior>();

        // ── IGameContext 属性 ─────────────────────────────────

        public DeckManager Deck => _deck;
        public BoardManager Board => _board;
        public IReadOnlyList<PlayerData> Players => _players.AsReadOnly();
        public int CurrentPlayerId { get; set; }
        public TurnManager TurnManager => _turnManager;

        // ── 对局状态 ──────────────────────────────────────────

        public enum MatchState
        {
            Inactive,
            Playing,
            WaitingForHumanInput,
            MatchOver,
        }

        public MatchState State { get; private set; } = MatchState.Inactive;
        public bool IsMatchActive => State == MatchState.Playing || State == MatchState.WaitingForHumanInput;

        private bool _waitingForHumanInput;
        private MatchResult _pendingResult;
        private Action<MatchResult> _onMatchComplete;

        // ── 外部组件引用 ──────────────────────────────────────

        [SerializeField] private InGameScenario _inGameScenario;
        private InGameEventMonitor _eventMonitor;

        // ── 生命周期 ──────────────────────────────────────────

        /// <summary>
        /// 开始一局对局。
        /// </summary>
        /// <param name="onComplete">对局完成回调，返回 MatchResult。</param>
        public void StartMatch(Action<MatchResult> onComplete = null)
        {
            _onMatchComplete = onComplete;

            // 创建玩家：1 人类 + 2 CPU
            _players.Clear();
            _players.Add(new PlayerData(0, "你", isCPU: false));
            _players.Add(new PlayerData(1, "瘦子", isCPU: true));
            _players.Add(new PlayerData(2, "胖子", isCPU: true));

            // 创建 CPU 行为
            _cpuBehaviors.Clear();
            for (int i = 0; i < CPUCharacteristics.DefaultConfigs.Length; i++)
            {
                _cpuBehaviors.Add(new CPUBehavior(CPUCharacteristics.DefaultConfigs[i]));
            }

            // 创建回合管理器
            _turnManager = new TurnManager(new int[] { 0, 1, 2 });
            _turnManager.OnGameWon += OnGameWon;

            // 初始化场地
            _board.ClearAll();
            foreach (var p in _players)
                _board.InitPlayer(p.Id);

            // 初始化事件监视器
            if (_inGameScenario != null)
            {
                _inGameScenario.ClearAll();
                _eventMonitor = new InGameEventMonitor(_turnManager, this, _inGameScenario);
                _eventMonitor.StartMonitoring();
            }

            // 开始游戏
            _turnManager.StartGame(this, _copiesPerCard);
            State = MatchState.Playing;

            StartCoroutine(ProcessTurns());
        }

        /// <summary>停止对局（强制结束）。</summary>
        public void StopMatch()
        {
            State = MatchState.MatchOver;
            _eventMonitor?.StopMonitoring();
            _waitingForHumanInput = false;
        }

        // ── 回合驱动（协程）──────────────────────────────────

        private IEnumerator ProcessTurns()
        {
            // 等待一帧，确保 GameMatchUI.Initialize() 已绑定事件
            yield return null;

            while (State == MatchState.Playing)
            {
                // 阶段 1 & 2：回合开始 + 检查场地
                _turnManager.StartTurn(this);
                _turnManager.CheckBoard(this);

                int playerId = _turnManager.CurrentPlayerId;
                var player = GetPlayer(playerId);

                // 被锁定（棺材）→ 跳过
                if (player == null || !player.CanPlay)
                {
                    yield return new WaitForSeconds(_skipDelay);
                    _turnManager.EndTurn();
                    continue;
                }

                // 阶段 3：摸牌
                var drawnCard = _turnManager.Draw(this);
                yield return null; // 让摸牌事件被 View 层处理

                // 牌堆为空触发保底 → 游戏已结束，退出协程
                if (State != MatchState.Playing)
                    yield break;

                // 阶段 4：出牌
                if (player.IsCPU)
                {
                    yield return new WaitForSeconds(_cpuThinkDelay);

                    int cpuIdx = playerId - 1; // CPU 玩家 ID 从 1 开始
                    if (cpuIdx >= 0 && cpuIdx < _cpuBehaviors.Count)
                    {
                        var action = _cpuBehaviors[cpuIdx].Decide(this, playerId);
                        if (action != null)
                            _turnManager.TryPlay(this, action);
                    }

                    yield return null;
                    _turnManager.EndTurn();
                }
                else
                {
                    // 人类玩家：等待输入
                    State = MatchState.WaitingForHumanInput;
                    _waitingForHumanInput = true;
                    yield return new WaitUntil(() => !_waitingForHumanInput);
                    State = MatchState.Playing;
                    _turnManager.EndTurn();
                }
            }
        }

        // ── 人类玩家输入入口 ──────────────────────────────────

        /// <summary>最近一次出牌失败的错误信息。</summary>
        public string LastPlayError { get; private set; } = "";

        /// <summary>
        /// 提交人类玩家的出牌动作（由 InputController 调用）。
        /// </summary>
        /// <returns>true 表示出牌成功。失败时 LastPlayError 包含原因。</returns>
        public bool SubmitHumanPlay(PlayAction action)
        {
            if (State != MatchState.WaitingForHumanInput)
            {
                LastPlayError = "当前不是你的出牌时机";
                return false;
            }

            int playerId = _turnManager.CurrentPlayerId;
            var player = GetPlayer(playerId);
            if (player == null || player.IsCPU)
            {
                LastPlayError = "非人类玩家";
                return false;
            }

            if (_turnManager.TryPlay(this, action))
            {
                _waitingForHumanInput = false;
                LastPlayError = "";
                return true;
            }
            // 获取验证失败的原因
            _turnManager.ValidatePlay(this, action, out string error);
            LastPlayError = error;
            return false;
        }

        /// <summary>当前是否轮到人类玩家出牌。</summary>
        public bool IsHumanTurn =>
            State == MatchState.WaitingForHumanInput &&
            GetPlayer(_turnManager?.CurrentPlayerId ?? -1)?.IsCPU == false;

        // ── 对局结束 ──────────────────────────────────────────

        private void OnGameWon(int winnerId)
        {
            State = MatchState.MatchOver;
            _eventMonitor?.StopMonitoring();

            var result = new MatchResult
            {
                WinnerId = winnerId,
                HumanPlayerId = 0,
                TotalRounds = _turnManager.Round,
            };
            foreach (var player in _players)
            {
                // 计分：场地上所有牌的牌面点数之和
                var board = this.Board.GetPlayerBoard(player.Id);
                int score = 0;
                foreach (var card in board)
                    score += card.Id;
                result.BoardPoints[player.Id] = score;
            }

            _pendingResult = result;
            _waitingForHumanInput = false; // 释放等待
            _onMatchComplete?.Invoke(result);
        }

        // ── IGameContext 实现 ────────────────────────────────

        public PlayerData GetPlayer(int playerId)
        {
            foreach (var p in _players)
                if (p.Id == playerId) return p;
            return null;
        }

        public List<int> GetOpponentIds(int playerId)
        {
            var opponents = new List<int>();
            foreach (var p in _players)
                if (p.Id != playerId) opponents.Add(p.Id);
            return opponents;
        }

        public void SwapHands(int playerA, int playerB)
        {
            var pa = GetPlayer(playerA);
            var pb = GetPlayer(playerB);
            if (pa == null || pb == null) return;

            var temp = new List<CardBase>(pa.Hand);
            pa.Hand.Clear();
            pa.Hand.AddRange(pb.Hand);
            pb.Hand.Clear();
            pb.Hand.AddRange(temp);
        }
    }
}
