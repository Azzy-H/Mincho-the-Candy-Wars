using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 发送一封原生信件（右侧信件栏）。
    ///
    /// 这是 Mincho 侧对 StellarisEvent 框架的扩展（框架本身没有发信件的 Effect）。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_SendLetter"&gt;
    ///     &lt;label&gt;回收队员到访&lt;/label&gt;
    ///     &lt;text&gt;三名回收公司成员来到了你的殖民地。&lt;/text&gt;
    ///     &lt;letterDef&gt;ThreatSmall&lt;/letterDef&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_SendLetter : StellarisEventEffect
    {
        /// <summary>信件标题</summary>
        [MustTranslate]
        public string? label;

        /// <summary>信件正文</summary>
        [MustTranslate]
        public string? text;

        /// <summary>信件类型：NeutralEvent（默认）/ PositiveEvent / NegativeEvent / ThreatSmall / ThreatBig</summary>
        public LetterDef? letterDef;

        public override void Execute()
        {
            if (label.NullOrEmpty())
            {
                Log.Warning("[StellarisEvent] Effect_SendLetter: label 为空，无操作");
                return;
            }

            LetterDef def = letterDef ?? LetterDefOf.NeutralEvent;
            Find.LetterStack.ReceiveLetter(label!.Translate(), (text ?? string.Empty).Translate(), def);
        }
    }
}
