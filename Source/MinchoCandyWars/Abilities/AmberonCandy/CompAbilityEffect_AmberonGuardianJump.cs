using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    /// <summary>
    /// 守护之名！—— 跳跃完成后在落点四周生成冰淇淋墙体。
    /// </summary>
    public class CompAbilityEffect_AmberonGuardianJump : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return;

            IntVec3 center = target.IsValid ? target.Cell : caster.Position;
            // 5x5 hollow outline: cells where |x| == 2 or |z| == 2
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    if (Mathf.Abs(dx) < 2 && Mathf.Abs(dz) < 2) continue; // skip interior
                    IntVec3 cell = center + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map)) continue;
                    if (cell.Filled(map) || !GenConstruct.CanPlaceBlueprintAt(MCW_DefOf.MCW_AmberonIceCreamWall, cell, Rot4.North, map, godMode: false).Accepted) continue;
                    GenSpawn.Spawn(MCW_DefOf.MCW_AmberonIceCreamWall, cell, map, Rot4.North);
                }
            }

            if (target.HasThing && target.Thing is Pawn ally && ally != caster && !ally.Dead && ally.Map == map)
            {
                // PawnFlyer with flyWithCarriedThing handles carrying the ally.
                // Walls spawn around the caster's landing position.
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn ally = target.Pawn;
            if (ally == null) return false;
            if (ally.HostileTo(parent.pawn.Faction))
            {
                if (throwMessages)
                {
                    Messages.Message("MinchoCandyWars.Abilities.GuardianNameNotFriendly".Translate(), ally, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }
}
