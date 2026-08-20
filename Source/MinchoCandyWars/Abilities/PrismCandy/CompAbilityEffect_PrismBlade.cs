using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    /// <summary>
    /// 琉璃剑气类技能的通用配置：半径、半角、伤害比例、伤害 Def 均通过 Prop 配置。
    /// </summary>
    public class CompProperties_AbilityPrismBlade : CompProperties_AbilityEffect
    {
        public float halfAngle = 20f;
        public float damageRatio = 0.2f;
        public DamageDef damageDef = null!;

        public CompProperties_AbilityPrismBlade()
        {
            compClass = typeof(CompAbilityEffect_PrismBlade);
        }
    }

    /// <summary>
    /// 琉璃千刃 / 冰霜剑影共用父类：向前方扇形区域造成爆炸型利器伤害。
    /// 具体数值由 CompProperties_AbilityPrismBlade 配置。
    /// </summary>
    public class CompAbilityEffect_PrismBlade : CompAbilityEffect
    {
        public new CompProperties_AbilityPrismBlade Props => (CompProperties_AbilityPrismBlade)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            float angle = PrismCandyAbilityUtility.AngleToTarget(caster.Position, target.Cell);
            int damage = Mathf.RoundToInt(Props.damageRatio * PrismCandyAbilityUtility.GetMaxCandyValue(caster));
            List<IntVec3> cells = PrismCandyAbilityUtility.GetCellsInSector(
                caster.Position, map, parent.verb.verbProps.range, angle, Props.halfAngle).ToList();

            GenExplosion.DoExplosion(
                caster.Position, map, parent.verb.verbProps.range, Props.damageDef, caster,
                damAmount: damage,
                overrideCells: cells);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            float angle = PrismCandyAbilityUtility.AngleToTarget(caster.Position, target.Cell);
            List<IntVec3> cells = PrismCandyAbilityUtility.GetCellsInSector(
                caster.Position, map, parent.verb.verbProps.range, angle, Props.halfAngle).ToList();
            GenDraw.DrawFieldEdges(cells);
        }
    }
}
