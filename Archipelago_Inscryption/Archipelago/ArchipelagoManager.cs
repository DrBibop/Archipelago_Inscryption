using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archipelago_Inscryption.Archipelago
{
    internal static class ArchipelagoManager
    {
        internal static Action<APItem> onItemReceived;

        internal const int CHECK_ID_OFFSET = 147000;
        internal const int ITEM_ID_OFFSET = 147000;

        // When one of the following events is completed, send the associated check.
        private static readonly Dictionary<StoryEvent, APCheck> storyCheckPairs = new Dictionary<StoryEvent, APCheck>()
        {
            { StoryEvent.ProspectorDefeated,            APCheck.CabinBossProspector },
            { StoryEvent.AnglerDefeated,                APCheck.CabinBossAngler },
            { StoryEvent.TrapperTraderDefeated,         APCheck.CabinBossTrapper },
            { StoryEvent.LeshyDefeated,                 APCheck.CabinBossLeshy },
            { StoryEvent.PhotographerDefeated,          APCheck.FactoryBossPhotographer },
            { StoryEvent.ArchivistDefeated,             APCheck.FactoryBossArchivist },
            { StoryEvent.CanvasDefeated,                APCheck.FactoryBossUnfinished },
            { StoryEvent.TelegrapherDefeated,           APCheck.FactoryBossG0lly },
            { StoryEvent.MycologistsBossDefeated,       APCheck.FactoryBossMycologists },
            { StoryEvent.Part3Completed,                APCheck.FactoryGreatTranscendence },
            { StoryEvent.Part3MetBonelord,              APCheck.FactoryBoneLordRoom },
            { StoryEvent.GooPlaneGoobertRevealed,       APCheck.FactoryGoobertPainting }
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
            { APItem.CabinCloverPlant,                  StoryEvent.CloverFound },
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
            { APItem.BoneLordFemur,                     StoryEvent.GBCBoneFound },
            { APItem.GBCCloverPlant,                    StoryEvent.GBCCloverFound },
            { APItem.FishbotCard,                       StoryEvent.TalkingAnglerCardDiscovered },
            { APItem.LonelyWizbotCard,                  StoryEvent.TalkingBlueMageCardDiscovered }
        };

        // When one of the following items is received, add the associated card(s) to the deck.
        private static readonly Dictionary<APItem, UnlockableCardInfo> itemCardPair = new Dictionary<APItem, UnlockableCardInfo>()
        {
            { APItem.StinkbugCard,                      new UnlockableCardInfo(false, ["Stinkbug_Talking"], ["Stinkbug_Talking", "Stoat_Talking"]) },
            { APItem.StuntedWolfCard,                   new UnlockableCardInfo(false, ["Wolf_Talking"], ["Wolf_Talking", "Stoat_Talking"]) },
            { APItem.SkinkCard,                         new UnlockableCardInfo(false, ["Skink"]) },
            { APItem.AntCards,                          new UnlockableCardInfo(false, ["Ant", "AntQueen"]) },
            { APItem.CagedWolfCard,                     new UnlockableCardInfo(false, ["CagedWolf"]) },
            { APItem.LonelyWizbotCard,                  new UnlockableCardInfo(true, ["BlueMage_Talking"]) },
            { APItem.FishbotCard,                       new UnlockableCardInfo(true, ["Angler_Talking"]) },
            { APItem.Ourobot,                           new UnlockableCardInfo(true, ["Ouroboros_Part3"]) }
        };

        // When one of the following items is received, add the associated card to the act 2 deck.
        private static readonly Dictionary<APItem, string> itemPixelCardPair = new Dictionary<APItem, string>()
        {
            { APItem.BoneLordHorn,                      "BonelordHorn" },
            { APItem.GreatKrakenCard,                   "Kraken" },
            { APItem.DrownedSoulCard,                   "DrownedSoul" },
            { APItem.SalmonCard,                        "Salmon" }
        };

        private static Dictionary<APCheck, CheckInfo> checkInfos = new Dictionary<APCheck, CheckInfo>();

        private static Queue<NetworkItem> itemQueue = new Queue<NetworkItem>();

        private static Queue<NetworkItem> itemsToVerifyQueue = new Queue<NetworkItem>();

        internal static void Init()
        {
            ArchipelagoClient.onConnectAttemptDone += OnConnectAttempt;
            ArchipelagoClient.onNewItemReceived += OnItemReceived;
            ArchipelagoClient.onProcessedItemReceived += OnItemToVerifyReceived;
        }

        private static void OnItemReceived(NetworkItem item)
        {
            itemQueue.Enqueue(item);
        }

        private static void OnItemToVerifyReceived(NetworkItem item)
        {
            itemsToVerifyQueue.Enqueue(item);
        }

        internal static bool ProcessNextItem()
        {
            if (itemQueue.Count > 0)
            {
                AudioController.Instance.PlaySound2D("creepy_rattle_lofi");

                NetworkItem item = itemQueue.Dequeue();

                string message;
                if (item.Player == ArchipelagoClient.session.ConnectionInfo.Slot)
                    message = "You have found your " + ArchipelagoClient.GetItemName(item.Item);
                else
                    message = "Received " + ArchipelagoClient.GetItemName(item.Item) + " from " + ArchipelagoClient.GetPlayerName(item.Player);

                Singleton<ArchipelagoUI>.Instance.LogImportant(message);
                ArchipelagoModPlugin.Log.LogMessage(message);

                APItem receivedItem = (APItem)(item.Item - ITEM_ID_OFFSET);

                ApplyItemReceived(receivedItem);

                Singleton<ArchipelagoUI>.Instance.QueueSave();

                return true;
            }

            return false;
        }

        internal static void ApplyItemReceived(APItem receivedItem)
        {
            if (itemStoryPairs.TryGetValue(receivedItem, out StoryEvent storyEvent))
            {
                StoryEventsData.SetEventCompleted(storyEvent);
            }

            if (itemCardPair.TryGetValue(receivedItem, out UnlockableCardInfo info))
            {
                for (int i = 0; i < info.cardsToUnlock.Length; i++)
                {
                    (info.isPart3 ? SaveManager.SaveFile.part3Data.deck : RunState.Run.playerDeck).AddCard(CardLoader.GetCardByName(info.cardsToUnlock[i]));
                }

                for (int i = 0; i < info.rigDraws.Length; i++)
                {
                    if (!SaveManager.SaveFile.RiggedDraws.Contains(info.rigDraws[i]))
                        SaveManager.SaveFile.RiggedDraws.Add(info.rigDraws[i]);
                }
            }
            else if (itemPixelCardPair.TryGetValue(receivedItem, out string cardName))
            {
                SaveManager.SaveFile.CollectGBCCard(CardLoader.GetCardByName(cardName));
            }

            if (receivedItem == APItem.Currency)
            {
                RunState.Run.currency++;
                SaveData.Data.currency++;
                Part3SaveData.Data.currency++;
            }
            else if (receivedItem == APItem.CardPack)
            {
                ArchipelagoData.Data.availableCardPacks++;
                RandomizerHelper.UpdatePackButtonEnabled();
            }
            else if (receivedItem == APItem.SquirrelTotemHead && !RunState.Run.totemTops.Contains(Tribe.Squirrel))
            {
                RunState.Run.totemTops.Add(Tribe.Squirrel);
            }
            else if (receivedItem == APItem.BeeFigurine && !RunState.Run.totemTops.Contains(Tribe.Insect))
            {
                RunState.Run.totemTops.Add(Tribe.Insect);
            }
            else if (receivedItem == APItem.MagnificusEye)
            {
                RunState.Run.eyeState = EyeballState.Wizard;
            }
            else if (receivedItem == APItem.ExtraCandle)
            {
                RunState.Run.maxPlayerLives = 3;
            }
            else if (receivedItem == APItem.Dagger && SaveManager.SaveFile.IsPart1)
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
            else if (receivedItem == APItem.AnglerHook && SaveManager.SaveFile.IsPart1)
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
            else if (receivedItem.ToString().Contains("Epitaph"))
            {
                int pieceCount = 0;

                if (receivedItem == APItem.EpitaphPiece)
                    pieceCount = ArchipelagoData.Data.receivedItems.Count(item => item.Item == ITEM_ID_OFFSET + (int)APItem.EpitaphPiece);
                else if (ArchipelagoOptions.epitaphPiecesRandomization == EpitaphPiecesRandomization.Groups)
                    pieceCount = ArchipelagoData.Data.receivedItems.Count(item => item.Item == ITEM_ID_OFFSET + (int)APItem.EpitaphPieces) * 3;
                else
                    pieceCount = 9;

                for (int i = 0; i < pieceCount; i++)
                {
                    if (i >= 9) break;

                    SaveData.Data.undeadTemple.epitaphPieces[i].found = true;
                }
                
            }
            else if (receivedItem == APItem.Monocle && Singleton<WizardMonocleEffect>.Instance)
            {
                Singleton<WizardMonocleEffect>.Instance.ShowLayer();
            }
            else if (receivedItem == APItem.CameraReplica)
            {
                SaveData.Data.natureTemple.hasCamera = true;
            }
            else if (receivedItem == APItem.MrsBombRemote && !Part3SaveData.Data.unlockedItems.Contains(Part3SaveData.ItemUnlock.BombRemote))
            {
                Part3SaveData.Data.unlockedItems.Add(Part3SaveData.ItemUnlock.BombRemote);
                Part3SaveData.Data.items.Add(Part3SaveData.ItemUnlock.BombRemote.ToString());
                if (Singleton<ItemsManager>.Instance && SaveManager.SaveFile.IsPart3)
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
            }
            else if (receivedItem == APItem.ExtraBattery && !Part3SaveData.Data.unlockedItems.Contains(Part3SaveData.ItemUnlock.Battery))
            {
                Part3SaveData.Data.unlockedItems.Add(Part3SaveData.ItemUnlock.Battery);
                Part3SaveData.Data.items.Add(Part3SaveData.ItemUnlock.Battery.ToString());
                if (Singleton<ItemsManager>.Instance && SaveManager.SaveFile.IsPart3)
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
            }
            else if (receivedItem == APItem.NanoArmorGenerator && !Part3SaveData.Data.unlockedItems.Contains(Part3SaveData.ItemUnlock.ShieldGenerator))
            {
                Part3SaveData.Data.unlockedItems.Add(Part3SaveData.ItemUnlock.ShieldGenerator);
                Part3SaveData.Data.items.Add(Part3SaveData.ItemUnlock.ShieldGenerator.ToString());
                if (Singleton<ItemsManager>.Instance && SaveManager.SaveFile.IsPart3)
                    Singleton<ItemsManager>.Instance.UpdateItems(false);
            }
            else if (receivedItem == APItem.HoloPelt)
            {
                Part3SaveData.Data.pelts++;
            }
            else if (receivedItem == APItem.Quill)
            {
                Part3SaveData.Data.foundUndeadTempleQuill = true;
            }

            if (Singleton<GameFlowManager>.Instance != null && SaveManager.SaveFile.IsPart1)
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
        }

        private static void OnConnectAttempt(LoginResult result)
        {
            Singleton<ArchipelagoUI>.Instance.UpdateConnectionStatus(result.Successful);
            if (result.Successful)
            {
                AudioController.Instance.PlaySound2D("creepy_rattle_glassy", MixerGroup.None, 0.5f);
            }
            else
            {
                AudioController.Instance.PlaySound2D("glitch", MixerGroup.None, 0.5f);
            }
        }

        internal static void InitializeFromServer()
        {
            if (ArchipelagoClient.slotData.TryGetValue("deathlink", out var deathlink))
                ArchipelagoOptions.deathlink = Convert.ToInt32(deathlink) != 0;
            if (ArchipelagoClient.slotData.TryGetValue("act1_deathlink_behaviour", out var act1Deathlink))
                ArchipelagoOptions.act1DeathLinkBehaviour = (Act1DeathLink)Convert.ToInt32(act1Deathlink);
            if (ArchipelagoClient.slotData.TryGetValue("optional_death_card", out var optionalDeathCard))
                ArchipelagoOptions.optionalDeathCard = (OptionalDeathCard)Convert.ToInt32(optionalDeathCard);
            if (ArchipelagoClient.slotData.TryGetValue("goal", out var goal))
                ArchipelagoOptions.goal = (Goal)Convert.ToInt32(goal);
            if (ArchipelagoClient.slotData.TryGetValue("randomize_codes", out var randomizeCodes))
                ArchipelagoOptions.randomizeCodes = Convert.ToInt32(randomizeCodes) != 0;
            if (ArchipelagoClient.slotData.TryGetValue("randomize_deck", out var randomizeDeck))
                ArchipelagoOptions.randomizeDeck = (RandomizeDeck)Convert.ToInt32(randomizeDeck);
            if (ArchipelagoClient.slotData.TryGetValue("randomize_sigils", out var randomizeSigils))
                ArchipelagoOptions.randomizeSigils = (RandomizeSigils)Convert.ToInt32(randomizeSigils);
            if (ArchipelagoClient.slotData.TryGetValue("skip_tutorial", out var skipTutorial))
                ArchipelagoOptions.skipTutorial = Convert.ToInt32(skipTutorial) != 0;
            if (ArchipelagoClient.slotData.TryGetValue("skip_epilogue", out var skipEpilogue))
                ArchipelagoOptions.skipEpilogue = Convert.ToInt32(skipEpilogue) != 0;
            if (ArchipelagoClient.slotData.TryGetValue("epitaph_pieces_randomization", out var piecesRandomization))
                ArchipelagoOptions.epitaphPiecesRandomization = (EpitaphPiecesRandomization)Convert.ToInt32(piecesRandomization);

            ArchipelagoData.Data.seed = ArchipelagoClient.session.RoomState.Seed;
            ArchipelagoData.Data.playerCount = ArchipelagoClient.session.Players.AllPlayers.Count() - 1;
            ArchipelagoData.Data.totalLocationsCount = ArchipelagoClient.session.Locations.AllLocations.Count();
            ArchipelagoData.Data.totalItemsCount = ArchipelagoData.Data.totalLocationsCount;
            ArchipelagoData.Data.goalType = ArchipelagoOptions.goal;
            ArchipelagoData.Data.skipEpilogue = ArchipelagoOptions.skipEpilogue;

            DeathLinkManager.DeathLinkService = ArchipelagoClient.session.CreateDeathLinkService();
            DeathLinkManager.Init();

            if (ArchipelagoOptions.randomizeCodes)
            {
                if (ArchipelagoData.Data.cabinClockCode.Count <= 0)
                {
                    int seed = int.Parse(ArchipelagoClient.session.RoomState.Seed.Substring(ArchipelagoClient.session.RoomState.Seed.Length - 6)) + 20 * ArchipelagoClient.session.ConnectionInfo.Slot;

                    ArchipelagoOptions.RandomizeCodes(seed);
                }

                ArchipelagoOptions.SetupRandomizedCodes();
            }

            if (ArchipelagoOptions.skipTutorial && !StoryEventsData.EventCompleted(StoryEvent.TutorialRun3Completed))
                ArchipelagoOptions.SkipTutorial();

            ScoutChecks();
            VerifyGoalCompletion();
            ArchipelagoClient.SendChecksToServerAsync();
        }

        internal static void VerifyAllItems()
        {
            while (itemsToVerifyQueue.Count() > 0)
            {
                NetworkItem nextItem = itemsToVerifyQueue.Dequeue();

                if (!VerifyItem(nextItem))
                {
                    ArchipelagoModPlugin.Log.LogWarning($"Item ID {nextItem.Item} ({ArchipelagoClient.GetItemName(nextItem.Item)}) didn't apply properly. Retrying...");
                    itemQueue.Enqueue(nextItem);
                }
            }
        }

        internal static bool VerifyItem(NetworkItem item)
        {
            APItem receivedItem = (APItem)(item.Item - ITEM_ID_OFFSET);

            if (itemStoryPairs.TryGetValue(receivedItem, out StoryEvent storyEvent) && !StoryEventsData.EventCompleted(storyEvent))
            {
                return false;
            }

            if (itemPixelCardPair.TryGetValue(receivedItem, out string cardName) && !SaveManager.SaveFile.gbcCardsCollected.Contains(cardName))
            {
                return false;
            }

            if (receivedItem.ToString().Contains("Epitaph"))
            {
                int pieceCount = 0;

                if (receivedItem == APItem.EpitaphPiece)
                    pieceCount = ArchipelagoData.Data.receivedItems.Count(i => i.Item == ITEM_ID_OFFSET + (int)APItem.EpitaphPiece);
                else if (ArchipelagoOptions.epitaphPiecesRandomization == EpitaphPiecesRandomization.Groups)
                    pieceCount = ArchipelagoData.Data.receivedItems.Count(i => i.Item == ITEM_ID_OFFSET + (int)APItem.EpitaphPieces) * 3;
                else
                    pieceCount = 9;

                for (int i = 0; i < pieceCount; i++)
                {
                    if (i >= 9) break;

                    if (!SaveData.Data.undeadTemple.epitaphPieces[i].found) return false;
                }
            }
            else if (receivedItem == APItem.CameraReplica && !SaveData.Data.natureTemple.hasCamera)
            {
                return false;
            }
            else if (receivedItem == APItem.MrsBombRemote && !Part3SaveData.Data.unlockedItems.Contains(Part3SaveData.ItemUnlock.BombRemote))
            {
                return false;
            }
            else if (receivedItem == APItem.ExtraBattery && !Part3SaveData.Data.unlockedItems.Contains(Part3SaveData.ItemUnlock.Battery))
            {
                return false;
            }
            else if (receivedItem == APItem.NanoArmorGenerator && !Part3SaveData.Data.unlockedItems.Contains(Part3SaveData.ItemUnlock.ShieldGenerator))
            {
                return false;
            }
            else if (receivedItem == APItem.Quill && !Part3SaveData.Data.foundUndeadTempleQuill)
            {
                return false;
            }
            else if (receivedItem == APItem.HoloPelt && Part3SaveData.Data.pelts + Part3SaveData.Data.collectedTarots.Count < ArchipelagoData.Data.receivedItems.Count(i => i.Item == ITEM_ID_OFFSET + (int)APItem.HoloPelt))
            {
                return false;
            }

            return true;
        }

        internal static void ScoutChecks()
        {
            checkInfos.Clear();
            ArchipelagoClient.ScoutLocationsAsync(OnScoutDone);
        }

        private static void OnScoutDone(LocationInfoPacket packet)
        {
            for (int i = 0; i < packet.Locations.Length; i++)
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
            if (ArchipelagoData.Data == null) return;

            long checkID = CHECK_ID_OFFSET + (long)check;

            if (!ArchipelagoData.Data.completedChecks.Contains(checkID))
            {
                ArchipelagoData.Data.completedChecks.Add(checkID);
                ArchipelagoClient.SendChecksToServerAsync();
                Singleton<ArchipelagoUI>.Instance.QueueSave();
            }
        }

        internal static bool HasCompletedCheck(APCheck check)
        {
            if (ArchipelagoData.Data == null) return false;

            long checkID = CHECK_ID_OFFSET + (long)check;

            return ArchipelagoData.Data.completedChecks.Contains(checkID);
        }

        internal static bool HasItem(APItem item)
        {
            if (ArchipelagoData.Data == null) return false;

            long itemID = ITEM_ID_OFFSET + (long)item;

            return ArchipelagoData.Data.receivedItems.Any(x => x.Item == itemID);
        }

        internal static void VerifyGoalCompletion()
        {
            if (ArchipelagoData.Data == null || ArchipelagoData.Data.goalCompletedAndSent) return;

            if ((ArchipelagoOptions.goal == Goal.AllActsInOrder || ArchipelagoOptions.goal == Goal.AllActsAnyOrder) && 
                ArchipelagoData.Data.act1Completed && ArchipelagoData.Data.act2Completed && ArchipelagoData.Data.act3Completed &&
                (ArchipelagoOptions.skipEpilogue || ArchipelagoData.Data.epilogueCompleted))
            {
                ArchipelagoClient.SendGoalCompleted();
            }
            else if (ArchipelagoOptions.goal == Goal.Act1Only && ArchipelagoData.Data.act1Completed)
            {
                ArchipelagoClient.SendGoalCompleted();
            }
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
        internal bool isPart3;

        public UnlockableCardInfo(bool isPart3, string[] cardsToUnlock)
        {
            this.cardsToUnlock = cardsToUnlock;
            this.rigDraws = new string[0];
            this.isPart3 = isPart3;
        }

        public UnlockableCardInfo(bool isPart3, string[] cardsToUnlock, string[] rigDraws)
        {
            this.cardsToUnlock = cardsToUnlock;
            this.rigDraws = rigDraws;
            this.isPart3 = isPart3;
        }
    }
}
