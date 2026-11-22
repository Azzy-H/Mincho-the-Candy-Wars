#nullable disable
using RimWorld.QuestGen;
using Verse;
using RimWorld.Planet;

namespace MinchoCandyWars.QuestStory;

public class NearSiteTile : QuestNode_GetSiteTile
{
    protected override void RunInt()
    {
        var slate = QuestGen.slate;
        if (!slate.Get(storeAs.GetValue(slate), PlanetTile.Invalid).Valid && !TestRunInt(slate))
            throw new InvalidOperationException("未能为同盟增援找到有效地点");
    }
    protected override bool TestRunInt(Slate slate)
    {
        var original = slate.Get("siteDistRange", new IntRange(7, 27));
        slate.Set("siteDistRange", new IntRange(1, 2));
        if (base.TestRunInt(slate)) { slate.Set("siteDistRange", original); return true; }
        slate.Set("siteDistRange", original);
        return base.TestRunInt(slate);
    }
}
