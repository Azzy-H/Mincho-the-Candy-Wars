using System.Linq;
using MinchoCandyWars;
using RimWorld;
using Verse;
using Verse.AI;

namespace MinchoCandyWars.Anomaly;

public class BucketReleaseProperties : CompProperties_Interactable
{
    public BucketReleaseProperties()
    {
        compClass = typeof(BucketRelease);
    }
}

/// <summary>
/// 消耗 1 个超凡碎片解除无尽桶的精神影响。
///
/// 解除的方式是**替换成另一个 ThingDef**（<c>MCW_EndlessIceCream_Released</c>）：
/// 营养 0.2 直接写在新 Def 的 statBases 里，因此不需要运行时状态、不需要 Nutrition StatPart，
/// 也不需要清理 Nutrition 缓存。旧桶销毁时由 <see cref="EndlessBucket.PostDestroy"/>
/// 统一解除所有依赖者。
/// </summary>
public class BucketRelease : CompInteractable
{
    public override AcceptanceReport CanInteract(Pawn activateBy = null!, bool checkOptionalItems = true)
    {
        if (parent.TryGetComp<CompStudyUnlocks>()?.Completed != true)
        {
            return "MinchoCandyWars.UI.Bucket.NotStudied".Translate();
        }

        AcceptanceReport result = base.CanInteract(activateBy, checkOptionalItems);
        if (!result.Accepted)
        {
            return result;
        }

        if (!checkOptionalItems)
        {
            return true;
        }

        if (activateBy != null)
        {
            if (!activateBy.HasReserved(ThingDefOf.Shard) && !activateBy.CanReserveAndReachableOfDef(ThingDefOf.Shard))
            {
                return "MinchoCandyWars.UI.Bucket.NeedShard".Translate();
            }
        }
        else if (!ReservationUtility.ExistsUnreservedAmountOfDef(parent.MapHeld, ThingDefOf.Shard, Faction.OfPlayer, 1))
        {
            return "MinchoCandyWars.UI.Bucket.NeedShard".Translate();
        }

        return true;
    }

    public override void OrderForceTarget(LocalTargetInfo target)
    {
        if (!ValidateTarget(target, false) || !target.Pawn.TryFindReserveAndReachableOfDef(ThingDefOf.Shard, out Thing shard))
        {
            return;
        }

        Job job = JobMaker.MakeJob(JobDefOf.InteractThing, parent, shard);
        job.count = 1;
        job.playerForced = true;
        target.Pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    protected override void OnInteracted(Pawn caster)
    {
        // 先记下位置与禁用状态，销毁后 parent 就不能再用了
        Map map = parent.MapHeld;
        IntVec3 position = parent.PositionHeld;
        Rot4 rotation = parent.Rotation;
        bool forbidden = parent.IsForbidden(Faction.OfPlayer);

        // 销毁旧桶：EndlessBucket.PostDestroy 会解除所有由它造成的依赖
        parent.Destroy(DestroyMode.Vanish);

        Thing released = ThingMaker.MakeThing(MCW_DefOf.MCW_EndlessIceCream_Released);
        GenSpawn.Spawn(released, position, map, rotation);

        if (forbidden)
        {
            released.SetForbidden(true, false);
        }
    }
}
