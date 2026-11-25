#nullable disable
using HarmonyLib;
using MinchoCandyWars;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.QuestStory;

[HarmonyPatch(typeof(CompMinchoCore), nameof(CompMinchoCore.CompGetGizmosExtra))]
static class ChiefCommands
{
    static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompMinchoCore __instance)
    {
        foreach (var gizmo in __result) yield return gizmo;
        if (Current.Game == null || !__instance.pawn.Spawned || !__instance.pawn.Map.IsPlayerHome) yield break;
        int role = Story.Current.Chiefs.FindIndex(p => p == __instance.pawn);
        if (role < 0 || !StoryCampaign.CanTalk(role)) yield break;
        var icon = MCW_ThingDefOf.MCW_Chocolate_Console.uiIcon;
        yield return new Command_Action { defaultLabel = "MinchoCandyWars.ChiefCommand.Chat".Translate(), defaultDesc = "MinchoCandyWars.ChiefCommand.ChatDesc".Translate(CharacterData.DisplayName(role)), icon = icon, iconProportions = new Vector2((float)icon.width / icon.height, 1), action = () => { Find.WindowStack.Add(new StellarisEventWindow(DefDatabase<StellarisEventDef>.GetNamed($"MCWQ101_Chat{role}"))); ConversationPages.Topics(role); } };
        yield return new Command_Action { defaultLabel = "MinchoCandyWars.ChiefCommand.Gift".Translate(), defaultDesc = "MinchoCandyWars.ChiefCommand.GiftDesc".Translate(), icon = MCW_ThingDefOf.Mincho_Mintchoco.uiIcon, action = Gifts.Open };
    }
}
