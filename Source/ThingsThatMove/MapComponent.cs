using System.Collections.Generic;
using System.Linq;
using Verse;

using ThingsThatMove.AI;

namespace ThingsThatMove
{
    public class MovableThing_MapComponent : MapComponent
    {
        public ThingPathPool thingPathPool;
        public ThingPathFinder thingPathFinder;

        public MovableThing_MapComponent(Map map) : base(map)
        {
            this.thingPathFinder = new ThingPathFinder(map);
            this.thingPathPool = new ThingPathPool(map);
        }



    }

    public static class MapHelper
    {
        public static ThingPathFinder GetThingPathFinder(this Map map) => map.GetComponent<MovableThing_MapComponent>().thingPathFinder;
        public static ThingPathPool GetThingPathPool(this Map map) => map.GetComponent<MovableThing_MapComponent>().thingPathPool;
    }

}
