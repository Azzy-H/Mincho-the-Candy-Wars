using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 向正在进行的任务发送一个信号 —— 让"事件里的选项"能够唤醒"任务里正在等待的节点"。
    ///
    /// 为什么需要：
    /// 事件窗口是在 WindowStack 上独立处理玩家选择的，它不是任务的一部分，
    /// 所以选项的 action 里没法直接触发任务节点（原版也没有这种补丁操作）。
    /// 本 Effect 把信号按 "Quest{任务id}.{signalName}" 广播给当前 Ongoing 的任务，
    /// 任务侧再用 &lt;li Class="QuestNode_Signal"&gt; 等待这个名字即可。
    /// （ProdrakeRace 里的 QuestPart_BroadcastSignal 是"任务→任务"，这里是"事件→任务"。）
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_SendQuestSignal"&gt;
    ///     &lt;signalName&gt;MCW_QS1_CorpResolved&lt;/signalName&gt;
    ///     &lt;questDefName&gt;MCW_QS1_HuntedMincho&lt;/questDefName&gt;
    ///   &lt;/li&gt;
    /// 任务侧对应写法（inSignal 不要带 Quest 前缀，运行时会自动拼接）：
    ///   &lt;li Class="QuestNode_Signal"&gt;
    ///     &lt;inSignal&gt;MCW_QS1_CorpResolved&lt;/inSignal&gt;
    ///     &lt;node Class="QuestNode_End"&gt;&lt;outcome&gt;Success&lt;/outcome&gt;&lt;/node&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_SendQuestSignal : StellarisEventEffect
    {
        /// <summary>要发送的信号名（不含任务 ID 前缀，运行时自动拼接 "Quest{id}."）</summary>
        [NoTranslate]
        public string? signalName;

        /// <summary>
        /// 可选：只发给 root 为该 QuestScriptDef 的任务。
        /// 留空则发给所有正在进行的任务 —— 任务线多起来之后建议填上，避免误唤醒别的任务。
        /// </summary>
        [NoTranslate]
        public string? questDefName;

        public override void Execute()
        {
            if (signalName.NullOrEmpty())
            {
                Log.Warning("[StellarisEvent] Effect_SendQuestSignal: signalName 为空，无操作");
                return;
            }

            if (Find.QuestManager == null || Find.SignalManager == null)
            {
                return;
            }

            int sent = 0;
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                // 任务的 QuestPart 默认 signalListenMode = OngoingOnly，
                // 所以只发给 Ongoing 的任务才有意义。
                if (quest == null || quest.State != QuestState.Ongoing)
                {
                    continue;
                }

                if (!questDefName.NullOrEmpty()
                    && (quest.root == null || quest.root.defName != questDefName))
                {
                    continue;
                }

                Find.SignalManager.SendSignal(new Signal("Quest" + quest.id + "." + signalName));
                sent++;
            }

            if (sent == 0 && Prefs.DevMode)
            {
                Log.Message($"[StellarisEvent] Effect_SendQuestSignal: 没有等待 '{signalName}' 的进行中任务");
            }
        }
    }
}
