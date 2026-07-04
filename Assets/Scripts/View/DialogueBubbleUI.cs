using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 局中气泡 UI — 简略气泡+文字，不遮挡操作区域。
    /// </summary>
    public class DialogueBubbleUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] private GameObject _bubblePanel;
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _contentText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.2f;

        [Header("上方提示")]
        [SerializeField] private TextMeshProUGUI _topPromptText;
        [SerializeField] private GameObject _topPromptPanel;

        [Header("打断模式")]
        [SerializeField] private GameObject _interruptPanel;
        [SerializeField] private TextMeshProUGUI _interruptText;
        [SerializeField] private Button _interruptContinueButton;

        /// <summary>打断模式下点击继续。</summary>
        public event Action OnInterruptContinue;

        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (_interruptContinueButton != null)
                _interruptContinueButton.onClick.AddListener(() => OnInterruptContinue?.Invoke());
            HideBubble();
            HideTopPrompt();
            HideInterrupt();
        }

        // ── 气泡 ──────────────────────────────────────────────

        public void ShowBubble(string speaker, string text, float duration)
        {
            if (_bubblePanel != null) _bubblePanel.SetActive(true);
            if (_speakerText != null) _speakerText.text = speaker;
            if (_contentText != null) _contentText.text = text;
            FadeCanvas(_canvasGroup, 0f, 1f, _fadeDuration);
        }

        public void HideBubble()
        {
            if (_bubblePanel != null) _bubblePanel.SetActive(false);
        }

        /// <summary>设置气泡水平位置（跟随发言人：瘦子右、胖子左、系统居中）。</summary>
        public void SetBubblePosition(float x)
        {
            if (_bubblePanel != null)
            {
                var rt = _bubblePanel.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            }
        }

        // ── 上方提示 ──────────────────────────────────────────

        public void ShowTopPrompt(string text)
        {
            if (_topPromptPanel != null) _topPromptPanel.SetActive(true);
            if (_topPromptText != null) _topPromptText.text = text;
        }

        public void HideTopPrompt()
        {
            if (_topPromptPanel != null) _topPromptPanel.SetActive(false);
        }

        // ── 打断模式 ──────────────────────────────────────────

        public void ShowInterrupt(string text)
        {
            if (_interruptPanel != null) _interruptPanel.SetActive(true);
            if (_interruptText != null) _interruptText.text = text;
        }

        public void HideInterrupt()
        {
            if (_interruptPanel != null) _interruptPanel.SetActive(false);
        }

        // ── 淡入淡出 ──────────────────────────────────────────

        private void FadeCanvas(CanvasGroup cg, float from, float to, float duration)
        {
            if (cg == null) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(cg, from, to, duration));
        }

        private System.Collections.IEnumerator FadeRoutine(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }
    }
}
