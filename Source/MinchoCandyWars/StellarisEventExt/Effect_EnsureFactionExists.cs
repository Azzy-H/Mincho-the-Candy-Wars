using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 确保指定派系在世界上存在；不存在就当场创建一个，并与所有现有派系建立初始关系。
    ///
    /// 为什么需要：
    /// FactionDef.requiredCountAtGameStart 只在【世界生成】时生效。
    /// 如果玩家是在已有存档里加载本 mod（任务线仍会在落地判定后启动），
    /// 或者世界生成因为其它原因没有创建该派系，
    /// Find.FactionManager 里就查不到它，后续的
    /// Effect_SpawnPawnFromFlag（factionDef 解析为 null → 生成无派系 Pawn）
    /// 和 Effect_SetFactionRelation（直接警告并跳过）都会失效。
    /// 在事件的 immediate 最前面放一个本 Effect 就能保证后续步骤一定可用。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_EnsureFactionExists"&gt;
    ///     &lt;factionDef&gt;MCW_RecycleCorp_Faction&lt;/factionDef&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_EnsureFactionExists : StellarisEventEffect
    {
        /// <summary>要确保存在的派系</summary>
        public FactionDef? factionDef;

        /// <summary>是否创建为隐藏派系（默认 false，即正常出现在派系列表里）</summary>
        public bool hidden;

        public override void Execute()
        {
            if (factionDef == null)
            {
                Log.Warning("[StellarisEvent] Effect_EnsureFactionExists: factionDef 为空，无操作");
                return;
            }

            if (Find.FactionManager == null)
            {
                return;
            }

            if (Find.FactionManager.FirstFactionOfDef(factionDef) != null)
            {
                return;
            }

            Faction faction = FactionGenerator.NewGeneratedFaction(
                new FactionGeneratorParms(factionDef, hidden: hidden));

            Find.FactionManager.Add(faction);

            // 与所有已有派系建立双向初始关系（照抄 FactionGenerator.GenerateFactionsIntoWorld 的做法）。
            // 缺少关系记录会让 RelationWith / GoodwillWith 等调用出错。
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                if (other != faction)
                {
                    faction.TryMakeInitialRelationsWith(other);
                }
            }

            if (Prefs.DevMode)
            {
                Log.Message($"[StellarisEvent] Effect_EnsureFactionExists: 已创建派系 {factionDef.defName}"
                    + $"（与玩家关系 {faction.RelationKindWith(Faction.OfPlayer)}）");
            }
        }
    }
}
