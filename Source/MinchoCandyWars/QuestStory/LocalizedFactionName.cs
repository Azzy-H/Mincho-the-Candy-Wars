using HarmonyLib;
using RimWorld;

namespace MinchoCandyWars.QuestStory;

[HarmonyPatch(typeof(Faction), nameof(Faction.Name), MethodType.Getter)]
static class LocalizedFactionName
{
    static void Postfix(Faction __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__instance.def?.fixedName)) return;

        // A fixed name is serialized when the faction is created. Recognize only shipped defaults.
        bool isDefault = __instance.def.defName switch
        {
            "MCW_Alliance_Faction" => __result is "珉巧同盟" or "Mincho Alliance" or "민초 동맹" or "Альянс минчо" or "ミンチョ同盟",
            "MCW_RecycleCorp_Faction" => __result is "回收公司" or "Recycle Corporation" or "회수 회사" or "Корпорация по утилизации" or "回収会社",
            _ => false
        };
        if (isDefault) __result = __instance.def.fixedName;
    }
}
