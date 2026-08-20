using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    /// <summary>
    /// 巧克力冰淇淋巨剑的通用配置：剑长、剑宽、伤害比例、伤害 Def 均通过 Prop 配置。
    /// </summary>
    public class CompProperties_AbilityPrismChocoGreatsword : CompProperties_AbilityEffect
    {
        public float swordWidth = 5f;
        public float damageRatio = 0.6f;
        public DamageDef damageDef = null!;

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach(var s in base.ConfigErrors(parentDef))
            {
                yield return s;
            }
            if (damageDef == null)
            {
                yield return $"CompProperties_AbilityPrismChocoGreatsword in {parentDef.defName} has null damageDef.";
            }
        }
        public CompProperties_AbilityPrismChocoGreatsword()
        {
            compClass = typeof(CompAbilityEffect_PrismChocoGreatsword);
        }
    }

    /// <summary>
    /// 巧克力冰淇淋巨剑——沿施法者朝向挥出巨剑，对范围内单位造成钝器伤害，并生成减速区域。
    /// 作用范围几何参考 Koelime CompAbilityEffect_LaserCannon。
    /// </summary>
    public class CompAbilityEffect_PrismChocoGreatsword : CompAbilityEffect
    {
        public new CompProperties_AbilityPrismChocoGreatsword Props => (CompProperties_AbilityPrismChocoGreatsword)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            IntVec3 start = caster.Position;
            IntVec3 end = PrismCandyAbilityUtility.GenEndPos(start, target.Cell, parent.verb.verbProps.range);
            float halfWidth = Props.swordWidth / 2f;

            List<IntVec3> cells = PrismCandyAbilityUtility.PointsWithinDistanceFast(start, end, halfWidth)
                .Where(c => c.InBounds(map))
                .ToList();

            int damage = Mathf.RoundToInt(Props.damageRatio * PrismCandyAbilityUtility.GetMaxCandyValue(caster));
            GenExplosion.DoExplosion(
                start, map, parent.verb.verbProps.range / 2f, Props.damageDef, caster,
                damAmount: damage,
                overrideCells: cells,
                ignoredThings: new List<Thing> { caster });

            // 生成减速区域（透明物体，每 tick 遍历 pawn）
            Thing zone = ThingMaker.MakeThing(MCW_DefOf.MCW_PrismSwordSlowZone);
            Comp_PrismSwordSlowZone? slowComp = zone.TryGetComp<Comp_PrismSwordSlowZone>();
            if (slowComp != null)
            {
                slowComp.Setup(start, end, halfWidth);
            }
            GenSpawn.Spawn(zone, start, map);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            IntVec3 start = caster.Position;
            IntVec3 end = PrismCandyAbilityUtility.GenEndPos(start, target.Cell, parent.verb.verbProps.range);
            float halfWidth = Props.swordWidth / 2f;

            List<IntVec3> cells = PrismCandyAbilityUtility.PointsWithinDistanceFast(start, end, halfWidth)
                .Where(c => c.InBounds(map))
                .ToList();

            GenDraw.DrawFieldEdges(cells);
        }
    }
}
