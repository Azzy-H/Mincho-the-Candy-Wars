using System;
using System.Linq;
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

/// <summary>
/// 剧情推进逻辑（从 Story.CampaignTick 迁出）。
///
/// 设计：Story 只负责 tick 驱动（遍历 Quest 调 ITickableQuestPart），
/// 具体的阶段推进、提案发放、任务结算等编排集中在这里，
/// 由 Story.GameComponentTick 每 60 tick 调用一次。
///
/// 状态全部通过 Story.Current 的 flag 化属性读写，本身无实例字段。
/// </summary>
public static class StoryCampaign
{
    public static void Tick(int now)
    {
        var s = Story.Current;
        if (s == null) return;

        NormalizePostVictoryOffer(now);
        CompletionTick(now);

        if (s.PendingRewards.Count > 0 && Find.AnyPlayerHomeMap is { } rewardMap)
        {
            PodDelivery.Send(s.PendingRewards[0].MakeThings(), rewardMap);
            s.PendingRewards.RemoveAt(0);
        }

        if (!s.Migrated101)
        {
            s.Migrated101 = true;
            if (s.CompletedBatch)
            {
                s.CompletedBatch = false;
                if (s.State.NextContactTick < 0) s.State.Schedule(now, 3 * 60000);
            }
        }

        // 每次剧情更新仅查找一次；沿用原任务目标与结算顺序。
        var active = s.State.ActiveQuest < 0
            ? null
            : Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.id == s.State.ActiveQuest);
        if (s.State.ActiveQuest >= 0 && (active == null || active.State != QuestState.Ongoing))
        {
            if (active?.State == QuestState.EndedSuccess)
            {
                active.PartsListForReading.OfType<AllianceSettlement>().FirstOrDefault()?.Cleanup();
                active.PartsListForReading.OfType<GuestSettlement>().FirstOrDefault()?.Cleanup();
            }
            if (s.State.ActiveQuest >= 0)
            {
                s.State.ActiveQuest = -1;
                FinishMission(s.ActiveKind, false);
            }
        }

        // QuestPart 的周期驱动已统一交给 Story.GameComponentTick 的 ITickableQuestPart 遍历。
        if (s.FinalAt >= 0 && now >= s.FinalAt && s.State.ActiveQuest < 0)
        {
            s.FinalAt = -1;
            s.State.NextContactTick = -1;
            s.Offer = 6;
            AssignOffer();
            s.ContactReady = true;
            s.OfferNumber++;
            ContactLetter.Send(s.OfferEvent);
            return;
        }

