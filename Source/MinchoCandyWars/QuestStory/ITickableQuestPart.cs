namespace MinchoCandyWars.QuestStory;

/// <summary>
/// 需要周期驱动的 QuestPart。
///
/// RimWorld 的 QuestPart 本身没有 Tick 虚方法，需要在任务存续期间
/// 定期检查状态（倒计时到期、目标达成、客人准备等）。
/// 实现该接口后，由 Story 的 GameComponentTick 统一遍历当前所有 Quest
/// 并调用 Tick()，QuestPart 自己只管判断逻辑。
/// </summary>
public interface ITickableQuestPart
{
    /// <summary>由 Story 的 tick 周期调用。实现内需自行判断任务状态，避免重复结算。</summary>
    void Tick();
}
