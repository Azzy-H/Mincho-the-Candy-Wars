using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>
/// 纳米塑形配方：配方的 &lt;products&gt; 只用于信息卡显示（写动物种族 ThingDef），
/// 实际产出由本扩展指定的 PawnKindDef 生成活体动物。挂在 RecipeDef 上。
/// </summary>
public class NanoAnimalProductExtension : DefModExtension
{
    public PawnKindDef pawnKind = null!;
}

/// <summary>
/// 纳米塑形配方完成时改为生成活体动物。
///
/// 原版 <c>GenRecipe.MakeRecipeProducts</c> 只支持 products（ThingDef）与
/// specialProducts（屠宰/熔炼/切块），无法让配方产出 Pawn，所以这条前缀必须保留。
/// </summary>
[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
public static class NanoAnimalRecipeOutput
{
    public static bool Prefix(RecipeDef recipeDef, ref IEnumerable<Thing> __result)
    {
        NanoAnimalProductExtension? ext = recipeDef.GetModExtension<NanoAnimalProductExtension>();
        if (ext == null) return true;

        if (ext.pawnKind == null)
        {
            Log.Error($"[MCW] 配方 {recipeDef.defName} 的 NanoAnimalProductExtension 没有配置 pawnKind。");
            __result = Array.Empty<Thing>();
            return false;
        }

        __result = new Thing[] { PawnGenerator.GeneratePawn(ext.pawnKind, Faction.OfPlayer) };
        return false;
    }
}