using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mincho_HPF;
using RimWorld;
using Verse;

namespace MinchoCandyWars.Ideology
{
    /// <summary>
    /// 信条「冰淇淋产量提升」：拥有对应 meme + precept 的珉巧收获冰淇淋时产量翻倍；
    /// 掷骰失败时不再浪费，而是拿到未提升时的正常产量。
    /// </summary>
    public static class IceCreamYield
    {
        /// <summary>
        /// 一次收获是否享受加成。两个 IL 注入点（浪费分支 / 产量调整）必须共用这一判定，
        /// 否则会出现"跳过去了却没翻倍"这类不一致。
        /// </summary>
        public static bool IsEligible(CompResourceHarvestable comp, ThingDef? product)
        {
            if (comp.parent is not Pawn producer) return false;
            if (producer.def != MCW_DefOf.Mincho_ThingDef) return false;
            if (!IsIceCream(product)) return false;

            Ideo? ideo = producer.Ideo;
            if (ideo == null) return false;

            // 未启用 Ideology 时这两个 DefOf 是 null（[MayRequireIdeology] 让 DefOfHelper 跳过），
            // 这里整体返回 false，功能自动关闭。
            if (MCW_DefOf.Structure_TheisMincho == null || MCW_DefOf.MCW_IceCreamYield_Boosted == null)
            {
                return false;
            }

            return ideo.HasMeme(MCW_DefOf.Structure_TheisMincho)
                && ideo.HasPrecept(MCW_DefOf.MCW_IceCreamYield_Boosted);
        }

        private static bool IsIceCream(ThingDef? product)
        {
            return product != null
                && MCW_DefOf.Mincho_IceCream != null
                && product.IsWithinCategory(MCW_DefOf.Mincho_IceCream);
        }

        /// <summary>浪费分支用：满足条件就跳回成功路径（跳过浪费提示）。</summary>
        public static bool CanRecover(CompResourceHarvestable comp) =>
            IsEligible(comp, comp.Props.thingDef);

        /// <summary>产量调整：正常情况下翻倍；被"救回"的失败保持原产量。</summary>
        public static int Apply(int originalCount, CompResourceHarvestable comp, ThingDef product, bool successful)
        {
            if (!IsEligible(comp, product)) return successful ? originalCount : 0;

            return successful ? originalCount * 2 : originalCount;
        }
    }

    /// <summary>
    /// 对 Mincho_HPF 的 <c>CompResourceHarvestable.Gathered</c> 做 IL 注入。
    ///
    /// 这是全项目唯一一处在第三方 mod 的方法上做 transpiler 的地方，
    /// 所以形状校验写得很硬：任何一处不符合预期都**只记录错误并放行原 IL**，
    /// 绝不抛异常 —— 抛异常会中断 Patch_Main 的 PatchAll，连带后面的补丁类一起失效。
    /// </summary>
    [HarmonyPatch(typeof(CompResourceHarvestable), nameof(CompResourceHarvestable.Gathered))]
    public static class Patch_IceCreamGathered
    {
        private static bool Prepare() => ModsConfig.IdeologyActive;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            MethodInfo makeThing = AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing), new[] { typeof(ThingDef), typeof(ThingDef) });
            MethodInfo round = AccessTools.Method(typeof(Math), nameof(Math.Round), new[] { typeof(double) });
            MethodInfo chance = AccessTools.Method(typeof(Rand), nameof(Rand.Chance), new[] { typeof(float) });

            int[] creates = IndexOfCalls(codes, makeThing);
            int[] rounds = IndexOfCalls(codes, round);
            int[] chances = IndexOfCalls(codes, chance);
            int[] wasteMessages = codes.Select((code, index) => (code, index))
                .Where(x => x.code.operand is MethodInfo m
                            && m.DeclaringType == typeof(MoteMaker)
                            && m.Name == nameof(MoteMaker.ThrowText))
                .Select(x => x.index)
                .ToArray();

            string? problem = ValidateShape(codes, creates, rounds, chances, wasteMessages);
            if (problem != null)
            {
                Log.Error("[MCW] 冰淇淋产量信条：Mincho_HPF 的 CompResourceHarvestable.Gathered IL 结构已变化（"
                          + problem + "），本次跳过产量加成，其余补丁不受影响。");
                return instructions;
            }

            LocalBuilder successful = generator.DeclareLocal(typeof(bool));
            object successTarget = codes[chances[0] + 1].operand;
            CodeInstruction productLocal = codes[creates[0] - 2];

            // 从后往前插入，原始下标才不会失效。
            codes.InsertRange(rounds[0] + 2, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(productLocal.opcode, productLocal.operand),
                new CodeInstruction(OpCodes.Ldloc, successful),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IceCreamYield), nameof(IceCreamYield.Apply)))
            });
            // 保留原版的浪费提示；满足信条的失败随后走正常产量计算。
            codes.InsertRange(wasteMessages[0] + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IceCreamYield), nameof(IceCreamYield.CanRecover))),
                new CodeInstruction(OpCodes.Brtrue, successTarget)
            });
            // 把这次的随机结果存进局部变量，既不重掷也不占用全局状态。
            codes.InsertRange(chances[0] + 1, new[]
            {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stloc, successful)
            });

            return codes;
        }

        private static int[] IndexOfCalls(List<CodeInstruction> codes, MethodInfo method) =>
            codes.Select((code, index) => (code, index))
                .Where(x => x.code.Calls(method))
                .Select(x => x.index)
                .ToArray();

        /// <summary>返回 null 表示形状符合预期，否则返回人可读的原因。</summary>
        private static string? ValidateShape(List<CodeInstruction> codes, int[] creates, int[] rounds,
            int[] chances, int[] wasteMessages)
        {
            if (creates.Length != 1) return $"期望 1 处 MakeThing，实际 {creates.Length}";
            if (rounds.Length != 1) return $"期望 1 处 Math.Round，实际 {rounds.Length}";
            if (chances.Length != 1) return $"期望 1 次 Rand.Chance，实际 {chances.Length}";
            if (wasteMessages.Length != 1) return $"期望 1 处浪费提示，实际 {wasteMessages.Length}";

            if (creates[0] < 2) return "MakeThing 前缺少 Ldloc/Ldnull";
            if (!codes[creates[0] - 2].IsLdloc()) return "MakeThing 前不是 Ldloc";
            if (codes[creates[0] - 1].opcode != OpCodes.Ldnull) return "MakeThing 前不是 Ldnull";

            if (rounds[0] + 1 >= codes.Count) return "Math.Round 后缺少指令";
            if (codes[rounds[0] + 1].opcode != OpCodes.Conv_I4) return "Math.Round 后不是 Conv_I4";

            if (chances[0] + 1 >= codes.Count) return "Rand.Chance 后缺少指令";
            OpCode afterChance = codes[chances[0] + 1].opcode;
            if (afterChance != OpCodes.Brtrue && afterChance != OpCodes.Brtrue_S) return "Rand.Chance 后不是 Brtrue";

            if (!(chances[0] < wasteMessages[0] && wasteMessages[0] < rounds[0]))
                return "掷骰/浪费提示/产量计算的顺序不符";

            return null;
        }
    }
}
