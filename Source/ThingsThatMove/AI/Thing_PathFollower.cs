using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;
using ThingsThatMove.Utils;

namespace ThingsThatMove.AI
{
    // Verse.AI.Pawn_PathFollower
    public class Thing_PathFollower : IExposable
    {
        private const int MaxMoveTicks = 450;
        private const int MaxCheckAheadNodes = 20;
        //private const float SnowReductionFromWalking = 0.001f;
        //private const int ClamorCellsInterval = 12;
        //private const int MinCostWalk = 50;
        //private const int MinCostAmble = 60;
        //private const float StaggerMoveSpeedFactor = 0.17f;
        private const int CheckForMovingCollidingPawnsIfCloserToTargetThanX = 30;
        //private const int AttackBlockingHostilePawnAfterTicks = 180; //PatherTick

        protected Thing thing;
        private bool moving;
        private IntVec3 lastCell;
        private int cellsUntilClamor;
        private LocalTargetInfo destination;
        private PathEndMode peMode;

        private int lastMovedTick = -999999;
        private int foundPathWhichCollidesWithThings = -999999;
        private int foundPathWithDanger = -999999;
        private int failedToFindCloseUnoccupiedCellTicks = -999999;

        public float nextCellCostLeft;
        public float nextCellCostTotal = 1f;
        public ThingPath curPath;
        public IntVec3 lastPathedTargetPosition;
        public IntVec3 nextCell;

        public Thing_PathFollower(Thing thing) => this.thing = thing;

