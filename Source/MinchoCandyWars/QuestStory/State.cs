#nullable disable
namespace MinchoCandyWars.QuestStory;

// Kept free of game APIs so settlement and timer boundaries can be tested directly.
public sealed class StoryState
{
    public const int GuestDays = 15;
    public static int BonusPackCount(bool first, int rolled) => first ? 1 : Math.Clamp(rolled, 1, 3);
    public int ActiveQuest = -1;
    public int LastSettled = -1;
    public int NextContactTick = -1;
    public bool Begin(int questId)
    {
        if (questId < 0 || ActiveQuest >= 0) return false;
        ActiveQuest = questId;
        NextContactTick = -1;
        return true;
    }
    public bool Settle(int questId)
    {
        if (questId != ActiveQuest || questId == LastSettled) return false;
        LastSettled = questId;
        ActiveQuest = -1;
        return true;
    }
    public void Schedule(int now, int delay) => NextContactTick = now + delay;
    public bool Due(int now) => NextContactTick >= 0 && now >= NextContactTick;
    public static int ProgressReward(bool success) => success ? 1 : 0;
    public static int GoodwillReward(bool success) => success ? 40 : 0;
}

public static class CampaignPolicy
{
    public static int[] Candidates(int rejected) => new[] { 1, 2, 3, 4, 5, 10, 11 }.Where(i => i != rejected).ToArray();
    public static int Progress(bool success, bool victory) => success && !victory ? 1 : 0;
    public static bool CanInvite(bool victory, int goodwill) => victory && goodwill >= 100;
    public static int GuardScale(float wealth) => Math.Clamp((int)(wealth / 12000f) + 1, 2, 6);
    public static bool ThreatDefeated(bool entered, int standingEnemies) => entered && standingEnemies == 0;
    public static bool AllowedAfterVictory(int kind) => kind is 1 or 4 or 7 or 8 or 10 or 11 or 13;
    public static int PostVictoryChoice(int roll) => roll < 25 ? 1 : roll < 45 ? 4 : roll < 60 ? 7 : roll < 75 ? 8 : roll < 85 ? 11 : 10;
    public static int[] PostVictoryCandidates(int rejected) => Enumerable.Range(0, 100).Select(PostVictoryChoice).Concat(Enumerable.Repeat(13, 10)).Where(i => i != rejected).ToArray();
}

public static class OfferPolicy
{
    public static int Kind(bool assigned, int rolled) => assigned ? rolled : 1;
    public static int[] Issuers(int kind) => kind switch
    {
        1 => [0, 2], 2 or 5 => [0, 1], 3 or 9 => [0, 4],
        12 => [0, 1, 2], 13 => [0, 2], 4 or 11 => [0, 3], 8 or 10 => [0, 5], _ => [0]
    };
}

public static class CompletionPolicy
{
    public const int CompanyDecisionTicks = 60000;
    public const int FinaleRetryTicks = 600000;
    public static bool CompanyDue(int now, int deadline, bool decided) => !decided && deadline >= 0 && now >= deadline;
    public static int RetryAt(int now, bool success) => success ? -1 : now + FinaleRetryTicks;
}

public static class HeadgearPolicy
{
    public static bool Merge(string candy, string headgear) =>
        candy == "LinglonCandy" && headgear == "MCW_LinglonTiara" ||
        candy == "AmberonCandy" && headgear == "Mincho_KnightlyTiara";
}
