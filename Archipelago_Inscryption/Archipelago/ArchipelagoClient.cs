using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Archipelago_Inscryption.Components;
using DiskCardGame;
using InscryptionAPI.Saves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Most of this class is based on the Messenger Archipelago Mod: https://github.com/alwaysintreble/TheMessengerRandomizerModAP/blob/archipelago/Archipelago/ArchipelagoClient.cs
namespace Archipelago_Inscryption.Archipelago
{
    internal static class ArchipelagoClient
    {
        internal static Action<LoginResult> onConnectAttemptDone;
        internal static Action<NetworkItem> onItemReceived;

        internal static bool IsConnecting => isConnecting;
        internal static bool IsConnected => isConnected;

        internal static Data serverData;

        private const string ArchipelagoVersion = "0.4.1";

        private delegate void OnConnectAttempt(LoginResult result);

        private static bool isConnecting = false;
        private static bool isConnected = false;

        private static ArchipelagoSession session;

        internal static void Init()
        {
            serverData = new Data("");

            List<long> storedCompletedChecks = ModdedSaveManager.SaveData.GetValueAsObject<List<long>>(ArchipelagoModPlugin.PluginGuid, "CompletedChecks");
            List<NetworkItem> storedReceivedItems = ModdedSaveManager.SaveData.GetValueAsObject<List<NetworkItem>>(ArchipelagoModPlugin.PluginGuid, "ReceivedItems");

            if (storedCompletedChecks != null) serverData.completedChecks = storedCompletedChecks;
            if (storedReceivedItems != null) serverData.receivedItems = storedReceivedItems;
        }

        internal static bool ConnectAsync(string hostName, int port, string slotName, string password)
        {
            if (isConnecting || isConnected) return false;

            serverData.hostName = hostName;
            serverData.port = port;
            serverData.slotName = slotName;
            serverData.password = password;

            isConnecting = true;

            ArchipelagoModPlugin.Log.LogInfo($"Connecting to {serverData.hostName}:{serverData.port} as {serverData.slotName}...");

            ThreadPool.QueueUserWorkItem(_ => Connect(OnConnected));

            return true;
        }

        internal static void Disconnect()
        {
            ArchipelagoModPlugin.Log.LogInfo("Disconnecting from server...");
            session?.Socket.DisconnectAsync();
            session = null;
            isConnected = false;
            isConnecting = false;
        }

        internal static void ScoutLocationsAsync(Action<LocationInfoPacket> callback)
        {
            if (!isConnected) return;

            session.Locations.ScoutLocationsAsync(session.Locations.AllLocations.ToArray()).ContinueWith(t => callback(t.Result));
        }

        internal static void SendChecksToServerAsync()
        {
            if (!isConnected) return;

            session.Locations.CompleteLocationChecksAsync(serverData.completedChecks.ToArray());
        }

        internal static void SendGoalCompleted()
        {
            if (!isConnected) return;

            StatusUpdatePacket statusUpdate = new StatusUpdatePacket() { Status = ArchipelagoClientState.ClientGoal };
            session.Socket.SendPacketAsync(statusUpdate);
        }

        private static ArchipelagoSession CreateSession()
        {
            var session = ArchipelagoSessionFactory.CreateSession(serverData.hostName, serverData.port);
            session.MessageLog.OnMessageReceived += OnMessageReceived;
            session.Socket.ErrorReceived += SessionErrorReceived;
            session.Socket.SocketClosed += SessionSocketClosed;
            session.Items.ItemReceived += OnItemReceived;
            return session;
        }

