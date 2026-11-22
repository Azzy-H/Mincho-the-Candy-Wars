using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

public class HuntedArrivalText : QuestNode
{
    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        QuestGen.slate.Set("mcwArrivalTitle", "MinchoCandyWars.StoryLetter.HuntedArrivalTitle".Translate().RawText);
        QuestGen.slate.Set("mcwArrivalBody", "MinchoCandyWars.StoryLetter.HuntedArrivalBody".Translate().RawText);
    }
}
