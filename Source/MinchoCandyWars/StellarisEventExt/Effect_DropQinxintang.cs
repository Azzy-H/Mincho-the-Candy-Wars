using System.Collections.Generic;
using MinchoCandyWars.StellarisEvent;
using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    /// <summary>
    /// 终局：沁心糖本人来到殖民地（事件 FF1 的「是」选项）。
    ///
    /// 生成一位强大的智慧珉巧：
    ///   · 技能：格斗 / 手工 / 社交 / 知识 = 19 级，射击 / 艺术 = 12 级，全部双火；
    ///   · 特性：开心果（Joyous）、沉鱼落雁（Beautiful）、超凡赐福（自定义 hediff）；
    ///   · 仿生体：超凡仿生臂 ×2、超凡仿生腿 ×2（珉巧辅助核心 TODO）；
    ///   · 糖饰：全部糖饰（切换能力 TODO）。
    ///
    /// 生成后空投到玩家主地图，并加入殖民地。
    /// </summary>
    public class Effect_DropQinxintang : StellarisEventEffect
    {
        private const string SmartKindDefName = "MCW_SmartMincho";
        private const string BlessingHediffDefName = "MCW_QinxintangBlessing";

        /// <summary>固定等级的技能。</summary>
        private static readonly (string skillDef, int level, Passion passion)[] Skills =
        {
            ("Melee", 19, Passion.Major),
            ("Shooting", 12, Passion.Major),
            ("Crafting", 19, Passion.Major),
            ("Artistic", 12, Passion.Major),
            ("Medicine", 19, Passion.Major),
            ("Social", 19, Passion.Major),
            ("Intellectual", 19, Passion.Major),
        };

        /// <summary>固定特性。</summary>
        private static readonly string[] TraitDefNames =
        {
            "Joyous",      // 开心果
            "Beautiful",   // 沉鱼落雁
        };

        /// <summary>仿生体（部位 → HediffDef）。</summary>
        private static readonly (string bodyPartDefName, string hediffDefName)[] Bionics =
        {
            ("Arm", "ArchotechArm"),
            ("Arm", "ArchotechArm"),
            ("Leg", "ArchotechLeg"),
            ("Leg", "ArchotechLeg"),
        };

        public override void Execute()
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                Log.Warning("[MCW] Effect_DropQinxintang: 没有玩家主地图，跳过");
                return;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(SmartKindDefName, errorOnFail: false);
            if (kind == null)
            {
                Log.Warning("[MCW] Effect_DropQinxintang: 找不到 PawnKindDef " + SmartKindDefName);
                return;
            }

            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, forceGenerateNewPawn: true));

            if (pawn == null)
            {
                return;
            }

            // 智慧珉巧激活
            CompMinchoCore core = pawn.GetComp<CompMinchoCore>();
            if (core != null && !core.Active)
            {
                core.Active = true;
            }

            // 技能
            foreach ((string skillDef, int level, Passion passion) in Skills)
            {
                SkillDef def = DefDatabase<SkillDef>.GetNamed(skillDef, errorOnFail: false);
                if (def == null || pawn.skills == null)
                {
                    continue;
                }
                SkillRecord record = pawn.skills.GetSkill(def);
                if (record == null)
                {
                    continue;
                }
                record.Level = level;
                record.passion = passion;
            }
            if (pawn.skills != null)
            {
                pawn.skills.Notify_SkillDisablesChanged();
            }

            // 特性
            if (pawn.story?.traits != null)
            {
                foreach (string traitName in TraitDefNames)
                {
                    TraitDef trait = DefDatabase<TraitDef>.GetNamed(traitName, errorOnFail: false);
                    if (trait != null && !pawn.story.traits.HasTrait(trait))
                    {
                        pawn.story.traits.GainTrait(new Trait(trait));
                    }
                }
            }

            // 超凡赐福 hediff
            HediffDef blessing = DefDatabase<HediffDef>.GetNamed(BlessingHediffDefName, errorOnFail: false);
            if (blessing != null && pawn.health != null && !pawn.health.hediffSet.HasHediff(blessing))
            {
                pawn.health.AddHediff(blessing);
            }

            // 仿生体
            AddBionics(pawn);

            // 空投到玩家主地图
            IntVec3 cell = DropCellFinder.RandomDropSpot(map);
            GenPlace.TryPlaceThing(pawn, cell, map, ThingPlaceMode.Near);

            Find.LetterStack.ReceiveLetter(
                "沁心糖来访",
                "沁心糖终于来到了你的殖民地，她说要在这里住上一段时间。",
                LetterDefOf.PositiveEvent, pawn);
        }

        /// <summary>给 pawn 安装左右各一对超凡仿生臂/腿。</summary>
        private static void AddBionics(Pawn pawn)
        {
            if (pawn.health == null || pawn.def?.race?.body == null)
            {
                return;
            }

            foreach ((string bodyPartName, string hediffName) in Bionics)
            {
                HediffDef hediff = DefDatabase<HediffDef>.GetNamed(hediffName, errorOnFail: false);
                if (hediff == null)
                {
                    continue;
                }

                BodyPartRecord part = FindFreeBodyPart(pawn, bodyPartName);
                if (part == null)
                {
                    continue;
                }

                pawn.health.AddHediff(hediff, part);
            }
        }

        private static BodyPartRecord FindFreeBodyPart(Pawn pawn, string bodyPartDefName)
        {
            if (pawn.health?.hediffSet == null)
            {
                return null;
            }

            foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part.def.defName != bodyPartDefName)
                {
                    continue;
                }
                if (pawn.health.hediffSet.HasDirectlyAddedPartFor(part))
                {
                    continue;
                }
                return part;
            }
            return null;
        }
    }
}
