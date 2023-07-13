using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago_Inscryption.Components;
using DiskCardGame;
using GBC;
using InscryptionAPI.Saves;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archipelago_Inscryption.Archipelago
{
    internal static class ArchipelagoManager
    {
        internal static Action<APItem> onItemReceived;

        private const int CHECK_ID_OFFSET = 147000;
        private const int ITEM_ID_OFFSET = 147000;

        // When one of the following events is completed, send the associated check.
        private static readonly Dictionary<StoryEvent, APCheck> storyCheckPairs = new Dictionary<StoryEvent, APCheck>()
        {
            { StoryEvent.ProspectorDefeated,            APCheck.CabinBossProspector },
            { StoryEvent.AnglerDefeated,                APCheck.CabinBossAngler },
            { StoryEvent.TrapperTraderDefeated,         APCheck.CabinBossTrapper },
            { StoryEvent.LeshyDefeated,                 APCheck.CabinBossLeshy }
        };

        // When one of the following items is received, set the associated story event as completed.
        private static readonly Dictionary<APItem, StoryEvent> itemStoryPairs = new Dictionary<APItem, StoryEvent>()
        {
            { APItem.StinkbugCard,                      StoryEvent.StinkbugCardDiscovered },
            { APItem.StuntedWolfCard,                   StoryEvent.TalkingWolfCardDiscovered },
            { APItem.FilmRoll,                          StoryEvent.FilmRollDiscovered },
            { APItem.SkinkCard,                         StoryEvent.SkinkCardDiscovered },
            { APItem.AntCards,                          StoryEvent.AntCardsDiscovered },
            { APItem.CagedWolfCard,                     StoryEvent.CageCardDiscovered },
            { APItem.SquirrelTotemHead,                 StoryEvent.SquirrelHeadDiscovered },
            { APItem.Dagger,                            StoryEvent.SpecialDaggerDiscovered },
            { APItem.CabinCloverPlant,                       StoryEvent.CloverFound },
            { APItem.ExtraCandle,                       StoryEvent.CandleArmFound },
            { APItem.BeeFigurine,                       StoryEvent.BeeFigurineFound },
            { APItem.GreaterSmoke,                      StoryEvent.ImprovedSmokeCardDiscovered },
            { APItem.AnglerHook,                        StoryEvent.FishHookUnlocked },
            { APItem.Ring,                              StoryEvent.RingFound },
            { APItem.PileOfMeat,                        StoryEvent.GBCDogFoodFound },
            { APItem.Monocle,                           StoryEvent.GBCMonocleFound },
            { APItem.AncientObol,                       StoryEvent.GBCObolFound },
            { APItem.MycologistsHoloKey,                StoryEvent.MycologistHutKeyFound },
            { APItem.BoneLordHoloKey,                   StoryEvent.BonelordHoloKeyFound },
            { APItem.BoneLordFemur,                     StoryEvent.GBCBoneFound }
        };

        // When one of the following items is received, add the associated card(s) to the deck.
        private static readonly Dictionary<APItem, UnlockableCardInfo> itemCardPair = new Dictionary<APItem, UnlockableCardInfo>()
        {
            { APItem.StinkbugCard,                      new UnlockableCardInfo(new string[1] { "Stinkbug_Talking" }, new string[2] { "Stinkbug_Talking", "Stoat_Talking" }) },
            { APItem.StuntedWolfCard,                   new UnlockableCardInfo(new string[1] { "Wolf_Talking" }, new string[2] { "Wolf_Talking", "Stoat_Talking" }) },
            { APItem.SkinkCard,                         new UnlockableCardInfo(new string[1] { "Skink" }) },
            { APItem.AntCards,                          new UnlockableCardInfo(new string[2] { "Ant", "AntQueen" }) },
            { APItem.CagedWolfCard,                     new UnlockableCardInfo(new string[1] { "CagedWolf" }) },
        };

        private static Dictionary<APCheck, CheckInfo> checkInfos = new Dictionary<APCheck, CheckInfo>();

        internal static void Init()
        {
            ArchipelagoClient.onConnectAttemptDone += OnConnectAttempt;
            ArchipelagoClient.onNewItemReceived += OnItemReceived;
        }

        private static void OnItemReceived(NetworkItem item)
        {
            AudioController.Instance.PlaySound2D("creepy_rattle_lofi");

            string message;
            if (ArchipelagoClient.GetPlayerName(item.Player) == ArchipelagoClient.serverData.slotName)
                message = "You have found your " + ArchipelagoClient.GetItemName(item.Item);
            else
                message = "Received " + ArchipelagoClient.GetItemName(item.Item) + " from " + ArchipelagoClient.GetPlayerName(item.Player);

            Singleton<ArchipelagoUI>.Instance.LogImportant(message);
            ArchipelagoModPlugin.Log.LogMessage(message);

            List<string> encodedItems = new List<string>();

            foreach (NetworkItem networkItem in ArchipelagoClient.serverData.receivedItems)
            {
                encodedItems.Add(ArchipelagoClient.EncodeItemToString(networkItem));
            }

            ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "ReceivedItems", encodedItems);

            APItem receivedItem = (APItem)(item.Item - ITEM_ID_OFFSET);

            ApplyItemReceived(receivedItem);
        }

        private static void ApplyItemReceived(APItem receivedItem)
        {
            if (itemStoryPairs.TryGetValue(receivedItem, out StoryEvent storyEvent))
            {
                StoryEventsData.SetEventCompleted(storyEvent);
            }

            if (itemCardPair.TryGetValue(receivedItem, out UnlockableCardInfo info))
            {
                for (int i = 0; i < info.cardsToUnlock.Length; i++)
                {
                    SaveManager.SaveFile.CurrentDeck.AddCard(CardLoader.GetCardByName(info.cardsToUnlock[i]));
                }

                for (int i = 0; i < info.rigDraws.Length; i++)
                {
                    if (!SaveManager.SaveFile.RiggedDraws.Contains(info.rigDraws[i]))
                        SaveManager.SaveFile.RiggedDraws.Add(info.rigDraws[i]);
                }
            }

            if (receivedItem == APItem.Currency)
            {
                if (SaveManager.SaveFile.IsPart2)
                    SaveData.Data.currency++;
                else
                    RunState.Run.currency++;
            }

            if (receivedItem == APItem.SquirrelTotemHead && !RunState.Run.totemTops.Contains(Tribe.Squirrel))
            {
                RunState.Run.totemTops.Add(Tribe.Squirrel);
            }

            if (receivedItem == APItem.BeeFigurine && !RunState.Run.totemTops.Contains(Tribe.Insect))
            {
                RunState.Run.totemTops.Add(Tribe.Insect);
            }

            if (receivedItem == APItem.MagnificusEye)
            {
                RunState.Run.eyeState = EyeballState.Wizard;
            }

            if (receivedItem == APItem.ExtraCandle)
            {
                RunState.Run.maxPlayerLives = 3;
            }

            if (receivedItem == APItem.Dagger && SaveManager.SaveFile.IsPart1)
            {
                if (RunState.Run.consumables.Count >= 3)
                {
                    string itemName = RunState.Run.consumables[0];
                    if (RunState.Run.consumables.Contains("Pliers"))
                    {
                        itemName = "Pliers";
                    }
                    else
                    {
                        for (int i = 2; i >= 0; i--)
                        {
                            if (RunState.Run.consumables[i] != "FishHook")
                            {
                                itemName = RunState.Run.consumables[i];
                                break;
                            }
                        }
                    }
                    if (Singleton<ItemsManager>.Instance)
                        Singleton<ItemsManager>.Instance.DestroyItem(itemName);
                    else
                        RunState.Run.consumables.Remove(itemName);
                }
                RunState.Run.consumables.Add("SpecialDagger");
                if (Singleton<ItemsManager>.Instance)
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
            }

            if (receivedItem == APItem.AnglerHook && SaveManager.SaveFile.IsPart1)
            {
                if (RunState.Run.consumables.Count >= 3)
                {
                    string itemName = RunState.Run.consumables[0];
                    for (int i = 2; i >= 0; i--)
                    {
                        if (RunState.Run.consumables[i] != "SpecialDagger")
                        {
                            itemName = RunState.Run.consumables[i];
                            break;
                        }
                    }

                    if (Singleton<ItemsManager>.Instance)
                        Singleton<ItemsManager>.Instance.DestroyItem(itemName);
                    else
                        RunState.Run.consumables.Remove(itemName);
                }
                RunState.Run.consumables.Add("FishHook");
                if (Singleton<ItemsManager>.Instance)
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
            }

            if (Singleton<GameFlowManager>.Instance != null)
            {
                if (receivedItem == APItem.MagnificusEye && Singleton<GameFlowManager>.Instance is Part1GameFlowManager)
                {
                    Singleton<UIManager>.Instance.Effects.GetEffect<WizardEyeEffect>().SetIntensity(1f, 0f);
                }

                if (receivedItem == APItem.ExtraCandle && Singleton<GameFlowManager>.Instance is Part1GameFlowManager)
                {
                    if (Singleton<TurnManager>.Instance == null || !(Singleton<TurnManager>.Instance.Opponent is Part1BossOpponent))
                        RunState.Run.playerLives = Mathf.Min(RunState.Run.maxPlayerLives, RunState.Run.playerLives + 1);

                    Singleton<CandleHolder>.Instance.UpdateArmsAndFlames();
                    Singleton<CandleHolder>.Instance.anim.Play("add_candle");
                }

                if (receivedItem == APItem.BeeFigurine && Singleton<CardDrawPiles>.Instance is Part1CardDrawPiles piles)
                {
                    piles.SetSidePileFigurine(SidePileFigurine.Bee);
                }
            }
            else
            {
                if (receivedItem == APItem.ExtraCandle)
                {
                    RunState.Run.playerLives = Mathf.Min(RunState.Run.maxPlayerLives, RunState.Run.playerLives + 1);
                }
            }

            if (onItemReceived != null)
                onItemReceived(receivedItem);

            SaveManager.SaveToFile(false);
        }

        private static void OnConnectAttempt(LoginResult result)
        {
            if (result.Successful)
            {
                AudioController.Instance.PlaySound2D("creepy_rattle_glassy", MixerGroup.None, 0.5f);
                ScoutChecks();
            }
            else
            {
                AudioController.Instance.PlaySound2D("glitch", MixerGroup.None, 0.5f);
            }
        }

        internal static void ScoutChecks()
        {
            checkInfos.Clear();
            ArchipelagoClient.ScoutLocationsAsync(OnScoutDone);
        }

        private static void OnScoutDone(LocationInfoPacket packet)
        {
            for (int i = 0;  i < packet.Locations.Length; i++)
            {
                NetworkItem location = packet.Locations[i];

                string recipientName = ArchipelagoClient.GetPlayerName(location.Player);
                string itemName = ArchipelagoClient.GetItemName(location.Item);

                checkInfos.Add((APCheck)(location.Location - CHECK_ID_OFFSET), new CheckInfo(
                    location.Location, 
                    location.Player, 
                    recipientName, 
                    location.Item, 
                    itemName)
                );
            }
        }

        internal static void SendStoryCheckIfApplicable(StoryEvent storyEvent)
        {
            if (storyCheckPairs.TryGetValue(storyEvent, out APCheck check))
            {
                SendCheck(check);
            }
        }

        internal static void SendCheck(APCheck check)
        {
            long checkID = CHECK_ID_OFFSET + (long)check;

            if (!ArchipelagoClient.serverData.completedChecks.Contains(checkID))
            {
                ArchipelagoClient.serverData.completedChecks.Add(checkID);
                ModdedSaveManager.SaveData.SetValueAsObject(ArchipelagoModPlugin.PluginGuid, "CompletedChecks", ArchipelagoClient.serverData.completedChecks);
                ArchipelagoClient.SendChecksToServerAsync();
                SaveManager.SaveToFile(false);
            }
        }

        internal static bool HasCompletedCheck(APCheck check)
        {
            long checkID = CHECK_ID_OFFSET + (long)check;

            return ArchipelagoClient.serverData.completedChecks.Contains(checkID);
        }

        internal static bool HasItem(APItem item)
        {
            long itemID = ITEM_ID_OFFSET + (long)item;

            return ArchipelagoClient.serverData.receivedItems.Any(x => x.Item == itemID);
        }

        internal static CheckInfo GetCheckInfo(APCheck check)
        {
            if (checkInfos.TryGetValue(check, out CheckInfo info))
            {
                return info;
            }

            CheckInfo basicInfo = new CheckInfo((int)check + CHECK_ID_OFFSET, 0, "Player", 0, check.ToString());

            return basicInfo;
        }
    }

    internal struct CheckInfo
    {
        internal long checkId;
        internal int recipientId;
        internal string recipientName;
        internal long itemId;
        internal string itemName;

        public CheckInfo(long checkId, int recipientId, string recipientName, long itemId, string itemName)
        {
            this.checkId = checkId;
            this.recipientId = recipientId;
            this.recipientName = recipientName;
            this.itemId = itemId;
            this.itemName = itemName;
        }
    }

    internal struct UnlockableCardInfo
    {
        internal string[] cardsToUnlock;
        internal string[] rigDraws;

        public UnlockableCardInfo(string[] cardsToUnlock)
        {
            this.cardsToUnlock = cardsToUnlock;
            this.rigDraws = new string[0];
        }

        public UnlockableCardInfo(string[] cardsToUnlock, string[] rigDraws)
        {
            this.cardsToUnlock = cardsToUnlock;
            this.rigDraws = rigDraws;
        }
    }
}
