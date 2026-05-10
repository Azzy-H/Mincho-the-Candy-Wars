using MinchoCandyWars.Gizmos;
using MinchoCandyWars.Interface;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Verse;

namespace MinchoCandyWars.Ability
{
    public class MinchoCandyAbility : RimWorld.Ability, IInitalizable
    {
        public MinchoAbilityDefModExtension minchoAbilityDef = null!;
        private int requiredMinchoCoreGrade => minchoAbilityDef.requiredMinchoCoreGrade;
        private int requiredMinchoBodyGrade => minchoAbilityDef.requiredMinchoBodyGrade;
        private CandyType candyType => minchoAbilityDef.candyType;
        private float requiredMinchoCandyValue => minchoAbilityDef.requiredMinchoCandyValue;
        private List<HediffDef> requiredHediffDefs => minchoAbilityDef.requiredHediffDefs;
        private CompMinchoCore compMinchoCore = null!;

        public MinchoCandyAbility()
        {
        }

        public MinchoCandyAbility(Pawn pawn) : base(pawn)
        {
        }

        public MinchoCandyAbility(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public MinchoCandyAbility(Pawn pawn, Precept sourcePrecept) : base(pawn, sourcePrecept)
        {
        }

        public MinchoCandyAbility(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def)
        {
        }

        //初始化数据
        void IInitalizable.Initialize()
        {
            minchoAbilityDef = def.GetModExtension<MinchoAbilityDefModExtension>();
            compMinchoCore = pawn.GetComp<CompMinchoCore>();
            if (minchoAbilityDef == null || compMinchoCore == null)
            {
                Log.ErrorOnce($"MinchoCandyWars: Ability {def.defName} is missing required MinchoAbilityDefModExtension or CompMinchoCore on pawn {pawn.LabelCap}.", 112421);
                pawn.abilities.RemoveAbility(this.def);
            }
        }

        //检查Gizmo是否禁用，如果糖果值不够则禁用
        public override bool GizmoDisabled(out string reason)
        {
            if (compMinchoCore.MinchoCandyValue < requiredMinchoCandyValue)
            {
                reason = "MinchoCandyWars.Ability.MinchoCandyValueDontEnough".Translate(requiredMinchoCandyValue);
                return true;
            }
            return base.GizmoDisabled(out reason);
        }

        //检查是否显示Gizmo
        private bool ShouldShowGizmo()
        {
            //检查核心等级和躯体等级
            if (compMinchoCore.MinchoCoreGrade < requiredMinchoCoreGrade || compMinchoCore.MinchoBodyGrade < requiredMinchoBodyGrade)
            {
                return false;
            }
            //检查糖饰种类
            if (compMinchoCore.CurrentCandyType != candyType)
            {
                return false;
            }
            //检查所需的Hediff,如果不需要hediff则不检查
            if (requiredHediffDefs.Count > 0)
            {
                foreach (var hediffDef in requiredHediffDefs)
                {
                    if (!pawn.health.hediffSet.HasHediff(hediffDef))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //控制Gizmo显示
        public override IEnumerable<Command> GetGizmos()
        {
            if (!ShouldShowGizmo())
            {
                yield break;
            }

            if (gizmo == null)
            {
                gizmo = new Command_MinchoAbility(this, pawn);
                gizmo.Order = def.uiOrder;
            }

            if (!pawn.Drafted || def.showWhenDrafted)
            {
                yield return gizmo;
            }

            if (SettingUtility.IsDebugMode() && OnCooldown && CanCooldown)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset cooldown",
                    action = delegate
                    {
                        ResetCooldown();//重置冷却
                        RemainingCharges = maxCharges;//重置次数
                    }
                };
            }
        }

        //控制额外Gizmo显示
        public override IEnumerable<Gizmo> GetGizmosExtra()
        {
            if (!ShouldShowGizmo())
            {
                yield break;
            }
            foreach (var gizmo in base.GetGizmosExtra())
            {
                yield return gizmo;
            }
        }

        //消耗对应的糖果值
        protected override void PreActivate(LocalTargetInfo? target)
        {
            base.PreActivate(target);
            compMinchoCore.MinchoCandyValue -= requiredMinchoCandyValue;
        }

        public string MinchoCandyValueConsumeText()
        {
            return "MinchoCandyWars.Ability.CandyValueConsume".Translate(requiredMinchoCandyValue);
        }

        //在Tooltip中显示消耗的糖果值
        public override string Tooltip
        {
            get
            {
                string text = base.Tooltip;
                text = text + "\n\n" + "MinchoCandyWars.Ability.CandyValueConsume".Translate(requiredMinchoCandyValue);
                return text;
            }
        }
    }
}
