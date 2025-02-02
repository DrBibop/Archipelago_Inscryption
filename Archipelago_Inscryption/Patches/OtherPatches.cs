using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using System.IO;
using BepInEx;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    internal class OtherPatches
    {
        [HarmonyPatch(typeof(AchievementManager), "Unlock")]
        [HarmonyPrefix]
        static bool PreventAchievementUnlock()
        {
            return false;
        }

        [HarmonyPatch(typeof(SaveManager), "get_SaveFolderPath")]
        [HarmonyPrefix]
        static bool ReplaceSaveFilePath(ref string __result)
        {
            if (ArchipelagoData.saveName == "") return true;

            __result = Path.Combine(Paths.GameRootPath, "ArchipelagoSaveFiles", ArchipelagoData.saveName) + "/";

            return false;
        }

        [HarmonyPatch(typeof(SaveManager), "get_SaveFilePath")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceSaveFileName(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            CodeInstruction fileNameInstruction = codes.Find(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "SaveFile.gwsave");

            fileNameInstruction.operand = "SaveFile-Archipelago.gwsave";

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(SaveManager), "SaveToFile")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceBackUpSaveFileName(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            CodeInstruction fileNameInstruction = codes.Find(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "SaveFile-Backup.gwsave");

            fileNameInstruction.operand = "SaveFile-Archipelago-Backup.gwsave";

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(SaveManager), "SaveToFile")]
        [HarmonyPostfix]
        static void SaveArchipelagoDataToFile()
        {
            ArchipelagoData.SaveToFile();
        }

        [HarmonyPatch(typeof(SaveFile), "GetCurrentRandomSeed")]
        [HarmonyPostfix]
        static void AddOpenedPacksToSeed(SaveFile __instance, ref int __result)
        {
            if (__instance.IsPart1 || __instance.IsPart3)
                __result += __instance.gbcData.packsOpened * 2;
            if (__instance.IsPart1)
                __result += RunState.Run.currentNodeId * 100 * SaveManager.saveFile.pastRuns.Count + 1;
            if (__instance.IsPart2)
                __result += (SaveManager.saveFile.gbcData.npcAttempts + 1) * 50;
            if (__instance.IsPart3)
                __result += (Part3SaveData.Data.bounty + 1) * 50;
        }

        [HarmonyPatch(typeof(PageContentLoader), "LoadPage")]
        [HarmonyPostfix]
        static void ChangeRulebookPassword(PageContentLoader __instance)
        {
            if (ArchipelagoOptions.randomizeCodes && __instance.currentAdditiveObjects.Count > 0 && __instance.currentAdditiveObjects.First().name.Contains("SafePassword"))
            {
                GameObject passwordObject = __instance.currentAdditiveObjects.First();
                TextMeshPro[] texts = passwordObject.GetComponentsInChildren<TextMeshPro>(true);

                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].gameObject.name.Contains("(1)"))
                        texts[i].text = ArchipelagoData.Data.cabinSafeCode[1].ToString();
                    else if (texts[i].gameObject.name.Contains("(2)"))
                        texts[i].text = ArchipelagoData.Data.cabinSafeCode[2].ToString();
                    else
                        texts[i].text = ArchipelagoData.Data.cabinSafeCode[0].ToString();
                }
            }
        }

        [HarmonyPatch(typeof(Part1FinaleSceneSequencer), "ShowArm")]
        [HarmonyPostfix]
        static void ChangeSmallClockPassword(Part1FinaleSceneSequencer __instance)
        {
            if (!ArchipelagoOptions.randomizeCodes || __instance.deckTrialSequencer.transform.parent.Find("SmallClockClue(Clone)") != null) return;

            GameObject table = __instance.deckTrialSequencer.transform.parent.Find("Cube").gameObject;
            table.GetComponent<MeshRenderer>().material.mainTexture = AssetsManager.boonTableTex;

            GameObject clue = GameObject.Instantiate(AssetsManager.smallClockCluePrefab, table.transform.parent);
            clue.GetComponent<MeshRenderer>().material.mainTexture = AssetsManager.smallClockClueTexs[ArchipelagoData.Data.cabinSmallClockCode[2]];
            clue.transform.localPosition = new Vector3(-1.6744f, 1.6f, -0.9f);
            clue.transform.localEulerAngles = new Vector3(90, 90, 0);
            clue.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);

        }

        [HarmonyPatch(typeof(DogFoodBowlVolume), "Start")]
        [HarmonyPostfix]
        static void ReplaceGBCSafeCodeClue(DogFoodBowlVolume __instance)
        {
            if (!ArchipelagoOptions.randomizeCodes) return;

            SpriteRenderer floor = __instance.transform.root.Find("OutdoorsCentral/Floor").GetComponent<SpriteRenderer>();

            floor.sprite = AssetsManager.editedNatureFloorSprite;

            GameObject codeClue = GameObject.Instantiate(AssetsManager.gbcSafeCluePrefab, floor.transform);
            codeClue.layer = LayerMask.NameToLayer("GBCPixel");
            string codeText = "";
            foreach (int digit in ArchipelagoData.Data.cabinSafeCode)
            {
                codeText += digit.ToString();
            }
            codeClue.GetComponent<TextMesh>().text = codeText;
            codeClue.GetComponent<MeshRenderer>().sortingOrder = -9;
            codeClue.transform.localPosition = new Vector3(-0.56f, 0.1f, 0f);
        }

        [HarmonyPatch(typeof(SafeVolume), "IsSolved")]
        [HarmonyPrefix]
        static bool ReplaceGBCSafeCode(SafeVolume __instance, ref bool __result)
        {
            if (ArchipelagoOptions.randomizeCodes)
            {
                __result = SaveData.Data.natureTemple.safeState.sliderPositions[0] == ArchipelagoData.Data.cabinSafeCode[0] 
                    && SaveData.Data.natureTemple.safeState.sliderPositions[1] == ArchipelagoData.Data.cabinSafeCode[1]
                    && SaveData.Data.natureTemple.safeState.sliderPositions[2] == ArchipelagoData.Data.cabinSafeCode[2];

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(TurnManager), "PlayerIsWinner")]
        [HarmonyPrefix]
        static bool PlayerLostIfDeathLink(ref bool __result)
        {
            if (DeathLinkManager.receivedDeath)
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Part1BossOpponent), "HasGrizzlyGlitchPhase")]
        [HarmonyPrefix]
        static bool RemoveGrizzlyScriptedDeath(ref bool __result)
        {
            if (!ArchipelagoOptions.skipTutorial && !ArchipelagoOptions.deathlink)
                return true;

            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(Part3GameFlowManager), "SceneSpecificInitialization")]
        [HarmonyPostfix]
        static void FixPart3TransitionAfterPart2()
        {
            if (!StoryEventsData.EventCompleted(StoryEvent.Part3Intro)) return;

            PauseMenu.pausingDisabled = false;
            GameObject transitionSound = GameObject.Find("GlitchTransitionSound");
            if (transitionSound != null)
            {
                AudioController.Instance.FadeSourceVolume(transitionSound.GetComponent<AudioSource>(), 0f, 4f);
            }
        }

        [HarmonyPatch(typeof(PedestalVolume), "Start")]
        [HarmonyPostfix]
        static void ChangePedestalCode(PedestalVolume __instance)
        {
            if (!ArchipelagoOptions.randomizeCodes) return;

            if (__instance.Index == 0)
                __instance.solution = ArchipelagoData.Data.wizardCode1.ToArray();
            else if (__instance.Index == 1)
                __instance.solution = ArchipelagoData.Data.wizardCode2.ToArray();
            else if (__instance.Index == 2)
            {
                __instance.solution = ArchipelagoData.Data.wizardCode3.ToArray();

                GameObject backroomClue = GameObject.Find("/Temple/BackRoom_3/WizardMarking_F3_2/icon");
                ArchipelagoOptions.SetClueSprite(backroomClue.GetComponent<SpriteRenderer>(), 2, 1);

                GameObject menuClue = GameObject.Find("/GBCCameras/UI/PauseMenu/MenuParent/Menu/OptionsUI/MainPanel/TabGroup_Audio/WizardMarking_F3_3/icon");
                ArchipelagoOptions.SetClueSprite(menuClue.GetComponent<SpriteRenderer>(), 2, 2);
            }
        }

        [HarmonyPatch(typeof(OilPaintingPuzzle), "GenerateSolution")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChangePaintingAnimal(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int codeIndex = codes.FindIndex(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "Squirrel");

            codes.RemoveAt(codeIndex);

            codes.Insert(codeIndex, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "GetPaintingAnimal")));

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(ProgressionData), "LearnedMechanic")]
        [HarmonyPrefix]
        static bool LearnedFirstPersonIfAct3(MechanicsConcept mechanic, ref bool __result)
        {
            if (mechanic == MechanicsConcept.FirstPersonNavigation && Singleton<GameFlowManager>.Instance is Part3GameFlowManager)
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(CardAbilityIcons), "PositionModIcons")]
        [HarmonyPostfix]
        static void ModIconList(List<Ability> defaultAbilities, List<Ability> mergeAbilities, List<AbilityIconInteractable> mergeIcons,
                        List<Ability> totemAbilities, List<AbilityIconInteractable> totemIcons, CardAbilityIcons __instance)
        {
            if (defaultAbilities.Count == 0)
            {
                if (mergeAbilities.Count == 1 && mergeIcons.Count > 0)
                {
                    mergeIcons[0].transform.localPosition = __instance.DefaultIconPosition;
                    return;
                }
                if (totemAbilities.Count == 1 && totemIcons.Count > 0)
                {
                    totemIcons[0].transform.localPosition = __instance.DefaultIconPosition;
                }
            }
        }

        [HarmonyPatch(typeof(FactoryScannerScreen), "CheckBuildACardMatch")]
        [HarmonyPostfix]
        static void RememberCustomCard(BuildACardInfo info)
        {
            RandomizerHelper.AddCustomMod(info.mod, info.GetName());
        }

        [HarmonyPatch(typeof(MycologistsBossOpponent), "AddMycoCardToDeck")]
        [HarmonyPostfix]
        static void RememberMycoCard()
        {
            RandomizerHelper.AddMycoMod(Part3SaveData.Data.deck.Cards.First(c => c.name == "!MYCOCARD_BASE").mods.First());
        }

        [HarmonyPatch(typeof(Part1RareChoiceGenerator), "GenerateChoices")]
        [HarmonyPrefix]
        static bool RareCardChooserAct1(ref List<CardChoice> __result, Part1RareChoiceGenerator __instance, CardChoicesNodeData data, int randomSeed)
        {
            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
            __result = new List<CardChoice>();
            for (int i = 0; i < __instance.NUM_CHOICES; i++)
            {
                CardInfo card = RandomizerHelper.RandomRareCardInAct1(seed++);
                if (CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature).Count >= __instance.NUM_CHOICES)
                {
                    while (__result.Exists((CardChoice x) => x.CardInfo.name == card.name) && (ArchipelagoManager.HasItem(APItem.GreatKrakenCard) || card.name != "Kraken"))
                        card = RandomizerHelper.RandomRareCardInAct1(seed++);
                }
                __result.Add(new CardChoice { CardInfo = card });
            }
            return false;
        }
    }

    [HarmonyPatch]
    class DeathPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(Part1GameFlowManager).GetNestedType("<PlayerLostBattleSequence>d__9", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CallPreDeathInstead(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(ViewManager), "SwitchToView")));

            index += 3;

            codes.RemoveAt(index);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "PrePlayerDeathSequence"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class AfterDeathPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(Part1GameFlowManager).GetNestedType("<KillPlayerSequence>d__13", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CallPreDeathInstead(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.opcode == OpCodes.Ldstr && (string)x.operand == "Demo_End");

            index -= 3;

            codes.RemoveRange(index, 5);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "AfterPlayerDeathSequence"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class DeathLinkPatch
    {
        [HarmonyPatch(typeof(CardBattleNPC), "PostCombatEncounterSequence")]
        [HarmonyPrefix]
        static bool SendDeathLinkOnPart2(bool playerDefeated)
        {
            if (DeathLinkManager.receivedDeath)
                return true;
            if (playerDefeated)
            {
                DeathLinkManager.SendDeathLink();
            }
            return true;
        }

        [HarmonyPatch(typeof(Part3GameFlowManager), "PlayerRespawnSequence")]
        [HarmonyPrefix]
        static bool SendDeathLinkOnPart3()
        {
            if (DeathLinkManager.receivedDeath)
                return true;
            DeathLinkManager.SendDeathLink();
            return true;
        }
    }

    [HarmonyPatch]
    class BlowOutCandlePatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(Part1GameFlowManager).GetNestedType("<PlayerLostBattleSequence>d__9", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> BlowOutAllCandlesIfDeathLink(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(CandleHolder), "BlowOutCandleSequence")));

            index -= 2;

            codes.RemoveRange(index, 3);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "BlowOutOneOrAllCandles"))
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

            codes.Insert(index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "OldDataOpened")));

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class Act3EndingPatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(Part3FinaleAreaSequencer).GetNestedType("<FallSequence>d__2", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> GoToMenuIfAnyOrder(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(SceneLoader), "StartAsyncLoad")));

            index--;

            codes.RemoveRange(index, 7);

            codes.Insert(index, new CodeInstruction(OpCodes.Pop));

            codes.Insert(index + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RandomizerHelper), "OnStartLoadEpilogue")));

            index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(SceneLoader), "CompleteAsyncLoad")));

            index -= 2;

            codes.RemoveRange(index, 3);

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    class CampfirePatch
    {
        static MethodBase TargetMethod()
        {
            return typeof(CardStatBoostSequencer).GetNestedType("<StatBoostSequence>d__12", BindingFlags.NonPublic | BindingFlags.Instance).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CheckForSkipTutorial(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.LoadsField(AccessTools.Field(typeof(SaveFile), "pastRuns")));

            index += 5;

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ArchipelagoOptions), "skipTutorial")),
                new CodeInstruction(OpCodes.Or)
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }
    }
}