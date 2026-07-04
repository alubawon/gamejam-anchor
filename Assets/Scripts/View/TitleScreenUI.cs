using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 标题画面 + 主菜单 UI。
    /// <para>使用已有资产：startanime 动画背景、playstar.png 开始按钮、playend.png 退出按钮。</para>
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Image _background;
        [SerializeField] private Animator _backgroundAnimator;
        [SerializeField] private TextMeshProUGUI _titleText;

        /// <summary>点击开始游戏。</summary>
        public event Action OnStartClicked;

        /// <summary>点击退出游戏。</summary>
        public event Action OnQuitClicked;

        private void Awake()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(() => OnStartClicked?.Invoke());
            if (_quitButton != null)
                _quitButton.onClick.AddListener(() => OnQuitClicked?.Invoke());
        }

        private void Start()
        {
            if (_backgroundAnimator != null)
                _backgroundAnimator.speed = 0.5f; // 与 controller 中 Speed=0.5 一致
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
