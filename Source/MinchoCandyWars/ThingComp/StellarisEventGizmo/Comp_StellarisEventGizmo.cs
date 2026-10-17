using System.Collections.Generic;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// ThingComp：提供多个 Gizmo 按钮，每个按钮点击后触发对应的 StellarisEvent。
    ///
    /// 移植自 ProdrakeRace 的 PDR.Comp_StellarisEventGizmo（Thing 版本），
    /// 供本 mod 的建筑（如冰淇淋通讯台）使用。
    ///
    /// flagAsCooldown 机制：
    /// - 若指定了 flagAsCooldown，Gizmo 将使用 Command_ActionWithCooldown 构建。
    /// - 存在同名 Float Flag 时，按钮禁用并显示冷却进度条。
    /// - Flag.floatValue = 冷却总时长（tick），Flag.remainingTicks = 剩余冷却。
    /// - Flag 到期被 StellarisEventFlagManager 自动移除后，按钮恢复可用。
    ///
    /// 禁用优先级（由高到低）：
    /// 1. Flag 冷却中 → 禁用 + 冷却进度条
    /// 2. 事件 trigger 不通过 → 禁用 + 条件不满足提示
    /// </summary>
    public class Comp_StellarisEventGizmo : ThingComp
    {
        public CompProperties_StellarisEventGizmo Props =>
            (CompProperties_StellarisEventGizmo)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent == null || parent.Destroyed)
            {
                yield break;
            }

            // 仅在单独选中该 Thing 时显示按钮
            if (Find.Selector.SingleSelectedThing != parent)
            {
                yield break;
            }

            if (Props.gizmos == null)
            {
                yield break;
            }

            foreach (StellarisEventGizmoEntry entry in Props.gizmos)
            {
                if (entry == null || entry.eventDef == null)
                {
                    continue;
                }

                // existFlag 条件：指定了则必须满足才显示
                if (!entry.existFlag.NullOrEmpty())
                {
                    StellarisEventFlag existFlag = StellarisEventFlagManager.GetFlag(entry.existFlag);
                    if (existFlag == null
                        || existFlag.valueType != FlagValueType.Bool
                        || existFlag.boolValue != entry.existFlagValue)
                    {
                        continue;
                    }
                }

                // 好感度门槛：低于阈值不显示
                if (entry.factionDef != null)
                {
                    Faction faction = Find.FactionManager.FirstFactionOfDef(entry.factionDef);
                    if (faction == null || faction.PlayerGoodwill < entry.minGoodwill)
                    {
                        continue;
                    }
                }

                // 浮点 flag 门槛（例如终局进度 >= 10）
                if (!entry.minFloatFlag.NullOrEmpty())
                {
                    StellarisEventFlag floatFlag = StellarisEventFlagManager.GetFlag(entry.minFloatFlag);
                    if (floatFlag == null
                        || floatFlag.valueType != FlagValueType.Float
                        || floatFlag.floatValue < entry.minFloatValue)
                    {
                        continue;
                    }
                }

                // 加载图标
                Texture2D icon = null;
                if (!entry.iconPath.NullOrEmpty())
                {
                    icon = ContentFinder<Texture2D>.Get(entry.iconPath, false);
                }

                // 捕获局部变量供 lambda 使用
                StellarisEventDef eventDef = entry.eventDef;
                string cooldownFlagKey = entry.flagAsCooldown;
                bool hasCooldown = !cooldownFlagKey.NullOrEmpty();

                Command command;

                if (hasCooldown)
                {
                    // ────── 带 Flag 冷却的 Command_ActionWithCooldown ──────
                    var cmd = new Command_ActionWithCooldown
                    {
                        defaultLabel = entry.label ?? eventDef.label ?? "Event",
                        defaultDesc = entry.desc ?? eventDef.description ?? "",
                        icon = icon ?? BaseContent.BadTex,
                        action = () =>
                        {
                            StellarisEventManager.FireEvent(eventDef);
                        },
                        cooldownPercentGetter = () => GetCooldownReadyPercent(cooldownFlagKey)
                    };
                    command = cmd;
                }
                else
                {
                    // ────── 无冷却的普通 Command_Action ──────
                    command = new Command_Action
                    {
                        defaultLabel = entry.label ?? eventDef.label ?? "Event",
                        defaultDesc = entry.desc ?? eventDef.description ?? "",
                        icon = icon ?? BaseContent.BadTex,
                        action = () =>
                        {
                            StellarisEventManager.FireEvent(eventDef);
                        }
                    };
                }

                // ────── 禁用判定 ──────

                // 优先级 1: Flag 冷却中
                if (hasCooldown && IsOnCooldown(cooldownFlagKey))
                {
                    string reason = GetCooldownDisabledReason(cooldownFlagKey);
                    command.Disable(reason);
                }
                // 优先级 2: 事件 trigger 不通过
                else if (!eventDef.CanTrigger())
                {
                    command.Disable("MCW_StellarisEventGizmo_CannotTrigger".Translate());
                }

                yield return command;
            }
        }

        // ──────────────────── 冷却 Flag 查询 ────────────────────

        /// <summary>
        /// 检查指定 key 的冷却 Flag 是否存在且有效的 Float 类型。
        /// </summary>
        private static bool IsOnCooldown(string flagKey)
        {
            if (flagKey.NullOrEmpty())
            {
                return false;
            }

            StellarisEventFlag flag = StellarisEventFlagManager.GetFlag(flagKey);
            if (flag == null)
            {
                return false;
            }
            if (flag.valueType != FlagValueType.Float)
            {
                return false;
            }

            // remainingTicks.HasValue == false → 永久 Flag → 永久锁定
            // remainingTicks > 0 → 冷却中
            return true;
        }

        /// <summary>
        /// 计算冷却就绪百分比（0 = 完全冷却, 1 = 就绪可用）。
        /// 用于 Command_ActionWithCooldown.cooldownPercentGetter。
        /// </summary>
        private static float GetCooldownReadyPercent(string flagKey)
        {
            if (flagKey.NullOrEmpty())
            {
                return 1f;
            }

            StellarisEventFlag flag = StellarisEventFlagManager.GetFlag(flagKey);
            if (flag == null || flag.valueType != FlagValueType.Float)
            {
                return 1f;
            }

            float totalTicks = flag.floatValue;
            if (totalTicks <= 0f)
            {
                return 1f;
            }

            if (!flag.remainingTicks.HasValue)
            {
                return 0f; // 永久 Flag，永远就绪为 0
            }

            float remaining = flag.remainingTicks.Value;
            if (remaining <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (remaining / totalTicks));
        }

        /// <summary>
        /// 获取冷却禁用原因文本（含剩余时间）。
        /// </summary>
        private static string GetCooldownDisabledReason(string flagKey)
        {
            StellarisEventFlag flag = StellarisEventFlagManager.GetFlag(flagKey);
            if (flag == null || flag.valueType != FlagValueType.Float)
            {
                return "";
            }

            if (!flag.remainingTicks.HasValue)
            {
                return "MCW_StellarisEventGizmo_CooldownPermanent".Translate();
            }

            int remainingTicks = flag.remainingTicks.Value;
            if (remainingTicks <= 0)
            {
                return "";
            }

            float remainingHours = remainingTicks / 2500f;
            return "MCW_StellarisEventGizmo_CooldownRemaining".Translate(
                remainingHours.ToString("F1"));
        }
    }
}
