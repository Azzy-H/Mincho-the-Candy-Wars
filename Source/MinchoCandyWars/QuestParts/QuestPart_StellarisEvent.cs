using RimWorld;
using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// 接收信号后触发 StellarisEvent 的 QuestPart。
    /// 当收到 inSignal 时，调用 StellarisEventManager.FireEvent 启动指定事件。
    /// </summary>
    public class QuestPart_StellarisEvent : QuestPart
    {
        public string? inSignal;
        public StellarisEventDef? eventDef;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal && eventDef != null)
            {
                StellarisEventManager.FireEvent(eventDef);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal!, "inSignal");
            Scribe_Defs.Look(ref eventDef, "eventDef");
        }

        public override void AssignDebugData()
        {
            base.AssignDebugData();
            inSignal = "DebugSignal" + Rand.Int;
        }
    }
}
