using MinchoCandyWars.Buff;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.NectarCandy
{
    /// <summary>
    /// 心之甘露 —— 挂载在 Nectar 躯体5级的 Mincho 上。
    /// 仅在 PostAdd / Notify_Spawned 时向地图上的 GameCondition_MannaHeart 注册自己。
    /// 不负责注销 —— GameCondition 每 tick 自行清理无效条目。
    /// </summary>
    public class Hediff_MannaHeart : Hediff_Buff
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            RegisterSelf();
        }

        public override void Notify_Spawned()
        {
            base.Notify_Spawned();
            RegisterSelf();
        }

        private void RegisterSelf()
        {
            Map map = pawn.Map;
            if (map == null) return;

            GameCondition_MannaHeart cond = map.gameConditionManager.GetActiveCondition<GameCondition_MannaHeart>();
            if (cond == null)
            {
                cond = (GameCondition_MannaHeart)GameConditionMaker.MakeConditionPermanent(MCW_DefOf.MCW_MannaHeartCondition);
                map.GameConditionManager.RegisterCondition(cond);
            }

            cond.RegisterHediff(this);
        }
    }
}