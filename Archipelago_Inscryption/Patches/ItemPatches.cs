using Archipelago_Inscryption.Archipelago;
using DiskCardGame;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        [HarmonyPatch(typeof(RunState), "Initialize")]
        [HarmonyPostfix]
        static void SetEyeStateIfEyeReceived(RunState __instance)
        {
            if (ArchipelagoManager.HasItem(APItem.MagnificusEye))
            {
                __instance.eyeState = EyeballState.Wizard;
            }
        }

        [HarmonyPatch(typeof(RunState), "InitializeStarterDeckAndItems")]
        [HarmonyPostfix]
        static void AddInsectTotemHeadIfNeeded(RunState __instance)
        {
            if (StoryEventsData.EventCompleted(StoryEvent.BeeFigurineFound) && !__instance.totemTops.Contains(Tribe.Insect))
            {
                __instance.totemTops.Add(Tribe.Insect);
            }
        }

        [HarmonyPatch(typeof(WolfTalkingCard), "get_OnDrawnDialogueId")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> IDontNeedYourReminderJustShutUp(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            int codeIndex = codes.FindIndex(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "WolfFilmRollReminder");

            codes.RemoveAt(codeIndex);

            var newCodes = new List<CodeInstruction>() 
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(WolfTalkingCard), "OnDrawnFallbackDialogueId"))
            };

            codes.InsertRange(codeIndex, newCodes);

            return codes.AsEnumerable();
        }
    }
}
