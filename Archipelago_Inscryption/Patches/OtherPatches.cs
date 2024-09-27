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

        [HarmonyPatch(typeof(DeckInfo), "InitializeAsPlayerDeck")]
        [HarmonyPrefix]
        static bool RandomizeStarterDeckAct1(DeckInfo __instance)
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.StarterOnly)
                return true;

            int nbCardsToAdd = 4;

            if (StoryEventsData.EventCompleted(StoryEvent.CageCardDiscovered) && !StoryEventsData.EventCompleted(StoryEvent.WolfCageBroken))
            {
                __instance.AddCard(CardLoader.GetCardByName("CagedWolf"));
                nbCardsToAdd--;
            }

            List<CardInfo> cardsInfoRandomPool = RandomizerHelper.GenerateCardPoolAct1();

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            for (int i = 0; i < nbCardsToAdd; i++)
            {
                CardInfo card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
                cardsInfoRandomPool.Remove(card);
                __instance.AddCard(card.Mods.Any(x => x.deathCardInfo != null) ? card : CardLoader.Clone(card));
            }

            return false;
        }

        [HarmonyPatch(typeof(StarterDecks), "GetDeck")]
        [HarmonyPrefix]
        static bool RandomizeStarterDeckAct2(CardTemple temple, ref List<string> __result)
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.StarterOnly)
                return true;

            __result = new List<string>();
            List<CardInfo> cardsRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.metaCategories.Contains(CardMetaCategory.GBCPlayable)
                                         && ConceptProgressionTree.Tree.CardUnlocked(x, false) && x.pixelPortrait != null && !x.metaCategories.Contains(CardMetaCategory.Rare));

            cardsRandomPool.RemoveAll(c => c.name == "Kraken");
            cardsRandomPool.RemoveAll(c => c.name == "BonelordHorn");
            cardsRandomPool.RemoveAll(c => c.name == "DrownedSoul");
            cardsRandomPool.RemoveAll(c => c.name == "Salmon");

            switch (temple)
            {
                case CardTemple.Nature:
                    for (int i = 0; i < 7; i++)
                    {
                        __result.Add("Squirrel");
                    }
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Nature && x.name != "Squirrel");
                    break;
                case CardTemple.Undead:
                    for (int i = 0; i < 7; i++)
                    {
                        __result.Add("Skeleton");
                    }
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Undead && x.name != "Skeleton");
                    break;
                case CardTemple.Tech:
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Tech);
                    break;
                case CardTemple.Wizard:
                    for (int i = 0; i < 3; i++)
                    {
                        __result.Add("MoxSapphire");
                        __result.Add("MoxRuby");
                        __result.Add("MoxEmerald");
                    }
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Wizard && x.name != "MoxSapphire" && x.name != "MoxRuby" && x.name != "MoxEmerald");
                    break;
                default:
                    __result = StarterDecks.NATURE_STARTER;
                    break;
            }

            int seed = SaveManager.saveFile.GetCurrentRandomSeed();

            while (__result.Count < 20)
            {
                __result.Add(cardsRandomPool[SeededRandom.Range(0, cardsRandomPool.Count, seed++)].name);
            }

            return false;
        }

        [HarmonyPatch(typeof(Part3GameFlowManager), "SceneSpecificInitialization")]
        [HarmonyPrefix]
        static bool RandomizeStarterDeckAct3()
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.StarterOnly || StoryEventsData.EventCompleted(StoryEvent.Part3Intro))
                return true;

            Part3SaveData.Data.deck.RemoveCardByName("BatteryBot");
            Part3SaveData.Data.deck.RemoveCardByName("Shieldbot");
            Part3SaveData.Data.deck.RemoveCardByName("Sniper");
            Part3SaveData.Data.deck.RemoveCardByName("CloserBot");

            List<CardInfo> randomCards = new List<CardInfo>();
            int randomSeed = SaveManager.SaveFile.GetCurrentRandomSeed();
            for (int i = 0; i < 4; i++)
            {
                CardInfo card = CardLoader.GetRandomChoosableCard(randomSeed++, CardTemple.Tech);
                while (randomCards.Exists(x => x.name == card.name))
                {
                    card = CardLoader.GetRandomChoosableCard(randomSeed++, CardTemple.Tech);
                }

                randomCards.Add(card);
            }

            foreach (CardInfo card in randomCards)
            {
                Part3SaveData.Data.deck.AddCard(card);
            }

            return true;
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
        static bool RandomizeDeckAct1(MapNode __instance)
        {
            if ((ArchipelagoOptions.randomizeDeck == RandomizeDeck.Disable || ArchipelagoOptions.randomizeDeck == RandomizeDeck.StarterOnly) && ArchipelagoOptions.randomizeSigils == RandomizeSigils.Disable)
                return true;

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType || ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPool = RandomizerHelper.GenerateCardPoolAct1();
                List<string> newCardsIds = new List<string>();

                foreach (CardInfo c in RunState.Run.playerDeck.Cards)
                {
                    CardInfo card = c;
                    if (c.name == "CagedWolf")
                    {
                        newCardsIds.Add(c.name);
                        newCards.Add(c);
                        continue;
                    }
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.HasTrait(Trait.Pelt))
                        {
                            newCardsIds.Add(c.name);
                            newCards.Add(c);
                            continue;
                        }
                        else if (c.metaCategories.Contains(CardMetaCategory.Rare))
                        {
                            card = RandomizerHelper.RandomRareCardInAct1(seed++);
                        }
                        else
                        {
                            card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];

                            RandomizerHelper.RemoveUniqueAct1CardIfApplicable(ref cardsInfoRandomPool, ref card);
                        }
                    }
                    else
                    {
                        card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];

                        RandomizerHelper.RemoveUniqueAct1CardIfApplicable(ref cardsInfoRandomPool, ref card);
                    }

                    if (!card.mods.Any(x => x.deathCardInfo != null))
                        card = (CardInfo)card.Clone();

                    foreach (Ability ability in c.Abilities)
                    {
                        if (!ProgressionData.LearnedAbility(ability))
                        {
                            ProgressionData.SetAbilityLearned(ability);
                        }
                    }

                    foreach (CardModificationInfo mod in c.Mods)
                    {
                        if (mod.deathCardInfo != null)
                        {
                            continue;
                        }

                        card.mods.Add(mod);
                    }

                    card.decals = c.decals;
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }

                RunState.Run.playerDeck.CardInfos = newCards;
                RunState.Run.playerDeck.cardIds = newCardsIds;
            }

            if (ArchipelagoOptions.randomizeSigils != RandomizeSigils.Disable)
            {
                foreach (CardInfo c in RunState.Run.playerDeck.Cards)
                {
                    List<AbilityInfo> learnedAbilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(
                        x => x.metaCategories.Contains(AbilityMetaCategory.Part1Modular) 
                        && x.metaCategories.Contains(AbilityMetaCategory.Part1Rulebook) 
                        && x.ability != Ability.RandomAbility
                        && x.ability != Ability.CreateEgg
                        && x.ability != Ability.HydraEgg);

                    foreach (CardModificationInfo mod in c.Mods)
                    {
                        if (mod.deathCardInfo != null)
                        {
                            continue;
                        }
                        if (mod.fromCardMerge)
                        {
                            if (mod.abilities.Count > 0)
                            {
                                int abilityCount = mod.abilities.Count;
                                mod.abilities = new List<Ability>();
                                for (int l = 0; l < abilityCount; l++)
                                {
                                    learnedAbilities.RemoveAll(x => c.HasAbility(x.ability));
                                    if (learnedAbilities.Count > 0)
                                    {
                                        mod.abilities.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                                    }
                                }
                            }
                        }
                    }

                    if (ArchipelagoOptions.randomizeSigils == RandomizeSigils.RandomizeAll)
                    {
                        CardModificationInfo deathCardMod = c.Mods.FirstOrDefault(m => m.deathCardInfo != null);
                        bool isDeathCard = deathCardMod != null;

                        int abilityCount = isDeathCard ? deathCardMod.abilities.Count : c.abilities.Count;

                        if (isDeathCard)
                            deathCardMod.abilities = new List<Ability>();
                        else
                            c.abilities = new List<Ability>();

                        for (int t = 0; t < abilityCount; t++)
                        {
                            learnedAbilities.RemoveAll(x => c.HasAbility(x.ability));
                            if (learnedAbilities.Count > 0)
                            {
                                if (isDeathCard)
                                    deathCardMod.abilities.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                                else
                                    c.abilities.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                            }
                        }
                    }
                }
            }

            RunState.Run.playerDeck.UpdateModDictionary();

            return true;
        }

        [HarmonyPatch(typeof(GBCEncounterManager), "StartEncounter")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct2()
        {
            if ((ArchipelagoOptions.randomizeDeck == RandomizeDeck.Disable || ArchipelagoOptions.randomizeDeck == RandomizeDeck.StarterOnly) && ArchipelagoOptions.randomizeSigils == RandomizeSigils.Disable)
                return true;

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType || ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
            {
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
                                         && ConceptProgressionTree.Tree.CardUnlocked(x, false) && x.pixelPortrait != null);

                if (!ArchipelagoManager.HasItem(APItem.GreatKrakenCard))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "Kraken");
                }
                if (!ArchipelagoManager.HasItem(APItem.BoneLordHorn))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "BonelordHorn");
                }
                if (!ArchipelagoManager.HasItem(APItem.DrownedSoulCard))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "DrownedSoul");
                }
                if (!ArchipelagoManager.HasItem(APItem.SalmonCard))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "Salmon");
                }

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
                    cardsInfoRandomPoolNature = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Nature);
                    cardsInfoRandomPoolNatureRare = cardsInfoRandomPoolNature.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolNature = cardsInfoRandomPoolNature.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolUndead = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Undead);
                    cardsInfoRandomPoolUndeadRare = cardsInfoRandomPoolUndead.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolUndead = cardsInfoRandomPoolUndead.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolTech = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Tech);
                    cardsInfoRandomPoolTechRare = cardsInfoRandomPoolTech.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolTech = cardsInfoRandomPoolTech.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolWizard = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Wizard);
                    cardsInfoRandomPoolWizardRare = cardsInfoRandomPoolWizard.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolWizard = cardsInfoRandomPoolWizard.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));

                    cardsInfoRandomPoolNature.RemoveAll(x => x.name == "Squirrel");
                    cardsInfoRandomPoolUndead.RemoveAll(x => x.name == "Skeleton");
                    cardsInfoRandomPoolWizard.RemoveAll(x => x.name == "MoxSapphire" || x.name == "MoxRuby" || x.name == "MoxEmerald");
                }

                foreach (var c in SaveData.Data.deck.Cards)
                {
                    if (cardAdded > 0)
                    {
                        cardAdded--;
                        continue;
                    }
                    CardInfo card = c;
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.metaCategories.Contains(CardMetaCategory.Rare))
                        {
                            switch (c.temple)
                            {
                                case CardTemple.Nature:
                                    card = cardsInfoRandomPoolNatureRare[SeededRandom.Range(0, cardsInfoRandomPoolNatureRare.Count, seed++)];
                                    break;
                                case CardTemple.Undead:
                                    card = cardsInfoRandomPoolUndeadRare[SeededRandom.Range(0, cardsInfoRandomPoolUndeadRare.Count, seed++)];
                                    break;
                                case CardTemple.Tech:
                                    card = cardsInfoRandomPoolTechRare[SeededRandom.Range(0, cardsInfoRandomPoolTechRare.Count, seed++)];
                                    break;
                                case CardTemple.Wizard:
                                    card = cardsInfoRandomPoolWizardRare[SeededRandom.Range(0, cardsInfoRandomPoolWizardRare.Count, seed++)];
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            if (c.name == "Squirrel" || c.name == "Skeleton" || c.name == "MoxSapphire" || c.name == "MoxRuby" || c.name == "MoxEmerald")
                            {
                                card = c;
                            }

                            switch (c.temple)
                            {
                                case CardTemple.Nature:
                                    card = cardsInfoRandomPoolNature[SeededRandom.Range(0, cardsInfoRandomPoolNature.Count, seed++)];
                                    break;
                                case CardTemple.Undead:
                                    card = cardsInfoRandomPoolUndead[SeededRandom.Range(0, cardsInfoRandomPoolUndead.Count, seed++)];
                                    break;
                                case CardTemple.Tech:
                                    card = cardsInfoRandomPoolTech[SeededRandom.Range(0, cardsInfoRandomPoolTech.Count, seed++)];
                                    break;
                                case CardTemple.Wizard:
                                    card = cardsInfoRandomPoolWizard[SeededRandom.Range(0, cardsInfoRandomPoolWizard.Count, seed++)];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        card = cardsInfoRandomPoolAll[SeededRandom.Range(0, cardsInfoRandomPoolAll.Count, seed++)];
                    }

                    card = (CardInfo)card.Clone();

                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                SaveData.Data.deck.CardInfos = newCards;
                SaveData.Data.deck.cardIds = newCardsIds;
            }

            if (ArchipelagoOptions.randomizeSigils == RandomizeSigils.RandomizeAll)
            {
                foreach (CardInfo card in SaveData.Data.deck.Cards)
                {
                    List<AbilityInfo> learnedAbilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(x => x.pixelIcon != null 
                    && x.ability != Ability.ActivatedSacrificeDrawCards && x.ability != Ability.CreateEgg 
                    && x.ability != Ability.HydraEgg && x.ability != Ability.Tutor);

                    int baseAbilityCount = card.abilities.Count;

                    card.abilities = new List<Ability>();

                    for (int t = 0; t < baseAbilityCount; t++)
                    {
                        learnedAbilities.RemoveAll(x => card.HasAbility(x.ability));
                        if (learnedAbilities.Count > 0)
                        {
                            AbilityInfo randomAbility = learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)];
                            card.abilities.Add(randomAbility.ability);

                            if (randomAbility.activated)
                                learnedAbilities.RemoveAll(x => x.activated);

                            if (randomAbility.conduit)
                                learnedAbilities.RemoveAll(x => x.conduit);
                        }
                    }
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(HoloMapNode), "OnSelected")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct3()
        {
            if ((ArchipelagoOptions.randomizeDeck == RandomizeDeck.Disable || ArchipelagoOptions.randomizeDeck == RandomizeDeck.StarterOnly) && ArchipelagoOptions.randomizeSigils == RandomizeSigils.Disable)
                return true;

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType || ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.temple == CardTemple.Tech && x.portraitTex != null 
                                                     && x.name != "!BOUNTYHUNTER_BASE" && x.name != "Librarian" && !x.name.Contains("EmptyVessel") 
                                                     && x.name != "!MYCOCARD_BASE" && x.name != "CaptiveFile" && x.name != "!BUILDACARD_BASE");
                cardsInfoRandomPool.AddRange(RandomizerHelper.GetAllCustomCards());
                List<CardInfo> cardsInfoRandomGemPool = cardsInfoRandomPool;
                List<CardInfo> cardsInfoRandomConduitPool = cardsInfoRandomPool;
                if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                {
                    cardsInfoRandomConduitPool = cardsInfoRandomPool.FindAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell"));
                    cardsInfoRandomGemPool = cardsInfoRandomPool.FindAll(x => x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                    cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell") || x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                }
                else
                {
                    if (!StoryEventsData.EventCompleted(StoryEvent.GemsModuleFetched))
                        cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                    if (!Part3SaveData.Data.sideDeckAbilities.Contains(Ability.ConduitNull))
                        cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell"));
                }
                if (ArchipelagoManager.HasItem(APItem.LonelyWizbotCard))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("BlueMage_Talking"));
                if (ArchipelagoManager.HasItem(APItem.FishbotCard))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("Angler_Talking"));
                if (!ArchipelagoManager.HasItem(APItem.Ourobot))
                    cardsInfoRandomPool.RemoveAll(x => x.name == "Ouroboros_Part3");
                foreach (CardInfo c in Part3SaveData.Data.deck.Cards)
                {
                    CardInfo card = c;
                    if (card.name == "!MYCOCARD_BASE" && card.mods.Count > 0)
                    {
                        card.mods.Remove(card.mods.First());
                    }

                    int abilityCount = 0;
                    do
                    {
                        card = RandomizerHelper.RandomizeOneCardAct3(ref seed, ref cardsInfoRandomPool, ref cardsInfoRandomGemPool, ref cardsInfoRandomConduitPool, c);
                        if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                            abilityCount = card.mods[0].abilities.Count;
                        else
                            abilityCount = card.abilities.Count;
                        foreach (var modCurrent in c.Mods)
                        {
                            if (modCurrent.buildACardPortraitInfo != null)
                                continue;
                            if (modCurrent.abilities.Count > 0)
                            {
                                foreach (var ability in modCurrent.abilities)
                                    abilityCount++;
                            }
                        }
                    } while (abilityCount > 4);

                    foreach (var modCurrent in c.Mods)
                    {
                        if (modCurrent.buildACardPortraitInfo != null)
                        {
                            continue;
                        }
                        
                        card.mods.Add(modCurrent);
                    }

                    card.decals = c.decals;
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                Part3SaveData.Data.deck.CardInfos = newCards;
                Part3SaveData.Data.deck.cardIds = newCardsIds;
            }

            if (ArchipelagoOptions.randomizeSigils != RandomizeSigils.Disable)
            {
                foreach (CardInfo card in Part3SaveData.Data.deck.Cards)
                {
                    List<AbilityInfo> learnedAbilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(x => x.metaCategories.Contains(AbilityMetaCategory.Part3Modular));
                    foreach (var modCurrent in card.Mods)
                    {
                        if (modCurrent.buildACardPortraitInfo != null)
                        {
                            continue;
                        }

                        if (ArchipelagoOptions.randomizeSigils != RandomizeSigils.Disable)
                        {
                            if (modCurrent.abilities.Count > 0)
                            {
                                int moddedAbilityCount = modCurrent.abilities.Count;
                                if (modCurrent.abilities.Contains(Ability.PermaDeath))
                                    modCurrent.attackAdjustment--;
                                modCurrent.abilities = new List<Ability>();
                                for (int l = 0; l < moddedAbilityCount; l++)
                                {
                                    learnedAbilities.RemoveAll(x => card.HasAbility(x.ability));
                                    if (learnedAbilities.Count > 0)
                                    {
                                        Ability ab = learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability;
                                        modCurrent.abilities.Add(ab);

                                        if (ab == Ability.PermaDeath)
                                            modCurrent.attackAdjustment++;
                                    }
                                }
                            }
                        }
                    }

                    if (ArchipelagoOptions.randomizeSigils == RandomizeSigils.RandomizeAll)
                    {
                        List<Ability> abilityList = (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE") ? card.mods.First().abilities : card.abilities;
                        int baseAbilityCount = abilityList.Count;

                        if (abilityList.Contains(Ability.PermaDeath))
                        {
                            if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                                card.mods.First().attackAdjustment--;
                            else
                                card.baseAttack--;
                        }

                        if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                            card.mods.First().abilities = new List<Ability>();
                        else
                            card.abilities = new List<Ability>();

                        abilityList = (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE") ? card.mods.First().abilities : card.abilities;

                        for (int t = 0; t < baseAbilityCount; t++)
                        {
                            learnedAbilities.RemoveAll(x => card.HasAbility(x.ability));
                            if (learnedAbilities.Count > 0)
                            {
                                abilityList.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                            }
                        }

                        if (abilityList.Contains(Ability.PermaDeath))
                        {
                            if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                                card.mods.First().attackAdjustment++;
                            else
                                card.baseAttack++;
                        }
                    }
                }
            }

            Part3SaveData.Data.deck.UpdateModDictionary();

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
            RandomizerHelper.AddCustomMod(info.mod , info.GetName());
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