using Verse;

namespace MinchoCandyWars.Golem
{
    /// <summary>机械傀儡体内藏着的原尸体。</summary>
    public class CompProperties_GolemRemains : CompProperties
    {
        public CompProperties_GolemRemains() => compClass = typeof(CompGolemRemains);
    }
    /// <summary>
    /// 持有被收进体内的原机械傀儡尸体。
    ///
    /// 释放时机是「持有者死亡的那一刻」（<see cref="Notify_Killed"/>），不是等尸体被销毁：
    /// 原版迷途傀儡的死亡动作会销毁自己的尸体（防止玩家回收），
    /// 而普通尸体也会因腐烂/被删除而销毁 —— 提前释放就不依赖任何一条销毁路径。
    /// </summary>
    public class CompGolemRemains : ThingComp, IThingHolder
    {
        private ThingOwner<Thing> remains;

        public CompGolemRemains() => remains = new ThingOwner<Thing>(this);

        public bool HasRemains => remains.Count > 0;

        IThingHolder IThingHolder.ParentHolder => (IThingHolder)parent;

        public ThingOwner GetDirectlyHeldThings() => remains;

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, remains);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref remains, "mcwMechanicalRemains", this);
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo)
        {
            base.Notify_Killed(prevMap, dinfo);
            if (HasRemains) Release(prevMap, parent.PositionHeld);
        }

        /// <summary>把原尸体放回地面；放不下则继续留在体内，避免凭空丢失。</summary>
        public bool Release(Map map, IntVec3 cell)
        {
            if (map == null || !HasRemains) return false;

            Thing corpse = remains[0];
            remains.Remove(corpse);
            if (GenPlace.TryPlaceThing(corpse, cell, map, ThingPlaceMode.Near)) return true;

            remains.TryAdd(corpse);
            return false;
        }
    }
}
