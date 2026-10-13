using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 改变某个派系与玩家派系的外交关系（中立 / 同盟 / 敌对），可选"永久敌对"。
    ///
    /// 这是 Mincho 侧对 StellarisEvent 框架的扩展（框架本身没有外交相关的 Effect）。
    /// 放在 StellarisEventExt 目录下，因此不会影响 StellarisEvent 目录与 ProdrakeRace 的同步。
    ///
    /// 实现要点：
    /// - RimWorld 里关系类型由「好感度」驱动：好感度 &gt;= 75 → 同盟，&lt;= -75 → 敌对，否则中立
    ///   （见 FactionRelation.CheckKindThresholds）。所以对有外交好感度的派系，
    ///   正确做法是改好感度，而不是直接写关系类型。
    /// - Faction.SetRelationDirect 在「双方都使用好感度」时会直接 Log.Error 并返回，
    ///   因此只有隐藏派系 / 永久敌对派系（HasGoodwill == false）才走这条路。
    /// - permanent=true 时额外把 FactionDef.permanentEnemy 置为 true，使其无法再被改善关系。
    ///   Def 上的改动不参与存档，所以同时写一个持久 flag，
    ///   由 MCW_PermanentEnemyRestorer 在读档后重新施加。
    ///
    /// XML 示例:
    ///   &lt;li Class="MinchoCandyWars.StellarisEvent.Effect_SetFactionRelation"&gt;
    ///     &lt;factionDef&gt;MCW_Alliance_Faction&lt;/factionDef&gt;
    ///     &lt;relation&gt;Hostile&lt;/relation&gt;
    ///     &lt;permanent&gt;true&lt;/permanent&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class Effect_SetFactionRelation : StellarisEventEffect
    {
        /// <summary>持久 flag 名前缀。凡以该前缀开头的 flag 都表示"该派系应被设为永久敌对"。</summary>
        public const string PermanentEnemyFlagPrefix = "MCW_PermanentEnemy_";

        /// <summary>目标派系</summary>
        public FactionDef? factionDef;

        /// <summary>目标关系：Neutral / Ally / Hostile</summary>
        public FactionRelationKind relation = FactionRelationKind.Neutral;

        /// <summary>是否设为永久敌对（置 FactionDef.permanentEnemy = true，并持久化）</summary>
        public bool permanent;

        /// <summary>关系变为敌对时是否发敌对信件</summary>
        public bool canSendHostilityLetter = true;

        /// <summary>好感度变化时是否弹提示消息</summary>
        public bool canSendMessage = true;

        public override void Execute()
        {
            if (factionDef == null)
            {
                Log.Warning("[StellarisEvent] Effect_SetFactionRelation: factionDef 为空，无操作");
                return;
            }

            Faction? faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null)
            {
                Log.Warning($"[StellarisEvent] Effect_SetFactionRelation: 找不到派系 {factionDef.defName}");
                return;
            }

            Faction player = Faction.OfPlayer;
            if (faction == player)
            {
                return;
            }

            // 先改关系，再置永久敌对：permanentEnemy 会关闭好感度改动通道。
            ApplyRelation(faction, player);

            if (permanent)
            {
                StellarisEventFlagManager.SetFlag(PermanentEnemyFlagPrefix + factionDef.defName, true);
                faction.def.permanentEnemy = true;
            }
        }

        private void ApplyRelation(Faction faction, Faction player)
        {
            // 关系记录缺失时（例如派系是运行时新建的）先补上，
            // 否则 GoodwillWith / RelationKindWith 等调用会出错。
            if (faction.RelationWith(player, allowNull: true) == null)
            {
                faction.TryMakeInitialRelationsWith(player);
            }

            // 双方都有好感度、且对方不是永久敌对派系时，关系类型由好感度决定。
            bool usesGoodwill = faction.HasGoodwill && player.HasGoodwill && !faction.def.permanentEnemy;

            if (!usesGoodwill)
            {
                faction.SetRelationDirect(player, relation, canSendHostilityLetter);
                return;
            }

            int targetGoodwill = relation switch
            {
                FactionRelationKind.Ally => 100,
                FactionRelationKind.Hostile => -100,
                _ => 0
            };

            int change = targetGoodwill - faction.PlayerGoodwill;
            if (change != 0 && !faction.TryAffectGoodwillWith(
                    player, change,
                    canSendMessage: canSendMessage,
                    canSendHostilityLetter: canSendHostilityLetter))
            {
                Log.Warning($"[StellarisEvent] Effect_SetFactionRelation: 无法改变与 {faction.def.defName} 的好感度"
                    + "（可能被任务锁定或该派系已被击败）");
                return;
            }

            // 好感度被自然好感度曲线截断时会到不了目标值，这里只提示，不强行写关系类型。
            FactionRelationKind actual = faction.RelationKindWith(player);
            if (actual != relation)
            {
                Log.Warning($"[StellarisEvent] Effect_SetFactionRelation: {faction.def.defName} 期望关系 {relation}，"
                    + $"实际为 {actual}（好感度 {faction.PlayerGoodwill}）");
            }
        }
    }
}
