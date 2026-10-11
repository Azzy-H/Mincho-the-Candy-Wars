using Verse;

namespace MinchoCandyWars.Render
{
    /// <summary>
    /// 糖饰的渲染节点。
    ///
    /// 只做一件事：把 <see cref="PawnRenderNode.GraphicFor"/> 接到当前糖饰的 GraphicData 上，
    /// 让绘制走引擎默认管线（worker 无需覆写 AppendDrawRequests 去绕开材质缓存）。
    ///
    /// 糖饰种类变化不会改变 facing/posture/flags，因此 PawnDrawParms.ShouldRecache 不会触发，
    /// 必须由 CompMinchoCore 在切换糖饰时调用 renderTree.SetDirty()。
    /// SetDirty 会重建整棵树并重新构造本节点，GraphicFor 随之再被调用，材质自然更新。
    ///
    /// texPath / graphicClass / shaderType / drawSize / color 全部来自 CandyTypeDef 的 GraphicData。
    /// </summary>
    public class PawnRenderNode_CandyDecoration : PawnRenderNode
    {
        public PawnRenderNode_CandyDecoration(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
        }

        public override Graphic? GraphicFor(Pawn pawn)
        {
            CompMinchoCore? core = pawn.GetComp<CompMinchoCore>();
            if (core == null || !core.Active)
            {
                return null;
            }

            return core.CurrentCandyType?.decorationGraphic?.Graphic;
        }
    }
}
