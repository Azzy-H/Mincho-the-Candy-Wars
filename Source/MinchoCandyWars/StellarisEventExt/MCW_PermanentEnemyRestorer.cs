using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 读档 / 重启游戏后重新施加"永久敌对"派系状态。
    ///
    /// 背景：FactionDef.permanentEnemy 是 Def 上的字段，而 Def 不参与存档 ——
    /// 同一个游戏进程内它一直是改过的值，但重启游戏读档时 Def 会被重新加载，
    /// 改动就丢了，派系会退回成"可以改善关系"。
    ///
    /// Effect_SetFactionRelation 在把某派系设为永久敌对时，会额外写一个
    /// 以 <see cref="Effect_SetFactionRelation.PermanentEnemyFlagPrefix"/> 开头的持久 flag
    /// （持久 flag 是随存档保存的），本组件在进入游戏后读这些 flag 并把 permanentEnemy 重新置为 true。
    ///
    /// 不需要在 XML 里注册 &lt;gameComponents&gt;：RimWorld 会自动实例化
    /// 所有非抽象 GameComponent 子类（Verse.Game.FillComponents）。
    /// </summary>
    public class MCW_PermanentEnemyRestorer : GameComponent
    {
        /// <summary>本次进入游戏后是否已经施加过</summary>
        private bool applied;

        public MCW_PermanentEnemyRestorer(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // 读档后需要重新施加（Def 的改动不随存档恢复）。
            // GameComponent 实例在同一会话内读另一个存档时会被复用，
            // 这里把标志清掉，保证换存档后也会重新施加一次。
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                applied = false;
            }
        }

        public override void GameComponentTick()
        {
            if (applied)
            {
                return;
            }

            if (Current.ProgramState != ProgramState.Playing || Find.FactionManager == null)
            {
                return;
            }

            applied = true;
            ApplyAll();
        }

        /// <summary>
        /// 扫描所有以永久敌对前缀命名的 flag，恢复对应派系的 permanentEnemy。
        /// </summary>
        public static void ApplyAll()
        {
            const string prefix = Effect_SetFactionRelation.PermanentEnemyFlagPrefix;

            foreach (StellarisEventFlag flag in StellarisEventFlagManager.AllFlags())
            {
                if (flag?.name == null || !flag.name.StartsWith(prefix))
                {
                    continue;
                }

                string defName = flag.name.Substring(prefix.Length);
                FactionDef? def = DefDatabase<FactionDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    Log.Warning($"[MCW] MCW_PermanentEnemyRestorer: 找不到 FactionDef '{defName}'（可能 mod 已移除）");
                    continue;
                }

                def.permanentEnemy = true;
            }
        }
    }
}
