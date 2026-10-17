using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestNodes
{
    /// <summary>
    /// 在任务里挂上 QuestPart_SetRandomNextQuest：
    /// 任务结束时随机决定下一个可接的任务（写入 MCW_NextQuest flag）。
    ///
    /// XML 用法（放在 QuestScriptDef 的 root 节点列表里即可，位置不重要，
    /// 因为真正生效的是任务结束时的 Cleanup）：
    ///   &lt;li Class="MinchoCandyWars.QuestNode_SetRandomNextQuest" /&gt;
    /// </summary>
    public class QuestNode_SetRandomNextQuest : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }

        protected override void RunInt()
        {
            QuestGen.quest.AddPart(new QuestParts.QuestPart_SetRandomNextQuest());
        }
    }
}
