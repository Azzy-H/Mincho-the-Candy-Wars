using System.Collections.Generic;
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
