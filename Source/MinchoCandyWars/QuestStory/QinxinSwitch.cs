#nullable disable
using HarmonyLib;
using MinchoCandyWars;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

// Only the actual invited leader gets lossless switches. Specialists keep normal rules.
[HarmonyPatch(typeof(CompMinchoCore), nameof(CompMinchoCore.CompGetGizmosExtra))]
static class QinxinSwitch
{
    static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompMinchoCore __instance)
    {
        foreach (var gizmo in __result) yield return gizmo;
        if (Current.Game == null || Story.Current.Chiefs.ElementAtOrDefault(0) != __instance.pawn) yield break;
        string[] candies = ["MechaCandy", "NectarCandy", "AmberonCandy", "PrismCandy", "LinglonCandy"];
        string[] items = ["MCW_MechaCandyChangeItem", "MCW_NectarCandyChangeItem", "MCW_AmberonCandyChangeItem", "MCW_PrismCandyChangeItem", "MCW_LinglonCandyChangeItem"];
        for (int i = 0; i < candies.Length; i++)
        {
            var candy = DefDatabase<CandyTypeDef>.GetNamed(candies[i]);
            yield return new Command_Action
            {
                defaultLabel = "MinchoCandyWars.ChiefCommand.Switch".Translate(candy.LabelCap),
                defaultDesc = "MinchoCandyWars.ChiefCommand.SwitchDesc".Translate(),
                icon = DefDatabase<ThingDef>.GetNamed(items[i]).uiIcon,
                action = () => { __instance.CurrentCandyType = candy; __instance.MinchoCandyValue = __instance.CurrentMaxCandyValue; }
            };
        }
    }
}
