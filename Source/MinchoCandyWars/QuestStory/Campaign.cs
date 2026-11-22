#nullable disable
using RimWorld;
using RimWorld.QuestGen;
using MinchoCandyWars.StellarisEvent;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    public int Offer = 1, ActiveKind = 1, Rejected, Viewed, Succeeded;
    public int LastBucketCargo = -1;
    public int FinalAt = -1;
    public int OfferIssuer = -1, ActiveIssuer;
    public int OfferNumber;
    public bool AssignedOffer;
    public List<RewardPlan> PendingRewards = new();
    public string IssuerName => DisplayName(ActiveIssuer);
    void AssignOffer()
    {
        OfferIssuer = AssignedOffer ? OfferPolicy.Issuers(Offer).Where(CanTalk).DefaultIfEmpty(-1).RandomElement() : (CanTalk(0) ? 0 : -1);
        if (OfferIssuer >= 0) AssignedOffer = true;
    }
    public bool Victory, FinalAttempted, Migrated101, KitchenViewed;
    public string OfferEvent => Offer > 6 ? $"MCWQ101_Event{Offer}" : Offer == 6 ? "MCW_Event_QuestF1_AllianceContact" : $"MCW_Event_QuestA{Offer}_AllianceContact";
    public static readonly string[] MissionDefs = [GuestDef, "MCW_QuestB2_SmartMinchoRescue", "MCW_QuestB3_RecycleOutpost", "MCW_QuestB4_BetaFluidRuin", "MCW_QuestB5_AllianceOutpostDefense", "MCW_QuestFF1_RecycleHQ", "MCWQ101_Delivery7", "MCWQ101_Delivery8", "MCWQ101_Remnant", "MCWQ103_Delivery10", "MCWQ103_Delivery11", "MCWQ105_Crash", "MCWQ105_Cargo"];
    public static readonly string[] Speakers = ["甘露糖", "琥珀糖", "琉璃糖", "千机糖", "琥珀糖", "沁心糖", "沁心糖", "玲珑糖", "琉璃糖", "玲珑糖", "千机糖", "琥珀糖", "甘露糖"];
    void CampaignExpose()
    {
        CharactersExpose(); GiftsExpose(); CompletionExpose();
        Scribe_Collections.Look(ref PendingRewards, "pendingAllianceRewards", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) PendingRewards ??= new();
        Scribe_Values.Look(ref LastBucketCargo, "lastBucketCargo105", -1);
        Scribe_Values.Look(ref OfferIssuer, "offerIssuer103", -1);
        Scribe_Values.Look(ref OfferNumber, "allianceOfferNumber");
        Scribe_Values.Look(ref ActiveIssuer, "activeIssuer103");
        Scribe_Values.Look(ref AssignedOffer, "assignedOffer103");
        Scribe_Values.Look(ref Offer, "offer101", 1); Scribe_Values.Look(ref ActiveKind, "activeKind101", 1);
        Scribe_Values.Look(ref Rejected, "rejected101"); Scribe_Values.Look(ref Viewed, "viewed101"); Scribe_Values.Look(ref Succeeded, "succeeded101");
        Scribe_Values.Look(ref FinalAt, "finalAt101", -1); Scribe_Values.Look(ref Victory, "victory101");
        Scribe_Values.Look(ref FinalAttempted, "finalAttempted101"); Scribe_Values.Look(ref Migrated101, "migrated101");
        Scribe_Values.Look(ref KitchenViewed, "kitchenViewed101");
    }
    void CampaignTick(int now)
    {
        NormalizePostVictoryOffer(now);
        CompletionTick(now);
        if (PendingRewards.Count > 0 && Find.AnyPlayerHomeMap is { } rewardMap)
        {
            PodDelivery.Send(PendingRewards[0].MakeThings(), rewardMap);
            PendingRewards.RemoveAt(0);
        }
        if (!Migrated101)
        {
            Migrated101 = true;
            if (CompletedBatch) { CompletedBatch = false; if (State.NextContactTick < 0) State.Schedule(now, 3 * 60000); }
        }
        // 每次剧情更新仅查找一次；沿用原任务目标与结算顺序。
        var active = State.ActiveQuest < 0 ? null : Find.QuestManager.QuestsListForReading
            .FirstOrDefault(q => q.id == State.ActiveQuest);
        if (State.ActiveQuest >= 0 && (active == null || active.State != QuestState.Ongoing))
        {
            if (active?.State == QuestState.EndedSuccess)
            {
                active.PartsListForReading.OfType<AllianceSettlement>().FirstOrDefault()?.Cleanup();
                active.PartsListForReading.OfType<GuestSettlement>().FirstOrDefault()?.Cleanup();
            }
            if (State.ActiveQuest >= 0)
            {
                State.ActiveQuest = -1;
                FinishMission(ActiveKind, false);
            }
        }
        if (active?.State == QuestState.Ongoing)
        {
            active.PartsListForReading.OfType<GuestSettlement>().FirstOrDefault()?.Prepare(true);
            active.PartsListForReading.OfType<MissionObjective>().FirstOrDefault()?.Check();
            active.PartsListForReading.OfType<DeliveryPart>().FirstOrDefault()?.Check();
            active.PartsListForReading.OfType<BucketReceipt>().FirstOrDefault()?.Check();
        }
        if (FinalAt >= 0 && now >= FinalAt && State.ActiveQuest < 0)
        {
            FinalAt = -1;
            State.NextContactTick = -1;
            Offer = 6;
            AssignOffer();
            ContactReady = true;
            OfferNumber++;
            ContactLetter.Send(OfferEvent);
            return;
        }
        if (!State.Due(now) || State.ActiveQuest >= 0) return;
        State.NextContactTick = -1;
        var pool = (Victory ? CampaignPolicy.PostVictoryCandidates(Rejected) : CampaignPolicy.Candidates(Rejected).Concat(new[]{12,13})).Where(k => k != Rejected && BucketMissions.Available(k)).ToArray();
        Offer = OfferPolicy.Kind(AssignedOffer, pool.RandomElement());
        AssignOffer();
        Rejected = 0;
        if (AvailableContact() < 0) { State.Schedule(now, 3 * 60000); return; }
        ContactReady = true; OfferNumber++; ContactLetter.Send(OfferEvent);
    }
    // Existing saves may still have a company offer or finale timer queued.
    void NormalizePostVictoryOffer(int now)
    {
        if (!Victory) return;
        FinalAt = -1;
        if (State.ActiveQuest >= 0 || !ContactReady || CampaignPolicy.AllowedAfterVictory(Offer)) return;
        ContactReady = false;
        Offer = 1;
        OfferIssuer = -1;
        if (State.NextContactTick < 0) State.Schedule(now, 3 * 60000);
    }
    public void AcceptOffer(int kind)
    {
        if (!CanAccept || Offer != kind) return;
        ActiveIssuer = AvailableContact();
        if (ActiveIssuer < 0) return;
        if (kind == 1) { ActiveKind = 1; KitchenViewed = false; AcceptGuests(); return; }
        var map = Find.AnyPlayerHomeMap; if (map == null) return;
        void ReportGenerationFailure() => Letter("MinchoCandyWars.Campaign.GenerationTitle".Translate().RawText,
            "MinchoCandyWars.Campaign.GenerationBody".Translate().RawText);
        if (kind == 12 && !BucketCrashRoot.CanStart(map))
        {
            ReportGenerationFailure();
            return;
        }

        EnsureSeries();
        var slate = new Slate();
        slate.Set("map", map);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(MissionDefs[kind - 1]), slate);
        var settlement = quest?.PartsListForReading.OfType<AllianceSettlement>().SingleOrDefault();
        if (settlement == null)
        {
            if (quest != null) Find.QuestManager.Remove(quest);
            ReportGenerationFailure();
            return;
        }

        ActiveKind = kind;
        if (!State.Begin(quest.id)) throw new InvalidOperationException("已有同盟任务进行中");
        if (quest.State == QuestState.NotYetAccepted) quest.Accept(null);
        ContactReady = false;
        if (kind == 13) LastBucketCargo = Find.TickManager.TicksGame;
        if (kind == 6) FinalAttempted = true;
        RefreshMissionText(quest, kind, settlement.Plan);
        Result = quest.name + "进行中";
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        RefreshChiefDefaultNames();
        if (State.ActiveQuest < 0 || ActiveIssuer < 0 || ActiveIssuer >= Names.Length) return;
        var quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.id == State.ActiveQuest);
        if (quest?.State != QuestState.Ongoing) return;
        if (quest.PartsListForReading.OfType<GuestSettlement>().FirstOrDefault()?.Plan is { } hospitalityPlan)
        {
            RefreshHospitalityText(quest, hospitalityPlan);
            return;
        }
        var settlement = quest.PartsListForReading.OfType<AllianceSettlement>().FirstOrDefault();
        if (settlement?.Plan != null)
            RefreshMissionText(quest, settlement.Kind, settlement.Plan);
    }

    void RefreshMissionText(Quest quest, int kind, RewardPlan plan)
    {
        if (kind is 7 or 8 or 10 or 11)
        {
            quest.name = $"MinchoCandyWars.DeliveryQuestLog.{kind}.Title".Translate().RawText;
            quest.description = $"MinchoCandyWars.DeliveryQuestLog.{kind}.Description".Translate().RawText
                + "\n\n" + "MinchoCandyWars.DeliveryQuestLog.Instructions".Translate().RawText;
        }
        else if (kind is 12 or 13)
        {
            quest.name = $"MinchoCandyWars.BucketQuestLog.{kind}.Title".Translate().RawText;
            quest.description = $"MinchoCandyWars.BucketQuestLog.{kind}.Description".Translate().RawText;
        }
        else if ($"MinchoCandyWars.OperationQuestLog.{kind}.Title".TryTranslate(out var title) &&
                 $"MinchoCandyWars.OperationQuestLog.{kind}.Description".TryTranslate(out var description))
        {
            quest.name = title.RawText;
            quest.description = description.RawText;
        }
        else return;

        quest.description = "MinchoCandyWars.Alliance.Issuer".Translate(IssuerName).RawText + "\n\n" + quest.description;
        quest.description += "\n\n" + "MinchoCandyWars.Alliance.Reward".Translate(plan.Goodwill, IssuerName).RawText;
    }
    public void RejectOffer(int kind)
    {
        if (!CanAccept || Offer != kind || kind == 6) return;
        ContactReady = false; Rejected = kind;
        if (kind <= 5 || kind is 10 or 11) new Effect_AdjustFactionGoodwill { factionDef = MCW_DefOf.MCW_Alliance_Faction, goodwillDelta = kind == 5 ? -30 : -10 }.Execute();
        Result = $"已婉拒{Names[AvailableContact()]}的本次请求";
        State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(3, 5) * 60000);
        Letter("MinchoCandyWars.Campaign.ReplyTitle".Translate(DisplayName(AvailableContact())).RawText, OfferDialogue.Rejected(kind, AvailableContact()));
    }
    public void FinishMission(int kind, bool success)
    {
        DepartAt = -1; ContactReady = false; CompletedBatch = false;
        if (success) { Succeeded |= 1 << kind; AddAffinity(ActiveIssuer, 5); }
        Result = success ? (PendingRewards.Count > 0 ? "任务成功，有奖励等待家园空投" : "任务成功，奖励已空投") : "任务未成功";
        if (kind != 6 || !success) Letter("MinchoCandyWars.Campaign.ReplyTitle".Translate(IssuerName).RawText, OfferDialogue.Outcome(kind, success, ActiveIssuer));
        if (kind == 6)
        {
            if (success)
            {
                Victory = true;
                State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(3, 5) * 60000);
                HeadquartersVictory.Apply(null);
                StellarisEventFlagManager.SetFlag("MCW_QuestFF1_Done", true);
                Letter("MinchoCandyWars.Campaign.VictoryTitle".Translate().RawText, "MinchoCandyWars.Campaign.VictoryBody".Translate().RawText);
            }
            if (!success)
            {
                FinalAt = CompletionPolicy.RetryAt(Find.TickManager.TicksGame, false);
                State.NextContactTick = -1;
                Letter("MinchoCandyWars.Campaign.RetryTitle".Translate().RawText, "MinchoCandyWars.Campaign.RetryBody".Translate().RawText);
            }
            return;
        }
        float progress = StellarisEventFlagManager.GetFlag("MCW_EndgameProgress")?.floatValue ?? 0;
        if (success && !Victory && progress >= 10 && !FinalAttempted && FinalAt < 0)
        {
            FinalAt = Find.TickManager.TicksGame + 10 * 60000; State.NextContactTick = -1;
            Letter("MinchoCandyWars.Campaign.PrepareTitle".Translate().RawText, "MinchoCandyWars.Campaign.PrepareBody".Translate().RawText);
        }
        else if (FinalAt < 0) State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(3, 5) * 60000);
    }
}

public class OfferTrigger : StellarisEventTrigger
{
    public int kind;
    public override bool CanTrigger() => Current.Game != null && Story.Current.CanAccept && Story.Current.Offer == kind;
}
public class OfferEffect : StellarisEventEffect
{
    public int kind; public bool reject;
    public override void Execute() { if (reject) Story.Current.RejectOffer(kind); else Story.Current.AcceptOffer(kind); }
}

