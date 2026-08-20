using System.Reflection;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    /// <summary>
    /// 琉璃霞的飞行器：飞行过程中落点跟随目标 pawn 移动。
    /// 目标 pawn 从基类 PawnFlyer 的私有字段 target 读取（原版 DoJump 已写入该字段）。
    /// </summary>
    public class PawnFlyer_PrismGlow : PawnFlyer
    {
        private static readonly FieldInfo? TargetFieldInfo = typeof(PawnFlyer).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic);

        protected override void TickInterval(int delta)
        {
            UpdateDestinationToTarget();
            base.TickInterval(delta);
        }

        protected override void RespawnPawn()
        {
            UpdateDestinationToTarget();
            base.RespawnPawn();
        }

        private Pawn? GetTargetPawn()
        {
            object? value = TargetFieldInfo?.GetValue(this);
            return value is LocalTargetInfo target ? target.Pawn : null;
        }

        private void UpdateDestinationToTarget()
        {
            Pawn? targetPawn = GetTargetPawn();
            if (targetPawn == null || !targetPawn.Spawned || targetPawn.Dead || Map == null)
            {
                return;
            }

            IntVec3 targetCell = targetPawn.Position;
            if (!JumpUtility.ValidJumpTarget(FlyingPawn, Map, targetCell))
            {
                IntVec3 found = IntVec3.Invalid;
                foreach (IntVec3 c in GenRadial.RadialCellsAround(targetCell, 1.9f, false))
                {
                    if (JumpUtility.ValidJumpTarget(FlyingPawn, Map, c))
                    {
                        found = c;
                        break;
                    }
                }

                if (!found.IsValid)
                {
                    return;
                }
                targetCell = found;
            }

            FieldInfo? destCellField = typeof(PawnFlyer).GetField("destCell", BindingFlags.Instance | BindingFlags.NonPublic);
            destCellField?.SetValue(this, targetCell);
        }
    }
}
