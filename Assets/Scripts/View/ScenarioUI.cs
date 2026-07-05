using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// 对局前/后剧情演出 UI。
    /// <para>全屏覆盖，居中显示对话文本，点击全屏任意位置推进。</para>
    /// </summary>
    public class ScenarioUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI 引用")]
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _dialogueText;
        [SerializeField] private Button _continueButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _fullScreenClickArea;
        [SerializeField] private float _fadeDuration = 0.5f;

        /// <summary>用户点击继续。</summary>
        public event Action OnContinue;

        private DialogueLine[] _lines;
        private int _currentIndex;
        private bool _canAdvance;

        private void Awake()
        {
            if (_continueButton != null)
                _continueButton.onClick.AddListener(Advance);
        }

        /// <summary>点击全屏任意位置推进。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_canAdvance)
                Advance();
        }

        /// <summary>播放一组对话行。</summary>
        public void Play(DialogueLine[] lines, Action onComplete = null)
        {
            _lines = lines;
            _currentIndex = 0;
            _canAdvance = false;
            StartCoroutine(PlaySequence(onComplete));
        }

        private IEnumerator PlaySequence(Action onComplete)
        {
            // 淡入
            yield return Fade(0f, 1f);

            _canAdvance = true;

            while (_lines != null && _currentIndex < _lines.Length)
            {
                ShowLine(_lines[_currentIndex]);
                yield return new WaitUntil(() => _currentIndex > _currentDisplayIndex);
            }

            _canAdvance = false;

            // 淡出
            yield return Fade(1f, 0f);
            onComplete?.Invoke();
        }

        private int _currentDisplayIndex = -1;

        private void ShowLine(DialogueLine line)
        {
            _currentDisplayIndex = _currentIndex;
            if (_speakerText != null)
                _speakerText.text = line.speaker;
            if (_dialogueText != null)
                _dialogueText.text = line.text;
        }

        private void Advance()
        {
            _currentIndex++;
            OnContinue?.Invoke();
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_canvasGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
