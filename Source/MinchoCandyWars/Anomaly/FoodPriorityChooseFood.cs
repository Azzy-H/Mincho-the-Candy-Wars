using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 成瘾者主动找食物时优先挑档位更高的薄荷类食物：先看背包，再看地图。
///
/// <c>FoodUtility.TryFindBestFoodSourceFor</c> 同样是原版静态方法，只能打补丁。
/// 候选来自 <c>BestFoodSourceOnMap</c>，到达性、预约、食物政策、新鲜度等检查全部沿用原版。
/// </summary>
[HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor))]
public static class MintFoodChoice
{
    public static void Postfix(Pawn getter, Pawn eater, bool desperate, ref Thing foodSource,
        ref ThingDef foodDef, bool canUseInventory, bool allowForbidden, bool allowCorpse,
        bool allowSociallyImproper, bool allowHarvest, bool forceScanWholeMap,
        bool ignoreReservations, bool calculateWantedStackCount, bool allowVenerated,
        FoodPreferability minPrefOverride, ref bool __result)
    {
        if (!MintFoodPriority.IsAffected(eater) || getter?.Map == null) return;

        int bestRank = MintFoodPriority.Rank(foodSource);
        if (bestRank == 2) return;

        // 原版挑背包食物时只取第一个合法餐食，可能在地图扫描之前就定了。
        // 这里只做"升档"，同档位保持原版选择。
        if (canUseInventory && getter.RaceProps.ToolUser
            && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
            && getter.inventory != null)
        {
            foreach (Thing food in getter.inventory.innerContainer)
            {
                int rank = MintFoodPriority.Rank(food);
                if (rank <= bestRank || !food.IngestibleNow
                    || !eater.WillEat(food, getter, careIfNotAcceptableForTitle: true, allowVenerated)
                    || food.def.ingestible.preferability == FoodPreferability.NeverForNutrition
                    || food.def.ingestible.preferability > FoodPreferability.MealLavish
                    || (minPrefOverride != FoodPreferability.Undefined
                        && food.def.ingestible.preferability < minPrefOverride))
                {
                    continue;
                }

                foodSource = food;
                foodDef = food.def;
                __result = true;
                bestRank = rank;
            }
        }

        if (bestRank == 2) return;

        // 复用原版的到达、预约、食物政策与新鲜度检查；只有档位更高时才顶替原结果。
        // 把最低偏好降到 DesperateOnly，才能在地图上找到生冰淇淋这类通常不会被考虑的食品；
        // 找不到更高档位时不会动原版返回的普通食物。
        Thing candidate = FoodUtility.BestFoodSourceOnMap(getter, eater, desperate, out ThingDef candidateDef,
            FoodPreferability.MealLavish, allowPlant: false, allowDrug: false, allowCorpse: allowCorpse,
            allowDispenserFull: false, allowDispenserEmpty: false, allowForbidden: allowForbidden,
            allowSociallyImproper: allowSociallyImproper, allowHarvest: false,
            forceScanWholeMap: forceScanWholeMap, ignoreReservations: ignoreReservations,
            calculateWantedStackCount: calculateWantedStackCount,
            minPrefOverride: minPrefOverride == FoodPreferability.Undefined
                ? FoodPreferability.DesperateOnly : minPrefOverride, allowVenerated: allowVenerated);

        if (MintFoodPriority.Rank(candidate) > bestRank)
        {
            foodSource = candidate;
            foodDef = candidateDef;
            __result = true;
        }
    }
}
