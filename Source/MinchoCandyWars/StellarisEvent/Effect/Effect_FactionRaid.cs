using UnityEngine;
using Verse;
using RimWorld;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 产生一场指定派系的袭击。
    /// 使用 RimWorld 原生的袭击机制，支持自定义派系、点数、单位种类、到达方式等。
    ///
    /// 派系来源优先级: factionFlagName (String flag) > factionDef > 报错
    ///
    /// XML 示例 1 —— 直接指定派系:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_FactionRaid"&gt;
    ///     &lt;factionDef&gt;ARA_MinchoCandyWars&lt;/factionDef&gt;
    ///     &lt;points&gt;500&lt;/points&gt;
    ///   &lt;/li&gt;
    ///
    /// XML 示例 2 —— 从 flag 读取派系（需先在 immediate 中设置 flag）:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_FactionRaid"&gt;
    ///     &lt;factionFlagName&gt;enemyFaction&lt;/factionFlagName&gt;
    ///     &lt;points&gt;1000&lt;/points&gt;
    ///     &lt;arrivalMode&gt;CenterDrop&lt;/arrivalMode&gt;
    ///   &lt;/li&gt;
    ///
    /// XML 示例 3 —— 指定单一单位种类的袭击:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_FactionRaid"&gt;
    ///     &lt;factionDef&gt;Mechanoid&lt;/factionDef&gt;
    ///     &lt;pawnKindDef&gt;Mech_Centipede&lt;/pawnKindDef&gt;
    ///     &lt;points&gt;2000&lt;/points&gt;
    ///   &lt;/li&gt;
    ///
    /// XML 示例 4 —— 完整自定义:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_FactionRaid"&gt;
    ///     &lt;factionDef&gt;Pirate&lt;/factionDef&gt;
    ///     &lt;points&gt;800&lt;/points&gt;
    ///     &lt;pawnKindDef&gt;Pirate&lt;/pawnKindDef&gt;
    ///     &lt;arrivalMode&gt;EdgeWalkIn&lt;/arrivalMode&gt;
    ///     &lt;canTimeoutOrFlee&gt;false&lt;/canTimeoutOrFlee&gt;
    ///     &lt;customLetterLabel&gt;海盗复仇&lt;/customLetterLabel&gt;
    ///     &lt;customLetterText&gt;海盗们卷土重来，这次他们不打算撤退。&lt;/customLetterText&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_FactionRaid : StellarisEventEffect
    {
        /// <summary>派系定义（直接指定）</summary>
        public FactionDef? factionDef;

        /// <summary>
        /// 可选：从 String flag 中读取派系 defName。
        /// 若指定了此字段，将覆盖 factionDef 直接指定的值。
        /// flag 值应为 FactionDef 的 defName 字符串。
        /// </summary>
        public string? factionFlagName;

        /// <summary>
        /// 袭击点数。负数表示使用当前故事叙述者难度下的默认威胁点数。
        /// 点数决定袭击规模（人数、装备质量等）。
        /// </summary>
        public float points = -1f;

        /// <summary>
        /// 指定袭击单位种类。非空则整场袭击仅由该种类单位组成。
        /// 为空则按派系正常 PawnGroup 生成混合单位。
        /// </summary>
        public PawnKindDef? pawnKindDef;

        /// <summary>
        /// 到达方式 defName（如 "EdgeWalkIn", "CenterDrop", "RandomDrop", "EdgeDrop" 等）。
        /// 为空则随机选择。
        /// </summary>
        public string? arrivalMode;

        /// <summary>
        /// 袭击是否可以因时间过长而自行撤退。默认 true。
        /// 设为 false 表示死战不退。
        /// </summary>
        public bool canTimeoutOrFlee = true;

        /// <summary>
        /// 自定义通知标题。支持 {key} 占位（由 immediate 阶段的 localParams 注入）。
        /// 为空则使用默认标题。
        /// </summary>
        public string? customLetterLabel;

        /// <summary>
        /// 自定义通知文本。支持 {key} 占位（由 immediate 阶段的 localParams 注入）。
        /// 为空则使用默认文本。
        /// </summary>
        public string? customLetterText;

        // ──────────────────── Execute ────────────────────

        public override void Execute()
        {
            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map == null)
            {
                Log.Warning("[StellarisEvent] Effect_FactionRaid: 未找到玩家殖民地地图");
                return;
            }

            // ── 解析派系 ──
            Faction faction = ResolveFaction();
            if (faction == null)
            {
                Log.Warning("[StellarisEvent] Effect_FactionRaid: 无法解析派系 —— 请检查 factionDef 或 factionFlagName");
                return;
            }

            // ── 计算点数 ──
            float raidPoints = points;
            if (raidPoints < 0f)
            {
                raidPoints = StorytellerUtility.DefaultThreatPointsNow(map);
            }
            raidPoints = Mathf.Max(raidPoints, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));

            // ── 构建 IncidentParms ──
            IncidentParms parms = new IncidentParms();
            parms.target = map;
            parms.faction = faction;
            parms.points = raidPoints;
            parms.forced = true;
            parms.canTimeoutOrFlee = canTimeoutOrFlee;

            // ── 单一种类袭击 ──
            if (pawnKindDef != null)
            {
                parms.pawnKind = pawnKindDef;
                parms.pawnCount = Mathf.Max(1, Mathf.RoundToInt(raidPoints / pawnKindDef.combatPower));
            }

            // ── 自定义通知 ──
            if (!customLetterLabel.NullOrEmpty())
                parms.customLetterLabel = customLetterLabel;
            if (!customLetterText.NullOrEmpty())
                parms.customLetterText = customLetterText;

            // ── 到达方式 ──
            IncidentWorker_Raid raidWorker = (IncidentWorker_Raid)IncidentDefOf.RaidEnemy.Worker;
            if (!arrivalMode.NullOrEmpty())
            {
                var modeDef = DefDatabase<PawnsArrivalModeDef>.GetNamedSilentFail(arrivalMode);
                if (modeDef != null)
                    parms.raidArrivalMode = modeDef;
            }

            // ── 执行袭击 ──
            // TryExecute 内部会处理: 袭击策略、到达方式（未指定时随机）、年龄限制、生成单位、发送通知
            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                Log.Warning($"[StellarisEvent] Effect_FactionRaid: 袭击执行失败 —— 派系={faction.Name}, 点数={raidPoints}");
            }
        }

        // ──────────────────── 派系解析 ────────────────────

        /// <summary>
        /// 解析派系：优先从 factionFlagName 对应的 String flag 读取，
        /// 其次使用 factionDef 直接指定的值。
        /// </summary>
        private Faction ResolveFaction()
        {
            // Priority 1: 从 String flag 读取派系 defName
            if (!factionFlagName.NullOrEmpty())
            {
                var flag = StellarisEventFlagManager.GetFlag(factionFlagName!);
                if (flag != null && flag.valueType == FlagValueType.String && !flag.stringValue.NullOrEmpty())
                {
                    var def = DefDatabase<FactionDef>.GetNamedSilentFail(flag.stringValue);
                    if (def != null)
                    {
                        var faction = Find.FactionManager.FirstFactionOfDef(def);
                        if (faction != null)
                            return faction;
                    }
                }
            }

            // Priority 2: 使用 factionDef 直接指定
            if (factionDef != null)
                return Find.FactionManager.FirstFactionOfDef(factionDef);

            return null!;
        }
    }
}
