using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 盾牌举盾 / 背盾系统（移植自 OrdinulDragonului 的征召举盾系统，已去除方向盾与镀金武器依赖）
    ///
    /// 全自动运作，无手动切换：
    ///   征召中 + 举盾状态 → 挂 hediffWhenDrafted（防御加成 + 正面盾牌贴图）
    ///   其余情况（非征召 / 已背盾） → 挂 hediffWhenUndrafted（仅背面盾牌贴图）
    /// 举盾时提供的防御能力由 hediffWhenDrafted 的 stages/statOffsets 决定。
    /// </summary>
    public class CompProperties_MCWShieldApparel : CompProperties
    {
        public HediffDef? hediffWhenDrafted;
        public HediffDef? hediffWhenUndrafted;

        /// <summary>非玩家派系是否始终视为举盾（忽略征召状态）</summary>
        public bool nonPlayerAlwaysShielded = true;

        public CompProperties_MCWShieldApparel()
        {
            compClass = typeof(Comp_MCWShieldApparel);
        }
    }

    public class Comp_MCWShieldApparel : ThingComp
    {
        private Pawn? wearer;
        private bool wasDraftedLastCheck;

        /// <summary>盾牌是否处于举起状态（默认举起）</summary>
        public bool shieldRaised = true;

        public CompProperties_MCWShieldApparel Props => (CompProperties_MCWShieldApparel)props;

        public bool ShieldRaised
        {
            get
            {
                if (Props.nonPlayerAlwaysShielded && wearer != null && wearer.Faction != Faction.OfPlayer)
                    return true;
                return shieldRaised;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref wearer, "wearer");
            Scribe_Values.Look(ref wasDraftedLastCheck, "wasDraftedLastCheck");
            Scribe_Values.Look(ref shieldRaised, "shieldRaised", true);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            TryUnequipExistingShield(pawn);
            wearer = pawn;
            wasDraftedLastCheck = pawn.Drafted;
            UpdateShieldState();
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveShieldHediffs(pawn);
            wearer = null;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (wearer == null || wearer.Dead || !wearer.Spawned)
                return;

            if (Props.nonPlayerAlwaysShielded && wearer.Faction != Faction.OfPlayer)
                return;

            if (wearer.Drafted != wasDraftedLastCheck)
            {
                wasDraftedLastCheck = wearer.Drafted;
                UpdateShieldState();
            }
        }

        /// <summary>确保同一时间只持有一个本系统的盾牌（服装或武器）。</summary>
        private void TryUnequipExistingShield(Pawn pawn)
        {
            if (pawn.apparel?.WornApparel != null)
            {
                for (int i = pawn.apparel.WornApparel.Count - 1; i >= 0; i--)
                {
                    Apparel a = pawn.apparel.WornApparel[i];
                    if (a == parent) continue;
                    if (a.TryGetComp<Comp_MCWShieldApparel>() != null)
                        pawn.apparel.TryDrop(a, out _, pawn.Position);
                }
            }

            if (pawn.equipment?.Primary != null && pawn.equipment.Primary != parent)
            {
                if (pawn.equipment.Primary.TryGetComp<Comp_MCWShieldApparel>() != null)
                    pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out _, pawn.Position);
            }
        }

        /// <summary>按征召状态刷新 Hediff。</summary>
        public void UpdateShieldState()
        {
            if (wearer == null || wearer.health == null || wearer.health.hediffSet == null)
                return;

            RemoveShieldHediffs(wearer);

            if (Props.nonPlayerAlwaysShielded && wearer.Faction != Faction.OfPlayer)
            {
                AddHediffIfValid(Props.hediffWhenDrafted);
                return;
            }

            if (wearer.Drafted && shieldRaised)
            {
                AddHediffIfValid(Props.hediffWhenDrafted);
            }
            else
            {
                AddHediffIfValid(Props.hediffWhenUndrafted);
            }
        }

        private void AddHediffIfValid(HediffDef? hediffDef)
        {
            if (hediffDef != null && wearer != null && !wearer.health.hediffSet.HasHediff(hediffDef))
            {
                wearer.health.AddHediff(hediffDef);
            }
        }

        private void RemoveShieldHediffs(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null)
                return;

            if (Props.hediffWhenDrafted != null)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffWhenDrafted);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }

            if (Props.hediffWhenUndrafted != null)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffWhenUndrafted);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }
}
