using MinchoCandyWars.Gizmos;
using MinchoCandyWars.Interface;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Verse;

namespace MinchoCandyWars.Abilities
{
    public class MinchoCandyAbility : Ability, IInitalizable
    {
        public MinchoAbilityDefModExtension minchoAbilityDef = null!;
        private float requiredMinchoCandyValue => minchoAbilityDef.requiredMinchoCandyValue;
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
            }
        }

        /// <summary>
        /// 当前技能是否满足所有显示/使用条件。
        /// </summary>
        public bool IsAvailable => minchoAbilityDef?.IsAvailableFor(pawn) ?? false;

        //检查Gizmo是否禁用，如果糖果值不够则禁用
        public override bool GizmoDisabled(out string reason)
        {
            if (compMinchoCore.MinchoCandyValue < requiredMinchoCandyValue)
            {
                reason = "MinchoCandyWars.Abilities.MinchoCandyValueDontEnough".Translate(requiredMinchoCandyValue);
                return true;
            }
            return base.GizmoDisabled(out reason);
        }

        //检查是否显示Gizmo
        protected virtual bool ShouldShowGizmo()
        {
            return IsAvailable;
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
            return "MinchoCandyWars.Abilities.CandyValueConsume".Translate(requiredMinchoCandyValue);
        }

        //在Tooltip中显示消耗的糖果值
        public override string Tooltip
        {
            get
            {
                string text = base.Tooltip;
                text = text + "\n\n" + "MinchoCandyWars.Abilities.CandyValueConsume".Translate(requiredMinchoCandyValue);
                return text;
            }
        }
    }
}
