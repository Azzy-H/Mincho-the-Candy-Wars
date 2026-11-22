#nullable disable
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;
public sealed partial class Story
{
    public bool FirstTemporary;
    void JoinFirst()
    {
        if (!FirstTemporary || First == null || First.Dead || First.Faction != Faction.OfPlayer) return;
        foreach (var quest in Find.QuestManager.QuestsListForReading)
            foreach (var part in quest.PartsListForReading.OfType<QuestPart_ExtraFaction>())
                if (quest.root == MCW_DefOf.MCW_QS1_HuntedMincho && part.affectedPawns.Remove(First))
                    part.extraFaction.faction.Notify_MemberLeftExtraFaction(First);
        FirstTemporary = false;
        Letter("MinchoCandyWars.StoryLetter.FirstJoinedTitle".Translate().RawText,
            "MinchoCandyWars.StoryLetter.FirstJoinedBody".Translate(First.LabelShortCap).RawText);
    }
}
