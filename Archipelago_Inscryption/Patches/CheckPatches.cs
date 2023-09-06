using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
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
            if (__instance.chosenReward.Info.name.Contains("Archipelago"))
            {
                if (__instance.chosenReward.Info.name.Contains("ArchipelagoCheck"))
                {
                    string cardName = __instance.chosenReward.Info.name;
                    string checkName = cardName.Substring(cardName.IndexOf('_') + 1);
                    APCheck check = Enum.GetValues(typeof(APCheck)).Cast<APCheck>().FirstOrDefault(c => c.ToString() == checkName);
                    ArchipelagoManager.SendCheck(check);
                }

                __instance.deckPile.AddToPile(__instance.chosenReward.transform);

                return false;
            }

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

            if (ArchipelagoManager.randomizeCodes)
            {
                __instance.correctLockPositions = ArchipelagoManager.cabinSafeCode.Select(digit => (10 - digit) % 10).ToList();
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
            else if (__instance.name.Contains("4"))
            {
                check = APCheck.CabinDrawer4;
                storyEvent = StoryEvent.WardrobeDrawer4Opened;

                Transform squirrelHead = __instance.drawerContents[0].transform;
                squirrelHead.eulerAngles = new Vector3(90, 114, 0);
                squirrelHead.localScale = Vector3.one * 0.7114f;
            }
            else if (__instance.drawerContents[0].name.Contains("Card"))
            {
                check = APCheck.FactoryDrawer2;
                storyEvent = StoryEvent.FactoryWardrobe2Opened;
            }
            else
            {
                check = APCheck.FactoryDrawer1;
                storyEvent = StoryEvent.FactoryWardrobe1Opened;
            }

            DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(__instance.drawerContents[0].gameObject, check, true, storyEvent);
            __instance.drawerContents.Clear();

            if (checkCard)
                __instance.drawerContents.Add(checkCard);

            return true; 
        }

        [HarmonyPatch(typeof(CuckooClock), "Start")]
        [HarmonyPrefix]
        static bool ReplaceClockContentsWithChecks(CuckooClock __instance)
        {
            if (SaveManager.SaveFile.IsPart3)
            {
                GameObject ourobotCard = __instance.largeCompartmentContents[0].gameObject;
                __instance.largeCompartmentContents.Clear();

                DiscoverableCheckInteractable checkCard = RandomizerHelper.CreateDiscoverableCardCheck(ourobotCard, APCheck.FactoryClock, true, StoryEvent.FactoryCuckooClockOpenedLarge);

                int fplLayer = LayerMask.NameToLayer("FirstPersonLighting");

                if (checkCard)
                {
                    __instance.largeCompartmentContents.Add(checkCard);
                    checkCard.gameObject.SetLayerRecursive(fplLayer);
                    if (!StoryEventsData.EventCompleted(StoryEvent.FactoryCuckooClockOpenedLarge))
                        checkCard.SetEnabled(false);
                }

                if (ArchipelagoManager.randomizeCodes)
                {
                    __instance.solutionPositionsLarge = ArchipelagoManager.factoryClockCode.ToArray();
                    __instance.solutionPositionsSmall = ArchipelagoManager.cabinSmallClockCode.ToArray();
                }
            }
            else
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

                if (ArchipelagoManager.randomizeCodes)
                {
                    __instance.solutionPositionsLarge = ArchipelagoManager.cabinClockCode.ToArray();
                    __instance.solutionPositionsSmall = ArchipelagoManager.cabinSmallClockCode.ToArray();

                    Transform clock = __instance.transform.Find("CuckooClock");

                    List<Transform> children = new List<Transform>();
                    foreach (Transform child in clock)
                    {
                        children.Add(child);
                    }

                    children.First(x => x.gameObject.name == "WizardMark_Tall" && x.eulerAngles.z > 270).gameObject.SetActive(false);
                    children.First(x => x.gameObject.name == "WizardMark_Tall" && x.eulerAngles.z < 270).gameObject.SetActive(false);
                    children.First(x => x.gameObject.name == "WizardMark_Short").gameObject.SetActive(false);

                    GameObject cluesObject = GameObject.Instantiate(AssetsManager.clockCluesPrefab, clock);

                    cluesObject.transform.localPosition = new Vector3(0f, 1.9459f, 0.6f);
                    cluesObject.transform.eulerAngles = new Vector3(0, 180, 0);

                    Transform secondHandClue = cluesObject.transform.Find("SecondCluePivot");
                    Transform minuteHandClue = cluesObject.transform.Find("MinuteCluePivot");
                    Transform hourHandClue = cluesObject.transform.Find("HourCluePivot");

                    secondHandClue.localEulerAngles = new Vector3(0, 0, 360 - 30 * ArchipelagoManager.cabinClockCode[0]);
                    minuteHandClue.localEulerAngles = new Vector3(0, 0, 360 - 30 * ArchipelagoManager.cabinClockCode[1]);
                    hourHandClue.localEulerAngles = new Vector3(0, 0, 360 - 30 * ArchipelagoManager.cabinClockCode[2]);

                    cluesObject.SetLayerRecursive(LayerMask.NameToLayer("WizardEyeVisible"));
                }
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
        static bool ReplacePickupWithCheck(PickupObjectVolume __instance)
        {
            if (__instance.unlockStoryEvent)
            {
                if (__instance.storyEventToUnlock == StoryEvent.GBCCloverFound)
                {
                    __instance.unlockStoryEvent = false;
                    __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCClover));
                    __instance.textLines.Clear();
                    __instance.textLines.Add("You picked the clover leaf from the stem...");
                    __instance.textLines.Add("...but it suddenly turned itself into a strange card.");
                }
                else if (__instance.storyEventToUnlock == StoryEvent.GBCBoneFound)
                {
                    __instance.unlockStoryEvent = false;
                    __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCBoneLordFemur));
                    __instance.textLines.Clear();
                    __instance.textLines.Add("You took the Bone Lord's femur from the pedestal...");
                    __instance.textLines.Add("...but it suddenly turned itself into a strange card.");
                }
                else if (__instance.storyEventToUnlock == StoryEvent.BonelordHoloKeyFound)
                {
                    __instance.unlockStoryEvent = false;
                    __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCBoneLordHoloKey));
                    __instance.textLines.Clear();
                    __instance.textLines.Add("You found a strange flickering key...");
                    __instance.textLines.Add("...but as you touched it, the key turned itself into a card.");
                }
                else if (__instance.storyEventToUnlock == StoryEvent.MycologistHutKeyFound)
                {
                    __instance.unlockStoryEvent = false;
                    __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCMycologistsHoloKey));
                    __instance.textLines.Clear();
                    __instance.textLines.Add("You found a strange flickering key...");
                    __instance.textLines.Add("...but as you touched it, the key turned itself into a card.");
                }
            }
            else if (SceneLoader.ActiveSceneName == "GBC_Temple_Tech" && __instance.gameObject.name == "RecyclingBinVolume")
            {
                __instance.pickupEvent = new EventTrigger.TriggerEvent();
                __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCFactoryTrashCan));
                __instance.textLines.Clear();
                __instance.textLines.Add("You rummage through the junk cards... And find a strange card that didn't seem to belong with the others.");
            }
            else if (SceneLoader.ActiveSceneName == "GBC_Temple_Undead" && __instance.gameObject.name == "Card")
            {
                __instance.pickupEvent = new EventTrigger.TriggerEvent();
                __instance.postTextEvent.AddListener(data => RandomizerHelper.GiveGBCCheck(APCheck.GBCBoneLordHorn));
            }

            return true;
        }

        [HarmonyPatch(typeof(BrokenCoin), "RespondsToOtherCardAssignedToSlot")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> AllowObolRepairIfCheckAvailable(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(StoryEventsData), "EventCompleted")));

            index--;

            codes.RemoveRange(index, 2);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCAncientObol),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArchipelagoManager), "HasCompletedCheck"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(InspectorNPC), "Suicide")]
        [HarmonyPostfix]
        static void GiveInspectorCheck()
        {
            RandomizerHelper.GiveGBCCheck(APCheck.GBCBattleInspector);
        }

        [HarmonyPatch(typeof(SmelterNPC), "Suicide")]
        [HarmonyPostfix]
        static void GiveMelterCheck()
        {
            RandomizerHelper.GiveGBCCheck(APCheck.GBCBattleMelter);
        }

        [HarmonyPatch(typeof(GainMonocleVolume), "Start")]
        [HarmonyPostfix]
        static void ChangeMonocleMessage(GainMonocleVolume __instance)
        {
            __instance.textLines.Add("Your vision was suddenly obstructed. You take off the monocle...");
            __instance.textLines.Add("...or so you thought. It was a strange card instead.");
        }

        [HarmonyPatch(typeof(GainMonocleVolume), "OnPostMessage")]
        [HarmonyPrefix]
        static bool GiveMonocleCheck(GainMonocleVolume __instance)
        {
            __instance.SaveState.boolVal = true;
            __instance.Hide();
            RandomizerHelper.GiveGBCCheck(APCheck.GBCMonocle);

            return false;
        }

        [HarmonyPatch(typeof(CubeChestInteractable), "Start")]
        [HarmonyPostfix]
        static void ReplaceAnglerCardWithCheck(CubeChestInteractable __instance)
        {
            GameObject card = __instance.GetComponentInChildren<DiscoverableTalkingCardInteractable>(true).gameObject;
            RandomizerHelper.CreateDiscoverableCardCheck(card, APCheck.FactoryChest, true);
        }

        [HarmonyPatch(typeof(HoloMapArea), "Start")]
        [HarmonyPrefix]
        static void ReplaceMapNodesWithChecks(HoloMapArea __instance)
        {
            switch (__instance.name)
            {
                case "HoloMapArea_StartingIslandBattery(Clone)":
                    RandomizerHelper.CreateHoloMapNodeCheck(__instance.transform.Find("Nodes/UnlockItemNode3D_Battery").gameObject, APCheck.FactoryExtraBattery);
                    break;
                case "HoloMapArea_Shop(Clone)":
                    RandomizerHelper.CreateHoloMapNodeCheck(__instance.transform.Find("Nodes/ShopNode3D_ShieldGenItem/UnlockItemNode3D_ShieldGenerator").gameObject, APCheck.FactoryNanoArmorGenerator);
                    RandomizerHelper.CreateHoloMapNodeCheck(__instance.transform.Find("Nodes/ShopNode3D_PickupPelt/PickupPeltNode3D").gameObject, APCheck.FactoryHoloPelt1);
                    break;
                case "HoloMapArea_TempleWizardSide(Clone)":
                    Transform clue = __instance.transform.Find("Splatter/clue");
                    clue.GetComponent<MeshRenderer>().material.mainTexture = AssetsManager.factoryClockClueTexs[ArchipelagoManager.factoryClockCode[2]];
                    break;
            }
        }

        [HarmonyPatch(typeof(InspectionMachineInteractable), "Start")]
        [HarmonyPrefix]
        static void CreateBatteryCheck(InspectionMachineInteractable __instance)
        {
            GameObject reference = new GameObject();
            reference.transform.position = new Vector3(90.3f, 5f, 5f);
            reference.transform.eulerAngles = new Vector3(0, 90, 0);
            reference.transform.localScale = Vector3.one * 0.7114f;
            reference.AddComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.2f);

            RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.FactoryInspectometerBattery, true);

            HoldableBattery battery = __instance.GetComponentInChildren<HoldableBattery>();

            if (battery)
            {
                if (!ArchipelagoManager.HasItem(APItem.InspectometerBattery))
                {
                    battery.transform.parent.gameObject.SetActive(false);
                    __instance.gameObject.AddComponent<ActivateOnItemReceived>().Init(battery.transform.parent.gameObject, APItem.InspectometerBattery);
                }
                else
                {
                    battery.transform.parent.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(HoloMapPeltMinigame), "Start")]
        [HarmonyPrefix]
        static bool ReplacePeltWithCheck(HoloMapPeltMinigame __instance)
        {
            APCheck check = APCheck.FactoryHoloPelt1;

            switch (__instance.GetComponentInParent<HoloMapArea>().gameObject.name)
            {
                case "HoloMapArea_NeutralWest_Secret(Clone)":
                    check = APCheck.FactoryHoloPelt2;
                    break;
                case "HoloMapArea_NatureSecret(Clone)":
                    check = APCheck.FactoryHoloPelt3;
                    break;
                case "HoloMapArea_TempleUndeadShop(Clone)":
                    check = APCheck.FactoryHoloPelt4;
                    break;
                case "HoloMapArea_WizardSecret(Clone)":
                    check = APCheck.FactoryHoloPelt5;
                    break;
            }

            HoloMapNode checknode = RandomizerHelper.CreateHoloMapNodeCheck(__instance.rewardNode.gameObject, check);

            if (checknode)
            {
                __instance.rewardNode = checknode;
                return true;
            }

            if (__instance.trapInteractable.Completed)
            {
                __instance.rabbitAnim.gameObject.SetActive(false);
                __instance.trapAnim.Play("shut", 0, 1f);
            }

            return false;
        }

        [HarmonyPatch(typeof(HoloMapLukeFile), "OnFolderHitMapKeyframe")]
        [HarmonyPostfix]
        static void GiveLukeFileCheck()
        {
            APCheck check = APCheck.FactoryLukeFileEntry1 + (Mathf.Clamp(Part3SaveData.Data.lukeFileIndex, 0, HoloMapLukeFile.FILE_NAMES.Length) - 1);

            ArchipelagoManager.SendCheck(check);
        }

        [HarmonyPatch(typeof(HoloMapWell), "Start")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChangeDredgedCondition(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.LoadsField(AccessTools.Field(typeof(Part3SaveData), "foundUndeadTempleQuill")));

            index--;

            codes.RemoveRange(index, 2);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.FactoryWell),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArchipelagoManager), "HasCompletedCheck"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(HoloMapWell), "Start")]
        [HarmonyPostfix]
        static void ReplaceQuillWithCheck(HoloMapWell __instance)
        {
            HoloMapNode checkNode = RandomizerHelper.CreateHoloMapNodeCheck(__instance.itemNodes[0].gameObject, APCheck.FactoryWell);

            if (checkNode)
            {
                __instance.itemNodes[0] = checkNode;
            }
        }

        [HarmonyPatch(typeof(FactoryGemsDrone), "Start")]
        [HarmonyPostfix]
        static void CreateGemsDroneCheck(FactoryGemsDrone __instance)
        {
            GameObject reference = new GameObject();
            reference.transform.SetParent(__instance.transform.Find("Anim"));
            reference.transform.localPosition = new Vector3(0.0109f, 0.2764f, 1.6309f);
            reference.transform.localEulerAngles = new Vector3(90, 0, 0);
            reference.transform.localScale = Vector3.one * 0.7114f;
            reference.AddComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.2f);

            RandomizerHelper.CreateDiscoverableCardCheck(reference, APCheck.FactoryGemsDrone, true);

            if (__instance.shelf.CurrentHoldable && !ArchipelagoManager.HasItem(APItem.GemsModule))
            {
                __instance.shelf.gameObject.SetActive(false);
                __instance.gameObject.AddComponent<ActivateOnItemReceived>().Init(__instance.shelf.gameObject, APItem.GemsModule);
            }
        }

        [HarmonyPatch(typeof(FactoryGemsDrone), "OnGemsTaken")]
        [HarmonyPrefix]
        static bool SendDroneCheckIfNotCompleted(FactoryGemsDrone __instance)
        {
            if (!__instance.shelf.gameObject.activeSelf) return false;

            if (!ArchipelagoManager.HasCompletedCheck(APCheck.FactoryGemsDrone))
            {
                DiscoverableCheckInteractable checkCard = __instance.GetComponentInChildren<DiscoverableCheckInteractable>();

                if (checkCard)
                {
                    checkCard.Discover();
                }
                else
                {
                    ArchipelagoManager.SendCheck(APCheck.FactoryGemsDrone);
                }
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

    [HarmonyPatch]
    class Act2SafeCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(SafeVolume).GetNestedType("<GainDogFoodSequence>d__6", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceMeatWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(TextBox), "ShowUntilInput")));

            index -= 11;

            codes.RemoveRange(index, 12);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCCabinSafe),
                new CodeInstruction(OpCodes.Ldstr, "You found a strange card inside."),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GiveGBCCheckWithMessageSequence"))
            };

            codes.InsertRange(index, newCodes);

            index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(StoryEventsData), "SetEventCompleted")));

            index -= 3;

            codes.RemoveRange(index, 4);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2ObolCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(BrokenCoin).GetNestedType("<OnOtherCardAssignedToSlot>d__3", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceObolWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            CodeInstruction messageInstruction = codes.Find(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "You received an Ancient Obol.");
            messageInstruction.operand = $"You received an Ancient Obol...but it turned itself into a strange card.";

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(StoryEventsData), "SetEventCompleted")));

            index -= 3;

            codes.RemoveRange(index, 4);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCAncientObol),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArchipelagoManager), "SendCheck"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act2CameraCheckPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(LeshyDialogueNPC).GetNestedType("<PostDialogueSequence>d__5", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceCameraWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.LoadsField(AccessTools.Field(typeof(NatureTempleSaveData), "hasCamera")));

            index -= 2;

            codes.RemoveRange(index, 3);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCCameraReplica),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ArchipelagoManager), "HasCompletedCheck"))
            };

            codes.InsertRange(index, newCodes);

            index = codes.FindIndex(x => x.StoresField(AccessTools.Field(typeof(NatureTempleSaveData), "hasCamera")));

            index -= 3;

            codes.RemoveRange(index, 4);

            newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4, (int)APCheck.GBCCameraReplica),
                new CodeInstruction(OpCodes.Ldstr, "The camera suddenly turned into a strange card."),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GiveGBCCheckWithMessage"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class TarotCardsPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(TraderMaskInteractable).GetNestedType("<DialogueSequence>d__11", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceTarotCardsWithCheck(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(TraderMaskInteractable), "ChooseTarotSequence")));

            codes.RemoveAt(index);

            codes.Insert(index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "TraderPeltRewardCheckSequence")));

            return codes.AsEnumerable();
        }
    }
}
