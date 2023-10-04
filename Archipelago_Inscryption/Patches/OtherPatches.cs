using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using Unity;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using System;
using Archipelago.MultiClient.Net.Models;

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

        [HarmonyPatch(typeof(SaveManager), "CreateNewSaveFile")]
        [HarmonyPrefix]
        static bool EraseArchipelagoData()
        {
            if (ArchipelagoClient.IsConnected)
                ArchipelagoClient.Disconnect();
            ArchipelagoData.Data.Reset();
            ArchipelagoData.Data.seed = "";
            return true;
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
    class RandomizeDeckPatch
    {

        [HarmonyPatch(typeof(MapNode), "OnArriveAtNode")]
        [HarmonyPrefix]
        static bool RandomizeDeckWhenArrivingOnNode(MapNode __instance)
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.Disable)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<CardInfo> allAddedCards = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPool = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
                if (ArchipelagoManager.HasItem(APItem.CagedWolfCard) && !StoryEventsData.EventCompleted(StoryEvent.WolfCageBroken) && !(__instance.Data is CardRemoveNodeData))
                    allAddedCards.Add(CardLoader.GetCardByName("CagedWolf"));
                if (ArchipelagoManager.HasItem(APItem.StinkbugCard))
                    allAddedCards.Add(CardLoader.GetCardByName("Stinkbug_Talking"));
                if (ArchipelagoManager.HasItem(APItem.StuntedWolfCard))
                    allAddedCards.Add(CardLoader.GetCardByName("Wolf_Talking"));
                allAddedCards.AddRange(RandomizerHelper.GetAllDeathCards());
                if (!StoryEventsData.EventCompleted(StoryEvent.WolfCageBroken) && ArchipelagoManager.HasItem(APItem.CagedWolfCard) && __instance.Data is CardRemoveNodeData)
                {
                    CardInfo card = CardLoader.GetCardByName("CagedWolf");
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                {
                    cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.temple == CardTemple.Nature
                                          && x.metaCategories.Contains(CardMetaCategory.ChoiceNode) && !x.metaCategories.Contains(CardMetaCategory.AscensionUnlock)
                                          && !x.metaCategories.Contains(CardMetaCategory.Rare) && ConceptProgressionTree.Tree.CardUnlocked(x, false)
                                          && (ArchipelagoManager.HasItem(APItem.GreatKrakenCard) || x.name != "Kraken"));
                }
                else
                {
                    cardsInfoRandomPool.AddRange(ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => ((x.metaCategories.Contains(CardMetaCategory.Rare) || x.metaCategories.Contains(CardMetaCategory.ChoiceNode))
                                                 && x.temple == CardTemple.Nature && x.portraitTex != null && !x.metaCategories.Contains(CardMetaCategory.AscensionUnlock) && ConceptProgressionTree.Tree.CardUnlocked(x, false)
                                                 && (ArchipelagoManager.HasItem(APItem.GreatKrakenCard) || x.name != "Kraken")) || x.name == "Ouroboros"));
                }
                allAddedCards.Add(CardLoader.GetCardByName("Stoat_Talking"));
                cardsInfoRandomPool.AddRange(allAddedCards);
                foreach (CardInfo c in RunState.Run.playerDeck.Cards)
                {
                    CardInfo card = ScriptableObject.CreateInstance<CardInfo>();
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.metaCategories.Contains(CardMetaCategory.Rare))
                        {
                            card = RandomizerHelper.RandomRareCardInAct1(seed++);
                        }
                        else if (c.HasTrait(Trait.Pelt))
                        {
                            card = c;
                            continue;
                        }
                        else
                        {
                            card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
                            if (!card.mods.Any((CardModificationInfo x) => x.deathCardInfo != null))
                                card = (CardInfo)card.Clone();

                            RandomizerHelper.OnlyPutOneTalkingCardInDeckAct1(ref cardsInfoRandomPool, ref card);
                        }
                    }
                    else if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
                    {
                        card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
                        if (!card.mods.Any((CardModificationInfo x) => x.deathCardInfo != null))
                            card = (CardInfo)card.Clone();

                        RandomizerHelper.OnlyPutOneTalkingCardInDeckAct1(ref cardsInfoRandomPool, ref card);
                    }
                    else
                    {
                        if (!c.mods.Any(x => x.deathCardInfo != null))
                            card = (CardInfo)c.Clone();
                        else
                            card = (CardInfo)c;
                    }
                    foreach (CardModificationInfo mod in c.Mods)
                    {
                        if (mod.deathCardInfo != null)
                        {
                            continue;
                        }
                        if (ArchipelagoOptions.randomizeAbilities != RandomizeAbilities.Disable)
                        {
                            if (mod.fromCardMerge)
                            {
                                List<Ability> newAbilityMod = new List<Ability>();
                                if (mod.abilities.Count > 0)
                                {
                                    for (int l = 0; l < mod.abilities.Count; l++)
                                    {
                                        Ability abil = AbilitiesUtil.GetRandomLearnedAbility(seed++);
                                        while (card.HasAbility(abil))
                                            abil = AbilitiesUtil.GetRandomLearnedAbility(seed++);
                                        newAbilityMod.Add(abil);
                                    }
                                    mod.abilities = newAbilityMod;
                                }
                            }
                        }
                        card.mods.Add(mod);
                    }
                    if (ArchipelagoOptions.randomizeAbilities == RandomizeAbilities.RandomizeAll)
                    {
                        int abilityCount = card.abilities.Count;
                        card.abilities.Clear();
                        for (int t = 0; t < abilityCount; t++)
                            card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(seed++));
                    }
                    card.decals = c.decals;
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                RunState.Run.playerDeck.CardInfos = newCards;
                RunState.Run.playerDeck.cardIds = newCardsIds;
                RunState.Run.playerDeck.UpdateModDictionary();
            }
            return true;
        }

        [HarmonyPatch(typeof(GBCEncounterManager), "StartEncounter")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct2()
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.Disable)
            {
                int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPoolAll = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolNature = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolNatureRare = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolUndead = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolUndeadRare = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolTech = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolTechRare = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolWizard = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolWizardRare = new List<CardInfo>();
                int cardAdded = 0;
                cardsInfoRandomPoolAll = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.metaCategories.Contains(CardMetaCategory.GBCPlayable)
                                         && ConceptProgressionTree.Tree.CardUnlocked(x, false) && x.pixelPortrait != null && (ArchipelagoManager.HasItem(APItem.GreatKrakenCard) || x.name != "Kraken"));
                if (!ArchipelagoManager.HasCompletedCheck(APCheck.GBCAncientObol))
                {
                    CardInfo obolLeft = CardLoader.GetCardByName("CoinLeft");
                    newCards.Add(obolLeft);
                    newCardsIds.Add(obolLeft.name);
                    CardInfo obolRight = CardLoader.GetCardByName("CoinRight");
                    newCards.Add(obolRight);
                    newCardsIds.Add(obolRight.name);
                    cardAdded += 2;
                }
                if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                {
                    cardsInfoRandomPoolNature = cardsInfoRandomPoolAll.FindAll(x =>  x.temple == CardTemple.Nature);
                    cardsInfoRandomPoolNatureRare = cardsInfoRandomPoolNature.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolUndead = cardsInfoRandomPoolAll.FindAll(x =>  x.temple == CardTemple.Undead);
                    cardsInfoRandomPoolUndeadRare = cardsInfoRandomPoolUndead.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolTech = cardsInfoRandomPoolAll.FindAll(x =>  x.temple == CardTemple.Tech);
                    cardsInfoRandomPoolTechRare = cardsInfoRandomPoolTech.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolWizard = cardsInfoRandomPoolAll.FindAll(x =>  x.temple == CardTemple.Wizard);
                    cardsInfoRandomPoolWizardRare = cardsInfoRandomPoolWizard.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                }
                if (ArchipelagoManager.HasItem(APItem.DrownedSoulCard))
                {
                    cardsInfoRandomPoolAll.Add(CardLoader.GetCardByName("DrownedSoul"));
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        cardsInfoRandomPoolNature.Add(CardLoader.GetCardByName("DrownedSoul"));
                        cardsInfoRandomPoolUndead.Add(CardLoader.GetCardByName("DrownedSoul"));
                        cardsInfoRandomPoolTech.Add(CardLoader.GetCardByName("DrownedSoul"));
                        cardsInfoRandomPoolWizard.Add(CardLoader.GetCardByName("DrownedSoul"));
                    }

                }
                List<AbilityInfo> abilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(x => x.metaCategories.Contains(AbilityMetaCategory.GrimoraRulebook)
                                              || x.metaCategories.Contains(AbilityMetaCategory.MagnificusRulebook) || x.metaCategories.Contains(AbilityMetaCategory.Part1Modular)
                                              || x.metaCategories.Contains(AbilityMetaCategory.Part3Modular));
                foreach (var c in SaveData.Data.deck.Cards)
                {
                    if (cardAdded > 0)
                    {
                        cardAdded--;
                        continue;
                    }
                    CardInfo card = ScriptableObject.CreateInstance<CardInfo>();
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.metaCategories.Contains(CardMetaCategory.Rare))
                        {
                            switch (c.temple)
                            {
                                case CardTemple.Nature:
                                    card = (CardInfo)cardsInfoRandomPoolNatureRare[SeededRandom.Range(0, cardsInfoRandomPoolNatureRare.Count, seed++)].Clone();
                                    break;
                                case CardTemple.Undead:
                                    card = (CardInfo)cardsInfoRandomPoolUndeadRare[SeededRandom.Range(0, cardsInfoRandomPoolUndeadRare.Count, seed++)].Clone();
                                    break;
                                case CardTemple.Tech:
                                    card = (CardInfo)cardsInfoRandomPoolTechRare[SeededRandom.Range(0, cardsInfoRandomPoolTechRare.Count, seed++)].Clone();
                                    break;
                                case CardTemple.Wizard:
                                    card = (CardInfo)cardsInfoRandomPoolWizardRare[SeededRandom.Range(0, cardsInfoRandomPoolWizardRare.Count, seed++)].Clone();
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            switch (c.temple)
                            {
                                case CardTemple.Nature:
                                    card = (CardInfo)cardsInfoRandomPoolNature[SeededRandom.Range(0, cardsInfoRandomPoolNature.Count, seed++)].Clone();
                                    break;
                                case CardTemple.Undead:
                                    card = (CardInfo)cardsInfoRandomPoolUndead[SeededRandom.Range(0, cardsInfoRandomPoolUndead.Count, seed++)].Clone();
                                    break;
                                case CardTemple.Tech:
                                    card = (CardInfo)cardsInfoRandomPoolTech[SeededRandom.Range(0, cardsInfoRandomPoolTech.Count, seed++)].Clone();
                                    break;
                                case CardTemple.Wizard:
                                    card = (CardInfo)cardsInfoRandomPoolWizard[SeededRandom.Range(0, cardsInfoRandomPoolWizard.Count, seed++)].Clone();
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else 
                        card = (CardInfo)cardsInfoRandomPoolAll[SeededRandom.Range(0, cardsInfoRandomPoolAll.Count, seed++)].Clone();
                    if (card.name == "DrownedSoul")
                    {
                        if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                        {
                            cardsInfoRandomPoolNature.Remove(card);
                            cardsInfoRandomPoolTech.Remove(card);
                            cardsInfoRandomPoolUndead.Remove(card);
                            cardsInfoRandomPoolWizard.Remove(card);
                        }
                        else 
                        {
                            cardsInfoRandomPoolAll.Remove(card);
                        }
                    }
                    if (ArchipelagoOptions.randomizeAbilities == RandomizeAbilities.RandomizeAll)
                    {
                        int rand = 0;
                        int abilityCount = card.abilities.Count;
                        card.abilities.Clear();
                        for (int t = 0; t < abilityCount; t++)
                        {
                            rand = UnityEngine.Random.Range(0, 4);
                            if (rand == 0)
                                card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(seed++, false, 0, 5, AbilityMetaCategory.MagnificusRulebook));
                            else if (rand == 1)
                                card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(seed++, false, 0, 5, AbilityMetaCategory.GrimoraRulebook));
                            else if (rand == 2)
                                card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(seed++, false, 0, 5, AbilityMetaCategory.Part3Modular));
                            else
                                card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(seed++, false, 0, 5, AbilityMetaCategory.Part1Modular));
                        }
                    }
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                SaveData.Data.deck.CardInfos = newCards;
                SaveData.Data.deck.cardIds = newCardsIds;
                SaveData.Data.deck.UpdateModDictionary();
            }
            return true;
        }


        [HarmonyPatch(typeof(HoloMapNode), "OnSelected")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct3()
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.Disable)
            {
                int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.temple == CardTemple.Tech && x.portraitTex != null 
                                                     && x.name != "!BOUNTYHUNTER_BASE" && x.name != "Librarian" && !x.name.Contains("EmptyVessel") && x.name != "!MYCOCARD_BASE" 
                                                     && x.name != "CaptiveFile");
                if (!ArchipelagoManager.HasItem(APItem.GemsModule))
                    cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                if (!Part3SaveData.Data.sideDeckAbilities.Contains(Ability.ConduitNull))
                    cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell"));
                if (ArchipelagoManager.HasItem(APItem.LonelyWizbotCard))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("BlueMage_Talking"));
                if (ArchipelagoManager.HasItem(APItem.FishbotCard))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("Angler_Talking"));
                if (ArchipelagoManager.HasItem(APItem.Ourobot))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("Ouroboros_Part3"));
                foreach (CardInfo c in Part3SaveData.Data.deck.Cards)
                {
                    CardInfo card = ScriptableObject.CreateInstance<CardInfo>();
                    card = (CardInfo)cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)].Clone();
                    if (card.name == "BlueMage_Talking" || card.name == "Angler_Talking" || card.name == "Ouroboros_Part3")
                        cardsInfoRandomPool.Remove(card);
                    foreach (var modCurrent in c.Mods)
                    {
                        if (ArchipelagoOptions.randomizeAbilities != RandomizeAbilities.Disable)
                        {
                            if (modCurrent.fromCardMerge)
                            {
                                List<Ability> newAbilityMod = new List<Ability>();
                                if (modCurrent.abilities.Count > 0)
                                {
                                    for (int l = 0; l < modCurrent.abilities.Count; l++)
                                    {
                                        Ability abil = AbilitiesUtil.GetRandomLearnedAbility(seed++, false, 0, 5, AbilityMetaCategory.Part3Modular);
                                        while (card.HasAbility(abil))
                                            abil = AbilitiesUtil.GetRandomLearnedAbility(seed++, false, 0, 5, AbilityMetaCategory.Part3Modular);
                                        newAbilityMod.Add(abil);
                                    }
                                    modCurrent.abilities = newAbilityMod;
                                }
                            }
                        }
                        card.mods.Add(modCurrent);
                    }
                    card.decals = c.decals;
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                Part3SaveData.Data.deck.CardInfos = newCards;
                Part3SaveData.Data.deck.cardIds = newCardsIds;
                Part3SaveData.Data.deck.UpdateModDictionary();
            }
            return true;
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