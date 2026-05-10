using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Ability.QianJiCandy.WorkbenchAcceleration;

//工作台加速的自comp，提供加速状态的维护和加速效果的计算
public class CompWorkbenchAcceleration : ThingComp
{
    private int acceleratedUntilTick = -1;
    private float accelerationFactor = 1f;

    private bool IsActive
    {
        get
        {
            CleanupExpiredState();
            return acceleratedUntilTick > Find.TickManager.TicksGame;
        }
    }

    private int RemainingTicks
    {
        get
        {
            CleanupExpiredState();
            return Mathf.Max(acceleratedUntilTick - Find.TickManager.TicksGame, 0);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref acceleratedUntilTick, "acceleratedUntilTick", -1);
        Scribe_Values.Look(ref accelerationFactor, "accelerationFactor", 1f);
    }

    public void ApplyAcceleration(int durationTicks, float factor)
    {
        if (durationTicks <= 0 || factor <= 1f)
        {
            return;
        }

        CleanupExpiredState();

        if (IsActive)
        {
            accelerationFactor = Mathf.Max(accelerationFactor, factor);
        }
        else
        {
            accelerationFactor = factor;
        }

        acceleratedUntilTick = Find.TickManager.TicksGame + durationTicks;
    }

    public override float GetStatFactor(StatDef stat)
    {
        if (stat != StatDefOf.WorkTableWorkSpeedFactor || !IsActive)
        {
            return 1f;
        }

        return accelerationFactor;
    }

    public override string CompInspectStringExtra()
    {
        if (!IsActive)
        {
            return string.Empty;
        }

        return "MinchoCandyWars.Ability.WorkbenchAcceleration.Inspect".Translate(
            accelerationFactor.ToStringPercent(),
            RemainingTicks.ToStringTicksToPeriod());
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
    {
        if (stat != StatDefOf.WorkTableWorkSpeedFactor || !IsActive)
        {
            return;
        }

        sb.AppendLine($"{whitespace}{"MinchoCandyWars.Ability.WorkbenchAcceleration.StatExplanation".Translate()}");
    }

    private void CleanupExpiredState()
    {
        if (acceleratedUntilTick > Find.TickManager.TicksGame)
        {
            return;
        }

        acceleratedUntilTick = -1;
        accelerationFactor = 1f;
    }
}

public class CompProperties_WorkbenchAcceleration : CompProperties
{
    public CompProperties_WorkbenchAcceleration()
    {
        compClass = typeof(CompWorkbenchAcceleration);
    }
}
