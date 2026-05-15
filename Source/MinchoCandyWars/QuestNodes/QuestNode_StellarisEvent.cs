using RimWorld.QuestGen;
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 监听信号并在收到后触发 StellarisEvent 的 QuestNode。
    ///
    /// XML 用法示例：
    /// <code>
    /// &lt;li Class="MinchoCandyWars.StellarisEvent.QuestNode_StellarisEvent"&gt;
    ///   &lt;inSignal&gt;MySignal&lt;/inSignal&gt;
    ///   &lt;eventDef&gt;MyStellarisEventDef&lt;/eventDef&gt;
    /// &lt;/li&gt;
    /// </code>
    /// </summary>
    public class QuestNode_StellarisEvent : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> inSignal;

        public SlateRef<StellarisEventDef> eventDef;

        protected override bool TestRunInt(Slate slate)
        {
            return eventDef.GetValue(slate) != null;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            Quest quest = QuestGen.quest;

            QuestPart_StellarisEvent part = new QuestPart_StellarisEvent();
            part.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate))
                ?? QuestGen.slate.Get<string>("inSignal");
            part.eventDef = eventDef.GetValue(slate);
            quest.AddPart(part);
        }
    }
}
