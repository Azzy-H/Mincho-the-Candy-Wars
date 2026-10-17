using System.Collections.Generic;
using System.Linq;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestParts
{
    /// <summary>
    /// 任务结束时（无论成功/失败）随机挑一个「尚未处理」的 questA 事件，
    /// 写入 MCW_NextQuest flag（String 类型）。
    ///
    /// 通讯台的统一入口 MCW_Event_AllianceGreeting 会读取这个 flag，
    /// 用 Trigger_FlagCheck 只显示被随机到的那个任务选项 ——
    /// 这就是「每次完成任务后，随机决定下次可以接到哪个任务」的实现。
    /// </summary>
    public class QuestPart_SetRandomNextQuest : QuestPart
    {
        public const string NextQuestFlag = "MCW_NextQuest";

        /// <summary>参与随机的事件池。</summary>
        private static readonly string[] Candidates =
        {
            "MCW_Event_QuestA1_AllianceContact",
            "MCW_Event_QuestA2_AllianceContact",
            "MCW_Event_QuestA3_AllianceContact",
            "MCW_Event_QuestA4_AllianceContact",
            "MCW_Event_QuestA5_AllianceContact",
        };

        public override void Cleanup()
        {
            base.Cleanup();
            SetRandomNextQuest();
        }

        /// <summary>随机选择一个尚未处理的任务并写入 flag。</summary>
        public static void SetRandomNextQuest()
        {
            List<string> available = Candidates
                .Where(c => !IsQuestHandled(c))
                .ToList();

            if (available.Count == 0)
            {
                // 全部处理过：清掉 flag，入口事件只会显示问候
                StellarisEventFlagManager.RemoveFlag(NextQuestFlag);
                return;
            }

            string picked = available.RandomElement();
            StellarisEventFlagManager.SetFlag(NextQuestFlag, picked);
        }

        /// <summary>该任务是否已经接受过或拒绝过（"待定"不算）。</summary>
        public static bool IsQuestHandled(string eventDefName)
        {
            string prefix = "MCW_Quest" + eventDefName
                .Replace("MCW_Event_Quest", "")
                .Replace("_AllianceContact", "");

            return StellarisEventFlagManager.HasFlag(prefix + "_Accepted")
                || StellarisEventFlagManager.HasFlag(prefix + "_Declined");
        }
    }
}
