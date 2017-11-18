using System.Collections.Generic;
using Verse;
using Verse.AI;
using UnityEngine;
using System;
using RimWorld;

using ThingsThatMove.Utils;

namespace ThingsThatMove.AI
{
    public class ThingPathFinder
    {
        internal struct CostNode
        {
            public int index;
            public int cost;

            public CostNode(int index, int cost)
            {
                this.index = index;
                this.cost = cost;
            }
        }

        private struct PathFinderNodeFast
        {
            public int knownCost;
            public int heuristicCost;
            public int parentIndex;
            public int costNodeCost;
            public ushort status;
        }

        internal class CostNodeComparer : IComparer<ThingPathFinder.CostNode>
        {
            public int Compare(ThingPathFinder.CostNode a, ThingPathFinder.CostNode b) => a.cost.CompareTo(b.cost);
        }

        public const int DefaultMoveTicksCardinal = 13;
        private const int DefaultMoveTicksDiagonal = 18;
        private const int SearchLimit = 160000;
        private const int Cost_DoorToBash = 300;
        private const int Cost_BlockedWall = 60;
        private const float Cost_BlockedWallPerHitPoint = 0.1f;
        public const int Cost_OutsideAllowedArea = 600;
        private const int Cost_PawnCollision = 175;
        private const int NodesToOpenBeforeRegionBasedPathing = 2000;
        private const float ExtraRegionHeuristicWeight = 5f;
        private const float NonRegionBasedHeuristicStrengthAnimal = 1.75f;

        private Map map;
        private FastPriorityQueue<ThingPathFinder.CostNode> openList;
        private ThingPathFinder.PathFinderNodeFast[] calcGrid;
        private List<int> disallowedCornerIndices = new List<int>(4);
        private ushort statusOpenValue = 1;
        private ushort statusClosedValue = 2;
        // TODO: implemement RegionCostCalculator
        //private RegionCostCalculatorWrapper regionCostCalculator;
        private int mapSizeX;
        private int mapSizeZ;
        private PathGrid pathGrid;
        private Building[] edificeGrid;
        private CellIndices cellIndices;

        private static readonly int[] Directions = new int[] { 0, 1, 0, -1, 1, 1, -1, -1, -1, 0, 1, 0, -1, 1, 1, -1 };

        // TODO: rename and understand
        /*private static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve = new SimpleCurve
        {
            { new CurvePoint(40f, 1f), true },
            { new CurvePoint(120f, 2.8f), true }
        };*/

        public ThingPathFinder(Map map)
        {
            this.map = map;
            this.mapSizeX = map.Size.x;
            this.mapSizeZ = map.Size.z;
            this.calcGrid = new ThingPathFinder.PathFinderNodeFast[this.mapSizeX * this.mapSizeZ];
            this.openList = new FastPriorityQueue<ThingPathFinder.CostNode>(new ThingPathFinder.CostNodeComparer());
            // this.regionCostCalculator = new RegionCostCalculatorWrapper(map);
        }

        public ThingPath FindPath(IntVec3 start, LocalTargetInfo dest, Thing thing, PathEndMode peMode = PathEndMode.OnCell)
        {
            bool canBash = false;
            /*if (thing != null && pawn.CurJob != null && pawn.CurJob.canBash)
                canBash = true;*/
            return this.FindPath(start, dest, TraverseParms.For(thing, Danger.Deadly, TraverseMode.ByPawn, canBash), peMode);
        }

