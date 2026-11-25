using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

/// <summary>
/// 剧情的通用静态工具：
///   · Letter             —— 发一封中性信件
///   · GuestDescription   —— 庇护任务的动态描述（依赖当前剧情实例）
///   · EnsureLeaderRegeneration —— 给领袖补再生 hediff
///
/// 这些方法原先以 partial 成员形式散在 Story.cs / Completion.cs 中，
/// 与 Story 的实例状态无关，抽到独立静态类便于复用与阅读。
/// </summary>
public static class StoryUtility
{
    /// <summary>发一封中性信件。</summary>
    public static void Letter(string title, string text) =>
        Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent);

    /// <summary>庇护任务的描述文本（剩余时间 + 奖励摘要）。</summary>
    public static string GuestDescription(RewardPlan plan) =>
        "MinchoCandyWars.Hospitality.Description".Translate(
            Math.Max(0, Story.Current.DepartAt - Find.TickManager.TicksGame).ToStringTicksToPeriod(),
            plan.Summary).RawText;

    /// <summary>确保领袖带有再生 hediff（仅 Anomaly 激活时）。</summary>
    public static void EnsureLeaderRegeneration(Pawn pawn)
    {
        if (!ModsConfig.AnomalyActive)
        {
            return;
        }

        var def = MCW_HediffDefOf.Regeneration;
        if (!pawn.health.hediffSet.HasHediff(def))
        {
            pawn.health.AddHediff(def).Severity = 0.1f;
        }
    }
}