        public LocalTargetInfo Destination { get => this.destination; }
        public bool Moving { get => moving; }
        public bool MovingNow { get => this.moving && !this.WillCollideWithThingOnNextPathCell();  }

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.moving, "moving", true, false);
            Scribe_Values.Look<IntVec3>(ref this.nextCell, "nextCell", default(IntVec3), false);
            Scribe_Values.Look<float>(ref this.nextCellCostLeft, "nextCellCostLeft", 0f, false);
            Scribe_Values.Look<float>(ref this.nextCellCostTotal, "nextCellCostInitial", 0f, false);
            Scribe_Values.Look<PathEndMode>(ref this.peMode, "peMode", PathEndMode.None, false);
            Scribe_Values.Look<int>(ref this.cellsUntilClamor, "cellsUntilClamor", 0, false);
            Scribe_Values.Look<int>(ref this.lastMovedTick, "lastMovedTick", -999999, false);
            if (this.moving)
                Scribe_TargetInfo.Look(ref this.destination, "destination");
        }

        public void StartPath(LocalTargetInfo dest, PathEndMode peMode)
        {
            dest = (LocalTargetInfo)GenPath.ResolvePathMode(this.thing, dest.ToTargetInfo(this.thing.Map), ref peMode);
            if (dest.HasThing && dest.ThingDestroyed)
            {
                Log.Error(this.thing + " pathing to destroyed thing " + dest.Thing);
                this.PatherFailed();
                return;
            }

            if (!this.ThingCanOccupy(this.thing.Position) && !this.TryRecoverFromUnwalkablePosition(dest))
                return;

            if (this.moving && this.curPath != null && this.destination == dest && this.peMode == peMode)
                return;

            // TODO: polishing
            /*if (!this.pawn.Map.reachability.CanReach(this.pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
            {
                this.PatherFailed();
                return;
            }*/

            this.peMode = peMode;
            this.destination = dest;

            if (!this.IsNextCellWalkable()) this.nextCell = this.thing.Position;

            // NOTE: reservations
            /*if (!this.destination.HasThing && this.pawn.Map.pawnDestinationManager.DestinationReservedFor(this.pawn) != this.destination.Cell)
                this.pawn.Map.pawnDestinationManager.UnreserveAllFor(this.pawn);*/

            if (this.AtDestinationPosition())
            {
                this.PatherArrived();
                return;
            }

            // TODO: handle destroyed thing?
            /*if (this.pawn.Downed)
            {
                Log.Error(this.pawn.LabelCap + " tried to path while incapacitated. This should never happen.");
                this.PatherFailed();
                return;
            }*/

            if (this.curPath != null)
                this.curPath.ReleaseToPool();

            this.curPath = null;
            this.moving = true;
        }

        public void StopDead()
        {
            if (this.curPath != null)
                this.curPath.ReleaseToPool();

            this.curPath = null;
            this.moving = false;
            this.nextCell = this.thing.Position;
        }

        public void PatherTick()
        {
            // TODO: if thing busy => return

            if (this.WillCollideWithThingAt(this.thing.Position))
            {
                // TODO: verify this method is good
                /*if (!this.FailedToFindCloseUnoccupiedCellRecently())
                {
                    IntVec3 position;
                    if (CellFinder.TryFindBestPawnStandCell(this.pawn, out position))
                    {
                        this.pawn.Position = position;
                        this.ResetToCurrentPosition();
                        if (this.moving && this.TrySetNewPath())
                        {
                            this.TryEnterNextPathCell();
                        }
                    }
                    else
                    {
                        this.failedToFindCloseUnoccupiedCellTicks = Find.TickManager.TicksGame;
                    }
                }*/
                Log.Error("FailedToFindCloseUnoccupiedCellRecently unimplemented for thing");
                return;
            }
            if (!this.moving || !this.WillCollideWithThingOnNextPathCell())
            {
                this.lastMovedTick = Find.TickManager.TicksGame;
                if (this.nextCellCostLeft > 0f)
                    this.nextCellCostLeft -= this.CostToPayThisTick();
                else if (this.moving)
                    this.TryEnterNextPathCell();
                return;
            }
        }

        public void TryResumePathingAfterLoading()
        {
            if (this.moving)
            {
                this.StartPath(this.destination, this.peMode);
                // NOTE: reservations
                //this.pawn.Map.pawnDestinationManager.ReserveDestinationFor(this.pawn, this.destination.Cell);
            }
        }

        public void Notify_Teleported_Int()
        {
            this.StopDead();
            this.ResetToCurrentPosition();
        }

        public void ResetToCurrentPosition() => this.nextCell = this.thing.Position;

        private bool ThingCanOccupy(IntVec3 c)
        {
            if (!c.Walkable(this.thing.Map))
                return false;

            Building edifice = c.GetEdifice(this.thing.Map);
            if (edifice != null)
            {
                /*Building_Door building_Door = edifice as Building_Door;
                if (building_Door != null && !building_Door.PawnCanOpen(this.pawn) && !building_Door.Open)
                    return false;*/
                Log.Error("Unimplemented: handle ThingCanOccupy doorway");
            }
            return true;
        }

        public Building BuildingBlockingNextPathCell()
        {
            Building edifice = this.nextCell.GetEdifice(this.thing.Map);
            if (edifice != null && edifice.BlocksThing())
                return edifice;
            return null;
        }

        public bool WillCollideWithThingOnNextPathCell() => this.WillCollideWithThingAt(this.nextCell);

        // NOTE: IsNextCellTraversable?
        private bool IsNextCellWalkable()
        {
            return this.nextCell.Walkable(this.thing.Map) && !this.WillCollideWithThingAt(this.nextCell);
        }

        // NOTE: WillCollideWithPawntAt 
        private bool WillCollideWithThingAt(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList(thing.Map);
            if (thingList.Count == 0)
                return false;
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i].def.passability == Traversability.Impassable)
                    return true;
            }
            return false;
        }

        public Building_Door NextCellDoorToManuallyOpen()
        {
            Building_Door building_Door = this.thing.Map.thingGrid.ThingAt<Building_Door>(this.nextCell);
            // NOTE: SlowsPawns?
            if (building_Door != null && building_Door.SlowsPawns && !building_Door.Open && building_Door.ThingCanOpen(this.thing))
                return building_Door;
            return null;
        }

        public void PatherDraw()
        {
            if (DebugViewSettings.drawPaths && this.curPath != null && Find.Selector.IsSelected(this.thing))
                this.curPath.DrawPath(this.thing);
        }

        public bool MovedRecently(int ticks) => Find.TickManager.TicksGame - this.lastMovedTick <= ticks;

        private bool TryRecoverFromUnwalkablePosition(LocalTargetInfo originalDest)
        {
            bool flag = false;
            for (int i = 0; i < GenRadial.RadialPattern.Length; i++)
            {
                IntVec3 intVec = this.thing.Position + GenRadial.RadialPattern[i];
                if (this.ThingCanOccupy(intVec))
                {
                    Log.Warning($"{this.thing} on unwalkable cell {this.thing.Position}. Teleporting to  {intVec}.");
                    this.thing.Position = intVec;
                    this.moving = false;
                    this.nextCell = this.thing.Position;
                    this.StartPath(originalDest, this.peMode);
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                this.thing.Destroy(DestroyMode.Vanish);
                Log.Error($"{this.thing} on unwalkable cell {this.thing.Position}. Could not find walkable position nearby. Destroyed.");
            }
            return flag;
        }

        // NOTE => this.pawn.jobs.curDriver.Notify_PatherArrived();
        private void PatherArrived()
        {
            this.StopDead();
        }

        // NOTE => this.pawn.jobs.curDriver.Notify_PatherFailed();
        private void PatherFailed()
        {
            this.StopDead();
        }

        private static readonly MethodInfo MI_Building_Door_DoorOpen = AccessTools.Method(typeof(Building_Door), "DoorOpen");
        private static readonly MethodInfo MI_Building_Door_DoorTryClose = AccessTools.Method(typeof(Building_Door), "DoorTryClose");
        private void TryEnterNextPathCell()
        {
            Building building = this.BuildingBlockingNextPathCell();
            if (building != null)
            {
                Building_Door building_Door = building as Building_Door;
                if (building_Door == null || !building_Door.FreePassage)
                {
                    // NOTE: can things bash doors?
                    this.PatherFailed();
                    return;
                }
            }

            Building_Door building_Door2 = this.NextCellDoorToManuallyOpen();
            if (building_Door2 != null)
            {
                // NOTE: can things have stances?
                // RimWorld.Building_Door.StartManualOpenBy(Pawn opener)
                MI_Building_Door_DoorOpen.Invoke(building_Door2, new object[] { });
                return;
            }

            this.lastCell = this.thing.Position;
            this.thing.Position = this.nextCell;
            // NOTE: can things leave filth?

            Building_Door building_Door3 = this.thing.Map.thingGrid.ThingAt<Building_Door>(this.lastCell);
            if (building_Door3 != null && !building_Door3.BlockedOpenMomentary && !this.thing.HostileTo(building_Door3))
            {
                building_Door3.FriendlyTouched();
                if (!building_Door3.HoldOpen && building_Door3.SlowsPawns)
                {
                    //building_Door3.StartManualCloseBy(this.thing);
                    MI_Building_Door_DoorTryClose.Invoke(building_Door3, new object[] { });
                    return;
                }
            }

            if (this.NeedNewPath() && !this.TrySetNewPath())
                return;

            if (this.AtDestinationPosition())
                this.PatherArrived();
            else
            {
                if (this.curPath.NodesLeftCount == 0)
                {
                    // NOTE: reserverations?
                    Log.Error(this.thing + " ran out of path nodes. Force-arriving.");
                    this.PatherArrived();
                    return;
                }
                this.SetupMoveIntoNextCell();
            }
        }

        private void SetupMoveIntoNextCell()
        {
            if (this.curPath.NodesLeftCount <= 1)
            {
                Log.Error($"{this.thing} at {this.thing.Position} ran out of path nodes while pathing to {this.destination}.");
                this.PatherFailed();
                return;
            }
            this.nextCell = this.curPath.ConsumeNextNode();

            if (!this.nextCell.Walkable(this.thing.Map))
                Log.Error($"{this.thing} entering {this.nextCell} which is unwalkable.");

            // TODO: notify door thing is approaching
            /*Building_Door building_Door = this.thing.Map.thingGrid.ThingAt<Building_Door>(this.nextCell);
            if (building_Door != null)
                building_Door.Notify_PawnApproaching(this.pawn);*/

            int num = this.CostToMoveIntoCell(this.nextCell);
            this.nextCellCostTotal = (float)num;
            this.nextCellCostLeft = (float)num;
        }

        private int CostToMoveIntoCell(IntVec3 c)
        {
            int num;
            if (c.x == this.thing.Position.x || c.z == this.thing.Position.z)
                num = (this.thing as IMovableThing).TicksPerMoveCardinal;
            else
                num = (this.thing as IMovableThing).TicksPerMoveDiagonal;

            num += this.thing.Map.pathGrid.CalculatedCostAt(c, false, this.thing.Position);

            // TODO: handle PathWalkCostFor from building
            /*Building edifice = c.GetEdifice(this.thing.Map);
            if (edifice != null)
                num += (int)edifice.PathWalkCostFor(this.thing);*/

            if (num > MaxMoveTicks)
                num = MaxMoveTicks;

            // TODO: movement speeds.
            /*if (this.pawn.jobs.curJob != null)
            {
                switch (this.pawn.jobs.curJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        num *= 3;
                        if (num < 60)
                        {
                            num = 60;
                        }
                        break;
                    case LocomotionUrgency.Walk:
                        num *= 2;
                        if (num < 50)
                        {
                            num = 50;
                        }
                        break;
                    case LocomotionUrgency.Jog:
                        num *= 1;
                        break;
                    case LocomotionUrgency.Sprint:
                        num = Mathf.RoundToInt((float)num * 0.75f);
                        break;
                }
            }*/
            return Mathf.Max(num, 1);
        }

        private float CostToPayThisTick()
        {
            float num = 1f;
            // NOTE: is there such a thing as a staggered thing?
            if (num < this.nextCellCostTotal / 450f)
                num = this.nextCellCostTotal / 450f;
            return num;
        }

        private bool TrySetNewPath()
        {
            ThingPath thingPath = this.GenerateNewPath();
            if (!thingPath.Found)
            {
                this.PatherFailed();
                return false;
            }
            if (this.curPath != null)
                this.curPath.ReleaseToPool();
            this.curPath = thingPath;
            int num = 0;
            while (num < 20 && num < this.curPath.NodesLeftCount)
            {
                IntVec3 c = this.curPath.Peek(num);
                //if (PawnUtility.AnyPawnBlockingPathAt(c, this.pawn, false, false))
                if (this.WillCollideWithThingAt(c))
                    this.foundPathWhichCollidesWithThings = Find.TickManager.TicksGame;

                // TODO: foundPathWithDanger 
                /*if (PawnUtility.KnownDangerAt(c, this.pawn))
                    this.foundPathWithDanger = Find.TickManager.TicksGame;*/

                if (this.foundPathWhichCollidesWithThings == Find.TickManager.TicksGame && this.foundPathWithDanger == Find.TickManager.TicksGame)
                    break;
                num++;
            }
            return true;
        }

        private ThingPath GenerateNewPath()
        {
            this.lastPathedTargetPosition = this.destination.Cell;
            // TODO: pathfinder on map...
            return this.thing.Map.GetThingPathFinder().FindPath(this.thing.Position, this.destination, this.thing, this.peMode);
        }

        private bool AtDestinationPosition() => this.thing.CanReachImmediate(this.destination, this.peMode);

        // TODO: naming/understanding
        private bool NeedNewPath()
        {
            if (this.curPath == null || !this.curPath.Found || this.curPath.NodesLeftCount == 0)
                return true;

            if (this.destination.HasThing && this.destination.Thing.Map != this.thing.Map)
                return true;

            if (this.lastPathedTargetPosition != this.destination.Cell)
            {
                float num = (float)(this.thing.Position - this.destination.Cell).LengthHorizontalSquared;
                float num2;
                if (num > 900f)
                    num2 = 10f;
                else if (num > 289f)
                    num2 = 5f;
                else if (num > 100f)
                    num2 = 3f;
                else if (num > 49f)
                    num2 = 2f;
                else
                    num2 = 0.5f;

                if ((float)(this.lastPathedTargetPosition - this.destination.Cell).LengthHorizontalSquared > num2 * num2)
                    return true;
            }
            bool collidesWithPawns = false; // TODO -> PawnUtility.ShouldCollideWithPawns(this.thing);
            bool flag2 = this.curPath.NodesLeftCount < CheckForMovingCollidingPawnsIfCloserToTargetThanX;
            IntVec3 other = IntVec3.Invalid;
            int num3 = 0;
            while (num3 < 20 && num3 < this.curPath.NodesLeftCount)
            {
                IntVec3 intVec = this.curPath.Peek(num3);
                if (!intVec.Walkable(this.thing.Map))
                    return true;
                if (collidesWithPawns && !this.BestPathHadPawnsInTheWayRecently() && (ThingHelper.AnyThingBlockingPathAt(intVec, this.thing)))
                    return true;
                // TODO: handle danger
                /*if (!this.BestPathHadDangerRecently() && PawnUtility.KnownDangerAt(intVec, this.thing))
                    return true;*/
                
                // TODO: handle doors
                /*Building_Door building_Door = intVec.GetEdifice(this.thing.Map) as Building_Door;
                if (building_Door != null)
                {
                    if (!building_Door.CanPhysicallyPass(this.thing) && !this.thing.HostileTo(building_Door))
                        return true;
                    if (building_Door.IsForbiddenToPass(this.thing))
                        return true;
                }*/
                if (num3 != 0 && intVec.AdjacentToDiagonal(other) && (PathFinder.BlocksDiagonalMovement(intVec.x, other.z, this.thing.Map) || PathFinder.BlocksDiagonalMovement(other.x, intVec.z, this.thing.Map)))
                    return true;
                other = intVec;
                num3++;
            }
            return false;
        }

        private bool BestPathHadPawnsInTheWayRecently() => this.foundPathWhichCollidesWithThings + 240 > Find.TickManager.TicksGame;

        private bool BestPathHadDangerRecently() => this.foundPathWithDanger + 240 > Find.TickManager.TicksGame;

        private bool FailedToFindCloseUnoccupiedCellRecently() => this.failedToFindCloseUnoccupiedCellTicks + 100 > Find.TickManager.TicksGame;

    }

}
