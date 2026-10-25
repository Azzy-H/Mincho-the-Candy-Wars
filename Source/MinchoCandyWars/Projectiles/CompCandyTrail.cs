using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Projectiles;

/// <summary>糖霜拖尾的 comp 配置。</summary>
public class CompProperties_CandyTrail : CompProperties
{
    public CompProperties_CandyTrail() => compClass = typeof(CompCandyTrail);
}

/// <summary>
/// 子个体机枪弹丸的薄荷色拖尾。
///
/// 由弹丸自带的 comp 每 tick 画一条「上一 tick 位置 → 当前位置」的连线，
/// 不再需要 Harmony 补丁 <c>Projectile.TickInterval</c>。
///
/// 注意：原版的 <c>Projectile.TickInterval</c> 是先 tick comps 再移动，
/// 所以这里拿到的 <c>parent.DrawPos</c> 是**移动前**的位置 ——
/// 拖尾在空间上是连续的，但末端会比弹丸本身落后一个 tick 的位移（speed 70 时约 1.2 格）。
/// 这是用 comp 驱动换掉补丁的代价。
/// </summary>
public class CompCandyTrail : ThingComp
{
    private const float TrailWidth = 0.035f;

    private Vector3 lastPosition;
    private int lastDrawnTick = -1;
    private bool started;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        lastPosition = parent.DrawPos;
        started = true;
    }

    public override void CompTick() => DrawTrail();

    public override void CompTickInterval(int delta)
    {
        if (delta > 0) DrawTrail();
    }

    private void DrawTrail()
    {
        // CompTick 与 CompTickInterval 可能同时被调用，同一 tick 只画一段。
        int tick = Find.TickManager?.TicksGame ?? 0;
        if (tick == lastDrawnTick) return;
        lastDrawnTick = tick;

        if (!parent.Spawned || parent.Map == null) return;

        Vector3 current = parent.DrawPos;
        if (started && current != lastPosition)
        {
            FleckMaker.ConnectingLine(lastPosition, current, MCW_DefOf.MCW_CandyTrail047, parent.Map, TrailWidth);
        }

        lastPosition = current;
        started = true;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref lastPosition, "candyTrailLast");
        Scribe_Values.Look(ref lastDrawnTick, "candyTrailTick", -1);
        Scribe_Values.Look(ref started, "candyTrailStarted");
    }
}
