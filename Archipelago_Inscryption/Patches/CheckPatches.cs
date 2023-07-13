using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using GBC;
using HarmonyLib;
using Pixelplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    internal class CheckPatches
    {
        [HarmonyPatch(typeof(StoryEventsData), "SetEventCompleted")]
        [HarmonyPrefix]
        static bool SendCheckOnStoryEvent(StoryEvent storyEvent)
        {
            if (storyEvent == StoryEvent.NUM_EVENTS) return false;

            ArchipelagoManager.SendStoryCheckIfApplicable(storyEvent);

            return true;
        }

        [HarmonyPatch(typeof(CardSingleChoicesSequencer), "AddChosenCardToDeck")]
        [HarmonyPrefix]
        static bool DontAddIfCheckCard(CardSingleChoicesSequencer __instance)
        {
            if (__instance.chosenReward.name.Contains("Archipelago")) return false;

            return true;
        }

        [HarmonyPatch(typeof(SaveFile), "CollectGBCCard")]
        [HarmonyPrefix]
        static bool SendCheckInsteadOfAddingCard(CardInfo card)
        {
            if (card.name.Contains("Archipelago"))
            {
                string checkName = card.name.Substring(card.name.IndexOf('_') + 1);
                APCheck check = Enum.GetValues(typeof(APCheck)).Cast<APCheck>().FirstOrDefault(c => c.ToString() == checkName);
                ArchipelagoManager.SendCheck(check);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(SafeInteractable), "Start")]
        [HarmonyPrefix]
        static bool ReplaceStinkbugCardWithCheck(SafeInteractable __instance)
        {
            GameObject stinkbugCard = __instance.regularContents.GetComponentInChildren<DiscoverableTalkingCardInteractable>(true).gameObject;
            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(stinkbugCard, APCheck.CabinSafe, true, StoryEvent.SafeOpened);

            MainInputInteractable key = __instance.interiorObjects[1];

            if (!ArchipelagoManager.HasItem(APItem.WardrobeKey))
            {
                key.transform.parent.gameObject.SetActive(false);
                __instance.gameObject.AddComponent<ActivateOnItemReceived>().Init(key.transform.parent.gameObject, APItem.WardrobeKey);
            }
            else
            {
                key.transform.parent.gameObject.SetActive(true);
            }

            __instance.interiorObjects.Clear();
            __instance.interiorObjects.Add(key);

            if (checkCard)
            {
                __instance.interiorObjects.Add(checkCard);
                checkCard.SetEnabled(false);
            }

            return true;
        }

        [HarmonyPatch(typeof(WardrobeDrawerInteractable), "Start")]
        [HarmonyPrefix]
        static bool ReplaceWardrobeCardWithCheck(WardrobeDrawerInteractable __instance)
        {
            APCheck check;
            StoryEvent storyEvent;
            if (__instance.name.Contains("1"))
            {
                check = APCheck.CabinDrawer1;
                storyEvent = StoryEvent.WardrobeDrawer1Opened;
            }
            else if (__instance.name.Contains("2"))
            {
                check = APCheck.CabinDrawer2;
                storyEvent = StoryEvent.WardrobeDrawer2Opened;
            }
            else if (__instance.name.Contains("3"))
            {
                check = APCheck.CabinDrawer3;
                storyEvent = StoryEvent.WardrobeDrawer3Opened;
            }
            else
            {
                check = APCheck.CabinDrawer4;
                storyEvent = StoryEvent.WardrobeDrawer4Opened;

                Transform squirrelHead = __instance.drawerContents[0].transform;
                squirrelHead.eulerAngles = new Vector3(90, 114, 0);
                squirrelHead.localScale = Vector3.one * 0.7114f;
            }

            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(__instance.drawerContents[0].gameObject, check, true, storyEvent);
            __instance.drawerContents.Clear();

            if (checkCard)
                __instance.drawerContents.Add(checkCard);

            return true; 
        }

        [HarmonyPatch(typeof(CuckooClock), "Start")]
        [HarmonyPrefix]
        static bool ReplaceStuntedWolfAndRingWithChecks(CuckooClock __instance)
        {
            GameObject stuntedWolfCard = __instance.largeCompartmentContents[0].gameObject;
            GameObject ring = __instance.smallCompartmentContents[0].gameObject;
            ring.transform.eulerAngles = new Vector3(0, 180, 0);
            ring.transform.localScale = Vector3.one * 0.7114f;

            DiscoverableCheckInteractable checkCard1 = RandomizerHelper.CreateDiscoverableCardCheck(stuntedWolfCard, APCheck.CabinClock1, true, StoryEvent.ClockCompartmentOpened);
            DiscoverableCheckInteractable checkCard2 = RandomizerHelper.CreateDiscoverableCardCheck(ring, APCheck.CabinClock2, true, StoryEvent.ClockSmallCompartmentOpened);
            GameObject.Destroy(__instance.largeCompartmentContents[1].gameObject);
            __instance.largeCompartmentContents.Clear();
            __instance.smallCompartmentContents.Clear();

            int fplLayer = LayerMask.NameToLayer("FirstPersonLighting");

            if (checkCard1)
            {
                __instance.largeCompartmentContents.Add(checkCard1);
                checkCard1.gameObject.SetLayerRecursive(fplLayer);
                if (!StoryEventsData.EventCompleted(StoryEvent.ClockCompartmentOpened))
                    checkCard1.SetEnabled(false);
            }

            if (checkCard2)
            {
                checkCard2.closeUpEulers = Vector3.zero;
                checkCard2.closeUpDistance = 2.2f;
                checkCard2.GetComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.4f);

                __instance.smallCompartmentContents.Add(checkCard2);
                checkCard2.gameObject.SetLayerRecursive(fplLayer);
                if (!StoryEventsData.EventCompleted(StoryEvent.ClockCompartmentOpened))
                    checkCard2.SetEnabled(false);
            }

            return true;
        }

        [HarmonyPatch(typeof(WolfStatueSlotInteractable), "Start")]
        [HarmonyPrefix]
        static bool ReplaceDaggerWithCheck(WolfStatueSlotInteractable __instance)
        {
            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(__instance.dagger.gameObject, APCheck.CabinDagger, true);

            if (!checkCard) return true;

            checkCard.requireStoryEventToAddToDeck = true;
            checkCard.requiredStoryEvent = StoryEvent.WolfStatuePlaced;
            checkCard.closeUpDistance = 2.2f;
            checkCard.closeUpEulers = Vector3.zero;
            checkCard.GetComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.4f);

            checkCard.transform.position = new Vector3(28.1565f, 8.5275f, 11.2946f);
            checkCard.transform.eulerAngles = new Vector3(46.6807f, 140f, 0f);
            checkCard.transform.localScale = Vector3.one * 0.7114f;

            return true;
        }

        [HarmonyPatch(typeof(WolfStatueSlotInteractable), "UnlockDagger")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> IgnoreDagger(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(Animator), "Play", new System.Type[]{ typeof(string), typeof(int), typeof(float)})));

            index++;

            codes.RemoveRange(index, 10);

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(OilPaintingPuzzle), "OnRewardTaken")]
        [HarmonyPrefix]
        static bool TakeCardInstead(OilPaintingPuzzle __instance)
        {
            __instance.state.rewardTaken = true;
            RandomizerHelper.ClaimPaintingCheck(__instance.state.rewardIndex);
            __instance.DisplaySaveState(__instance.state);

            return false;
        }

        [HarmonyPatch(typeof(OilPaintingPuzzle.SaveState), "get_RewardRedeemed")]
        [HarmonyPrefix]
        static bool SkipStoryEventVerification(OilPaintingPuzzle.SaveState __instance, ref bool __result)
        {
            __result = __instance.rewardTaken;

            return false;
        }

        [HarmonyPatch(typeof(OilPaintingPuzzle), "Start")]
        [HarmonyPrefix]
        static bool ReplacePaintingRewardsWithChecks(OilPaintingPuzzle __instance)
        {
            GameObject reference = new GameObject();
            reference.transform.position = new Vector3(19.22f, 9.5f, -15.9f);
            reference.transform.eulerAngles = new Vector3(0, 180, 0);
            reference.transform.localScale = Vector3.one * 0.7114f;
            reference.AddComponent<BoxCollider>().size = new Vector3(0f, 0f, 0f);

            Transform rewardDisplayParent = __instance.rewardDisplayedItems[0].transform.parent;

            foreach (GameObject go in __instance.rewardDisplayedItems)
            {
                GameObject.Destroy(go);
            }

            __instance.rewardDisplayedItems.Clear();

            DiscoverableCheckInteractable checkCard1 = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinPainting1, false);
            DiscoverableCheckInteractable checkCard2 = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinPainting2, false);
            DiscoverableCheckInteractable checkCard3 = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinPainting3, true);

            int offscreenLayer = LayerMask.NameToLayer("CardOffscreen");

            if (checkCard1)
            {
                checkCard1.card.RenderCard();
                GameObject.Destroy(checkCard1.card.GetComponent<BoxCollider>());
                __instance.rewardDisplayedItems.Add(GameObject.Instantiate(checkCard1.card.gameObject, rewardDisplayParent));
                checkCard1.SetEnabled(false);
            }
            else
            {
                __instance.rewardDisplayedItems.Add(new GameObject());
            }

            if (checkCard2)
            {
                checkCard2.card.RenderCard();
                GameObject.Destroy(checkCard2.card.GetComponent<BoxCollider>());
                __instance.rewardDisplayedItems.Add(GameObject.Instantiate(checkCard2.card.gameObject, rewardDisplayParent));
                checkCard2.SetEnabled(false);
            }
            else
            {
                __instance.rewardDisplayedItems.Add(new GameObject());
            }

            if (checkCard3)
            {
                checkCard3.card.RenderCard();
                GameObject.Destroy(checkCard3.card.GetComponent<BoxCollider>());
                __instance.rewardDisplayedItems.Add(GameObject.Instantiate(checkCard3.card.gameObject, rewardDisplayParent));
                checkCard3.SetEnabled(false);
            }
            else
            {
                __instance.rewardDisplayedItems.Add(new GameObject());
            }

            foreach (GameObject go in __instance.rewardDisplayedItems)
            {
                go.transform.localPosition = new Vector3(0f, 0.8618f, 0.9273f);
                go.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                go.transform.localScale = Vector3.one * 0.7114f;
                go.SetLayerRecursive(offscreenLayer);
            }

            RandomizerHelper.SetPaintingRewards(checkCard1 , checkCard2 , checkCard3);

            return true;
        }

        [HarmonyPatch(typeof(WallCandlesPuzzle), "Start")]
        [HarmonyPrefix]
        static bool ReplaceGreaterSmokeWithCheck(WallCandlesPuzzle __instance)
        {
            GameObject reference = new GameObject();
            reference.transform.position = new Vector3(13.4709f, 11.3545f, 19.8158f);
            reference.transform.eulerAngles = Vector3.zero;
            reference.transform.localScale = Vector3.one * 0.7114f;
            reference.AddComponent<BoxCollider>().size = new Vector3(0f, 0f, 0f);

            __instance.card.gameObject.SetActive(false);

            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinSmoke, true);

            if (!checkCard) return true;

            checkCard.SetEnabled(false);
            __instance.card = checkCard;
            GameObject.Destroy(checkCard.card.GetComponent<BoxCollider>());

            return true;
        }

        [HarmonyPatch(typeof(WallCandlesPuzzle), "OnCandleClicked")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UnlockSmokeCheckInstead(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(WallCandlesPuzzle), "UnlockCardSequence")));

            index--;

            codes.RemoveRange(index, 4);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(WallCandlesPuzzle), "card")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(DiscoverableObjectInteractable), "Discover"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(ContainerVolume), "Start")]
        [HarmonyPostfix]
        static void ReplaceContainterContentWithCheck(ContainerVolume __instance)
        {
            if (__instance.transform.GetPath() == "Temple/BasementRoom/Casket/ContainerVolume") return;

            if (__instance.pickupEvent.GetPersistentEventCount() > 0)
            {
                __instance.pickupEvent = new EventTrigger.TriggerEvent();
                __instance.pickupEvent.AddListener(data => RandomizerHelper.GiveObjectRelatedCheck(__instance.gameObject));
            }
            else if (__instance.postTextEvent.GetPersistentEventCount() > 0)
            {
                __instance.postTextEvent = new EventTrigger.TriggerEvent();
                __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveObjectRelatedCheck(__instance.gameObject));
            }
            else
            {
                return;
            }

            if (!__instance.GetComponent<GainEpitaphPiece>())
            {
                __instance.textLines.Clear();
                __instance.textLines.Add("You found a strange card inside.");
            }
        }

        [HarmonyPatch(typeof(GainEpitaphPiece), "GetTextBoxPickupLine")]
        [HarmonyPrefix]
        static bool ReplaceEpitaphText(ref string __result)
        {
            __result = "...Upon closer inspection, it's actually a strange looking card.";

            return false;
        }

        [HarmonyPatch(typeof(GainEpitaphPiece), "Start")]
        [HarmonyPrefix]
        static bool AddEpitaphCheck(GainEpitaphPiece __instance)
        {
            PickupObjectVolume pickup = __instance.GetComponent<PickupObjectVolume>();

            if (pickup != null)
            {
                pickup.pickupEvent = new EventTrigger.TriggerEvent();
                pickup.postTextEvent.AddListener(data => RandomizerHelper.GiveObjectRelatedCheck(__instance.gameObject));
            }

            return true;
        }

        [HarmonyPatch(typeof(WellVolume), "OnPostMessage")]
        [HarmonyPrefix]
        static bool ReplaceWellItemsWithChecks(WellVolume __instance)
        {
            if (__instance.saveState.State.intVal == 0)
            {
                RandomizerHelper.GiveGBCCheck(APCheck.GBCEpitaphPiece9);
            }
            else if (__instance.saveState.State.intVal == 1)
            {
                RandomizerHelper.GiveGBCCheck(APCheck.GBCCryptWell);
            }

            __instance.saveState.State.intVal++;

            return false;
        }

        [HarmonyPatch(typeof(PickupObjectVolume), "Start")]
        [HarmonyPrefix]
        static bool ReplaceCloverWithCheck(PickupObjectVolume __instance)
        {
            if (__instance.unlockStoryEvent && __instance.storyEventToUnlock == StoryEvent.GBCCloverFound)
            {
                __instance.unlockStoryEvent = false;
                __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCClover));
                __instance.textLines.Clear();
                __instance.textLines.Add("You picked the clover leaf from the stem...");
                __instance.textLines.Add("...but it suddenly turned itself into a strange card.");
            }
            else if (SceneLoader.ActiveSceneName == "GBC_Temple_Tech" && __instance.gameObject.name == "RecyclingBinVolume")
            {
                __instance.pickupEvent = new EventTrigger.TriggerEvent();
                __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCFactoryTrashCan));
                __instance.textLines.Clear();
                __instance.textLines.Add("You rummage through the junk cards... And find a strange card that didn't seem to belong with the others.");
            }

            return true;
        }
    }

    [HarmonyPatch]
    class GoalPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(FinaleRedactedScene).GetNestedType("<OldDataScreenSequence>d__17", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TriggerGoalWhenOldDataClicked(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.StoresField(AccessTools.Field(typeof(PauseMenu), "pausingDisabled")));

            index++;

            codes.Insert(index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArchipelagoClient), "SendGoalCompleted")));

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class WizardEyePatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(ChooseEyeballSequencer).GetNestedType("<ChooseEyeball>d__5", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceWizardEyeWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(StoryEventsData), "EventCompleted")));

            index--;

            codes.RemoveRange(index, 3);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ChooseEyeballSequencer), "wizardEyeball")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "CreateWizardEyeCheck"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2BattlePatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(CardBattleNPC).GetNestedType("<PostCombatEncounterSequence>d__64", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceCardPackRewardWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(GainCardPacks), "OpenPacksSequence")));

            index--;

            codes.RemoveRange(index, 2);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "CombatRewardCheckSequence"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2CardGainMessagePatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(SingleCardGainUI).GetNestedType("<HideEndingSequence>d__10", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceMessageIfCheckCard(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "The card was added to your collection.");

            codes.RemoveAt(index);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SingleCardGainUI), "currentCard")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GetCardGainedMessage"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2GhoulEpitaphCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(GhoulNPC).GetNestedType("<OnDefeatedSequence>d__11", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceEpitaphWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(GainEpitaphPiece), "GainPiece")));

            index -= 2;

            codes.RemoveRange(index, 3);

            index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(Tween), "Shake")));

            index += 2;

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Component), "gameObject")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GiveObjectRelatedCheck"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2MagnificusCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(MagnificusNPC).GetNestedType("<GlitchOutSequence>d__8", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplacePackWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(GainCardPacks), "OpenPacksSequence")));

            index -= 2;

            codes.RemoveRange(index, 3);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCBossMagnificus),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GiveGBCCheckSequence"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2TentacleCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(TentacleInteractable).GetNestedType("<>c__DisplayClass14_0", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("<GiveCardSequence>b__1", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplacePackWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(GainSingleCards), "TriggerCardsSequence")));

            index -= 3;

            codes.RemoveRange(index, 4);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCTentacle),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GiveGBCCheck"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }
}
