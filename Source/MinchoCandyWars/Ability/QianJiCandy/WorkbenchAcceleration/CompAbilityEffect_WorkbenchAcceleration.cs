using RimWorld;
using Verse;

namespace MinchoCandyWars.Ability.QianJiCandy.WorkbenchAcceleration;

//技能触发效果，应用于工作台加速的技能效果组件，并且越高的等级提供越高的加成
public class CompAbilityEffect_WorkbenchAcceleration : CompAbilityEffect
{
    public CompProperties_AbilityEffect_WorkbenchAcceleration Props => (CompProperties_AbilityEffect_WorkbenchAcceleration)props;

    private CompMinchoCore? CompMinchoCore => parent.pawn.GetComp<CompMinchoCore>();

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        return target.Thing is Building_WorkTable workTable
            && workTable.Spawned
            && workTable.TryGetComp<CompWorkbenchAcceleration>() != null;
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (target.Thing is not Building_WorkTable workTable)
        {
            return;
        }

        CompWorkbenchAcceleration comp = workTable.TryGetComp<CompWorkbenchAcceleration>();
        if (comp == null)
        {
            return;
        }

        comp.ApplyAcceleration(GetDurationTicks(), GetAccelerationFactor());
        base.Apply(target, dest);
    }

    private int GetDurationTicks()
    {
        int bodyGrade = CompMinchoCore?.MinchoBodyGrade ?? 0;
        return Props.baseDurationTicks + Props.durationTicksPerBodyGrade * bodyGrade;
    }

    private float GetAccelerationFactor()
    {
        int coreGrade = CompMinchoCore?.MinchoCoreGrade ?? 0;
        return Props.baseAccelerationFactor + Props.accelerationFactorPerCoreGrade * coreGrade;
    }
}

public class CompProperties_AbilityEffect_WorkbenchAcceleration : CompProperties_AbilityEffect
{
    public int baseDurationTicks = 0;//基础持续时间
    public int durationTicksPerBodyGrade = 0;//每身体等级提供的额外时间
    public float baseAccelerationFactor = 1f;//基础加速因数
    public float accelerationFactorPerCoreGrade = 0f;//每核心等级提供的额外因数

    public CompProperties_AbilityEffect_WorkbenchAcceleration()
    {
        compClass = typeof(CompAbilityEffect_WorkbenchAcceleration);
    }
}
