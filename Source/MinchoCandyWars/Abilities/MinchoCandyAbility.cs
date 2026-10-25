using MinchoCandyWars.Interface;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
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

        // 这些成员在 Initialize() 里缓存；若在初始化之前就被查询（例如 UI 预览），退回按需解析。
        private CompMinchoCore? Core => compMinchoCore ?? pawn?.GetComp<CompMinchoCore>();
        private MinchoAbilityDefModExtension? Extension => minchoAbilityDef ?? def?.GetModExtension<MinchoAbilityDefModExtension>();

        /// <summary>
        /// 是否可以施放：已激活 + 技能对当前糖饰有效 + 糖果值足够。
        /// </summary>
        private bool IsReady => Core?.Active == true
            && Extension != null
            && Extension.IsAvailableFor(pawn)
            && Core.MinchoCandyValue >= Extension.requiredMinchoCandyValue;

        /// <summary>
        /// 原版 CanCast 只判断充能和冷却，这里补上本模组的条件。
        /// 原版已经拒绝时保留原版的原因，不覆盖。
        /// </summary>
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport report = base.CanCast;
                if (!report.Accepted) return report;

                return IsReady ? report : "MinchoCandyWars.UI.Ability.ConditionNotMet".Translate();
            }
        }

        /// <summary>兜底：绕过 Gizmo 直接调用 Activate 时同样不放行（两个重载都要拦）。</summary>
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
            => IsReady && base.Activate(target, dest);

        public override bool Activate(GlobalTargetInfo target)
            => IsReady && base.Activate(target);

        //检查Gizmo是否禁用，如果糖果值不够则禁用
        public override bool GizmoDisabled(out string reason)
        {
            if (compMinchoCore.MinchoCandyValue < requiredMinchoCandyValue)
            {
                reason = "MinchoCandyWars.Abilities.MinchoCandyValueDontEnough".Translate(requiredMinchoCandyValue.Named("VALUE"));
                return true;
            }
            return base.GizmoDisabled(out reason);
        }

        //检查是否显示Gizmo
        protected virtual bool ShouldShowGizmo()
        {
            return IsAvailable && compMinchoCore.ShowAbilityGizmos;
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
                gizmo = (Command)Activator.CreateInstance(def.gizmoClass, this, pawn)!;
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

        //在Tooltip中显示消耗的糖果值
        public override string Tooltip
        {
            get
            {
                string text = base.Tooltip;
                text = text + "\n\n" + "MinchoCandyWars.Abilities.CandyValueConsume".Translate(requiredMinchoCandyValue.Named("VALUE"));
                return text;
            }
        }
    }
}
