#nullable disable
using HarmonyLib;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MinchoCandyWars.QuestStory;

[StaticConstructorOnStartup]
public static class Bootstrap
{
    static Bootstrap()
    {
        var template = MCW_DefOf.Hospitality_Refugee;
        var guestDef = DefDatabase<QuestScriptDef>.GetNamed(Story.GuestDef);
        // Use the native translated grammar, including join offers and failure letters.
        guestDef.questContentRules = template.questContentRules;
        guestDef.questDescriptionRules = template.questDescriptionRules;
        guestDef.questDescriptionAndNameRules = template.questDescriptionAndNameRules;
        var series = MCW_DefOf.MCWQ100_AllianceSeries;
        foreach (var def in DefDatabase<QuestScriptDef>.AllDefsListForReading)
            if (def.defName is "MCW_QS1_HuntedMincho" or Story.GuestDef || def.defName.StartsWith("MCW_QuestB")) def.epicParent = series;
        Log.Message("[MinchoCandyWars] 同盟剧情已加载：角色联络、任务循环与实际目标；开发者菜单：珉巧剧情测试");
    }
}

// Capture the same first pawn before the upstream timed flag removes dead objects.
[HarmonyPatch(typeof(QuestNode_SetFlag), "RunInt")]
static class RememberFirst
{
    static void Postfix(QuestNode_SetFlag __instance)
    {
        if (__instance.flagName.GetValue(RimWorld.QuestGen.QuestGen.slate) == "MCW_QS1_HuntedMincho")
        {
            var story = Story.Current;
            story.First = __instance.thingValue.GetValue(RimWorld.QuestGen.QuestGen.slate) as Pawn;
            if (story.First != null)
            {
                story.ArrivalAt = -1;
                story.Result = "受庇护的珉巧暂住中，等待同盟联络";
                story.FirstTemporary = true;
                RimWorld.QuestGen.QuestGen.quest.ExtraFaction(story.First.Faction,
                    new[] { story.First }, ExtraFactionType.MiniFaction, areHelpers: false);
            }
        }
    }
}

[HarmonyPatch(typeof(StellarisEventDef), nameof(StellarisEventDef.TryFire))]
static class RememberContact
{
    static void Postfix(StellarisEventDef __instance, bool __result)
    {
        if (__result && __instance == MCW_EventDefOf.MCW_Event_QS1_CorpContact) StoryCampaign.StartCompanyTimer();
        if (__result && __instance.defName == Story.Current.OfferEvent) Story.Current.Viewed |= 1 << Story.Current.Offer;
        if (__result && __instance.defName == Story.A1) Story.Current.A1Viewed = true;
        if (__result && __instance == MCW_EventDefOf.MCW_Event_QS1_AllianceContact && !StellarisEventFlagManager.HasFlag("MCWQ097_AllianceContactLetter"))
        {
            ContactLetter.Send(__instance.defName);
            StellarisEventFlagManager.SetFlag("MCWQ097_AllianceContactLetter", true);
        }
    }
}

public class UndecidedTrigger : StellarisEventTrigger
{
    public string flag;
    public override bool CanTrigger() => !StellarisEventFlagManager.HasFlag(flag);
}

[HarmonyPatch(typeof(RimWorld.QuestGen.QuestGen), nameof(RimWorld.QuestGen.QuestGen.Generate), new[] { typeof(QuestScriptDef), typeof(RimWorld.QuestGen.Slate) })]
static class SeriesParent
{
    static void Postfix(QuestScriptDef root, Quest __result)
    {
        if (__result != null && root.epicParent == MCW_DefOf.MCWQ100_AllianceSeries) __result.parent = Story.Current.Series;
    }
}

// The offer remains repeatable after defer; its original immediate effects run once.
public class OnceEffects : StellarisEventEffect
{
    public string flag;
    public List<StellarisEventEffect> effects = new();
    public override void Execute()
    {
        if (StellarisEventFlagManager.HasFlag(flag)) return;
        foreach (var effect in effects) effect.Execute();
        StellarisEventFlagManager.SetFlag(flag, true);
    }
}
