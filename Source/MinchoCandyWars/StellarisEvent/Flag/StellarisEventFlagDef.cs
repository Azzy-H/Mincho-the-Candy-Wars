using Verse;

namespace MinchoCandyWars.StellarisEvent
{
    /// <summary>
    /// Flag 支持的数据类型
    /// </summary>
    public enum FlagValueType
    {
        Float,
        String,
        Bool,
        Thing
    }

    /// <summary>
    /// Thing 类型 flag 的追踪状态
    /// </summary>
    public enum ThingFlagStatus
    {
        /// <summary>Thing 存在且已生成在地图上</summary>
        Normal,
        /// <summary>Thing 存在但已从地图移除（如在远行队中）</summary>
        Despawn,
        /// <summary>Thing 已死亡、被销毁或无法检索</summary>
        DiedOrDisappeared
    }

    /// <summary>
    /// StellarisEvent Flag 数据记录器。
    /// 包含名字、类型化数据内容和可选的剩余 tick（到期后自动移除）。
    /// </summary>
    public class StellarisEventFlag : IExposable
    {
        /// <summary>Flag 唯一标识名</summary>
        public string? name;

        /// <summary>数据类型</summary>
        public FlagValueType valueType;

        /// <summary>float 值（valueType == Float 时有效）</summary>
        public float floatValue;

        /// <summary>string 值（valueType == String 时有效）</summary>
        public string? stringValue;

        /// <summary>bool 值（valueType == Bool 时有效）</summary>
        public bool boolValue;

        /// <summary>追踪的 Thing 唯一 ID（valueType == Thing 时有效）</summary>
        public int thingID = -1;

        /// <summary>
        /// 剩余 tick 数。null 表示永久 flag，不会自动过期。
        /// tick 由 StellarisEventFlagManager 每帧递减，归零后自动移除。
        /// </summary>
        public int? remainingTicks;

        /// <summary>
        /// 是否为本事件内的临时 flag。
        /// 仅由 Effect_SetFlag 在执行期内设定。
        /// 事件完整生命周期结束后（after 执行完毕），所有 local flag 将被批量移除。
        /// local flag 同样参与存档——事件窗口打开期间存盘可正常恢复。
        /// </summary>
        public bool isLocal;

        // ──────────────────── 构造 ────────────────────

        public StellarisEventFlag() { }

        public StellarisEventFlag(string name, float value, int? ticks = null)
        {
            this.name = name;
            valueType = FlagValueType.Float;
            floatValue = value;
            remainingTicks = ticks;
        }

        public StellarisEventFlag(string name, string value, int? ticks = null)
        {
            this.name = name;
            valueType = FlagValueType.String;
            stringValue = value;
            remainingTicks = ticks;
        }

        public StellarisEventFlag(string name, bool value, int? ticks = null)
        {
            this.name = name;
            valueType = FlagValueType.Bool;
            boolValue = value;
            remainingTicks = ticks;
        }

        public StellarisEventFlag(string name, Thing thing, int? ticks = null)
        {
            this.name = name;
            valueType = FlagValueType.Thing;
            thingID = thing?.thingIDNumber ?? -1;
            remainingTicks = ticks;
        }

        // ──────────────────── 值获取 ────────────────────

        /// <summary>
        /// 以 object 形式获取当前值，调用方需自行转换。
        /// </summary>
        public object GetValue()
        {
            switch (valueType)
            {
                case FlagValueType.Float:  return floatValue;
                case FlagValueType.String: return stringValue!;
                case FlagValueType.Bool:   return boolValue;
                case FlagValueType.Thing:  return ResolveThing();
                default:                   return null!;
            }
        }

        // ──────────────────── Thing 追踪 ────────────────────

        /// <summary>
        /// 通过 thingID 在所有地图中查找被追踪的 Thing。
        /// </summary>
        public Thing ResolveThing()
        {
            if (thingID < 0) return null!;
            foreach (Map map in Find.Maps)
            {
                foreach (Thing t in map.listerThings.AllThings)
                {
                    if (t.thingIDNumber == thingID)
                        return t;
                }
            }
            return null!;
        }

        /// <summary>
        /// 获取被追踪 Thing 的状态。
        /// </summary>
        public ThingFlagStatus GetThingStatus()
        {
            if (valueType != FlagValueType.Thing || thingID < 0)
                return ThingFlagStatus.DiedOrDisappeared;

            // 先尝试在所有地图中查找（含未 spawn 但在容器中的 thing）
            foreach (Map map in Find.Maps)
            {
                foreach (Thing t in map.listerThings.AllThings)
                {
                    if (t.thingIDNumber == thingID)
                        return t.Spawned ? ThingFlagStatus.Normal : ThingFlagStatus.Despawn;
                }
            }

            // Pawn：检查 WorldPawns（远行队 / 死亡但尚未清除）
            if (Find.WorldPawns != null)
            {
                foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
                {
                    if (p.thingIDNumber == thingID)
                        return ThingFlagStatus.Despawn;
                }
                foreach (Pawn p in Find.WorldPawns.AllPawnsDead)
                {
                    if (p.thingIDNumber == thingID)
                        return ThingFlagStatus.DiedOrDisappeared;
                }
            }

            // thingID 有效但未在任何已知位置找到 —— 可能在容器中（空投舱、库存等）
            // 标记为 Despawn 而非 DiedOrDisappeared，避免被自动清理误删
            return ThingFlagStatus.Despawn;
        }

        // ──────────────────── 序列化 ────────────────────

        public void ExposeData()
        {
            Scribe_Values.Look(ref name!, "name");
            Scribe_Values.Look(ref valueType, "valueType");
            Scribe_Values.Look(ref floatValue, "floatValue");
            Scribe_Values.Look(ref stringValue!, "stringValue");
            Scribe_Values.Look(ref boolValue, "boolValue");
            Scribe_Values.Look(ref thingID, "thingID", -1);

            // nullable int 拆为 hasTicks + ticksValue 两个字段序列化
            bool hasTicks = remainingTicks.HasValue;
            int ticks = remainingTicks ?? 0;
            Scribe_Values.Look(ref hasTicks, "hasTicks");
            Scribe_Values.Look(ref ticks, "ticks");
            remainingTicks = hasTicks ? ticks : (int?)null;
            Scribe_Values.Look(ref isLocal, "isLocal");
        }
    }
}
