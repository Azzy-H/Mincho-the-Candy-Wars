using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    /// <summary>
    /// 守护之名的跳跃动词。
    ///
    /// 原版 <c>Verb_CastAbilityJump.CanHitTargetFrom</c> 走 <c>JumpUtility.CanHitTargetFrom</c>，
    /// 那里硬编码了 <c>GenSight.LineOfSight</c> —— verbProps 里的 requireLineOfSight 对它无效。
    /// 这里保留原版的射程与落点合法性检查，只去掉视线要求，让跳跃可以越过屋顶和地形。
    /// </summary>
    public sealed class GuardianJumpVerb : Verb_CastAbilityJump
    {
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (!targ.IsValid || CasterPawn?.Map == null || !targ.Cell.InBounds(CasterPawn.Map)) return false;

            return root.DistanceToSquared(targ.Cell) <= EffectiveRange * EffectiveRange;
        }

        /// <summary>
        /// 让「只护友方」在**选目标阶段**就生效：无派系、中立、敌对的目标一律不可选，
        /// 而不是鼠标能指上去、点下去才报错。
        /// </summary>
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Pawn is Pawn targetPawn && !GuardianTarget.IsFriendly(CasterPawn, targetPawn))
            {
                return false;
            }

            return base.ValidateTarget(target, showMessages);
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            Pawn pawn = CasterPawn;
            if (pawn?.Map == null
                || !CanHitTargetFrom(pawn.Position, target)
                || !JumpUtility.ValidJumpTarget(pawn, pawn.Map, target.Cell))
            {
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.CastJump, target.Cell);
            job.verbToUse = this;
            if (pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                FleckMaker.Static(target.Cell, pawn.Map, FleckDefOf.FeedbackGoto);
            }
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            Pawn pawn = CasterPawn;
            if (pawn?.Map == null) return;

            if (target.IsValid && JumpUtility.ValidJumpTarget(pawn, pawn.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }

            GenDraw.DrawRadiusRing(pawn.Position, EffectiveRange, Color.white,
                cell => JumpUtility.ValidJumpTarget(pawn, pawn.Map, cell));
        }
    }

    /// <summary>
    /// 「守护之名」的友方判定：只认同派系或明确盟友。
    /// 动词（选目标阶段）与效果（施放阶段）共用这一份，避免两处规则漂移。
    /// </summary>
    public static class GuardianTarget
    {
        public static bool IsFriendly(Pawn? caster, Pawn target)
        {
            Faction? casterFaction = caster?.Faction;
            Faction? targetFaction = target.Faction;

            return casterFaction != null && targetFaction != null
                && (casterFaction == targetFaction
                    || casterFaction.RelationKindWith(targetFaction) == FactionRelationKind.Ally);
        }
    }

    /// <summary>
    /// 守护之名！—— 跳跃完成后在落点四周生成冰淇淋墙体；只能以本派系或明确盟友为目标。
    /// </summary>
    public class CompAbilityEffect_AmberonGuardianJump : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        /// <summary>墙体是 5x5 的空心环：|dx| == 半径 或 |dz| == 半径。</summary>
        private const int WallRadius = 2;

        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            ThingDef wall = MCW_DefOf.Mincho_MintchocoWall;

            IntVec3 center = target.IsValid ? target.Cell : caster.Position;
            for (int dx = -WallRadius; dx <= WallRadius; dx++)
            {
                for (int dz = -WallRadius; dz <= WallRadius; dz++)
                {
                    if (Mathf.Abs(dx) < WallRadius && Mathf.Abs(dz) < WallRadius) continue; // 跳过内部

                    IntVec3 cell = center + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map)) continue;
                    if (cell.Filled(map)) continue;
                    if (!GenConstruct.CanPlaceBlueprintAt(wall, cell, Rot4.North, map, godMode: false).Accepted)
                    {
                        continue;
                    }

                    GenSpawn.Spawn(wall, cell, map, Rot4.North);
                }
            }
        }

        /// <summary>只允许本派系或明确盟友；中立、敌对、无派系的目标一律拒绝。</summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn? ally = target.Pawn;
            if (ally != null && !GuardianTarget.IsFriendly(parent.pawn, ally))
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.GuardianNameNotFriendly".Translate(), ally,
                        MessageTypeDefOf.RejectInput, historical: false);
                }

                return false;
            }

            return base.Valid(target, throwMessages);
        }
    }
}
