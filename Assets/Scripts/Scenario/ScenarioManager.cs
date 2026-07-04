using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// 剧情演出管理器（基类）— 管理对局前初始剧情和对局后结局剧情。
    /// <para>InGameScenario 继承此类，扩展局中气泡演出能力。</para>
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        /// <summary>剧情播放完成回调。</summary>
        public event Action OnScenarioComplete;

        /// <summary>当前是否正在播放剧情。</summary>
        public bool IsPlaying { get; protected set; }

        /// <summary>
        /// 播放对局前初始剧情。
        /// </summary>
        /// <param name="onComplete">剧情播放完毕回调。</param>
        public virtual void PlayIntroStory(Action onComplete = null)
        {
            // 基类默认空实现，直接完成
            IsPlaying = true;
            Complete(onComplete);
        }

        /// <summary>
        /// 播放对局后结局剧情（根据胜负结果选择不同分支）。
        /// </summary>
        /// <param name="result">对局结果。</param>
        /// <param name="onComplete">剧情播放完毕回调。</param>
        public virtual void PlayEndingStory(MatchResult result, Action onComplete = null)
        {
            IsPlaying = true;
            Complete(onComplete);
        }

        /// <summary>播放一组对话序列（子类可重写实现具体展示方式）。</summary>
        protected virtual IEnumerator<DialogueLine> PlayDialogueLines(IReadOnlyList<DialogueLine> lines)
        {
            foreach (var line in lines)
                yield return line;
        }

        /// <summary>完成剧情播放，触发回调。</summary>
        protected void Complete(Action onComplete)
        {
            IsPlaying = false;
            onComplete?.Invoke();
            OnScenarioComplete?.Invoke();
        }
    }
}