        // NOTe: may need a new TraverseParms object
        public ThingPath FindPath(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            if (DebugSettings.pathThroughWalls)
                traverseParms.mode = TraverseMode.PassAllDestroyableThings;
            Thing thing = traverseParms.thing;
            /*
            if (pawn != null && pawn.Map != this.map)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=",
                    pawn,
                    " pawn.Map=",
                    pawn.Map,
                    " map=",
                    this.map
                }));
                return ThingPath.NotFound;
            }*/

            if (!start.IsValid)
            {
                // TODO: thing goes here
                Log.Error($"Tried to FindPath with invalid start {start}, thing = ");
                return ThingPath.NotFound;
            }
            if (!dest.IsValid)
            {
                Log.Error($"Tried to FindPath with invalid dest {start}, thing = ");
                return ThingPath.NotFound;
            }

            /*if (traverseParms.mode == TraverseMode.ByPawn)
            {
                if (!pawn.CanReach(dest, peMode, Danger.Deadly, traverseParms.canBash, traverseParms.mode))
                    return PawnPath.NotFound;
            }
            else*/
            
            if (!this.map.reachability.CanReach(start, dest, peMode, traverseParms))
                return ThingPath.NotFound;

            this.cellIndices = this.map.cellIndices;
            this.pathGrid = this.map.pathGrid;
            this.edificeGrid = this.map.edificeGrid.InnerArray;
            int x = dest.Cell.x;
            int z = dest.Cell.z;
            int num = this.cellIndices.CellToIndex(start);
            int num2 = this.cellIndices.CellToIndex(dest.Cell);

            // TODO: avoidgrid
            //ByteGrid byteGrid = (pawn == null) ? null : pawn.GetAvoidGrid();

            bool passAllDestroyableThings = traverseParms.mode == TraverseMode.PassAllDestroyableThings;
            bool doNotPassAllDestroyableThings = !passAllDestroyableThings;
            CellRect cellRect = this.CalculateDestinationRect(dest, peMode);
            bool flag3 = cellRect.Width == 1 && cellRect.Height == 1;
            int[] array = this.map.pathGrid.pathGrid;
            EdificeGrid edificeGrid = this.map.edificeGrid;
            int cellsSearched = 0;
            int num4 = 0;

            // TODO: allowedArea
            //Area allowedArea = this.GetAllowedArea(pawn);
            // TODO: ShouldCollideWithThings 
            //bool flag4 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);

            bool drawPaths = DebugViewSettings.drawPaths;
            bool flag6 = !passAllDestroyableThings && start.GetRegion(this.map, RegionType.Set_Passable) != null;
            bool flag7 = !passAllDestroyableThings || !doNotPassAllDestroyableThings;
            bool flag8 = false;
            int num5 = 0;
            int num6 = 0;

            float heuristicStrength = 1.75f;

            //float num7 = this.DetermineHeuristicStrength(pawn, start, dest);

            int ticksPerMoveCardinal;
            int ticksPerMoveDiagonal;
            // TODO: handle ticks per move
            /*if (pawn != null)
            {
                ticksPerMoveCardinal = pawn.TicksPerMoveCardinal;
                ticksPerMoveDiagonal = pawn.TicksPerMoveDiagonal;
            }
            else*/
            {
                ticksPerMoveCardinal = 13;
                ticksPerMoveDiagonal = 18;
            }

            this.CalculateAndAddDisallowedCorners(traverseParms, peMode, cellRect);
            this.InitStatusesAndPushStartNode(ref num, start);
            while (true)
            {
                if (this.openList.Count <= 0)
                {
                    break;
                }
                num5 += this.openList.Count;
                num6++;
                ThingPathFinder.CostNode costNode = this.openList.Pop();
                num = costNode.index;
                // TODO: cleanup
                if (costNode.cost != this.calcGrid[num].costNodeCost) { }
                else if (this.calcGrid[num].status == this.statusClosedValue) { }
                else
                {
                    IntVec3 c = this.cellIndices.IndexToCell(num);
                    int x2 = c.x;
                    int z2 = c.z;
                    if (drawPaths)
                        this.DebugFlash(c, (float)this.calcGrid[num].knownCost / 1500f, this.calcGrid[num].knownCost.ToString());

                    if (flag3)
                    {
                        if (num == num2)
                            return this.FinalizedPath(num);
                    }
                    else if (cellRect.Contains(c) && !this.disallowedCornerIndices.Contains(num))
                        return this.FinalizedPath(num);

                    if (cellsSearched > ThingPathFinder.SearchLimit)
                    {
                        Log.Warning($"{""} pathing from {start} to {dest} hit search limit of {ThingPathFinder.SearchLimit} cells.");
                        return ThingPath.NotFound;
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        uint num10 = (uint)(x2 + ThingPathFinder.Directions[i]);
                        uint num11 = (uint)(z2 + ThingPathFinder.Directions[i + 8]);
                        if ((ulong)num10 < (ulong)((long)this.mapSizeX) && (ulong)num11 < (ulong)((long)this.mapSizeZ))
                        {
                            int xPos = (int)num10;
                            int zPos = (int)num11;
                            int num14 = this.cellIndices.CellToIndex(xPos, zPos);
                            if (this.calcGrid[num14].status != this.statusClosedValue || flag8)
                            {
                                int num15 = 0;
                                bool flag9 = false;
                                if (!this.pathGrid.WalkableFast(num14))
                                {
                                    if (!passAllDestroyableThings)
                                    {
                                        if (drawPaths)
                                        {
                                            this.DebugFlash(new IntVec3(xPos, 0, zPos), 0.22f, "walk");
                                        }
                                        continue;
                                    }
                                    flag9 = true;
                                    num15 += 60;
                                    Building building = edificeGrid[num14];
                                    if (building == null)
                                        continue;
                                    if (!PathFinder.IsDestroyable(building))
                                        continue;
                                    num15 += (int)((float)building.HitPoints * 0.1f);
                                }
                                if (i > 3)
                                {
                                    switch (i)
                                    {
                                        case 4:
                                            if (this.BlocksDiagonalMovement(num - this.mapSizeX))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2, 0, z2 - 1), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            if (this.BlocksDiagonalMovement(num + 1))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2 + 1, 0, z2), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            break;
                                        case 5:
                                            if (this.BlocksDiagonalMovement(num + this.mapSizeX))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2, 0, z2 + 1), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            if (this.BlocksDiagonalMovement(num + 1))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2 + 1, 0, z2), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            break;
                                        case 6:
                                            if (this.BlocksDiagonalMovement(num + this.mapSizeX))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2, 0, z2 + 1), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            if (this.BlocksDiagonalMovement(num - 1))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2 - 1, 0, z2), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            break;
                                        case 7:
                                            if (this.BlocksDiagonalMovement(num - this.mapSizeX))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2, 0, z2 - 1), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            if (this.BlocksDiagonalMovement(num - 1))
                                            {
                                                if (flag7)
                                                {
                                                    if (drawPaths)
                                                        this.DebugFlash(new IntVec3(x2 - 1, 0, z2), 0.9f, "corn");
                                                    continue;
                                                }
                                                num15 += 60;
                                            }
                                            break;
                                    }
                                }
                                int num16 = (i <= 3) ? ticksPerMoveCardinal : ticksPerMoveDiagonal;
                                num16 += num15;
                                if (!flag9)
                                    num16 += array[num14];
                                /*if (byteGrid != null)
                                    num16 += (int)(byteGrid[num14] * 8);*/
                                /*if (allowedArea != null && !allowedArea[num14])
                                    num16 += 600;*/
                                if (ThingHelper.AnyThingBlockingPathAt(new IntVec3(xPos, 0, zPos), thing))
                                    num16 += 175;
                                Building building2 = this.edificeGrid[num14];
                                if (building2 != null)
                                {
                                    int buildingCost = ThingPathFinder.GetBuildingCost(building2, traverseParms, thing);
                                    if (buildingCost == 2147483647)
                                        continue;
                                    num16 += buildingCost;
                                }
                                int num17 = num16 + this.calcGrid[num].knownCost;
                                ushort status = this.calcGrid[num14].status;
                                if (status == this.statusClosedValue || status == this.statusOpenValue)
                                {
                                    int num18 = 0;
                                    if (status == this.statusClosedValue)
                                        num18 = ticksPerMoveCardinal;
                                    if (this.calcGrid[num14].knownCost <= num17 + num18)
                                        continue;
                                }
                                if (status != this.statusClosedValue && status != this.statusOpenValue)
                                {
                                    if (flag8)
                                    {
                                        Log.Error("GetPathCostFromDestToRegion unimplemented");
                                        //this.calcGrid[num14].heuristicCost = Mathf.RoundToInt((float)this.regionCostCalculator.GetPathCostFromDestToRegion(num14) * 5f);
                                    }
                                    else
                                    {
                                        int dx = Math.Abs(xPos - x);
                                        int dz = Math.Abs(zPos - z);
                                        int num19 = GenMath.OctileDistance(dx, dz, ticksPerMoveCardinal, ticksPerMoveDiagonal);
                                        this.calcGrid[num14].heuristicCost = Mathf.RoundToInt((float)num19 * heuristicStrength);
                                    }
                                }
                                int num20 = num17 + this.calcGrid[num14].heuristicCost;
                                this.calcGrid[num14].parentIndex = num;
                                this.calcGrid[num14].knownCost = num17;
                                this.calcGrid[num14].status = this.statusOpenValue;
                                this.calcGrid[num14].costNodeCost = num20;
                                num4++;
                                this.openList.Push(new ThingPathFinder.CostNode(num14, num20));
                            }
                        }
                    }
                    cellsSearched++;
                    this.calcGrid[num].status = this.statusClosedValue;
                    if (num4 >= 2000 && flag6 && !flag8)
                    {
                        flag8 = true;
                        //this.regionCostCalculator.Init(cellRect, traverseParms, ticksPerMoveCardinal, ticksPerMoveDiagonal, null, null, this.disallowedCornerIndices);
                        this.InitStatusesAndPushStartNode(ref num, start);
                    }
                }
            }

            //string job = (pawn == null || pawn.CurJob == null) ? "null" : pawn.CurJob.ToString();
            //string faction = (pawn == null || pawn.Faction == null) ? "null" : pawn.Faction.ToString();

            //Log.Warning($"{""} pathing from {start} to {dest} ran of cells to process.\nJob: {job}\nFaction: {faction}");
            // TODO
            Log.Error("Thing Pathing error!");
            return ThingPath.NotFound;
        }

        public static int GetBuildingCost(Building b, TraverseParms traverseParms, Thing thing)
        {
            // TODO: finish off
            /*Building_Door building_Door = b as Building_Door;
            if (building_Door != null)
            {
                switch (traverseParms.mode)
                {
                    case TraverseMode.ByPawn:
                        if (!traverseParms.canBash && building_Door.IsForbiddenToPass(pawn))
                        {
                            if (DebugViewSettings.drawPaths)
                                ThingPathFinder.DebugFlash(b.Position, b.Map, 0.77f, "forbid");
                            return 2147483647;
                        }
                        if (!building_Door.FreePassage)
                        {
                            if (building_Door.PawnCanOpen(pawn))
                                return building_Door.TicksToOpenNow;
                            if (traverseParms.canBash)
                                return 300;
                            if (DebugViewSettings.drawPaths)
                                ThingPathFinder.DebugFlash(b.Position, b.Map, 0.34f, "cant pass");
                            return 2147483647;
                        }
                        break;
                    case TraverseMode.NoPassClosedDoors:
                    case TraverseMode.NoPassClosedDoorsOrWater:
                        if (!building_Door.FreePassage)
                        {
                            return 2147483647;
                        }
                        break;
                }
            }
            else if (pawn != null)
                return (int)b.PathFindCostFor(pawn);
            */
            return 0;
        }

        public static bool IsDestroyable(Thing th) => th.def.useHitPoints && th.def.destroyable;

        private bool BlocksDiagonalMovement(int x, int z) => ThingPathFinder.BlocksDiagonalMovement(x, z, this.map);

        private bool BlocksDiagonalMovement(int index) => ThingPathFinder.BlocksDiagonalMovement(index, this.map);

        public static bool BlocksDiagonalMovement(int x, int z, Map map)
        {
            return ThingPathFinder.BlocksDiagonalMovement(map.cellIndices.CellToIndex(x, z), map);
        }

        public static bool BlocksDiagonalMovement(int index, Map map)
        {
            return !map.pathGrid.WalkableFast(index) || map.edificeGrid[index] is Building_Door;
        }

        private void DebugFlash(IntVec3 c, float colorPct, string str)
        {
            ThingPathFinder.DebugFlash(c, this.map, colorPct, str);
        }

        private static void DebugFlash(IntVec3 c, Map map, float colorPct, string str)
        {
            if (DebugViewSettings.drawPaths)
                map.debugDrawer.FlashCell(c, colorPct, str);
        }

        private ThingPath FinalizedPath(int finalIndex)
        {
            ThingPath emptyThingPath = this.map.GetThingPathPool().GetEmptyThingPath();
            int num = finalIndex;
            while (true)
            {
                ThingPathFinder.PathFinderNodeFast pathFinderNodeFast = this.calcGrid[num];
                int parentIndex = pathFinderNodeFast.parentIndex;
                emptyThingPath.AddNode(this.map.cellIndices.IndexToCell(num));
                if (num == parentIndex)
                    break;
                num = parentIndex;
            }
            emptyThingPath.SetupFound((float)this.calcGrid[finalIndex].knownCost);
            return emptyThingPath;
        }

        private void InitStatusesAndPushStartNode(ref int curIndex, IntVec3 start)
        {
            this.statusOpenValue += 2;
            this.statusClosedValue += 2;
            if (this.statusClosedValue >= ushort.MaxValue)
                this.ResetStatuses();
            curIndex = this.cellIndices.CellToIndex(start);
            this.calcGrid[curIndex].knownCost = 0;
            this.calcGrid[curIndex].heuristicCost = 0;
            this.calcGrid[curIndex].costNodeCost = 0;
            this.calcGrid[curIndex].parentIndex = curIndex;
            this.calcGrid[curIndex].status = this.statusOpenValue;
            this.openList.Clear();
            this.openList.Push(new ThingPathFinder.CostNode(curIndex, 0));
        }

        private void ResetStatuses()
        {
            int num = this.calcGrid.Length;
            for (int i = 0; i < num; i++)
                this.calcGrid[i].status = 0;
            this.statusOpenValue = 1;
            this.statusClosedValue = 2;
        }

        /*private float DetermineHeuristicStrength(Pawn pawn, IntVec3 start, LocalTargetInfo dest)
        {
            if (pawn != null && pawn.RaceProps.Animal)
            {
                return 1.75f;
            }
            float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
            return (float)Mathf.RoundToInt(ThingPathFinder.NonRegionBasedHeuristicStrengthHuman_DistanceCurve.Evaluate(lengthHorizontal));
        }*/

        private CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
        {
            CellRect result;
            if (!dest.HasThing || peMode == PathEndMode.OnCell)
                result = CellRect.SingleCell(dest.Cell);
            else
                result = dest.Thing.OccupiedRect();
            if (peMode == PathEndMode.Touch)
                result = result.ExpandedBy(1);
            return result;
        }

        private void CalculateAndAddDisallowedCorners(TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
        {
            this.disallowedCornerIndices.Clear();
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                if (!this.IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(minX, minZ));
                if (!this.IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(minX, maxZ));
                if (!this.IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(maxX, maxZ));
                if (!this.IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(maxX, minZ));
            }
        }

        private bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
        {
            return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, this.map);
        }

    }
}
