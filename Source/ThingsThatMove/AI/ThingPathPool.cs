using System.Collections.Generic;
using Verse;

namespace ThingsThatMove.AI
{
    public class ThingPathPool
    {
        private static readonly ThingPath NotFoundPathInt;

        private Map map;
        private List<ThingPath> paths = new List<ThingPath>(64);

        public static ThingPath NotFoundPath { get => ThingPathPool.NotFoundPathInt; }

        public ThingPathPool(Map map) => this.map = map;

        static ThingPathPool() => ThingPathPool.NotFoundPathInt = ThingPath.NewNotFound();

        public ThingPath GetEmptyThingPath()
        {
            for (int i = 0; i < this.paths.Count; i++)
            {
                if (!this.paths[i].inUse)
                {
                    this.paths[i].inUse = true;
                    return this.paths[i];
                }
            }
            
            // TODO: find a way to enforce number of paths
            /*if (this.paths.Count > this.map.mapPawns.AllPawnsSpawnedCount + 2)
            {
                Log.ErrorOnce("ThingPathPool leak: more paths than spawned pawns. Force-recovering.", 664788);
                this.paths.Clear();
            }*/

            ThingPath pawnPath = new ThingPath();
            this.paths.Add(pawnPath);
            pawnPath.inUse = true;
            return pawnPath;
        }
    }
}
