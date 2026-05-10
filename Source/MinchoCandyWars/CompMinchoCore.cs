using MinchoCandyWars.Data;
using RimWorld;
using Verse;

namespace MinchoCandyWars
{
    //核心数据组件
    public class CompMinchoCore : ThingComp
    {
        public CompProperties_MinchoCore Props => (CompProperties_MinchoCore)this.props;
        public Pawn pawn => (Pawn)this.parent;

        private int minchoCoreGrade = 0;

        private int minchoBodyGrade = 0;

        private CandyType currentCandyType = CandyType.None;

        private float minchoCandyValue = 0;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureMinchoAbilitiesGranted();
        }

        //数据存档
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref minchoCoreGrade, "minchoCoreGrade", 0);
            Scribe_Values.Look(ref minchoBodyGrade, "minchoBodyGrade", 0);
            Scribe_Values.Look(ref currentCandyType, "currentCandyType", CandyType.None);
            Scribe_Values.Look(ref minchoCandyValue, "minchoCandyValue", 0);
        }

        //核心等级
        public int MinchoCoreGrade
        {
            get => minchoCoreGrade;
            set
            {
                minchoCoreGrade = Math.Clamp(value, 0, 5);
                parent.BroadcastCompSignal(CompSignals.MinchoCoreDataChange);

            }
        }

        //躯体等级
        public int MinchoBodyGrade
        {
            get => minchoBodyGrade;
            set
            {
                minchoBodyGrade = Math.Clamp(value, 0, 5);
                parent.BroadcastCompSignal(CompSignals.MinchoCoreDataChange);

            }
        }

        //总等级
        public int MinchoTotalGrade => minchoCoreGrade + minchoBodyGrade;


        //当前糖饰种类
        public CandyType CurrentCandyType
        {
            get => currentCandyType;
            set
            {
                currentCandyType = value;
                parent.BroadcastCompSignal(CompSignals.MinchoCoreDataChange);
            }
        }

        //糖果值
        public float MinchoCandyValue
        {
            get => minchoCandyValue;
            set
            {
                minchoCandyValue = Math.Clamp(value, 0f, CurrentMaxCandyValue);
            }
        }

        //当前糖果值上限
        public float CurrentMaxCandyValue => MinchoTotalGrade * 50f;

        //获取温度增益系数
        public float GetTempGainFactor()
        {
            if (!pawn.Spawned) return 1f;
            float t = pawn.AmbientTemperature;
            if (t < -50f) return 3f;
            if (t < -30f) return 2f;
            if (t < -15f) return 1.5f;
            if (t < 0f) return 1.2f;
            return 1f;
        }

        public override void CompTick()
        {
            base.CompTick();

            //每10tick回复糖果值，总数值为每天回复当前糖果值上限的50%
            if (pawn.IsHashIntervalTick(10))
            {
                if (MinchoCandyValue < CurrentMaxCandyValue)
                {
                    float recoveryAmount = CurrentMaxCandyValue / 12000f;
                    recoveryAmount *= GetTempGainFactor();
                    MinchoCandyValue += recoveryAmount;
                }
            }
        }

        private void EnsureMinchoAbilitiesGranted()
        {
            if (pawn.abilities == null)
            {
                return;
            }

            foreach (AbilityDef abilityDef in DefDataPreloading.MinchoCandyAbilityDefs)
            {
                if (pawn.abilities.GetAbility(abilityDef) != null)
                {
                    continue;
                }

                pawn.abilities.GainAbility(abilityDef);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmos.MinchoCandyGizmo(pawn, this);

            if (SettingUtility.IsDebugMode())
            {
                yield return new Command_Action
                {
                    defaultLabel = "Dev:切换糖饰",
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        foreach (CandyType candyType in Enum.GetValues(typeof(CandyType)))
                        {
                            CandyType localCandyType = candyType;
                            string label = candyType.ToString();
                            if (candyType == CurrentCandyType)
                            {
                                label += "(当前)";
                            }
                            FloatMenuOption option = new FloatMenuOption(
                                label,
                                delegate
                                {
                                    CurrentCandyType = localCandyType;
                                }
                            );
                            if (candyType == CurrentCandyType)
                            {
                                option.Disabled = true;
                            }
                            options.Add(option);
                        }

                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:核心等级+1",
                    action = delegate
                    {
                        MinchoCoreGrade += 1;
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:躯体等级+1",
                    action = delegate
                    {
                        MinchoBodyGrade += 1;
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:增加100糖果值",
                    action = delegate
                    {
                        MinchoCandyValue += 100f;
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "Dev:全满",
                    action = delegate
                    {
                        MinchoCoreGrade = 5;
                        MinchoBodyGrade = 5;
                        MinchoCandyValue = CurrentMaxCandyValue;
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:清空",
                    action = delegate
                    {
                        MinchoCoreGrade = 0;
                        MinchoBodyGrade = 0;
                        MinchoCandyValue = 0f;
                    }
                };
            }
        }
    }

    public class CompProperties_MinchoCore : CompProperties
    {
        public CompProperties_MinchoCore()
        {
            this.compClass = typeof(CompMinchoCore);
        }
    }
}
