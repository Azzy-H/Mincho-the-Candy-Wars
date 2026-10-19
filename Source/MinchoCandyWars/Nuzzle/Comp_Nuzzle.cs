using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Nuzzle
{
    /// <summary>
    /// 「撒娇」的 comp 配置 —— 挂在机械体身上，承载亲昵的内置冷却。
    ///
    /// XML 示例（写在 ThingDef 的 comps 里）:
    ///   &lt;li Class="MinchoCandyWars.Nuzzle.CompProperties_Nuzzle"&gt;
    ///     &lt;cooldownTicks&gt;60000&lt;/cooldownTicks&gt;
    ///     &lt;contactTicks&gt;30&lt;/contactTicks&gt;
    ///     &lt;maxDistance&gt;40&lt;/maxDistance&gt;
    ///     &lt;thoughtDef&gt;MCW_NuzzledByMech&lt;/thoughtDef&gt;
    ///   &lt;/li&gt;
    /// </summary>
    public class CompProperties_Nuzzle : CompProperties
    {
        /// <summary>两次亲昵之间的冷却（tick）。默认 60000 = 1 天。</summary>
        public int cooldownTicks = 60000;

        /// <summary>贴上去接触的时长（tick）。默认 30 = 0.5 秒。</summary>
        public int contactTicks = 30;

        /// <summary>只在这么远以内找目标（格）。</summary>
        public float maxDistance = 40f;

        /// <summary>亲昵成功后给目标加的心情想法。</summary>
        public ThoughtDef? thoughtDef;

        public CompProperties_Nuzzle()
        {
            compClass = typeof(Comp_Nuzzle);
        }
    }

    /// <summary>
    /// 机械体的「撒娇」冷却载体。
    /// 冷却记在机械体自己身上（随存档保存），所以每台机械体各自算冷却。
    ///
    /// 不驱动任何 Tick，只在 WorkGiver 询问 / JobDriver 结束时读写。
    /// </summary>
    public class Comp_Nuzzle : ThingComp
    {
        /// <summary>上一次亲昵的 tick；负数表示从没亲昵过。</summary>
        private int lastNuzzleTick = -999999;

        public CompProperties_Nuzzle Props => (CompProperties_Nuzzle)props;

        /// <summary>冷却是否已经好了。</summary>
        public bool Ready
        {
            get
            {
                TickManager? ticks = Find.TickManager;
                if (ticks == null)
                {
                    return false;
                }
                return ticks.TicksGame >= lastNuzzleTick + Props.cooldownTicks;
            }
        }

        /// <summary>还要等多少 tick 才冷却好（0 = 已就绪）。</summary>
        public int TicksUntilReady
        {
            get
            {
                TickManager? ticks = Find.TickManager;
                if (ticks == null)
                {
                    return 0;
                }
                return Mathf.Max(0, lastNuzzleTick + Props.cooldownTicks - ticks.TicksGame);
            }
        }

        /// <summary>刚亲昵完，开始计时冷却。</summary>
        public void Notify_Nuzzled()
        {
            TickManager? ticks = Find.TickManager;
            if (ticks != null)
            {
                lastNuzzleTick = ticks.TicksGame;
            }
        }

        // 1.7 的 ThingComp 用 PostExposeData（没有 ExposeData 可重写）
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastNuzzleTick, "lastNuzzleTick", -999999);
        }

        public override string CompInspectStringExtra()
        {
            if (Ready)
            {
                return "";
            }
            return "MCW_Nuzzle_Cooldown".Translate((TicksUntilReady / 2500f).ToString("F1"));
        }
    }
}
