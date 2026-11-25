#nullable disable
using RimWorld;
using RimWorld.QuestGen;
using MinchoCandyWars.StellarisEvent;
using Verse;

namespace MinchoCandyWars.QuestStory;

public sealed partial class Story
{
    // 阶段状态：flag 单源（持久化由 StellarisEventFlagManager 负责）。
    public int Offer
    {
        get => StoryFlags.GetInt("MCW_Story_Offer", 1);
        set => StoryFlags.SetInt("MCW_Story_Offer", value);
    }

    public int ActiveKind
    {
        get => StoryFlags.GetInt("MCW_Story_ActiveKind", 1);
        set => StoryFlags.SetInt("MCW_Story_ActiveKind", value);
    }

    public int Rejected
    {
        get => StoryFlags.GetInt("MCW_Story_Rejected");
        set => StoryFlags.SetInt("MCW_Story_Rejected", value);
    }

    public int Viewed
    {
        get => StoryFlags.GetInt("MCW_Story_Viewed");
        set => StoryFlags.SetInt("MCW_Story_Viewed", value);
    }

    public int Succeeded
    {
        get => StoryFlags.GetInt("MCW_Story_Succeeded");
        set => StoryFlags.SetInt("MCW_Story_Succeeded", value);
    }

    public int LastBucketCargo
    {
        get => StoryFlags.GetInt("MCW_Story_LastBucketCargo", -1);
        set => StoryFlags.SetInt("MCW_Story_LastBucketCargo", value);
    }

    public int FinalAt
    {
        get => StoryFlags.GetInt("MCW_Story_FinalAt", -1);
        set => StoryFlags.SetInt("MCW_Story_FinalAt", value);
    }

    public int OfferIssuer
    {
        get => StoryFlags.GetInt("MCW_Story_OfferIssuer", -1);
        set => StoryFlags.SetInt("MCW_Story_OfferIssuer", value);
    }

    public int ActiveIssuer
    {
        get => StoryFlags.GetInt("MCW_Story_ActiveIssuer");
        set => StoryFlags.SetInt("MCW_Story_ActiveIssuer", value);
    }

    public int OfferNumber
    {
        get => StoryFlags.GetInt("MCW_Story_OfferNumber");
        set => StoryFlags.SetInt("MCW_Story_OfferNumber", value);
    }

    public bool AssignedOffer
    {
        get => StoryFlags.GetBool("MCW_Story_AssignedOffer");
        set => StoryFlags.SetBool("MCW_Story_AssignedOffer", value);
    }

    public bool Victory
    {
        get => StoryFlags.GetBool("MCW_Story_Victory");
        set => StoryFlags.SetBool("MCW_Story_Victory", value);
    }

    public bool FinalAttempted
    {
        get => StoryFlags.GetBool("MCW_Story_FinalAttempted");
        set => StoryFlags.SetBool("MCW_Story_FinalAttempted", value);
    }

    public bool Migrated101
    {
        get => StoryFlags.GetBool("MCW_Story_Migrated101");
        set => StoryFlags.SetBool("MCW_Story_Migrated101", value);
    }

    public bool KitchenViewed
    {
        get => StoryFlags.GetBool("MCW_Story_KitchenViewed");
        set => StoryFlags.SetBool("MCW_Story_KitchenViewed", value);
    }

    // 集合类保留为字段（需要原地 Add/Remove，属性化会丢失修改）。
    public List<RewardPlan> PendingRewards = new();
    public string IssuerName => CharacterData.DisplayName(ActiveIssuer);
    public string OfferEvent => Offer > 6 ? $"MCWQ101_Event{Offer}" : Offer == 6 ? "MCW_Event_QuestF1_AllianceContact" : $"MCW_Event_QuestA{Offer}_AllianceContact";
    void CampaignExpose()
    {
        CharactersExpose(); GiftsExpose(); CompletionExpose();
        Scribe_Collections.Look(ref PendingRewards, "pendingAllianceRewards", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) PendingRewards ??= new();
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        StoryCampaign.RefreshChiefDefaultNames();
        if (State.ActiveQuest < 0 || ActiveIssuer < 0 || ActiveIssuer >= CharacterData.Names.Length) return;
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

    internal void RefreshMissionText(Quest quest, int kind, RewardPlan plan)
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
}

public class OfferTrigger : StellarisEventTrigger
{
    public int kind;
    public override bool CanTrigger() => Current.Game != null && StoryCampaign.CanAccept && Story.Current.Offer == kind;
}
public class OfferEffect : StellarisEventEffect
{
    public int kind; public bool reject;
    public override void Execute() { if (reject) StoryCampaign.RejectOffer(kind); else StoryCampaign.AcceptOffer(kind); }
}

