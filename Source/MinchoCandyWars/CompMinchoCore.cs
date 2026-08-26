using MinchoCandyWars.Abilities;
using MinchoCandyWars.Data;
using RimWorld;
using System;
using Verse;
using static Mono.Math.BigInteger;

namespace MinchoCandyWars
{
    //核心数据组件
    public class CompMinchoCore : ThingComp
    {
        public CompProperties_MinchoCore Props => (CompProperties_MinchoCore)props;
        public Pawn pawn => (Pawn)parent;

        private int minchoCoreGrade = 0;

        private int minchoBodyGrade = 0;

        private CandyTypeDef? currentCandyType = null;

        private float minchoCandyValue = 0;

        public CandyTypeStage? BodyStage => MinchoBodyGrade > 0 ? CurrentCandyType?.bodyStages[MinchoBodyGrade - 1] : null;

        public CandyTypeStage? CoreStage => MinchoCoreGrade > 0 ? CurrentCandyType?.coreStages[MinchoCoreGrade - 1] : null;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RefreshMinchoAbilities();
        }

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == CompSignals.MinchoCoreDataChange)
            {
                RefreshMinchoAbilities();
            }
        }

        private bool active = false;
        public bool Active
        {
            get => active;
            set
            {
                active = value;
                if (!active)
                {
                    minchoCoreGrade = 0;
                    minchoBodyGrade = 0;
                    MinchoCandyValue = 0f;
                    RefreshMinchoAbilities();
                }
                else
                {
                    minchoCoreGrade = 1;
                    minchoBodyGrade = 1;
                    RefreshMinchoAbilities();
                }
            }
        }

        public HashSet<CandyTypeDef> candyTypeDefsAccessible = new HashSet<CandyTypeDef>();

        //数据存档
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref active, "active", false);
            Scribe_Collections.Look(ref candyTypeDefsAccessible, "candyTypeDefsAccessible", LookMode.Def);
            Scribe_Values.Look(ref minchoCoreGrade, "minchoCoreGrade", 0);
            Scribe_Values.Look(ref minchoBodyGrade, "minchoBodyGrade", 0);
            Scribe_Defs.Look(ref currentCandyType, "currentCandyType");
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

        public void AddCandyTypeDefAccessible(CandyTypeDef candyTypeDef)
        {
            if (!candyTypeDefsAccessible.Contains(candyTypeDef))
            {
                candyTypeDefsAccessible.Add(candyTypeDef);
            }
        }
        public void MinchoCoreGradeSet(int value)
        {
            MinchoCoreGrade = value;
            RefreshMinchoAbilities();
        }
        public void MinchoCoreBodySet(int value)
        {
            MinchoBodyGrade = value;
            RefreshMinchoAbilities();
        }
        //当前糖饰种类
        public CandyTypeDef? CurrentCandyType
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
            if(!active) return;

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

        /// <summary>
        /// 根据当前状态同步 Mincho 技能的赋予/移除：满足条件则添加，不满足则删除。
        /// </summary>
        private void RefreshMinchoAbilities()
        {
            if (pawn.abilities == null)
            {
                return;
            }

            foreach (AbilityDef abilityDef in DefDataPreloading.MinchoCandyAbilityDefs)
            {
                MinchoAbilityDefModExtension? extension = abilityDef.GetModExtension<MinchoAbilityDefModExtension>();
                if (extension == null) continue;

                bool shouldHave = extension.IsAvailableFor(pawn);
                bool hasIt = pawn.abilities.GetAbility(abilityDef, includeTemporary: false) != null;

                if (shouldHave && !hasIt)
                {
                    pawn.abilities.GainAbility(abilityDef);
                }
                else if (!shouldHave && hasIt)
                {
                    pawn.abilities.RemoveAbility(abilityDef);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if(active)
            {
                yield return new Gizmos.MinchoCandyGizmo(pawn, this);
            }

            if (SettingUtility.IsDebugMode())
            {
                yield return new Command_Action
                {
                    defaultLabel = "Dev:切换激活状态",
                    action = delegate
                    {
                        Active = !Active;
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:切换糖饰可用性",
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (CandyTypeDef candyTypeDef in DefDatabase<CandyTypeDef>.AllDefs)
                        {
                            CandyTypeDef localCandyTypeDef = candyTypeDef;
                            string label = candyTypeDef.defName;
                            if (candyTypeDefsAccessible.Contains(candyTypeDef))
                            {
                                label += "(可用)";
                            }
                            FloatMenuOption option = new FloatMenuOption(
                                label,
                                delegate
                                {
                                    if (candyTypeDefsAccessible.Contains(localCandyTypeDef))
                                    {
                                        candyTypeDefsAccessible.Remove(localCandyTypeDef);
                                    }
                                    else
                                    {
                                        candyTypeDefsAccessible.Add(localCandyTypeDef);
                                    }
                                }
                            );
                            options.Add(option);
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:切换糖饰",
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        foreach (CandyTypeDef candyTypeDef in DefDatabase<CandyTypeDef>.AllDefs)
                        {
                            CandyTypeDef localCandyTypeDef = candyTypeDef;
                            string label = candyTypeDef.defName;
                            if (candyTypeDef == CurrentCandyType)
                            {
                                label += "(当前)";
                            }
                            FloatMenuOption option = new FloatMenuOption(
                                label,
                                delegate
                                {
                                    CurrentCandyType = localCandyTypeDef;
                                }
                            );
                            if (candyTypeDef == CurrentCandyType)
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
                        Active = true;
                        MinchoCoreGrade = 5;
                        MinchoBodyGrade = 5;
                        MinchoCandyValue = CurrentMaxCandyValue;
                        candyTypeDefsAccessible.AddRange(DefDatabase<CandyTypeDef>.AllDefs);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "Dev:清空",
                    action = delegate
                    {
                        Active = false;
                        MinchoCoreGrade = 0;
                        MinchoBodyGrade = 0;
                        MinchoCandyValue = 0f;
                        candyTypeDefsAccessible.Clear();
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
