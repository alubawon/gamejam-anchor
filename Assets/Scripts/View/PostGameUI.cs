using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 对局后结算 UI — 显示胜负结果、场上点数、继续按钮。
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _returnMenuButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.5f;

        public event Action OnRestart;
        public event Action OnReturnMenu;

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => OnRestart?.Invoke());
            if (_returnMenuButton != null)
                _returnMenuButton.onClick.AddListener(() => OnReturnMenu?.Invoke());
            Hide();
        }

        /// <summary>显示结算画面。</summary>
        public void Show(MatchResult result)
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());

            if (_resultText != null)
            {
                _resultText.text = result.HumanWon ? "你赢了" : "你输了";
                _resultText.color = result.HumanWon ? Color.green : Color.red;
            }

            if (_scoreText != null)
            {
                string score = "";
                foreach (var kvp in result.BoardPoints)
                {
                    string name = kvp.Key switch { 0 => "你", 1 => "瘦子", 2 => "胖子", _ => $"P{kvp.Key}" };
                    score += $"{name}: {kvp.Value}点  ";
                }
                score += $"\n总回合: {result.TotalRounds}";
                _scoreText.text = score;
            }
        }

        public void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FadeIn()
        {
            if (_canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }
    }
}
