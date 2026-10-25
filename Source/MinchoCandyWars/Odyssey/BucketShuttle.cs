using HarmonyLib;
using MinchoCandyWars;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Odyssey;

/// <summary>
/// 起降尾焰：三片原版火焰平面，画在桶身之后。
/// 下面的偏移/缩放都是美术参数，集中在此便于调整。
/// </summary>
public static class BucketExhaust
{
    /// <summary>原版火焰 sprite 集合（Fire_a / Fire_b / …）。</summary>
    private const string FireTexturePath = "Things/Special/Fire";

    private const int FlameCount = 3;
    private const float FlameSpacing = 1.25f;
    private const float FlameDepth = -1.6f;
    private const float FlameWidth = 0.65f;
    private const float FlameHeight = 1.45f;
    private const float FlameRotationDegrees = 180f;

    /// <summary>整体下压一点，避免与桶底 z-fighting。</summary>
    private const float VerticalNudge = 0.015f;

    private const float PulseAmplitude = 0.08f;
    private const float PulseFrequency = 0.7f;

    /// <summary>火焰换帧节流：每 N tick 才重取一次随机子图。</summary>
    private const int FlickerFrameTicks = 5;

    private static Graphic? flame;

    public static void Draw(Skyfaller ship, Vector3 center)
    {
        flame ??= GraphicDatabase.Get<Graphic_Flicker>(
            FireTexturePath, ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);

        float pulse = 1f + PulseAmplitude * Mathf.Sin(Find.TickManager.TicksGame * PulseFrequency);
        center.y -= VerticalNudge;

        // Graphic_Flicker.MatSingle 的定义是 subGraphics[Rand.Range(...)].MatSingle，
        // 每次取值都会掷骰子。压住随机种子后，同一个飞行器在 FlickerFrameTicks 内
        // 固定用同一帧 —— 否则尾焰会以屏幕帧率（约 60 次/秒）乱跳，噪声过大。
        Rand.PushState(ship.thingIDNumber + Find.TickManager.TicksGame / FlickerFrameTicks);
        try
        {
            Material material = flame.MatSingle;
            for (int i = -(FlameCount / 2); i <= FlameCount / 2; i++)
            {
                Vector3 pos = center + new Vector3(i * FlameSpacing, 0f, FlameDepth);
                var matrix = Matrix4x4.TRS(
                    pos,
                    Quaternion.Euler(0f, FlameRotationDegrees, 0f),
                    new Vector3(FlameWidth, 1f, FlameHeight * pulse));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
        }
        finally
        {
            Rand.PopState();
        }
    }

    /// <summary>重力船渲染通道里原版会跳过机体绘制，此时也不画尾焰。</summary>
    public static void DrawUnlessGravship(Skyfaller ship, Vector3 drawLoc)
    {
        if (!WorldComponent_GravshipController.GravshipRenderInProgess)
        {
            Draw(ship, drawLoc);
        }
    }
}

/// <summary>
/// 起飞中的桶。
///
/// 覆写 <c>GetDrawPositionAndRotation</c> 而不是 <c>DrawAt</c>：
/// 原版 Skyfaller.DrawAt 内部就是「先算位置 → 再画机体 → 画阴影」，
/// 而 GetDrawPositionAndRotation 本来就是 protected virtual。
/// 这样位置只算一次、尾焰必然画在同一个坐标且天然位于机体之后。
/// </summary>
public class BucketLeaving : PassengerShuttleLeaving
{
    protected override void GetDrawPositionAndRotation(ref Vector3 drawLoc, out float extraRotation)
    {
        base.GetDrawPositionAndRotation(ref drawLoc, out extraRotation);
        BucketExhaust.DrawUnlessGravship(this, drawLoc);
    }
}

/// <summary>降落中的桶，绘制方式同 <see cref="BucketLeaving"/>。</summary>
public class BucketIncoming : PassengerShuttleIncoming
{
    protected override void GetDrawPositionAndRotation(ref Vector3 drawLoc, out float extraRotation)
    {
        base.GetDrawPositionAndRotation(ref drawLoc, out extraRotation);
        BucketExhaust.DrawUnlessGravship(this, drawLoc);
    }
}

/// <summary>
/// 把发射成本提示里的「化合燃料」换成「冰淇淋」。
///
/// 为什么必须打补丁：<see cref="CompProperties_Launchable"/> 没有燃料字段，
/// 而 <c>CompLaunchable</c> 里 Chemfuel 只出现在 TargetingLabelGetter 的文案里
/// （<c>"FuelAmount".Translate(num3, ThingDefOf.Chemfuel)</c>），Def 侧无法覆盖。
///
/// 补料本身走 CompRefuelable 的 fuelFilter（薄荷冰淇淋），与这里无关。
/// </summary>
[HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.TargetingLabelGetter))]
public static class BucketLaunchLabel
{
    public static void Postfix(CompLaunchable launchable, ref TaggedString __result)
    {
        if (launchable?.parent.def != MCW_DefOf.MCW_BucketShuttle)
        {
            return;
        }

        if (__result.Length == 0)
        {
            return;
        }

        // 用 TaggedString.Replace：它只替换 rawText，颜色标签保留。
        // 若写成 (string)__result，隐式转换会执行 StripTags()，颜色就丢了。
        __result = __result.Replace(
            ThingDefOf.Chemfuel.label,
            "MinchoCandyWars.UI.Term.IceCream".Translate().RawText);
    }
}
