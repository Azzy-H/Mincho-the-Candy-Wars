#nullable disable
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;
using LudeonTK;

namespace MinchoCandyWars.QuestStory;

public static class StoryDebug
{
    [DebugAction("珉巧剧情测试", "01 查看剧情状态", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Status() => Find.WindowStack.Add(new Dialog_MessageBox(Story.Current.Status()));

    [DebugAction("珉巧剧情测试", "03 受庇护的珉巧立即抵达", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Arrival()
    {
        if (Story.Current.ArrivalAt < 0) { Reject("主线默认开启；已抵达时不会重复生成。"); return; }
        Story.Current.ArrivalAt = Find.TickManager.TicksGame;
    }

    [DebugAction("珉巧剧情测试", "04 提前打开同盟结盟联络", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Alliance()
    {
        if (Story.Current.First is not { Dead: false }) { Reject("受庇护的珉巧尚未抵达或已死亡。"); return; }
        Fire("MCW_Event_QS1_AllianceContact");
    }

    [DebugAction("珉巧剧情测试", "05 提前打开公司联络", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Company()
    {
        if (Story.Current.First == null) { Reject("受庇护的珉巧尚未抵达。"); return; }
        Fire("MCW_Event_QS1_CorpContact");
    }

    [DebugAction("珉巧剧情测试", "06 快进下次联络计时", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Contact()
    {
        if (Story.Current.State.NextContactTick < 0) { Reject("当前没有待触发的联络；请先完成公司分支或B1。"); return; }
        Story.Current.State.NextContactTick = Find.TickManager.TicksGame;
    }

    [DebugAction("珉巧剧情测试", "07 打开A1庇护请求", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Offer() => Fire(Story.A1);

    [DebugAction("珉巧剧情测试", "08 庇护期缩至60tick后离开", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Departure()
    {
        var quest = GuestQuest(); if (quest == null) return;
        var delay = quest.PartsListForReading.OfType<QuestPart_Delay>().FirstOrDefault(x => x.debugLabel == "QuestDelay");
        if (delay == null) { Reject("未找到原版庇护离开计时器。"); return; }
        delay.delayTicks = Math.Max(0, delay.delayTicks - delay.TicksLeft + 60);
        Story.Current.DepartAt = Find.TickManager.TicksGame + 60;
        Messages.Message("60tick后原版客人将开始离开，请等待她们实际走出地图再查看成功结算。", MessageTypeDefOf.TaskCompletion, false);
    }

    [DebugAction("珉巧剧情测试", "09 强制B1失败（测试档）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Fail() => GuestQuest()?.End(QuestEndOutcome.Fail);

    [DebugAction("珉巧剧情测试", "10 无任务时再测A1", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Repeat()
    {
        var s = Story.Current;
        if (s.State.ActiveQuest >= 0) { Reject("先处理当前同盟任务。"); return; }
        s.Offer = 1; s.CompletedBatch = false; s.ContactReady = true; s.State.NextContactTick = -1;
        s.Result = "开发者重开A1，保留首次/重复记录与已有奖励";
        Fire(Story.A1);
    }
    [DebugAction("珉巧剧情测试", "11 选择后续任务联络（测试档）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Later()
    {
        var s = Story.Current;
        if (s.State.ActiveQuest >= 0) { Reject("先处理当前同盟任务。"); return; }
        Find.WindowStack.Add(new FloatMenu(Enumerable.Range(2, 12).Where(BucketMissions.Available).Select(kind => new FloatMenuOption($"{kind}：{Story.Speakers[kind - 1]}的请求", () =>
        { s.Offer = kind; s.OfferIssuer = OfferPolicy.Issuers(kind).Last(); s.AssignedOffer = true; s.ContactReady = true; s.State.NextContactTick = -1; Fire(s.OfferEvent); })).ToList()));
    }
    [DebugAction("珉巧剧情测试", "12 打开首席闲聊", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Chat() => new ConversationEffect { operation = "hub" }.Execute();
    [DebugAction("珉巧剧情测试", "13 查看当前地图任务目标", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Goal()
    {
        var objective = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.id == Story.Current.State.ActiveQuest)?.PartsListForReading.OfType<MissionObjective>().FirstOrDefault();
        if (objective == null) { Reject("当前没有站点任务。"); return; }
        Find.WindowStack.Add(new Dialog_MessageBox(objective.DescriptionPart));
    }

    [DebugAction("珉巧剧情测试", "14 选择当前请求发布人（测试档）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void Issuer()
    {
        var s = Story.Current;
        if (!s.CanAccept) { Reject("先生成一个待决定的请求。"); return; }
        Find.WindowStack.Add(new FloatMenu(OfferPolicy.Issuers(s.Offer).Where(s.CanTalk).Select(role =>
            new FloatMenuOption(Story.Names[role], () => { s.OfferIssuer = role; Fire(s.OfferEvent); })).ToList()));
    }

    static Quest GuestQuest()
    {
        var quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.id == Story.Current.State.ActiveQuest && q.State == QuestState.Ongoing);
        if (quest == null) Reject("当前没有进行中的B1庇护任务。");
        return quest;
    }
    static void Fire(string name)
    {
        if (Find.WindowStack.Windows.OfType<StellarisEventWindow>().Any()) { Reject("先处理或关闭当前联络窗口。"); return; }
        if (!StellarisEventManager.FireEvent(name)) Reject("此节点的条件未满足或已结算，请查看剧情状态。");
    }
    static void Reject(string text) => Messages.Message(text, MessageTypeDefOf.RejectInput, false);
}
