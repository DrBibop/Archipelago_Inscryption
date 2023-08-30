﻿using Archipelago_Inscryption.Archipelago;
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
                __result += SaveManager.saveFile.gbcData.collection.Cards.Count * 8;
            if (__instance.IsPart3)
                __result += Part3SaveData.Data.nodesActivated * 8;
        }

        [HarmonyPatch(typeof(SaveManager), "CreateNewSaveFile")]
        [HarmonyPrefix]
        static bool EraseArchipelagoData()
        {
            ArchipelagoClient.serverData.completedChecks.Clear();
            ArchipelagoClient.serverData.receivedItems.Clear();
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "CompletedChecks", new List<long>());
            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "ReceivedItems", new List<string>());
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
    [HarmonyDebug]
    class RandomizeDeckPatch
    {

        [HarmonyPatch(typeof(MapNode), "OnArriveAtNode")]
        [HarmonyPrefix]
        static bool RandomizeDeckWhenArrivingOnNode()
        {
            List<CardInfo> newCards = new List<CardInfo>();
            List<string> newCardsIds = new List<string>();
            Dictionary<string, List<CardModificationInfo>> newCardsMod = new Dictionary<string, List<CardModificationInfo>>();
            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
            foreach (CardInfo c in RunState.Run.playerDeck.Cards)
            {
                CardInfo card = new CardInfo();
                if (false/*Option to randomize card with card type*/)
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
                else if (true/*Option to randomize all card*/)
                {
                    //card = CardLoader.GetDistinctCardsFromPool(seed++, 1, ScriptableObjectLoader<CardInfo>.AllData.FindAll(
                    //    (CardInfo x) => (x.metaCategories.Contains(CardMetaCategory.ChoiceNode) || x.metaCategories.Contains(CardMetaCategory.Rare)) && 
                    //    x.energyCost == 0 && x.gemsCost.Count == 0 && !x.metaCategories.Contains(CardMetaCategory.AscensionUnlock) && 
                    //    x.abilities.FindAll((Ability z) => AbilitiesUtil.GetLearnedAbilities().Contains(z)).Count == x.abilities.Count && c).ToList()).First();
                    //card = CardLoader.GetDistinctCardsFromPool(seed++, 1, ScriptableObjectLoader<CardInfo>.AllData.FindAll(
                    //    (CardInfo x) => CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature).Contains(x) 
                    //    || CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature).Contains(x)).ToList()).First();
                    List<CardInfo> cardsInfoRandomPool = CardLoader.GetUnlockedCards(CardMetaCategory.ChoiceNode, CardTemple.Nature);
                    cardsInfoRandomPool.AddRange(CardLoader.GetUnlockedCards(CardMetaCategory.Rare, CardTemple.Nature));
                    card = CardLoader.GetDistinctCardsFromPool(seed++, 1, cardsInfoRandomPool).First();
                }
                else
                {
                    card = c.Clone() as CardInfo;
                }

                foreach (CardModificationInfo mod in c.Mods)
                {
                    if (mod.deathCardInfo != null)
                    {
                        Console.WriteLine($"Name Card {c.displayedNameLocId}");
                        Console.WriteLine($"DeathLink Card");
                        continue;
                    }
                    if (mod.fromCardMerge/*Option to randomize modded ability ability*/)
                    {
                        List<Ability> newAbilityMod = new List<Ability>();
                        if (mod.abilities.Count > 0)
                        {
                            for (int l = 0; l < mod.abilities.Count; l++)
                            {
                                Ability abil = AbilitiesUtil.GetRandomLearnedAbility(seed++, card);
                                while (card.HasAbility(abil))
                                {
                                    Console.WriteLine($"tried to put {mod.abilities.First()} on a deathcard");
                                    abil = AbilitiesUtil.GetRandomLearnedAbility(seed++, card);     
                                }
                                newAbilityMod.Add(abil);
                            }
                            mod.abilities = newAbilityMod;
                        }
                    }
                    card.mods.Add(mod);
                }
                if (false/*Option to randomize default ability &&card.abilities.Count > 0*/) //TODO
                {
                    int abilityCount = card.abilities.Count;
                    card.abilities.Clear();
                    for (int t = 0; t < abilityCount; t++)
                    {
                        card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(seed++, card));
                    }
                }
                card.decals = c.decals;
                newCardsIds.Add(card.name);
                newCards.Add(card);
            }
            RunState.Run.playerDeck.CardInfos = newCards;
            RunState.Run.playerDeck.cardIds = newCardsIds;
            RunState.Run.playerDeck.UpdateModDictionary();
            return true;
        }
        [HarmonyPatch(typeof(MapNode), "OnArriveAtNode")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct2()
        {
            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();
            CardInfo card = CardLoader.GetDistinctCardsFromPool(seed++, 1, ScriptableObjectLoader<CardInfo>.AllData.FindAll(
                (CardInfo x) => x.metaCategories.Contains(CardMetaCategory.GBCPack) || x.metaCategories.Contains(CardMetaCategory.GBCPlayable)).ToList()).First();
            return true;
        }
    }
}