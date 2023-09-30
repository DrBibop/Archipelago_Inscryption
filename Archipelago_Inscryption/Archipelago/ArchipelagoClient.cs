using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using System;
using System.Linq;
using System.Threading;

// Most of this class is based on the Messenger Archipelago Mod: https://github.com/alwaysintreble/TheMessengerRandomizerModAP/blob/archipelago/Archipelago/ArchipelagoClient.cs
namespace Archipelago_Inscryption.Archipelago
{
    internal static class ArchipelagoClient
    {
        internal static Action<LoginResult> onConnectAttemptDone;
        internal static Action<NetworkItem> onNewItemReceived;

        internal static bool IsConnecting => isConnecting;
        internal static bool IsConnected => isConnected;

        private const string ArchipelagoVersion = "0.4.1";

        private delegate void OnConnectAttempt(LoginResult result, Action<LoginResult> oneOffCallback);

        private static bool isConnecting = false;
        private static bool isConnected = false;

        internal static ArchipelagoSession session;

        internal static bool ConnectAsync(string hostName, int port, string slotName, string password, Action<LoginResult> oneOffCallback = null)
        {
            if (isConnecting || isConnected) return false;

            ArchipelagoData.Data.hostName = hostName;
            ArchipelagoData.Data.port = port;
            ArchipelagoData.Data.slotName = slotName;
            ArchipelagoData.Data.password = password;

            isConnecting = true;

            ArchipelagoModPlugin.Log.LogInfo($"Connecting to {hostName}:{port} as {slotName}...");

            ThreadPool.QueueUserWorkItem(_ => Connect(OnConnected, oneOffCallback));

            return true;
        }

        internal static void Disconnect()
        {
            ArchipelagoModPlugin.Log.LogInfo("Disconnecting from server...");
            session?.Socket.DisconnectAsync();
            session = null;
            isConnected = false;
            isConnecting = false;
            Singleton<ArchipelagoUI>.Instance.UpdateConnectionStatus(false);
        }

        internal static void ScoutLocationsAsync(Action<LocationInfoPacket> callback)
        {
            if (!isConnected) return;

            session.Locations.ScoutLocationsAsync(session.Locations.AllLocations.ToArray()).ContinueWith(t => callback(t.Result));
        }

        internal static void SendChecksToServerAsync()
        {
            if (!isConnected) return;

            session.Locations.CompleteLocationChecksAsync(ArchipelagoData.Data.completedChecks.ToArray());
        }

        internal static void SendGoalCompleted()
        {
            if (!isConnected) return;

            StatusUpdatePacket statusUpdate = new StatusUpdatePacket() { Status = ArchipelagoClientState.ClientGoal };
            session.Socket.SendPacketAsync(statusUpdate);
            ArchipelagoData.Data.goalCompletedAndSent = true;
        }

        private static ArchipelagoSession CreateSession()
        {
            var session = ArchipelagoSessionFactory.CreateSession(ArchipelagoData.Data.hostName, ArchipelagoData.Data.port);
            session.MessageLog.OnMessageReceived += OnMessageReceived;
            session.Socket.ErrorReceived += SessionErrorReceived;
            session.Socket.SocketClosed += SessionSocketClosed;
            session.Items.ItemReceived += OnItemReceived;
            return session;
        }

        private static void Connect(OnConnectAttempt attempt, Action<LoginResult> oneOffCallback)
        {
            if (isConnected) return;

            LoginResult result;

            try
            {
                session = CreateSession();
                result = session.TryConnectAndLogin(
                    "Inscryption",
                    ArchipelagoData.Data.slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(ArchipelagoVersion),
                    password: ArchipelagoData.Data.password == "" ? null : ArchipelagoData.Data.password
                );
            }
            catch (Exception e)
            {
                ArchipelagoModPlugin.Log.LogError(e.Message + "\n" + e.StackTrace);
                result = new LoginFailure(e.Message);
            }

            attempt(result, oneOffCallback);

            
        }

