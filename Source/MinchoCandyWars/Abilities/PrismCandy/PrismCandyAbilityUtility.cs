using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace MinchoCandyWars.Abilities.PrismCandy
{
    public static class PrismCandyAbilityUtility
    {
        /// <summary>
        /// 计算从 from 指向 to 的平面角度（与 DamageWorker.ExplosionCellsToHit 的 affectedAngle 约定一致）。
        /// </summary>
        public static float AngleToTarget(IntVec3 from, IntVec3 to)
        {
            return Mathf.Atan2(-(to.z - from.z), to.x - from.x) * Mathf.Rad2Deg;
        }

        public static float GetMaxCandyValue(Pawn pawn)
        {
            CompMinchoCore? comp = pawn?.GetComp<CompMinchoCore>();
            return comp?.CurrentMaxCandyValue ?? 0f;
        }

        /// <summary>
        /// 返回以 center 为圆心、radius 为半径、且相对 centerAngle 的夹角不超过 halfAngle 的格子。
        /// 使用 Mathf.DeltaAngle 归一化角度差，正确处理跨越 -180°/180° 的扇形。
        /// </summary>
        public static IEnumerable<IntVec3> GetCellsInSector(IntVec3 center, Map map, float radius, float centerAngle, float halfAngle)
        {
            int numCells = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = center + GenRadial.RadialPattern[i];
                if (!cell.InBounds(map) || !GenSight.LineOfSight(center, cell, map, skipFirstCell: true))
                {
                    continue;
                }

                float lengthHorizontal = (cell - center).LengthHorizontal;
                if (lengthHorizontal <= 0.5f)
                {
                    continue;
                }

                float angle = Mathf.Atan2(-(cell.z - center.z), cell.x - center.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(centerAngle, angle)) > halfAngle)
                {
                    continue;
                }

                yield return cell;
            }
        }

        /// <summary>
        /// 在施法者前方绘制一片「任意角度」的挥砍特效。
        ///
        /// 贴图（琉璃千刃 / 冰霜剑影）画布是正方形，可见内容只占中间一条横带，
        /// 主轴为 +X、发射点在可见内容的左缘，而技能方向由鼠标决定，
        /// Rot4 的 4 方向不够用，因此手搓 FleckCreationData 直接给 exactScale 与 rotation。
        /// 这与原版 FleckMaker.ConnectingLine 处理同一问题的方式一致，
        /// 且 Fleck 不写存档，不会出现读档后特效重播。
        ///
        /// 尺寸用「可见内容」反推，而不是直接拿 range 当 quad 长度：
        /// 若直接把 quad 长度设成 range，可见部分只会占 range 的 (x1-x0)，
        /// 贴图越扁偏差越大；反过来若按扇形弦长算宽度，90° 半角会得到 2*range*tan90°
        /// 这种被截断的巨宽幕布。因此这里按可见长宽比等比例计算 quad 尺寸。
        ///
        /// 角度约定：AngleToTarget 返回「东=0°、北=+90°」，与
        /// Quaternion.AngleAxis(θ, Vector3.up) 以及 FleckDrawData.rotation 完全一致，
        /// 因此 angle 直接使用，不需要取负或加偏移。
        /// </summary>
        /// <param name="map">所在地图</param>
        /// <param name="casterCell">施法者所在格，贴图左缘对齐到这里</param>
        /// <param name="angle">施法方向，取自 <see cref="AngleToTarget"/></param>
        /// <param name="visibleLength">特效长度（格），通常取技能射程</param>
        /// <param name="halfAngle">扇形半角，用于推算特效宽度</param>
        /// <param name="fleckDef">特效 FleckDef</param>
        public static void MakeSlashEffect(
            Map map,
            IntVec3 casterCell,
            float angle,
            float visibleLength,
            float halfAngle,
            FleckDef fleckDef)
        {
            if (map == null || fleckDef == null || visibleLength <= 0f)
            {
                return;
            }

            float width = 2f * visibleLength * Mathf.Sin(halfAngle * Mathf.Deg2Rad);

            float rad = angle * Mathf.Deg2Rad;

            // 与 AngleToTarget 同源的方向向量：东=+X，北=-Z。
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));

            // Fleck 以 quad 中心定位，因此整体前移半个长度，使贴图左缘落在施法者身上。
            Vector3 loc = casterCell.ToVector3Shifted() + dir * (visibleLength * 0.5f);

            FleckCreationData data = FleckMaker.GetDataStatic(loc, map, fleckDef);
            data.exactScale = new Vector3(visibleLength, 1f, width);
            data.rotation = angle;
            map.flecks.CreateFleck(data);
        }

        /// <summary>
        /// 从 start 出发、朝 target 方向延伸 length 得到的终点。
        /// </summary>
        public static IntVec3 GenEndPos(IntVec3 start, IntVec3 target, float length)
        {
            IntVec3 diff = target - start;
            if (diff == IntVec3.Zero)
            {
                return start;
            }
            Vector3 vec = diff.ToVector3();
            vec.Normalize();
            vec *= length;
            return start + vec.ToIntVec3();
        }

        /// <summary>
        /// 返回以 start-end 为轴、半径为 maxDistance 的胶囊形覆盖的格子。
        /// 参考 Koelime CompAbilityEffect_LaserCannon.PointsWithinDistanceFast。
        /// </summary>
        public static IEnumerable<IntVec3> PointsWithinDistanceFast(IntVec3 start, IntVec3 end, float maxDistance)
        {
            int maxDistanceInt = Mathf.CeilToInt(maxDistance);
            int minX = Mathf.Min(start.x, end.x) - maxDistanceInt;
            int maxX = Mathf.Max(start.x, end.x) + maxDistanceInt;
            int minZ = Mathf.Min(start.z, end.z) - maxDistanceInt;
            int maxZ = Mathf.Max(start.z, end.z) + maxDistanceInt;

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    IntVec3 point = new IntVec3(x, start.y, z);
                    if (IsPointInRange(point, start, end, maxDistance))
                    {
                        yield return point;
                    }
                }
            }
        }

        /// <summary>
        /// 判断点是否在 start-end 线段周围 maxDistance 范围内（点到线段距离 + 在线段两端之间）。
        /// </summary>
        public static bool IsPointInRange(IntVec3 point, IntVec3 start, IntVec3 end, float maxDistance)
        {
            float maxDistanceSquared = maxDistance * maxDistance;
            if (start == end)
            {
                return point.DistanceToSquared(start) <= maxDistanceSquared;
            }

            if (DistanceSquaredToLine(point, start, end) > maxDistanceSquared)
            {
                return false;
            }

            Vector3 diff = (end - start).ToVector3();
            Vector3 toPoint = (point - start).ToVector3();
            Vector3 fromEnd = (point - end).ToVector3();
            return Vector3.Dot(diff, toPoint) >= 0f && Vector3.Dot(-diff, fromEnd) >= 0f;
        }

        /// <summary>
        /// 点到线段（水平面投影）垂直距离的平方。
        /// </summary>
        public static float DistanceSquaredToLine(IntVec3 point, IntVec3 lineStart, IntVec3 lineEnd)
        {
            if (lineStart == lineEnd)
            {
                return point.DistanceToSquared(lineStart);
            }

            Vector2 lineVector = new Vector2(lineEnd.x - lineStart.x, lineEnd.z - lineStart.z);
            Vector2 pointVector = new Vector2(point.x - lineStart.x, point.z - lineStart.z);
            float crossProduct = lineVector.x * pointVector.y - lineVector.y * pointVector.x;
            return (crossProduct * crossProduct) / lineVector.sqrMagnitude;
        }
    }
}
