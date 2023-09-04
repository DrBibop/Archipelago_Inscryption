using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Saves;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using Unity;
using InscryptionAPI.Card;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using System;
using InscryptionAPI.Nodes;

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
            ArchipelagoClient.serverData.completedChecks.Clear();
            ArchipelagoClient.serverData.receivedItems.Clear();
            ArchipelagoManager.cabinSafeCode.Clear();
            ArchipelagoManager.cabinClockCode.Clear();
            ArchipelagoManager.cabinSmallClockCode.Clear();
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "CompletedChecks", new List<long>());
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "ReceivedItems", new List<string>());
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "CabinSafeCode", new List<int>());
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "CabinClockCode", new List<int>());
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "CabinSmallClockCode", new List<int>());
            if (ArchipelagoClient.IsConnected)
                ArchipelagoClient.Disconnect();
            return true;
        }

        [HarmonyPatch(typeof(PageContentLoader), "LoadPage")]
        [HarmonyPostfix]
        static void ChangeRulebookPassword(PageContentLoader __instance)
        {
            if (ArchipelagoManager.randomizeCodes && __instance.currentAdditiveObjects.Count > 0 && __instance.currentAdditiveObjects.First().name.Contains("SafePassword"))
            {
                GameObject passwordObject = __instance.currentAdditiveObjects.First();
                TextMeshPro[] texts = passwordObject.GetComponentsInChildren<TextMeshPro>(true);

                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].gameObject.name.Contains("(1)"))
                        texts[i].text = ArchipelagoManager.cabinSafeCode[1].ToString();
                    else if (texts[i].gameObject.name.Contains("(2)"))
                        texts[i].text = ArchipelagoManager.cabinSafeCode[2].ToString();
                    else
                        texts[i].text = ArchipelagoManager.cabinSafeCode[0].ToString();
                }
            }
        }

        [HarmonyPatch(typeof(Part1FinaleSceneSequencer), "ShowArm")]
        [HarmonyPostfix]
        static void ChangeSmallClockPassword(Part1FinaleSceneSequencer __instance)
        {
            if (!ArchipelagoManager.randomizeCodes || __instance.deckTrialSequencer.transform.parent.Find("SmallClockClue(Clone)") != null) return;

            GameObject table = __instance.deckTrialSequencer.transform.parent.Find("Cube").gameObject;
            table.GetComponent<MeshRenderer>().material.mainTexture = AssetsManager.boonTableTex;

            GameObject clue = Object.Instantiate(AssetsManager.smallClockCluePrefab, table.transform.parent);
            clue.GetComponent<MeshRenderer>().material.mainTexture = AssetsManager.smallClockClueTexs[ArchipelagoManager.cabinSmallClockCode[2]];
            clue.transform.localPosition = new Vector3(-1.6744f, 1.6f, -0.9f);
            clue.transform.localEulerAngles = new Vector3(90, 90, 0);
            clue.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);

        }

        [HarmonyPatch(typeof(DogFoodBowlVolume), "Start")]
        [HarmonyPostfix]
        static void ReplaceGBCSafeCodeClue(DogFoodBowlVolume __instance)
        {
            if (!ArchipelagoManager.randomizeCodes) return;

            SpriteRenderer floor = __instance.transform.root.Find("OutdoorsCentral/Floor").GetComponent<SpriteRenderer>();

            floor.sprite = AssetsManager.editedNatureFloorSprite;

            GameObject codeClue = GameObject.Instantiate(AssetsManager.gbcSafeCluePrefab, floor.transform);
            codeClue.layer = LayerMask.NameToLayer("GBCPixel");
            string codeText = "";
            foreach (int digit in ArchipelagoManager.cabinSafeCode)
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
            if (ArchipelagoManager.randomizeCodes)
            {
                __result = SaveData.Data.natureTemple.safeState.sliderPositions[0] == ArchipelagoManager.cabinSafeCode[0] 
                    && SaveData.Data.natureTemple.safeState.sliderPositions[1] == ArchipelagoManager.cabinSafeCode[1]
                    && SaveData.Data.natureTemple.safeState.sliderPositions[2] == ArchipelagoManager.cabinSafeCode[2];

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
    [HarmonyDebug]
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
    [HarmonyDebug]
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
                ArchipelagoModPlugin.Log.LogMessage("Rip bozo 2");
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
            ArchipelagoModPlugin.Log.LogMessage("Rip bozo 3");
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
}

    [HarmonyPatch]
    [HarmonyDebug]
    class RandomizeDeckPatch
    {

        [HarmonyPatch(typeof(MapNode), "OnArriveAtNode")]
        [HarmonyPrefix]
        static bool RandomizeDeckWhenArrivingOnNode()
        {
            if (ArchipelagoManager.randomizeDeck != RandomizeDeck.Disable)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                Dictionary<string, List<CardModificationInfo>> newCardsMod = new Dictionary<string, List<CardModificationInfo>>();
                int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
                foreach (CardInfo c in RunState.Run.playerDeck.Cards)
                {
                    CardInfo card = ScriptableObject.CreateInstance<CardInfo>();
                    if (ArchipelagoManager.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.HasAnyOfCardMetaCategories(CardMetaCategory.Rare))
                            card = CardLoader.GetRandomUnlockedRareCard(seed++);
                        else if (c.IsPelt())
                        {
                            card = c;
                            continue;
                        }
                        else
                            card = Singleton<Part1CardChoiceGenerator>.Instance.GenerateDirectChoices(seed++).First().info;
                    }
                    else if (ArchipelagoManager.randomizeDeck == RandomizeDeck.RandomizeAll)
                    {
                        List<CardInfo> cardsInfoRandomPool = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature);
                        cardsInfoRandomPool.AddRange(CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature));
                        card = CardLoader.GetDistinctCardsFromPool(seed++, 1, cardsInfoRandomPool).First();
                    }
                    else
                    {
                        card = c.Clone() as CardInfo;
                    }
                    if (ArchipelagoManager.randomizeAbilities != RandomizeAbilities.Disable)
                    {
                        foreach (CardModificationInfo mod in c.Mods)
                        {
                            if (mod.deathCardInfo != null)
                            {
                                Console.WriteLine($"Name Card {c.displayedNameLocId}");
                                Console.WriteLine($"DeathLink Card");
                                continue;
                            }
                            if (mod.fromCardMerge)
                            {
                                List<Ability> newAbilityMod = new List<Ability>();
                                if (mod.abilities.Count > 0)
                                {
                                    for (int l = 0; l < mod.abilities.Count; l++)
                                    {
                                        Ability abil = AbilitiesUtil.GetRandomLearnedAbility(seed++);
                                        while (card.HasAbility(abil))
                                        {
                                            abil = AbilitiesUtil.GetRandomLearnedAbility(seed++);
                                        }
                                        newAbilityMod.Add(abil);
                                    }
                                    mod.abilities = newAbilityMod;
                                }
                            }
                            card.mods.Add(mod);
                        }
                    }
                    if (ArchipelagoManager.randomizeAbilities == RandomizeAbilities.RandomizeAll)
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
            if (ArchipelagoManager.randomizeDeck != RandomizeDeck.Disable)
            {
                int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll((CardInfo x) => x.metaCategories.Contains(CardMetaCategory.GBCPlayable));
                List<AbilityInfo> abilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll((AbilityInfo x) => x.metaCategories.Contains(AbilityMetaCategory.GrimoraRulebook)
                || x.metaCategories.Contains(AbilityMetaCategory.MagnificusRulebook) || x.metaCategories.Contains(AbilityMetaCategory.Part1Modular) 
                || x.metaCategories.Contains(AbilityMetaCategory.Part3Modular));
                foreach (CardInfo c in SaveData.Data.deck.Cards)
                {
                    CardInfo card = ScriptableObject.CreateInstance<CardInfo>();
                    card = CardLoader.GetDistinctCardsFromPool(seed++, 1, cardsInfoRandomPool).First();
                    card.mods = c.mods;
                    if (ArchipelagoManager.randomizeAbilities == RandomizeAbilities.RandomizeAll)
                    {
                        int rand = 0;
                        int abilityCount = card.abilities.Count;
                        card.abilities.Clear();
                        for (int t = 0; t < abilityCount; t++)
                        {
                            rand = UnityEngine.Random.Range(0, 4);
                            Console.WriteLine($"Random : {rand}");
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
                    card.decals = c.decals;
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
            if (ArchipelagoManager.randomizeDeck != RandomizeDeck.Disable)
            {
                int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll((CardInfo x) => x.metaCategories.Contains(CardMetaCategory.Part3Random) && x.temple == CardTemple.Tech);
                foreach (CardInfo c in Part3SaveData.Data.deck.Cards)
                {
                    CardInfo card = ScriptableObject.CreateInstance<CardInfo>();
                    card = CardLoader.GetDistinctCardsFromPool(seed++, 1, cardsInfoRandomPool).First();
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                Part3SaveData.Data.deck.CardInfos = newCards;
                Part3SaveData.Data.deck.cardIds = newCardsIds;
                Part3SaveData.Data.deck.UpdateModDictionary();
            }
            return true;
        }
    }
}