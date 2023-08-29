using Archipelago_Inscryption.Archipelago;
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
                __result += RunState.Run.currentNodeId * 8;
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
            int i = 0;
            foreach (CardInfo c in RunState.Run.playerDeck.Cards)
            {
                CardInfo card = new CardInfo();
                if (c.HasAnyOfCardMetaCategories(CardMetaCategory.Rare))
                    card = CardLoader.GetRandomUnlockedRareCard(RandomizerHelper.GetCustomSeedDeckRandomization() + i);
                else
                    card = CardLoader.GetRandomChoosableCard(RandomizerHelper.GetCustomSeedDeckRandomization() + i);
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
                                Console.WriteLine($"Ability changed from {mod.abilities.First()}");
                                newAbilityMod.Add(AbilitiesUtil.GetRandomLearnedAbility(RandomizerHelper.GetCustomSeedDeckRandomization() + i, card));
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
                        card.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(RandomizerHelper.GetCustomSeedDeckRandomization() + i, card));
                    }
                }
                card.decals = c.decals;
                newCardsIds.Add(card.name);
                newCards.Add(card);
                i++;
            }
            RunState.Run.playerDeck.CardInfos = newCards;
            RunState.Run.playerDeck.cardIds = newCardsIds;
            return true;
        }

    }
}