using MinchoCandyWars.Buff;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.LinglonCandy
{
    /// <summary>
    /// 玲珑糖心灵光环——躯体5级被动，向地图 GameCondition_LinglonAura 注册自己。
    /// 不负责注销，GameCondition 每 tick 自行清理无效条目（参考 Hediff_MannaHeart）。
    /// </summary>
    public class Hediff_LinglonAura : Hediff_Buff
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

            GameCondition_LinglonAura cond = map.gameConditionManager.GetActiveCondition<GameCondition_LinglonAura>();
            if (cond == null)
            {
                cond = (GameCondition_LinglonAura)GameConditionMaker.MakeConditionPermanent(MCW_DefOf.MCW_LinglonAuraCondition);
                map.GameConditionManager.RegisterCondition(cond);
            }

            cond.RegisterHediff(this);
        }
    }
}
