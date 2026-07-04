using System;

namespace CardGame
{
    /// <summary>
    /// 单条对话数据 — 供 ScenarioManager 和 InGameScenario 共用。
    /// </summary>
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [UnityEngine.TextArea] public string text;
        public float duration;

        public DialogueLine(string speaker, string text, float duration = 3f)
        {
            this.speaker = speaker;
            this.text = text;
            this.duration = duration;
        }
    }
}
