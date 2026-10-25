using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Review;

/// <summary>
/// 举盾 / 背盾 Hediff：把"当前这面盾（含材质）实际提供的三防"写进 stage。
///
/// 为什么用子类而不是补丁：<c>Hediff.CurStage</c> 是 <c>public virtual</c>，
/// 而 <c>HediffDef.hediffClass</c> 本来就是原版预留的扩展点，本仓库其它 20 多个
/// HediffDef 也都在用它，没有理由为一个 Hediff 去 patch 原版 getter。
///
/// 数值也不在这里算：直接问 <c>StatWorker.StatOffsetFromGear</c>，它会读盾牌 Def 的
/// equippedStatOffsets（基值）并跑 MCW_ShieldArmor_* 上的 StatPart_Stuff（材质部分），
/// 与面板显示走的是同一条路径，因此面板数字和实际生效值不会分叉。
/// </summary>
public class Hediff_ShieldDefense : HediffWithComps
{
    /// <summary>object.MemberwiseClone：用于克隆被 Def 共享的 HediffStage。</summary>
    private static readonly System.Reflection.MethodInfo CloneStage =
        AccessTools.Method(typeof(object), "MemberwiseClone");

    /// <summary>举盾 Hediff → 提供防御的盾牌本体。用 DefOf 比较，代码里不出现 defName 字符串。</summary>
    private static ThingDef? ShieldDefFor(HediffDef hediff)
    {
        if (hediff == MCW_DefOf.MCW_TowerShield_DraftedHediff) return MCW_DefOf.MCW_TowerShield;
        if (hediff == MCW_DefOf.MCW_CookiesShield_DraftedHediff) return MCW_DefOf.MCW_CookiesShield;
        return null;
    }

    public override HediffStage CurStage
    {
        get
        {
            // 没有 stage 的 HediffDef 会返回 null，此时保持原样。
            HediffStage? maybeBase = base.CurStage;
            if (maybeBase == null) return null!;
            HediffStage baseStage = maybeBase;

            if (pawn == null) return baseStage;

            ThingDef? shieldDef = ShieldDefFor(def);
            if (shieldDef == null) return baseStage;

            Apparel? shield = pawn.apparel?.WornApparel.FirstOrDefault(a => a.def == shieldDef);
            if (shield == null) return baseStage;

            // HediffStage 是被 HediffDef 共享的对象，直接改会污染所有戴同款盾的单位，
            // 还可能覆盖别的模组在 stage 子类上挂的字段 —— 所以克隆一份再改。
            var stage = (HediffStage)CloneStage.Invoke(baseStage, null)!;

            // 只重写三防这几条，其它 modifier（例如射击精度惩罚）原样保留。
            stage.statOffsets = baseStage.statOffsets?
                .Where(s => s.stat != StatDefOf.ArmorRating_Sharp
                         && s.stat != StatDefOf.ArmorRating_Blunt
                         && s.stat != StatDefOf.ArmorRating_Heat)
                .ToList() ?? new List<StatModifier>();

            stage.statOffsets.Add(Modifier(shield, MCW_DefOf.MCW_ShieldArmor_Sharp, StatDefOf.ArmorRating_Sharp));
            stage.statOffsets.Add(Modifier(shield, MCW_DefOf.MCW_ShieldArmor_Blunt, StatDefOf.ArmorRating_Blunt));
            stage.statOffsets.Add(Modifier(shield, MCW_DefOf.MCW_ShieldArmor_Heat,  StatDefOf.ArmorRating_Heat));

            return stage;
        }
    }

    private static StatModifier Modifier(Apparel shield, StatDef source, StatDef target) =>
        new StatModifier
        {
            stat = target,
            value = StatWorker.StatOffsetFromGear(shield, source),
        };
}
