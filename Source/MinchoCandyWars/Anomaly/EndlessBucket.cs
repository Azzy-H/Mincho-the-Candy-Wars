using System.Linq;
using MinchoCandyWars;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Anomaly;

/// <summary>
/// 无尽冰淇淋桶：每次食用给一份营养，且不消耗堆叠（取之不尽）。
///
/// 未解除精神影响时让人类食用者上瘾并陷入恍惚；解除影响后整桶被换成
/// <see cref="ReleasedBucket"/>（另一个 ThingDef），
/// 所以营养值是静态的、写在各自 Def 的 statBases 里，运行时不需要任何状态。
/// </summary>
public class EndlessBucket : ThingWithComps
{
    /// <summary>恍惚持续时间：1 天。</summary>
    private const int DazeTicks = 60000;

    /// <summary>
    /// 份量取自 Def 的 <c>statBases/Nutrition</c>（活跃 1.0 / 已解除 0.2），代码里不写数值。
    ///
    /// <c>numTaken = 0</c> 才是"无尽"的关键：不消耗堆叠。
    /// 副作用是 <c>MintAddiction.Notify_IngestedThing</c> 会因 <c>numTaken &lt;= 0</c> 提前返回，
    /// 所以进食效果由 <see cref="PostIngested"/> 手工处理。
    /// </summary>
    protected override void IngestedCalculateAmounts(Pawn ingester, float nutritionWanted,
        out int numTaken, out float nutritionIngested)
    {
        numTaken = 0;
        nutritionIngested = def.GetStatValueAbstract(StatDefOf.Nutrition);
    }

    protected override void PostIngested(Pawn p)
    {
        base.PostIngested(p);

        MintAddiction.Feed(p);

        if (ShouldIntoxicate(p))
        {
            EnsureAddiction(p);
            ApplyDaze(p);
        }
    }

    /// <summary>食用后是否让食用者上瘾。<see cref="ReleasedBucket"/> 覆写为 false。</summary>
    protected virtual bool ShouldIntoxicate(Pawn p) =>
        !MintAddiction.IsMincho(p)
        && p.RaceProps.Humanlike
        && !p.health.hediffSet.HasHediff(MCW_DefOf.MCW_MintFreedom);

    /// <summary>
    /// 桶没了，依赖就该解除 —— 无论是玩家用碎片解除、被拆掉、还是被烧掉。
    /// 解除交互本身也是"销毁旧桶 + 生成新桶"，逻辑集中在这里就够。
    /// Thing 没有 PostDestroy 钩子，Destroy 才是 public virtual。
    /// </summary>
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        ReleaseDependents(this);
        base.Destroy(mode);
    }

    private void EnsureAddiction(Pawn p)
    {
        if (p.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MintAddiction) is MintAddiction)
        {
            return;
        }

        var addiction = (MintAddiction)p.health.AddHediff(MCW_DefOf.MCW_MintAddiction);
        addiction.source = this;
    }

    /// <summary>把"冰淇淋恍惚"续满 1 天。Def 没挂 Disappears comp 时安静跳过。</summary>
    private static void ApplyDaze(Pawn p)
    {
        Hediff? daze = p.health.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MintDaze);
        if (daze == null)
        {
            daze = p.health.AddHediff(MCW_DefOf.MCW_MintDaze);
        }

        daze.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear = DazeTicks;
    }

    /// <summary>
    /// 解除所有"由这一桶造成"的依赖（别的桶、别的来源不动）。
    /// 覆盖全地图、商队与临时地图 —— 玩家可能把桶带上商队。
    /// </summary>
    private static void ReleaseDependents(Thing source)
    {
        foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_Alive)
        {
            if (p.health?.hediffSet.GetFirstHediffOfDef(MCW_DefOf.MCW_MintAddiction) is MintAddiction addiction
                && addiction.source == source)
            {
                Cure(p);
            }
        }
    }

    /// <summary>移除依赖与恍惚、清掉"难以下咽"记忆；非珉巧额外获得永久免疫。</summary>
    private static void Cure(Pawn p)
    {
        foreach (Hediff h in p.health.hediffSet.hediffs
                     .Where(h => h.def == MCW_DefOf.MCW_MintAddiction || h.def == MCW_DefOf.MCW_MintDaze)
                     .ToList())
        {
            p.health.RemoveHediff(h);
        }

        p.needs?.mood?.thoughts.memories.RemoveMemoriesOfDef(MCW_DefOf.MCW_MintDisgust);

        if (!MintAddiction.IsMincho(p) && !p.health.hediffSet.HasHediff(MCW_DefOf.MCW_MintFreedom))
        {
            p.health.AddHediff(MCW_DefOf.MCW_MintFreedom);
        }
    }
}
