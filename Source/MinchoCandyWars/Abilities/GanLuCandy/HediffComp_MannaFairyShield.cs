using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    public class HediffCompProperties_MannaFairyShield : HediffCompProperties
    {
        public ThingDef? shieldThingDef;

        public int hitpoints = 200;

        public int hitpointsOnReset = 40;

        public int startingTicksToReset = 7200;

        public int empResetTicks = 7200;

        public int checkIntervalTicks = 30;

        public HediffCompProperties_MannaFairyShield()
        {
            compClass = typeof(HediffComp_MannaFairyShield);
        }
    }

    /// <summary>
    /// 甘露妖精护盾 —— 基于 DMS PersonalMechShield 架构。
    /// 生成一个跟随 pawn 的 MechShield，护盾值 = 清凉度上限。
    /// 护盾被打破后进入 CD。带 Gizmo 显示护盾状态。
    /// </summary>
    public class HediffComp_MannaFairyShield : HediffComp
    {
        private MechShield? mechShield;

        private int ticksToNextCheck;

        private int ticksToReset = -1;

        private int resetDurationTicks;

        public HediffCompProperties_MannaFairyShield Props => (HediffCompProperties_MannaFairyShield)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            EnsureShield();
        }

        public override void Notify_Spawned()
        {
            base.Notify_Spawned();
            EnsureShield();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (ticksToReset > 0)
            {
                ticksToReset--;
                if (ticksToReset <= 0)
                {
                    SpawnShieldWithHitpoints(Props.hitpointsOnReset > 0 ? Props.hitpointsOnReset : Props.hitpoints);
                }
                return;
            }

            ticksToNextCheck--;
            if (ticksToNextCheck > 0)
                return;

            ticksToNextCheck = Props.checkIntervalTicks > 0 ? Props.checkIntervalTicks : 30;

            Pawn shieldTarget = GetShieldTarget();
            if (shieldTarget == null) return;

            if (mechShield == null || mechShield.Destroyed)
            {
                SpawnShieldWithHitpoints(GetMaxHitPoints());
                return;
            }

            if (!mechShield.IsTargeting(shieldTarget))
            {
                mechShield.SetTarget(shieldTarget);
            }

            CompProjectileInterceptor interceptor = mechShield.GetComp<CompProjectileInterceptor>();
            if (interceptor == null || !interceptor.Active || interceptor.currentHitPoints <= 0)
            {
                StartReset(Props.startingTicksToReset);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn == null) yield break;
            if (!ShouldDisplayGizmo) yield break;
            if (mechShield == null || mechShield.Destroyed) yield break;

            yield return new Gizmo_MannaFairyShieldHitPoints
            {
                currentHitPoints = CurrentShieldHitPoints,
                hitPointsMax = MaxShieldHitPoints,
                resetTicksLeft = ticksToReset,
                resetTicksTotal = resetDurationTicks
            };
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Def == DamageDefOf.EMP)
            {
                StartReset(Props.empResetTicks > 0 ? Props.empResetTicks : Props.startingTicksToReset);
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            DestroyShield();
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff? culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            DestroyShield();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref mechShield, "mechShield");
            Scribe_Values.Look(ref ticksToNextCheck, "ticksToNextCheck", 0);
            Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
            Scribe_Values.Look(ref resetDurationTicks, "resetDurationTicks", 0);
        }

        private void EnsureShield()
        {
            SpawnShieldWithHitpoints(GetMaxHitPoints());
        }

        private int GetMaxHitPoints()
        {
            Pawn target = GetShieldTarget();
            if (target == null) return Props.hitpoints;

            CompMinchoCore core = target.GetComp<CompMinchoCore>();
            if (core != null)
            {
                return (int)core.CurrentMaxCandyValue;
            }
            return Props.hitpoints;
        }

        private Pawn GetShieldTarget()
        {
            return Pawn;
        }

        private void SpawnShieldWithHitpoints(int initialHitpoints)
        {
            Pawn target = GetShieldTarget();
            if (target == null || target.Dead || target.MapHeld == null || !target.Spawned || ticksToReset > 0)
                return;

            if (mechShield != null && !mechShield.Destroyed)
            {
                if (!mechShield.IsTargeting(target))
                    mechShield.SetTarget(target);
                return;
            }

            ThingDef shieldDef = Props.shieldThingDef ?? ThingDefOf.MechShield;
            Thing spawned = GenSpawn.Spawn(shieldDef, target.PositionHeld, target.MapHeld);
            if (target.Faction != null && spawned.def.CanHaveFaction)
            {
                spawned.SetFaction(target.Faction);
            }
            mechShield = spawned as MechShield;
            if (mechShield == null)
            {
                spawned.Destroy();
                return;
            }
            mechShield.SetTarget(target);

            CompProjectileInterceptor interceptor = mechShield.GetComp<CompProjectileInterceptor>();
            if (interceptor != null)
            {
                interceptor.maxHitPointsOverride = GetMaxHitPoints();
                interceptor.currentHitPoints = initialHitpoints > 0 ? initialHitpoints : GetMaxHitPoints();
                interceptor.Activate();
            }
        }

        private void StartReset(int resetTicks)
        {
            int duration = resetTicks > 0 ? resetTicks : Props.startingTicksToReset;
            DestroyShield();
            ticksToReset = duration > 0 ? duration : 1;
            resetDurationTicks = ticksToReset;
        }

        private void DestroyShield()
        {
            if (mechShield != null && !mechShield.Destroyed)
            {
                mechShield.Destroy();
            }
            mechShield = null;
        }

        private int CurrentShieldHitPoints
        {
            get
            {
                if (mechShield == null || mechShield.Destroyed) return 0;
                CompProjectileInterceptor interceptor = mechShield.GetComp<CompProjectileInterceptor>();
                if (interceptor == null || !interceptor.Active) return 0;
                return interceptor.currentHitPoints > 0 ? interceptor.currentHitPoints : 0;
            }
        }

        private int MaxShieldHitPoints => Props.hitpoints > 0 ? Props.hitpoints : 1;

        private bool ShouldDisplayGizmo
        {
            get
            {
                if (Pawn == null) return false;
                return Pawn.IsColonistPlayerControlled || Pawn.RaceProps.IsMechanoid;
            }
        }
    }

    public class Gizmo_MannaFairyShieldHitPoints : Gizmo
    {
        public int currentHitPoints;
        public int hitPointsMax = 1;
        public int resetTicksLeft = -1;
        public int resetTicksTotal;

        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Gizmo_MannaFairyShieldHitPoints()
        {
            Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect innerRect = outerRect.ContractedBy(6f);
            Widgets.DrawWindowBackground(outerRect);

            bool inRecovery = resetTicksLeft > 0;
            TaggedString label = inRecovery ? "ShieldTimeToRecovery".Translate() : "ShieldEnergy".Translate();
            float fillPercent;
            string valueText;

            if (inRecovery)
            {
                int total = resetTicksTotal > 0 ? resetTicksTotal : 1;
                fillPercent = Mathf.Clamp01((float)resetTicksLeft / total);
                valueText = resetTicksLeft.ToStringTicksToPeriod();
            }
            else
            {
                int max = hitPointsMax > 0 ? hitPointsMax : 1;
                int current = currentHitPoints > 0 ? currentHitPoints : 0;
                fillPercent = Mathf.Clamp01((float)current / max);
                valueText = current + " / " + max;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect labelRect = new Rect(innerRect.x, innerRect.y - 2f, innerRect.width, innerRect.height / 2f);
            Widgets.Label(labelRect, label);

            Rect barRect = new Rect(innerRect.x, labelRect.yMax, innerRect.width, innerRect.height / 2f);
            Widgets.FillableBar(barRect, fillPercent, FullBarTex, EmptyBarTex, doBorder: false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, valueText);
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}