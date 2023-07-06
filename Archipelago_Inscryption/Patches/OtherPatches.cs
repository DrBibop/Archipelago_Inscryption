using HarmonyLib;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    internal class OtherPatches
    {
        [HarmonyPatch(typeof(AchievementManager), "Unlock")]
        static bool PreventAchievementUnlock()
        {
            return false;
        }
    }
}
