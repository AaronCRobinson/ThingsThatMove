using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Harmony;

namespace ThingsThatMove
{
    [StaticConstructorOnStartup]
    public static class HandleMapClicks
    {
        static HandleMapClicks()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.thingsthatmove.testing");
            harmony.Patch(AccessTools.Method(typeof(Selector), "HandleMapClicks"), new HarmonyMethod(typeof(HandleMapClicks), nameof(HandleMapClicks_Prefix)), null);
        }

        public static void HandleMapClicks_Prefix(Selector __instance)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1 && __instance.SelectedObjects.Count > 0)
                {
                    foreach(object obj in __instance.SelectedObjects)
                    {
                        if (obj is IMovableThing thing)
                        {
                            // TODO: finish
                            thing.Pather.StartPath(UI.MouseCell(), PathEndMode.OnCell);
                        }
                    }
                }
            }
        }
    }
}
