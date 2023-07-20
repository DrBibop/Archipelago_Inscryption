﻿using Archipelago.MultiClient.Net;
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
        internal static Action<NetworkItem> onNewItemReceived;

        internal static bool IsConnecting => isConnecting;
        internal static bool IsConnected => isConnected;

        internal static Data serverData;

        private const string ArchipelagoVersion = "0.4.1";

        private delegate void OnConnectAttempt(LoginResult result, Action<LoginResult> oneOffCallback);

        private static bool isConnecting = false;
        private static bool isConnected = false;

        private static ArchipelagoSession session;

        internal static void Init()
        {
            serverData = new Data("");

            List<long> storedCompletedChecks = ModdedSaveManager.SaveData.GetValueAsObject<List<long>>(ArchipelagoModPlugin.PluginGuid, "CompletedChecks");
            List<string> storedReceivedItems = ModdedSaveManager.SaveData.GetValueAsObject<List<string>>(ArchipelagoModPlugin.PluginGuid, "ReceivedItems");

            if (storedCompletedChecks != null) serverData.completedChecks = storedCompletedChecks;
            if (storedReceivedItems != null)
            {
                foreach (string encodedItem in  storedReceivedItems)
                {
                    serverData.receivedItems.Add(DecodeItemFromString(encodedItem));
                }
            }
        }

        internal static bool ConnectAsync(string hostName, int port, string slotName, string password, Action<LoginResult> oneOffCallback = null)
        {
            if (isConnecting || isConnected) return false;

            serverData.hostName = hostName;
            serverData.port = port;
            serverData.slotName = slotName;
            serverData.password = password;

            isConnecting = true;

            ArchipelagoModPlugin.Log.LogInfo($"Connecting to {serverData.hostName}:{serverData.port} as {serverData.slotName}...");

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

        private static void Connect(OnConnectAttempt attempt, Action<LoginResult> oneOffCallback)
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

            attempt(result, oneOffCallback);
        }

        private static void OnConnected(LoginResult result, Action<LoginResult> oneOffCallback)
        {
            Singleton<ArchipelagoUI>.Instance.UpdateConnectionStatus(result.Successful);
            SendChecksToServerAsync();

            if (onConnectAttemptDone != null)
            {
                onConnectAttemptDone(result);
            }

            if (oneOffCallback != null)
            {
                oneOffCallback(result);
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

            serverData.index++;

            NetworkItem nextItem = helper.DequeueItem();
            NetworkItem matchedItem = serverData.receivedItems.FirstOrDefault(x => IsSameItem(x, nextItem));

            if (IsSameItem(matchedItem, default(NetworkItem)))
            {
                // This item is new
                serverData.receivedItems.Add(nextItem);

                if (onNewItemReceived != null)
                    onNewItemReceived(nextItem);
            }
        }

        internal static string EncodeItemToString(NetworkItem item)
        {
            return $"{item.Item}|{item.Location}|{item.Player}|{(int)item.Flags}";
        }

        private static NetworkItem DecodeItemFromString(string itemString)
        {
            string[] elements = itemString.Split('|');
            NetworkItem networkItem = new NetworkItem();
            if (long.TryParse(elements[0], out long item))
                networkItem.Item = item;
            if (long.TryParse(elements[1], out long location))
                networkItem.Location = location;
            if (int.TryParse(elements[2], out int player))
                networkItem.Player = player;
            if (Enum.TryParse(elements[3], out ItemFlags flags))
                networkItem.Flags = flags;

            return networkItem;
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