        if (!s.State.Due(now) || s.State.ActiveQuest >= 0) return;
        s.State.NextContactTick = -1;
        var pool = (s.Victory
                ? CampaignPolicy.PostVictoryCandidates(s.Rejected)
                : CampaignPolicy.Candidates(s.Rejected).Concat(new[] { 12, 13 }))
            .Where(k => k != s.Rejected && BucketMissions.Available(k))
            .ToArray();
        s.Offer = OfferPolicy.Kind(s.AssignedOffer, pool.RandomElement());
        AssignOffer();
        s.Rejected = 0;
        if (AvailableContact() < 0)
        {
            s.State.Schedule(now, 3 * 60000);
            return;
        }
        s.ContactReady = true;
        s.OfferNumber++;
        ContactLetter.Send(s.OfferEvent);
    }

    // Existing saves may still have a company offer or finale timer queued.
    public static void NormalizePostVictoryOffer(int now)
    {
        var s = Story.Current;
        if (s == null || !s.Victory) return;
        s.FinalAt = -1;
        if (s.State.ActiveQuest >= 0 || !s.ContactReady || CampaignPolicy.AllowedAfterVictory(s.Offer)) return;
        s.ContactReady = false;
        s.Offer = 1;
        s.OfferIssuer = -1;
        if (s.State.NextContactTick < 0) s.State.Schedule(now, 3 * 60000);
    }

    public static void AssignOffer()
    {
        var s = Story.Current;
        if (s == null) return;
        s.OfferIssuer = s.AssignedOffer
            ? OfferPolicy.Issuers(s.Offer).Where(CanTalk).DefaultIfEmpty(-1).RandomElement()
            : (CanTalk(0) ? 0 : -1);
        if (s.OfferIssuer >= 0) s.AssignedOffer = true;
    }

    public static void AcceptOffer(int kind)
    {
        var s = Story.Current;
        if (s == null || !s.CanAccept || s.Offer != kind) return;
        s.ActiveIssuer = AvailableContact();
        if (s.ActiveIssuer < 0) return;
        if (kind == 1)
        {
            s.ActiveKind = 1;
            s.KitchenViewed = false;
            AcceptGuests();
            return;
        }

        var map = Find.AnyPlayerHomeMap;
        if (map == null) return;

        void ReportGenerationFailure() => StoryUtility.Letter(
            "MinchoCandyWars.Campaign.GenerationTitle".Translate().RawText,
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
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(
            DefDatabase<QuestScriptDef>.GetNamed(CharacterData.MissionDefs[kind - 1]), slate);
        var settlement = quest?.PartsListForReading.OfType<AllianceSettlement>().SingleOrDefault();
        if (settlement == null)
        {
            if (quest != null) Find.QuestManager.Remove(quest);
            ReportGenerationFailure();
            return;
        }

        s.ActiveKind = kind;
        if (!s.State.Begin(quest.id)) throw new InvalidOperationException("已有同盟任务进行中");
        if (quest.State == QuestState.NotYetAccepted) quest.Accept(null);
        s.ContactReady = false;
        if (kind == 13) s.LastBucketCargo = Find.TickManager.TicksGame;
        if (kind == 6) s.FinalAttempted = true;
        s.RefreshMissionText(quest, kind, settlement.Plan);
        s.Result = quest.name + "进行中";
    }

    public static void RejectOffer(int kind)
    {
        var s = Story.Current;
        if (s == null || !s.CanAccept || s.Offer != kind || kind == 6) return;
        s.ContactReady = false;
        s.Rejected = kind;
        if (kind <= 5 || kind is 10 or 11)
        {
            new Effect_AdjustFactionGoodwill
            {
                factionDef = MCW_DefOf.MCW_Alliance_Faction,
                goodwillDelta = kind == 5 ? -30 : -10,
            }.Execute();
        }
        s.Result = $"已婉拒{CharacterData.Names[AvailableContact()]}的本次请求";
        s.State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(3, 5) * 60000);
        StoryUtility.Letter(
            "MinchoCandyWars.Campaign.ReplyTitle".Translate(CharacterData.DisplayName(AvailableContact())).RawText,
            OfferDialogue.Rejected(kind, AvailableContact()));
    }

    // ──────────────────── 公司期限 ────────────────────

    public static void StartCompanyTimer()
    {
        var s = Story.Current;
        if (s == null) return;
        if (s.CompanyDeadline < 0 && !StellarisEventFlagManager.HasFlag("MCW_QS1_CorpDecided"))
        {
            s.CompanyDeadline = Find.TickManager.TicksGame + CompletionPolicy.CompanyDecisionTicks;
        }
    }

    public static void CompletionTick(int now)
    {
        var s = Story.Current;
        if (s == null) return;

        if (CompletionPolicy.CompanyDue(now, s.CompanyDeadline, StellarisEventFlagManager.HasFlag("MCW_QS1_CorpDecided")))
        {
            s.CompanyDeadline = -1;
            var def = MCW_EventDefOf.MCW_Event_QS1_CorpContact;
            foreach (var window in Find.WindowStack.Windows.OfType<StellarisEventWindow>()
                         .Where(w => AccessTools.Field(typeof(StellarisEventWindow), "eventDef").GetValue(w) == def).ToList())
            {
                window.Close(false);
            }
            // Run the same rejection effects as the visible choice: faction change,
            // tracked delegation attack, flags and next contact. No second custom branch.
            def.options[1].action.Execute();
            StoryUtility.Letter(
                "MinchoCandyWars.StoryLetter.CorpDeadlineTitle".Translate().RawText,
                "MinchoCandyWars.StoryLetter.CorpDeadlineBody".Translate().RawText);
        }

        var leader = s.Chiefs.ElementAtOrDefault(0);
        if (leader is { Dead: false }) StoryUtility.EnsureLeaderRegeneration(leader);
    }

    // ──────────────────── 剧情推进 ────────────────────

    public static void StartStory()
    {
        var s = Story.Current;
        if (s == null || s.Started) return;
        s.Started = true;
        StellarisEventFlagManager.SetFlag("MCW_QuestLine_Accepted", true);
        StellarisEventFlagManager.RemoveFlag("MCW_QuestLine_Declined");
        s.ArrivalAt = Find.TickManager.TicksGame + 60000;
        s.Result = "主线已开启，受庇护的珉巧将在1天后抵达";
    }

    public static void CorpResolved(bool mincho)
    {
        var s = Story.Current;
        if (s == null) return;
        s.CompanyDeadline = -1;
        if (mincho) s.JoinFirst();
        s.Result = mincho ? "保护受庇护的珉巧，等待同盟请求" : "进入公司分支（本批不扩写）";
        if (mincho) s.State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(2, 3) * 60000);
    }

    public static bool CanAccept
    {
        get
        {
            var s = Story.Current;
            return s != null
                && (!s.Victory || CampaignPolicy.AllowedAfterVictory(s.Offer))
                && s.ContactReady
                && s.State.ActiveQuest < 0
                && AvailableContact() >= 0
                && BucketMissions.Available(s.Offer);
        }
    }

    public static Quest EnsureSeries()
    {
        var s = Story.Current;
        if (s == null) return null;
        if (s.Series == null)
        {
            var definition = MCW_DefOf.MCWQ100_AllianceSeries;
            s.Series = QuestUtility.GenerateQuestAndMakeAvailable(definition, 0f);
        }
        if (s.Series != null)
        {
            s.Series.name = "MinchoCandyWars.Hospitality.SeriesTitle".Translate().RawText;
            s.Series.description = "MinchoCandyWars.Hospitality.SeriesDescription".Translate().RawText;
        }
        return s.Series;
    }

    public static void AcceptGuests()
    {
        var s = Story.Current;
        if (s == null || !CanAccept) return;
        var map = Find.AnyPlayerHomeMap;
        if (map == null) return;
        EnsureSeries();
        var slate = new Slate();
        slate.Set("map", map);
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed(Story.GuestDef), slate);
        if (quest == null)
        {
            Log.Error("[MCWQ097] 庇护任务生成失败，请保留日志");
            return;
        }
        if (!s.State.Begin(quest.id)) throw new InvalidOperationException("已有正在进行的庇护任务");
        if (quest.State == QuestState.NotYetAccepted) quest.Accept(null);
        s.ContactReady = false;
        s.HasAcceptedHospitality = true;
        s.RefreshHospitalityText(quest, quest.PartsListForReading.OfType<GuestSettlement>().Single().Plan);
        // QuestManager.Add initiates auto-accepted quests.
        s.Result = "B1庇护进行中";
        StoryUtility.Letter(
            "MinchoCandyWars.StoryLetter.GuestsArrivedTitle".Translate().RawText,
            "MinchoCandyWars.StoryLetter.GuestsArrivedBody".Translate().RawText);
    }

    public static void FinishMission(int kind, bool success)
    {
        var s = Story.Current;
        if (s == null) return;

        s.DepartAt = -1;
        s.ContactReady = false;
        s.CompletedBatch = false;
        if (success)
        {
            s.Succeeded |= 1 << kind;
            AddAffinity(s.ActiveIssuer, 5);
        }
        s.Result = success
            ? (s.PendingRewards.Count > 0 ? "任务成功，有奖励等待家园空投" : "任务成功，奖励已空投")
            : "任务未成功";
        if (kind != 6 || !success)
        {
            StoryUtility.Letter(
                "MinchoCandyWars.Campaign.ReplyTitle".Translate(s.IssuerName).RawText,
                OfferDialogue.Outcome(kind, success, s.ActiveIssuer));
        }

        if (kind == 6)
        {
            if (success)
            {
                s.Victory = true;
                s.State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(3, 5) * 60000);
                HeadquartersVictory.Apply(null);
                StellarisEventFlagManager.SetFlag("MCW_QuestFF1_Done", true);
                StoryUtility.Letter(
                    "MinchoCandyWars.Campaign.VictoryTitle".Translate().RawText,
                    "MinchoCandyWars.Campaign.VictoryBody".Translate().RawText);
            }
            if (!success)
            {
                s.FinalAt = CompletionPolicy.RetryAt(Find.TickManager.TicksGame, false);
                s.State.NextContactTick = -1;
                StoryUtility.Letter(
                    "MinchoCandyWars.Campaign.RetryTitle".Translate().RawText,
                    "MinchoCandyWars.Campaign.RetryBody".Translate().RawText);
            }
            return;
        }

        float progress = StellarisEventFlagManager.GetFlag("MCW_EndgameProgress")?.floatValue ?? 0;
        if (success && !s.Victory && progress >= 10 && !s.FinalAttempted && s.FinalAt < 0)
        {
            s.FinalAt = Find.TickManager.TicksGame + 10 * 60000;
            s.State.NextContactTick = -1;
            StoryUtility.Letter(
                "MinchoCandyWars.Campaign.PrepareTitle".Translate().RawText,
                "MinchoCandyWars.Campaign.PrepareBody".Translate().RawText);
        }
        else if (s.FinalAt < 0)
        {
            s.State.Schedule(Find.TickManager.TicksGame, Rand.RangeInclusive(3, 5) * 60000);
        }
    }

    public static string Status()
    {
        var s = Story.Current;
        return s?.Status() ?? "";
    }

    // ──────────────────── 好感度 ────────────────────

    public static int Affinity(int role)
    {
        var s = Story.Current;
        if (s == null) return 0;
        while (s.Affinities.Count < CharacterData.Names.Length) s.Affinities.Add(0);
        return s.Affinities[role];
    }

    public static void AddAffinity(int role, int amount)
    {
        var s = Story.Current;
        if (s == null) return;
        _ = Affinity(role);
        s.Affinities[role] = Math.Clamp(s.Affinities[role] + amount, 0, 100);
    }

    public static bool CanInviteCharacter(int role) =>
        Story.Current is { Victory: true } && Affinity(role) >= 100;

    // ──────────────────── 每日聊天 ────────────────────

    static void RefreshChat()
    {
        var s = Story.Current;
        if (s == null) return;
        int day = Find.TickManager.TicksGame / 60000;
        if (day != s.ChatDay)
        {
            s.ChatDay = day;
            s.ChatUsed.Clear();
            s.ChatTopicsRead.Clear();
        }
        while (s.ChatUsed.Count < CharacterData.Names.Length) s.ChatUsed.Add(0);
    }

    public static int ChatRemaining(int role)
    {
        RefreshChat();
        var s = Story.Current;
        return s == null ? 0 : Math.Max(0, 3 - s.ChatUsed[role]);
    }

    public static string ChatReason(int role)
    {
        RefreshChat();
        var s = Story.Current;
        if (s == null) return "MinchoCandyWars.Chat.Unavailable".Translate().RawText;
        if (!CanTalk(role)) return "MinchoCandyWars.Chat.Unavailable".Translate().RawText;
        if ((s.ChatDay + role) % CharacterData.Names.Length >= 4) return "MinchoCandyWars.Chat.Away".Translate().RawText;
        return ChatRemaining(role) == 0 ? "MinchoCandyWars.Chat.DailyLimit".Translate().RawText : null;
    }

    public static bool HasReadTopic(int role, int topic)
    {
        RefreshChat();
        var s = Story.Current;
        return s != null && s.ChatTopicsRead.Contains(role * 100 + topic);
    }

    public static bool TryChat(int role, int? topic = null)
    {
        var s = Story.Current;
        if (s == null) return false;
        if (ChatReason(role) != null || (topic.HasValue && HasReadTopic(role, topic.Value))) return false;
        if (s.ChatUsed[role] == 0) AddAffinity(role, 1);
        s.ChatUsed[role]++;
        if (topic.HasValue) s.ChatTopicsRead.Add(role * 100 + topic.Value);
        return true;
    }
    // ──────────────────── 角色 / 联系人 ────────────────────

    public static int ContactRole
    {
        get { var s = Story.Current; return s == null || s.OfferIssuer < 0 ? 0 : s.OfferIssuer; }
    }

    public static int AvailableContact()
    {
        var s = Story.Current;
        if (s == null) return -1;
        if (s.OfferIssuer < 0) s.OfferIssuer = ContactRole;
        if (!CanTalk(s.OfferIssuer)) s.OfferIssuer = OfferPolicy.Issuers(s.Offer).FirstOrDefault(CanTalk, -1);
        return s.OfferIssuer;
    }

    public static Pawn Chief(int role)
    {
        var s = Story.Current;
        if (s == null) return null;
        while (s.Chiefs.Count < CharacterData.Names.Length) s.Chiefs.Add(null);
        while (s.ChiefCreated.Count < CharacterData.Names.Length) s.ChiefCreated.Add(false);
        if (s.Chiefs[role] != null) return s.Chiefs[role];
        var candidates = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead
            .Where(p => p.GetComp<MinchoCandyWars.CompMinchoCore>() != null && p.Name?.ToStringShort == CharacterData.Names[role]).ToList();
        if (candidates.Count == 1) { s.ChiefCreated[role] = true; return s.Chiefs[role] = candidates[0]; }
        return null;
    }

    public static bool CanTalk(int role)
    {
        var s = Story.Current;
        if (s == null) return false;
        var pawn = Chief(role);
        bool ambiguous = pawn == null && PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead
            .Count(p => p.GetComp<MinchoCandyWars.CompMinchoCore>() != null && p.Name?.ToStringShort == CharacterData.Names[role]) > 1;
        return !ambiguous && (pawn == null
            ? !s.ChiefCreated[role]
            : !pawn.Dead && !pawn.Downed && !pawn.IsPrisoner && !pawn.Suspended
              && (pawn.Faction == Faction.OfPlayer || pawn.Faction?.def == MCW_DefOf.MCW_Alliance_Faction));
    }

    public static bool CanKitchen
    {
        get
        {
            var s = Story.Current;
            return s != null && s.State.ActiveQuest >= 0 && s.ActiveKind == 1
                && s.Guests.Any(p => p is { Dead: false, Spawned: true } && p.Map == Find.AnyPlayerHomeMap);
        }
    }

    public static void RefreshChiefDefaultNames()
    {
        var s = Story.Current;
        if (s == null) return;
        for (int role = 0; role < Math.Min(s.Chiefs.Count, CharacterData.Names.Length); role++)
        {
            var pawn = s.Chiefs[role];
            string previousDefault = role < s.chiefDefaultNames.Count ? s.chiefDefaultNames[role] ?? CharacterData.Names[role] : CharacterData.Names[role];
            if (pawn?.Name is NameSingle name && name.ToStringShort == previousDefault && previousDefault != CharacterData.DisplayName(role))
                SetChiefDefaultName(pawn, role);
        }
    }

    public static void SetChiefDefaultName(Pawn pawn, int role)
    {
        var s = Story.Current;
        if (s == null) return;
        while (s.chiefDefaultNames.Count <= role) s.chiefDefaultNames.Add(null);
        string name = CharacterData.DisplayName(role);
        pawn.Name = new NameSingle(name);
        s.chiefDefaultNames[role] = name;
    }
}
