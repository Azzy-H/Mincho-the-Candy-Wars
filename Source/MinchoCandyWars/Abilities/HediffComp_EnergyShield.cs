using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MinchoCandyWars.Abilities
{
    /// <summary>
    /// 护盾腰带式能量护盾 —— 作为 HediffComp 直接吸收伤害，不生成 MechShield 实体。
    /// 仅吸收远程/爆炸伤害，不干扰投射物碰撞判定。
    /// </summary>
    public abstract class HediffComp_EnergyShield : HediffComp
    {
        protected float energy;
        public int ticksToReset = -1;
        protected int lastKeepDisplayTick = -9999;
        private int lastAbsorbDamageTick = -9999;
        private Vector3 impactAngleVect;

        public abstract float EnergyMax { get; }
        protected abstract float EnergyGainPerTick { get; }
        public abstract int StartingTicksToReset { get; }
        protected abstract float EnergyOnReset { get; }

        public float Energy => energy;
        public int LastAbsorbDamageTick => lastAbsorbDamageTick;
        public Vector3 ImpactAngleVect => impactAngleVect;

        public bool ShieldActive => ticksToReset <= 0;
        public bool ShieldResetting => ticksToReset > 0;

        public bool ShouldDraw
        {
            get
            {
                if (!Pawn.Spawned || Pawn.Dead || Pawn.Downed) return false;
                if (Pawn.InAggroMentalState) return true;
                if (Pawn.Drafted) return true;
                if (Pawn.Faction.HostileTo(Faction.OfPlayer) && !Pawn.IsPrisoner) return true;
                if (Pawn.Faction == Faction.OfPlayer && Find.Selector.SingleSelectedThing == Pawn) return true;
                if (Find.TickManager.TicksGame < lastKeepDisplayTick + 1000) return true;
                if (ModsConfig.BiotechActive && Pawn.IsColonyMech && Find.Selector.SingleSelectedThing == Pawn) return true;
                return false;
            }
        }

        public void KeepDisplaying()
        {
            lastKeepDisplayTick = Find.TickManager.TicksGame;
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            energy = EnergyMax;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || Pawn.Dead || Pawn.Downed)
            {
                return;
            }

            if (ticksToReset > 0)
            {
                ticksToReset--;
                if (ticksToReset <= 0)
                {
                    Reset();
                }
                return;
            }

            energy += EnergyGainPerTick;
            if (energy > EnergyMax)
            {
                energy = EnergyMax;
            }
        }

        /// <summary>
        /// 由 Harmony patch 调用，在 Pawn.PreApplyDamage 中拦截伤害。
        /// </summary>
        public bool TryAbsorbDamage(ref DamageInfo dinfo)
        {
            if (!ShieldActive || Pawn == null)
            {
                return false;
            }

            if (dinfo.Def == DamageDefOf.EMP)
            {
                energy = 0f;
                Break();
                return true;
            }

            if (!dinfo.Def.ignoreShields && (dinfo.Def.isRanged || dinfo.Def.isExplosive))
            {
                energy -= dinfo.Amount;
                if (energy < 0f)
                {
                    Break();
                }
                else
                {
                    AbsorbedDamage(dinfo);
                }
                return true;
            }

            return false;
        }

        private void AbsorbedDamage(DamageInfo dinfo)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
            impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
            Vector3 loc = Pawn.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
            float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
            FleckMaker.Static(loc, Pawn.Map, FleckDefOf.ExplosionFlash, num);
            int num2 = (int)num;
            for (int i = 0; i < num2; i++)
            {
                FleckMaker.ThrowDustPuff(loc, Pawn.Map, Rand.Range(0.8f, 1.2f));
            }
            lastAbsorbDamageTick = Find.TickManager.TicksGame;
            KeepDisplaying();
        }

        private void Break()
        {
            if (Pawn.Spawned)
            {
                float scale = Mathf.Lerp(0.5f, 1.5f, energy / Mathf.Max(EnergyMax, 1f));
                EffecterDefOf.Shield_Break.SpawnAttached(Pawn, Pawn.MapHeld, scale);
                FleckMaker.Static(Pawn.TrueCenter(), Pawn.Map, FleckDefOf.ExplosionFlash, 12f);
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowDustPuff(
                        Pawn.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f),
                        Pawn.Map, Rand.Range(0.8f, 1.2f));
                }
            }
            energy = 0f;
            ticksToReset = StartingTicksToReset;
        }

        private void Reset()
        {
            if (Pawn.Spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
                FleckMaker.ThrowLightningGlow(Pawn.TrueCenter(), Pawn.Map, 3f);
            }
            ticksToReset = -1;
            energy = EnergyOnReset > 0 ? EnergyOnReset : EnergyMax;
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            ticksToReset = -1;
            energy = 0f;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref energy, "energy", 0f);
            Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn == null) yield break;
            if (Pawn.Faction != Faction.OfPlayer && !Pawn.RaceProps.IsMechanoid) yield break;
            if (Find.Selector.SingleSelectedThing != Pawn) yield break;

            yield return new Gizmo_EnergyShieldStatus_Comp
            {
                shield = this
            };
        }
    }

    [StaticConstructorOnStartup]
    public class Gizmo_EnergyShieldStatus_Comp : Gizmo
    {
        public HediffComp_EnergyShield shield = null!;

        private static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Gizmo_EnergyShieldStatus_Comp()
        {
            Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);

            Rect labelRect = inner;
            labelRect.height = inner.height / 2f;
            Text.Font = GameFont.Tiny;
            bool resetting = shield.ShieldResetting;
            Widgets.Label(labelRect, resetting ? "ShieldTimeToRecovery".Translate() : (TaggedString)shield.parent.LabelCap);

            Rect barRect = inner;
            barRect.yMin = inner.y + inner.height / 2f;
            float fillPercent;
            string valueText;

            if (resetting)
            {
                fillPercent = 1f - (float)shield.ticksToReset / Mathf.Max(shield.StartingTicksToReset, 1);
                valueText = shield.ticksToReset.ToStringTicksToPeriod();
            }
            else
            {
                fillPercent = shield.Energy / Mathf.Max(shield.EnergyMax, 1f);
                valueText = (shield.Energy * 100f).ToString("F0") + " / " + (shield.EnergyMax * 100f).ToString("F0");
            }

            Widgets.FillableBar(barRect, fillPercent, FullBarTex, EmptyBarTex, doBorder: false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, valueText);
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(inner, "ShieldPersonalTip".Translate());
            return new GizmoResult(GizmoState.Clear);
        }
    }
}