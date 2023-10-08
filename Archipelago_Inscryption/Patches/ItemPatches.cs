using Archipelago.MultiClient.Net.Models;
using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using HarmonyLib;
using Pixelplacement;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

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

        [HarmonyPatch(typeof(SaveData), "Initialize")]
        [HarmonyPostfix]
        static void InitializeItemNewGame(SaveData __instance)
        {
            List<NetworkItem> receivedItem = ArchipelagoData.Data.receivedItems;
            int countCurrency = receivedItem.Count(item => item.Item == (ArchipelagoManager.ITEM_ID_OFFSET + (long)APItem.Currency));
            __instance.currency = countCurrency;
            for (APItem i = APItem.EpitaphPiece1; i <= APItem.EpitaphPiece9; i++)
            {
                if (ArchipelagoManager.HasItem(i))
                {
                    __instance.undeadTemple.epitaphPieces[(int)(i - APItem.EpitaphPiece1)].found = true;
                }
            }
            if (ArchipelagoManager.HasItem(APItem.CameraReplica))
            {
                __instance.natureTemple.hasCamera = true;
            }

            if (ArchipelagoManager.HasItem(APItem.DrownedSoulCard))
                ArchipelagoManager.ApplyItemReceived(APItem.DrownedSoulCard);
            if (ArchipelagoManager.HasItem(APItem.SalmonCard))
                ArchipelagoManager.ApplyItemReceived(APItem.SalmonCard);
            if (ArchipelagoManager.HasItem(APItem.GreatKrakenCard))
                ArchipelagoManager.ApplyItemReceived(APItem.GreatKrakenCard);
            if (ArchipelagoManager.HasItem(APItem.BoneLordHorn))
                ArchipelagoManager.ApplyItemReceived(APItem.BoneLordHorn);
        }

        [HarmonyPatch(typeof(SaveFile), "ResetGBCSaveData")]
        [HarmonyPostfix]
        static void InitializeStoryEventsNewGame(SaveFile __instance)
        {
            KeyValuePair<APItem, StoryEvent>[] itemStoryEvents = {
                new KeyValuePair<APItem, StoryEvent>( APItem.PileOfMeat,                        StoryEvent.GBCDogFoodFound ),
                new KeyValuePair<APItem, StoryEvent>( APItem.Monocle,                           StoryEvent.GBCMonocleFound ),
                new KeyValuePair<APItem, StoryEvent>( APItem.AncientObol,                       StoryEvent.GBCObolFound ),
                new KeyValuePair<APItem, StoryEvent>( APItem.BoneLordFemur,                     StoryEvent.GBCBoneFound ),
                new KeyValuePair<APItem, StoryEvent>( APItem.GBCCloverPlant,                    StoryEvent.GBCCloverFound )
            };

            for (int i = 0; i < itemStoryEvents.Length; i++)
            {
                if (ArchipelagoManager.HasItem(itemStoryEvents[i].Key))
                    StoryEventsData.SetEventCompleted(itemStoryEvents[i].Value, false, false);
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

        [HarmonyPatch(typeof(DeckReviewSequencer), "OnEnterDeckView")]
        [HarmonyPostfix]
        static void SpawnCardPackPile(DeckReviewSequencer __instance)
        {
            if (ArchipelagoData.Data.availableCardPacks <= 0 || Singleton<GameFlowManager>.Instance.CurrentGameState != GameState.Map || !Singleton<GameMap>.Instance.FullyUnrolled) return;

            RandomizerHelper.SpawnPackPile(__instance);
        }

        [HarmonyPatch(typeof(Part3DeckReviewSequencer), "OnEnterDeckView")]
        [HarmonyPostfix]
        static void SpawnCardPackPile(Part3DeckReviewSequencer __instance)
        {
            if (!StoryEventsData.EventCompleted(StoryEvent.GemsModuleFetched) || ArchipelagoData.Data.availableCardPacks <= 0 || Singleton<GameFlowManager>.Instance.CurrentGameState != GameState.Map) return;

            RandomizerHelper.SpawnPackPile(__instance);
        }

        [HarmonyPatch(typeof(DeckReviewSequencer), "OnExitDeckView")]
        [HarmonyPostfix]
        static void DestroyCardPackPile(DeckReviewSequencer __instance)
        {
            RandomizerHelper.DestroyPackPile();
        }

        [HarmonyPatch(typeof(Part3DeckReviewSequencer), "OnExitDeckView")]
        [HarmonyPostfix]
        static void DestroyPart3CardPackPile(Part3DeckReviewSequencer __instance)
        {
            RandomizerHelper.DestroyPackPile();
        }
    }
    
    [HarmonyPatch]
    class AnglerHookRemovalPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(RunIntroSequencer).GetNestedType("<RunIntroSequence>d__1", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PreventFishHook(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.LoadsConstant(0x7A));

            index -= 2;

            codes.RemoveRange(index, 4);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_I4_1)
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }
}

