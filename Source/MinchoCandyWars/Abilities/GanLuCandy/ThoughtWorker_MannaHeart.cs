using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.GanLuCandy
{
    /// <summary>
    /// 检测地图上是否存在 GameCondition_MannaHeart，给予 +5 心情。
    /// </summary>
    public class ThoughtWorker_MannaHeart : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            Map map = p.MapHeld;
            if (map == null)
                return ThoughtState.Inactive;

            GameCondition_MannaHeart cond = map.gameConditionManager.GetActiveCondition<GameCondition_MannaHeart>();
            if (cond != null)
                return ThoughtState.ActiveDefault;

            return ThoughtState.Inactive;
        }
    }
}