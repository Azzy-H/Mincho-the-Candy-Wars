using HarmonyLib;
using RimWorld;
using Verse;

namespace MinchoCandyWars.QuestStory;

[HarmonyPatch(typeof(Quest), nameof(Quest.ExposeData))]
static class QuestDescriptionCleanup
{
    static void Postfix(Quest __instance)
    {
        string description = __instance.description.ToString();
        if (Scribe.mode != LoadSaveMode.PostLoadInit || string.IsNullOrEmpty(description) ||
            !description.StartsWith("发布人：") ||
            !Story.Names.Any(name => description.StartsWith("发布人：" + name)))
            return;
        __instance.description = description
            .Replace("失败不发成功奖励。", "")
            .Replace("失败无成功奖励。", "")
            .Replace("接受后5天内通过同盟联络菜单确认交付。成功增加同盟友好度20、终局进度1（终局胜利后不再增加），获得明确列出的空投物资、随机糖饰与1至3个改造包；", "请在5天内通过同盟联络菜单交付。")
            .Replace("失败不发成功奖。", "")
            .Replace("同盟请求接收一桶异常薄荷冰淇淋。接受后通过原版空投舱送达，送达并确认接收后约1—3小时结算。货物遗失、销毁或放弃地图则失败。任务结束后桶保留，由你自行研究并承担原有精神影响；任务完成不等于桶已解除影响。此类货物两次接受间隔至少240天。",
                "同盟有一桶来历不明的薄荷冰淇淋，希望交给你们保管。接受后，货物将通过空投舱送达。请妥善接收，等待同盟确认。");
    }
}


