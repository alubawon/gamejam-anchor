using System;
using UnityEngine;

namespace CardGame.UI
{
    /// <summary>
    /// UI 管理器 — 协调五个阶段的 UI 面板切换。
    /// <para>由 SystemManager 驱动，根据 GamePhase 激活/隐藏对应 UI。</para>
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("阶段 UI 引用")]
        [SerializeField] private TitleScreenUI _titleScreen;
        [SerializeField] private ScenarioUI _preGameScenario;
        [SerializeField] private GameMatchUI _gameMatch;
        [SerializeField] private PostGameUI _postGame;
        [SerializeField] private DialogueBubbleUI _bubbleUI;

        [Header("系统引用")]
        [SerializeField] private SystemManager _systemManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private InGameScenario _inGameScenario;

        /// <summary>初始化 — 绑定 SystemManager 阶段事件。</summary>
        public void Initialize()
        {
            if (_systemManager != null)
                _systemManager.OnPhaseChanged += OnPhaseChanged;

            // 标题画面
            if (_titleScreen != null)
                _titleScreen.OnStartClicked += OnStartGame;

            // 对局后
            if (_postGame != null)
            {
                _postGame.OnRestart += OnRestartMatch;
                _postGame.OnReturnMenu += OnReturnToMenu;
            }

            // 初始状态
            ShowOnly(GamePhase.Menu);
        }

        private void Start()
        {
            // 自动初始化（确保在所有组件就绪后执行）
            if (_systemManager != null && _systemManager.CurrentPhase == GamePhase.Menu)
                Initialize();
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            ShowOnly(phase);
        }

        private void ShowOnly(GamePhase phase)
        {
            HideAll();

            switch (phase)
            {
                case GamePhase.Menu:
                    _titleScreen?.Show();
                    break;

                case GamePhase.IntroStory:
                    // 只显示面板，剧情播放由 SystemManager 驱动
                    _preGameScenario?.Show();
                    break;

                case GamePhase.Match:
                    _gameMatch?.gameObject.SetActive(true);
                    if (_gameManager != null && _inGameScenario != null)
                        _gameMatch?.Initialize(_gameManager, _inGameScenario);
                    _bubbleUI?.gameObject.SetActive(true);
                    break;

                case GamePhase.EndingStory:
                    _preGameScenario?.Show();
                    // 结局剧情由 SystemManager 通过 MatchResult 触发
                    break;

                case GamePhase.GameComplete:
                    if (_systemManager != null && _systemManager.LastMatchResult != null)
                        _postGame?.Show(_systemManager.LastMatchResult);
                    break;
            }
        }

        private void HideAll()
        {
            _titleScreen?.Hide();
            _preGameScenario?.Hide();
            if (_gameMatch != null) _gameMatch.gameObject.SetActive(false);
            _postGame?.Hide();
        }

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnStartGame()
        {
            _systemManager?.StartGameFlow();
        }

        private void OnRestartMatch()
        {
            _systemManager?.StartNewMatch();
        }

        private void OnReturnToMenu()
        {
            _systemManager?.ReturnToMenu();
        }
    }
}
