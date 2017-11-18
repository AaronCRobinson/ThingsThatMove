using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ThingsThatMove
{
    public class Thing_RotationTracker : IExposable
    {
        private Thing thing;

        public Thing_RotationTracker(Thing thing) => this.thing = thing;
        
        public void Notify_Spawned() => this.UpdateRotation();
        public void RotationTrackerTick() => this.UpdateRotation();

        public void UpdateRotation()
        {
            if (this.thing.Destroyed)
                return;
            if (this.thing is IMovableThing movableThing)
            {
                if (movableThing.Pather.curPath == null || movableThing.Pather.curPath.NodesLeftCount < 1)
                    return;
                this.FaceAdjacentCell(movableThing.Pather.nextCell);
                return;
            }
            // TODO: more cases?
        }

        private void FaceAdjacentCell(IntVec3 c)
        {
            if (c == this.thing.Position)
                return;
            IntVec3 intVec = c - this.thing.Position;
            if (intVec.x > 0)
                this.thing.Rotation = Rot4.East;
            else if (intVec.x < 0)
                this.thing.Rotation = Rot4.West;
            else if (intVec.z > 0)
                this.thing.Rotation = Rot4.North;
            else
                this.thing.Rotation = Rot4.South;
        }

        public void ExposeData()
        {
            throw new NotImplementedException();
        }
    }
}
