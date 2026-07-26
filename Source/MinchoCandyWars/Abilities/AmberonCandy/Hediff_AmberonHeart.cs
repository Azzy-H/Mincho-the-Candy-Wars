using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy
{
    /// <summary>
    /// 琥珀糖心 —— 30秒无敌、不可阻挡、靠近敌人减速。
    /// 无敌由 HediffComp_AmberonHeartInvincible 处理。
    /// </summary>
    public class Hediff_AmberonHeart : HediffWithComps
    {
        private CompMinchoCore? _cachedCore;
        private CompMinchoCore Core
        {
            get
            {
                if (_cachedCore == null) _cachedCore = pawn.GetComp<CompMinchoCore>();
                return _cachedCore;
            }
        }

        public override bool ShouldRemove
        {
            get
            {
                if (pawn.Dead) return true;
                HediffComp_Disappears? disappear = this.TryGetComp<HediffComp_Disappears>();
                if (disappear != null && disappear.CompShouldRemove) return true;
                return false;
            }
        }
    }
}