        private static void Connect(OnConnectAttempt attempt)
        {
            if (isConnected) return;

            LoginResult result;

            try
            {
                session = CreateSession();
                result = session.TryConnectAndLogin(
                    "Inscryption",
                    serverData.slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(ArchipelagoVersion),
                    password: serverData.password == "" ? null : serverData.password
                );
            }
            catch (Exception e)
            {
                ArchipelagoModPlugin.Log.LogError(e.Message + "\n" + e.StackTrace);
                result = new LoginFailure(e.Message);
            }

            if (result.Successful)
            {
                LoginSuccessful successfulResult = (LoginSuccessful)result;
                serverData.slotData = successfulResult.SlotData;
                serverData.seed = session.RoomState.Seed;
                isConnected = true;
            }
            else
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {serverData.hostName}: ";
                errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");

                ArchipelagoModPlugin.Log.LogError(errorMessage);

                Disconnect();
            }

            attempt(result);
        }

        private static void OnConnected(LoginResult result)
        {
            Singleton<ArchipelagoUI>.Instance.UpdateConnectionStatus(result.Successful);
            SendChecksToServerAsync();

            if (onConnectAttemptDone != null)
            {
                onConnectAttemptDone(result);
            }
        }

        private static void SessionSocketClosed(string reason)
        {
            Singleton<ArchipelagoUI>.Instance.UpdateConnectionStatus(false);
            ArchipelagoModPlugin.Log.LogInfo($"Connection lost: {reason}");
            Disconnect();
        }

        private static void SessionErrorReceived(Exception e, string message)
        {
            ArchipelagoModPlugin.Log.LogError($"Archipelago error: {message}");
            ArchipelagoModPlugin.Log.LogError(e.Message);
        }

        private static void OnMessageReceived(LogMessage message)
        {
            ArchipelagoModPlugin.Log.LogMessage(message.ToString());
            Singleton<ArchipelagoUI>.Instance.LogMessage(message.ToString());
        }

        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            if (serverData.index >= helper.Index) return;

            List<NetworkItem> itemsToReceive = new List<NetworkItem>();
            List<NetworkItem> tempAlreadyReceivedItems = new List<NetworkItem>(serverData.receivedItems);

            // We need to flush out what we already collected.
            while (serverData.index < serverData.receivedItems.Count)
            {
                NetworkItem nextItem = helper.DequeueItem();
                NetworkItem itemToRemove = tempAlreadyReceivedItems.FirstOrDefault(x => IsSameItem(x, nextItem));
                if (IsSameItem(itemToRemove, default(NetworkItem)))
                {
                    // We already received and processed this item
                    tempAlreadyReceivedItems.Remove(itemToRemove);
                    serverData.index++;
                }
                else
                {
                    // This item is new
                    itemsToReceive.Add(nextItem);
                }
            }

            // Add the rest of the new items
            while (itemsToReceive.Count < helper.Index - serverData.index)
            {
                itemsToReceive.Add(helper.DequeueItem());
            }

            // Process the new items
            foreach (NetworkItem receivedItem in itemsToReceive)
            {
                serverData.receivedItems.Add(receivedItem);
                serverData.index++;

                if (onItemReceived != null)
                    onItemReceived(receivedItem);
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

        internal struct Data
        {
            public string hostName;
            public int port;
            public string slotName;
            public string password;
            public bool deathlink;
            public string seed;
            public Dictionary<string, object> slotData;
            public List<long> completedChecks;
            public List<NetworkItem> receivedItems;
            public uint index;

            public Data(
                string hostName                     = "archipelago.gg",
                int port                            = 38281,
                string slotName                     = "",
                string password                     = "",
                bool deathlink                      = false,
                string seed                         = "Unknown",
                Dictionary<string, object> slotData = null,
                List<long> completedChecks          = null,
                List<NetworkItem> receivedItems     = null
                )
            {
                this.hostName           = hostName;
                this.port               = port;
                this.slotName           = slotName;
                this.password           = password;
                this.deathlink          = deathlink;
                this.seed               = seed;
                this.slotData           = slotData == null ? new Dictionary<string, object>() : slotData;
                this.completedChecks    = completedChecks == null ? new List<long>() : completedChecks;
                this.receivedItems      = receivedItems == null ? new List<NetworkItem>() : receivedItems;
                this.index = 0;
            }
        }
    }
}
