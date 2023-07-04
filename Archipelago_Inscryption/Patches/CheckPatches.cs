using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    class CheckPatches
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
        static bool DontAddIfCheckCard(CardSingleChoicesSequencer __instance)
        {
            if (__instance.chosenReward.name.Contains("Archipelago")) return false;

            return true;
        }

        [HarmonyPatch(typeof(SafeInteractable), "Start")]
        [HarmonyPrefix]
        static bool ReplaceStinkbugCardWithCheck(SafeInteractable __instance)
        {
            GameObject stinkbugCard = __instance.regularContents.GetComponentInChildren<DiscoverableTalkingCardInteractable>(true).gameObject;
            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(stinkbugCard, APCheck.CabinSafe, true, StoryEvent.SafeOpened);

            if (!ArchipelagoManager.HasItem(APItem.WardrobeKey))
            {
                GameObject key = __instance.interiorObjects[1].gameObject;
                key.SetActive(false);
                __instance.gameObject.AddComponent<ActivateOnItemReceived>().Init(key, APItem.WardrobeKey);
            }

            __instance.interiorObjects.Clear();

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
            Object.Destroy(__instance.largeCompartmentContents[1].gameObject);
            __instance.largeCompartmentContents.Clear();
            __instance.smallCompartmentContents.Clear();

            int fplLayer = LayerMask.NameToLayer("FirstPersonLighting");

            if (checkCard1)
            {
                __instance.largeCompartmentContents.Add(checkCard1);
                checkCard1.gameObject.SetLayerRecursive(fplLayer);
                checkCard1.SetEnabled(false);
            }

            if (checkCard2)
            {
                checkCard2.closeUpEulers = Vector3.zero;
                checkCard2.closeUpDistance = 2.2f;
                checkCard2.GetComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.4f);

                __instance.smallCompartmentContents.Add(checkCard2);
                checkCard2.gameObject.SetLayerRecursive(fplLayer);
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
                Object.Destroy(go);
            }

            __instance.rewardDisplayedItems.Clear();

            DiscoverableCheckInteractable checkCard1 = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinPainting1, false);
            DiscoverableCheckInteractable checkCard2 = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinPainting2, false);
            DiscoverableCheckInteractable checkCard3 = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinPainting3, true);

            int offscreenLayer = LayerMask.NameToLayer("CardOffscreen");

            if (checkCard1)
            {
                checkCard1.card.RenderCard();
                Object.Destroy(checkCard1.card.GetComponent<BoxCollider>());
                __instance.rewardDisplayedItems.Add(Object.Instantiate(checkCard1.card.gameObject, rewardDisplayParent));
                checkCard1.SetEnabled(false);
            }

            if (checkCard2)
            {
                checkCard2.card.RenderCard();
                Object.Destroy(checkCard2.card.GetComponent<BoxCollider>());
                __instance.rewardDisplayedItems.Add(Object.Instantiate(checkCard2.card.gameObject, rewardDisplayParent));
                checkCard2.SetEnabled(false);
            }

            if (checkCard3)
            {
                checkCard3.card.RenderCard();
                Object.Destroy(checkCard3.card.GetComponent<BoxCollider>());
                __instance.rewardDisplayedItems.Add(Object.Instantiate(checkCard3.card.gameObject, rewardDisplayParent));
                checkCard3.SetEnabled(false);
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

            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.CabinSmoke, true);

            if (!checkCard) return true;

            checkCard.SetEnabled(false);
            __instance.card = checkCard;
            Object.Destroy(checkCard.card.GetComponent<BoxCollider>());

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
}
