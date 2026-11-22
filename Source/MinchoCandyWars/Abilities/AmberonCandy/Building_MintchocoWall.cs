using RimWorld;
using Verse;

namespace MinchoCandyWars.Abilities.AmberonCandy;

/// <summary>
/// 珉巧墙的自定义类型。守护技能生成的墙会在自身记录消失时间，
/// 到点自行 Destroy；玩家自己建造的墙不登记（<see cref="expiryTick"/> 保持 -1），
/// 因此既不倒计时也不显示剩余时间。
/// </summary>
public class Building_MintchocoWall : Building
{
    /// <summary>守护墙的存在时长（ticks）。10000 ticks = 4 游戏小时。</summary>
    public const int GuardianLifetimeTicks = 10000;

    /// <summary>-1 表示不是技能生成的墙，不计时。</summary>
    private int expiryTick = -1;

    public void RegisterGuardian(int durationTicks = GuardianLifetimeTicks)
        => expiryTick = Find.TickManager.TicksGame + durationTicks;

    // 该 def 的 tickerType 是 Long（自愈 comp 依赖 Long 的 2000 tick 周期），
    // 所以这里用 TickLong 检查；销毁粒度因此是 2000 ticks 而不是原来的 60。
    public override void TickLong()
    {
        base.TickLong();
        if (expiryTick >= 0 && Find.TickManager.TicksGame >= expiryTick)
        {
            expiryTick = -1;
            Destroy(DestroyMode.Vanish);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref expiryTick, "guardianExpiryTick", -1);
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (!Spawned || expiryTick < 0) return text;

        int remaining = System.Math.Max(0, expiryTick - Find.TickManager.TicksGame);
        string lifetime = "MinchoCandyWars.UI.GuardianWall.Lifetime"
            .Translate(remaining.ToStringTicksToPeriod()).RawText;
        return text.NullOrEmpty() ? lifetime : text + "\n" + lifetime;
    }
}
