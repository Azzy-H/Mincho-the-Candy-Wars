#nullable disable
using RimWorld;
using Verse;
namespace MinchoCandyWars.QuestStory;
public sealed partial class Story
{
    internal List<int> Affinities = new();
    void AffinityExpose()
    {
        Scribe_Collections.Look(ref Affinities, "characterAffinity144", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Affinities ??= new();
    }
    // 好感度的读写逻辑已迁到 StoryCampaign（Affinity / AddAffinity / CanInviteCharacter）。
}
