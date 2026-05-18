using MinchoCandyWars.Patch;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MinchoCandyWars
{
    /// <summary>
    /// 死亡分裂属性定义。死亡后产生 2 个同种类但退回上一年龄阶段的个体。
    /// XML 用法：
    ///   <deathAction Class="MinchoCandyWars.DeathActionProperties_RevertDivide" />
    /// </summary>
    public class DeathActionProperties_RevertDivide : DeathActionProperties
    {
        public DeathActionProperties_RevertDivide()
        {
            workerClass = typeof(DeathActionWorker_RevertDivide);
        }
    }

    /// <summary>
    /// 死亡分裂 Worker —— 死亡后生成 2 个同 PawnKind 的子体，年龄退回上一生命阶段起始。
    /// 如果是动物，子体继承原体的训练状态、主人和跟随征召设置。
    /// 如果当前已是第一生命阶段（无可回退阶段），则不分裂，正常死亡。
    /// </summary>
    public class DeathActionWorker_RevertDivide : DeathActionWorker
    {
        public override bool DangerousInMelee
        {
            get
            {
                return false;
            }
        }
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            Pawn innerPawn;
            IntVec3 position;
            Map map;

            innerPawn = corpse.InnerPawn;
            position = corpse.PositionHeld;
            map = corpse.MapHeld;

            if (innerPawn == null || map == null) return;

            // 尸体存在时才做尸体相关操作
            if (corpse != null)
            {
                FilthMaker.TryMakeFilth(position, map, MCW_DefOf.Mincho_Filth_BloodDef, 5, FilthSourceFlags.None);
                Thing thing = ThingMaker.MakeThing(MCW_DefOf.Mincho_Mintchoco, null);
                thing.stackCount = (int)(40 * innerPawn.BodySize);
                GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near, null, null, default);
                innerPawn.DropAndForbidEverything();
            }

            // 获取种族的生命阶段列表（按 minAge 升序）
            var lifeStageAges = innerPawn.RaceProps.lifeStageAges;
            if (lifeStageAges.NullOrEmpty()) return;

            float currentAge = innerPawn.ageTracker.AgeBiologicalYearsFloat;

            // 从后往前找到当前所处的生命阶段，再取前一个
            int prevStageIndex = -1;
            float prevStageMinAge = 0f;

            for (int i = lifeStageAges.Count - 1; i >= 0; i--)
            {
                if (lifeStageAges[i].minAge <= currentAge)
                {
                    if (i > 0)
                    {
                        prevStageIndex = i - 1;
                        prevStageMinAge = lifeStageAges[i - 1].minAge;
                    }
                    break;
                }
            }

            // 没有可回退的阶段 → 不分裂，照常死亡
            if (prevStageIndex < 0) return;

            // 生成 2 个子体：同 kind，年龄退回上一阶段起始
            PawnKindDef kind = innerPawn.kindDef;
            Faction faction = innerPawn.Faction;

            for (int n = 0; n < 2; n++)
            {
                Pawn child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer, null,
                    forceGenerateNewPawn: true, allowDead: false, allowDowned: false,
                    canGeneratePawnRelations: true, mustBeCapableOfViolence: false,
                    1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true,
                    allowPregnant: false, allowFood: true, allowAddictions: true,
                    inhabitant: false, certainlyBeenInCryptosleep: false,
                    forceRedressWorldPawnIfFormerColonist: false,
                    worldPawnFactionDoesntMatter: false,
                    0f, 0f, null, 1f,
                    null, null, null, null, null,
                    fixedBiologicalAge: prevStageMinAge,
                    fixedChronologicalAge: prevStageMinAge));

                // 转移训练状态（动物）
                TransferTraining(innerPawn, child);

                // 转移主人和跟随征召设置
                TransferPlayerSettings(innerPawn, child);

                // 在尸体位置生成子体
                GenSpawn.Spawn(child, position, map, WipeMode.VanishOrMoveAside);
                prevLord?.AddPawn(child);
            }

            // 销毁尸体（仅当存在时）
            corpse?.Destroy();
        }

        /// <summary>
        /// 将原体的所有已学训练转移到子体。
        /// </summary>
        private void TransferTraining(Pawn oldPawn, Pawn newPawn)
        {
            if (oldPawn.training == null || newPawn.training == null) return;

            foreach (TrainableDef td in DefDatabase<TrainableDef>.AllDefs)
            {
                if (oldPawn.training.HasLearned(td) &&
                    newPawn.training.CanAssignToTrain(td).Accepted)
                {
                    // 以子体自身作为 trainer 完成训练（避免 null trainer）
                    newPawn.training.Train(td, newPawn, complete: true);
                }
            }
        }

        /// <summary>
        /// 转移主人和跟随征召等玩家设置。
        /// </summary>
        private void TransferPlayerSettings(Pawn oldPawn, Pawn newPawn)
        {
            if (oldPawn.playerSettings == null || newPawn.playerSettings == null) return;

            newPawn.playerSettings.Master = oldPawn.playerSettings.Master;
            newPawn.playerSettings.followDrafted = oldPawn.playerSettings.followDrafted;
        }
    }
}
