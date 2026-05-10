using MinchoCandyWars.Data;
using RimWorld;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Verse;

namespace MinchoCandyWars.Buff
{
    public class CompMinchoCandyBuffApply : ThingComp
    {
        private CompMinchoCore cachedCoreComp = null!;
        private List<MinchoCandyBuffEffect> cachedEffects = new List<MinchoCandyBuffEffect>();
        private bool effectsDirty = true;
        private MinchoCandyBuffGrade? cachedCoreGrade;
        private MinchoCandyBuffGrade? cachedBodyGrade;

        public Pawn pawn => (Pawn)parent;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            cachedCoreComp = pawn.GetComp<CompMinchoCore>();
            
            effectsDirty = true;
        }

        
        public override void ReceiveCompSignal(string signal)
        {
            if (signal == CompSignals.MinchoCoreDataChange)
            {
                effectsDirty = true;
            }
        }

        // 根据当前糖饰类型，选择 pawn 当前应使用的 BuffDef。
        private MinchoCandyBuffDef? GetCurrentBuffDef()
        {
            if (cachedCoreComp == null)
            {
                return null;
            }

            if (cachedCoreComp.CurrentCandyType == CandyType.None)
            {
                return null;
            }

            if (DefDataPreloading.MinchoCandyBuffDefs.TryGetValue(cachedCoreComp.CurrentCandyType, out var buffDef))
            {
                return buffDef;
            }

            return null;
        }

        //选出符合的最高档buff，做单层选择，非逐层累加
        private MinchoCandyBuffGrade? FindMaxGrade(List<MinchoCandyBuffGrade> grades, int currentGrade)
        {
            MinchoCandyBuffGrade? maxGrade = null;
            int maxGradeLevel = 0;

            foreach (var grade in grades)
            {
                if (grade.requiredGrade <= currentGrade &&
                    grade.requiredGrade > maxGradeLevel)
                {
                    maxGrade = grade;
                    maxGradeLevel = grade.requiredGrade;
                }
            }

            return maxGrade;
        }

        private bool TryGetCurrentBuffDef([NotNullWhen(true)] out MinchoCandyBuffDef? buffDef)
        {
            buffDef = GetCurrentBuffDef();
            return buffDef != null;
        }

        private MinchoCandyBuffGrade? GetCurrentCoreGrade(MinchoCandyBuffDef buffDef)
        {
            return FindMaxGrade(buffDef.coreGrades, cachedCoreComp.MinchoCoreGrade);
        }

        private MinchoCandyBuffGrade? GetCurrentBodyGrade(MinchoCandyBuffDef buffDef)
        {
            return FindMaxGrade(buffDef.bodyGrades, cachedCoreComp.MinchoBodyGrade);
        }

        private void ClearCachedState()
        {
            cachedEffects.Clear();
            cachedCoreGrade = null;
            cachedBodyGrade = null;
        }

        //缓存增益
        private void AddEffectsFromGrade(MinchoCandyBuffGrade? grade)
        {
            if (grade?.effects == null)
            {
                return;
            }

            cachedEffects.AddRange(grade.effects);
        }

        // 重新建立当前生效的缓存。
        // 当前实现的规则是：
        // 1. 先根据糖饰类型找到当前 BuffDef。
        // 2. coreGrades 只取当前核心等级可命中的最高一档。
        // 3. bodyGrades 只取当前躯体等级可命中的最高一档。
        // 4. 把这两档的 effects 合并成最终 stat 修正列表。
        private void RefreshEffectsCache()
        {
            ClearCachedState();

            if (!TryGetCurrentBuffDef(out var buffDef))
            {
                effectsDirty = false;
                return;
            }

            cachedCoreGrade = GetCurrentCoreGrade(buffDef);
            cachedBodyGrade = GetCurrentBodyGrade(buffDef);

            AddEffectsFromGrade(cachedCoreGrade);
            AddEffectsFromGrade(cachedBodyGrade);

            effectsDirty = false;
        }

        // 返回当前生效的 effect 列表。
        // 只有在核心数据变更后，才会重新解析并刷新缓存。
        private List<MinchoCandyBuffEffect> GetApplicableEffects()
        {
            if (cachedCoreComp == null)
            {
                ClearCachedState();
                return cachedEffects;
            }

            if (!effectsDirty)
            {
                return cachedEffects;
            }

            RefreshEffectsCache();
            return cachedEffects;
        }

        private float GetTotalStatFactor(List<MinchoCandyBuffEffect> effects, StatDef stat)
        {
            float totalFactor = 1f;
            foreach (var effect in effects)
            {
                if (effect.statDef == stat)
                {
                    totalFactor *= effect.statFactor;
                }
            }

            return totalFactor;
        }

        private float GetTotalStatOffset(List<MinchoCandyBuffEffect> effects, StatDef stat)
        {
            float totalOffset = 0f;
            foreach (var effect in effects)
            {
                if (effect.statDef == stat)
                {
                    totalOffset += effect.statOffset;
                }
            }

            return totalOffset;
        }

        // 如果指定等级档位对该 stat 有影响，则追加一行说明文本。
        private void AppendGradeExplanation(MinchoCandyBuffGrade? grade, StatDef stat, MinchoCandyBuffDef buffDef, StringBuilder sb, string whitespace)
        {
            if (!HasStatEffect(grade, stat))
            {
                return;
            }

            sb.AppendLine($"{whitespace}{grade.label}（{buffDef.label}）");
        }

        // 应用属性乘数。
        // 如果核心线和躯体线都对同一个 stat 提供 factor，则最终结果为连乘。
        public override float GetStatFactor(StatDef stat)
        {
            var effects = GetApplicableEffects();
            return GetTotalStatFactor(effects, stat);
        }

        // 应用属性偏移。
        // 如果核心线和躯体线都对同一个 stat 提供 offset，则最终结果为累加。
        public override float GetStatOffset(StatDef stat)
        {
            var effects = GetApplicableEffects();
            return GetTotalStatOffset(effects, stat);
        }

        // 输出 stat 面板中的来源说明。
        // 这里只说明“是哪一档在生效”，不展开具体数值计算过程。
        public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
        {
            
            var buffDef = GetCurrentBuffDef();
            if (buffDef == null)
            {
                return;
            }

            // 确保当前命中的核心档位/躯体档位已经刷新到缓存。
            GetApplicableEffects();

            AppendGradeExplanation(cachedCoreGrade, stat, buffDef, sb, whitespace);
            AppendGradeExplanation(cachedBodyGrade, stat, buffDef, sb, whitespace);
        }

        private static bool HasStatEffect([NotNullWhen(true)] MinchoCandyBuffGrade? grade, StatDef stat)
        {
            if (grade == null || grade.effects == null)
            {
                return false;
            }

            foreach (var effect in grade.effects)
            {
                if (effect.statDef == stat)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class CompProperties_MinchoCandyBuffApply : CompProperties
    {
        public CompProperties_MinchoCandyBuffApply()
        {
            this.compClass = typeof(CompMinchoCandyBuffApply);
        }
    }
}