        private static void OnConnected(LoginResult result, Action<LoginResult> oneOffCallback)
        {
            isConnecting = false;

            if (result.Successful)
            {
                if (ArchipelagoData.Data.seed != session.RoomState.Seed)
                {
                    if (ArchipelagoData.Data.seed != "")
                    {
                        string resetMessage = "New MultiWorld detected! Reset your save file properly before starting.";
                        ArchipelagoModPlugin.Log.LogWarning(resetMessage);
                        Singleton<ArchipelagoUI>.Instance.LogImportant(resetMessage);

                        ArchipelagoData.Data.Reset();
                    }

                    ArchipelagoData.Data.seed = session.RoomState.Seed;
                }

                LoginSuccessful successfulResult = (LoginSuccessful)result;

                var slotData = successfulResult.SlotData;

                if (slotData.TryGetValue("deathlink", out var deathlink))
                    ArchipelagoOptions.deathlink = Convert.ToInt32(deathlink) != 0;
                if (slotData.TryGetValue("optional_death_card", out var optionalDeathCard))
                    ArchipelagoOptions.optionalDeathCard = (OptionalDeathCard)Convert.ToInt32(optionalDeathCard);
                if (slotData.TryGetValue("goal", out var goal))
                    ArchipelagoOptions.goal = (Goal)Convert.ToInt32(goal);
                if (slotData.TryGetValue("randomize_codes", out var randomizeCodes))
                    ArchipelagoOptions.randomizeCodes = Convert.ToInt32(randomizeCodes) != 0;
                if (slotData.TryGetValue("randomize_deck", out var randomizeDeck))
                    ArchipelagoOptions.randomizeDeck = (RandomizeDeck)Convert.ToInt32(randomizeDeck);
                if (slotData.TryGetValue("randomize_abilities", out var randomizeAbilities))
                    ArchipelagoOptions.randomizeAbilities = (RandomizeAbilities)Convert.ToInt32(randomizeAbilities);
                if (slotData.TryGetValue("skip_tutorial", out var skipTutorial))
                    ArchipelagoOptions.skipTutorial = Convert.ToInt32(skipTutorial) != 0;

                DeathLinkManager.DeathLinkService = session.CreateDeathLinkService();
                DeathLinkManager.Init();

                if (ArchipelagoOptions.randomizeCodes && ArchipelagoData.Data.cabinClockCode.Count <= 0)
                {
                    int seed = int.Parse(session.RoomState.Seed.Substring(session.RoomState.Seed.Length - 6)) + 20 * session.ConnectionInfo.Slot;

                    ArchipelagoOptions.RandomizeCodes(seed);
                }

                if (ArchipelagoOptions.skipTutorial && !StoryEventsData.EventCompleted(StoryEvent.TutorialRun3Completed))
                    RandomizerHelper.SkipTutorial();

                Singleton<ArchipelagoUI>.Instance.QueueSave();
                isConnected = true;
                SendChecksToServerAsync();
            }
            else
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {ArchipelagoData.Data.hostName}: ";
                errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");

                ArchipelagoModPlugin.Log.LogError(errorMessage);

                Disconnect();
            }
            onConnectAttemptDone?.Invoke(result);
            oneOffCallback?.Invoke(result);
        }

        private static void SessionSocketClosed(string reason)
        {
            ArchipelagoModPlugin.Log.LogInfo($"Connection lost: {reason}");
            if (session != null)
                Disconnect();
        }

        private static void SessionErrorReceived(Exception e, string message)
        {
            Singleton<ArchipelagoUI>.Instance.LogError(message);
            ArchipelagoModPlugin.Log.LogError($"Archipelago error: {message}");
            Disconnect();
        }

        private static void OnMessageReceived(LogMessage message)
        {
            ArchipelagoModPlugin.Log.LogMessage(message.ToString());
            Singleton<ArchipelagoUI>.Instance.LogMessage(message.ToString());
        }

        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            if (ArchipelagoData.Data.index >= helper.Index) return;

            ArchipelagoData.Data.index++;

            NetworkItem nextItem = helper.DequeueItem();
            NetworkItem matchedItem = ArchipelagoData.Data.itemsUnaccountedFor.FirstOrDefault(x => IsSameItem(x, nextItem));

            if (IsSameItem(matchedItem, default(NetworkItem)))
            {
                // This item is new
                ArchipelagoData.Data.receivedItems.Add(nextItem);

                if (onNewItemReceived != null)
                    onNewItemReceived(nextItem);
            }
            else
            {
                ArchipelagoData.Data.itemsUnaccountedFor.Remove(matchedItem);

                if (!ArchipelagoManager.VerifyItem(nextItem))
                {
                    ArchipelagoModPlugin.Log.LogWarning("Item ID " + nextItem.Item + " didn't apply properly. Retrying...");
                    if (onNewItemReceived != null)
                        onNewItemReceived(nextItem);

                    if (!ArchipelagoManager.VerifyItem(nextItem))
                        ArchipelagoModPlugin.Log.LogError("Item ID " + nextItem.Item + " has failed to apply. Contact us in the Archipelago Discord server or open an issue in our GitHub repository.");
                }
            }
        }

        private static bool IsSameItem(NetworkItem left, NetworkItem right)
        {
            return left.Item == right.Item 
                && left.Location == right.Location 
                && left.Player == right.Player;
        }

        internal static string GetPlayerName(int player)
        {
            if (!isConnected) return "";

            return session.Players.GetPlayerName(player);
        }

        internal static string GetItemName(long item)
        {
            if (!isConnected) return "";

            return session.Items.GetItemName(item);
        }
    }
}
