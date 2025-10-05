using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago_Inscryption.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// Most of this class is based on the Messenger Archipelago Mod: https://github.com/alwaysintreble/TheMessengerRandomizerModAP/blob/archipelago/Archipelago/ArchipelagoClient.cs
namespace Archipelago_Inscryption.Archipelago
{
    internal static class ArchipelagoClient
    {
        internal static Action<LoginResult> onConnectAttemptDone;
        internal static Action<InscryptionItemInfo> onNewItemReceived;
        internal static Action<InscryptionItemInfo> onProcessedItemReceived;

        internal static bool IsConnecting => isConnecting;
        internal static bool IsConnected => isConnected;

        private const string ArchipelagoVersion = "0.6.0";

        private delegate void OnConnectAttempt(LoginResult result);

        private static bool isConnecting = false;
        private static bool isConnected = false;

        internal static ArchipelagoSession session;

        internal static Dictionary<string, object> slotData = new Dictionary<string, object>();

        internal static void ConnectAsync(string hostName, int port, string slotName, string password)
        {
            if (isConnecting || isConnected) return;

            ArchipelagoData.Data.hostName = hostName;
            ArchipelagoData.Data.port = port;
            ArchipelagoData.Data.slotName = slotName;
            ArchipelagoData.Data.password = password;

            isConnecting = true;

            ArchipelagoModPlugin.Log.LogInfo($"Connecting to {hostName}:{port} as {slotName}...");

            ThreadPool.QueueUserWorkItem(_ => Connect(OnConnected));
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

        internal static void ScoutLocationsAsync(Action<Dictionary<long, ScoutedItemInfo>> callback)
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

            session.SetGoalAchieved();
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

        private static void Connect(OnConnectAttempt attempt)
        {
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

            attempt(result);
        }

        private static void OnConnected(LoginResult result)
        {
            if (result.Successful)
            {
                slotData = ((LoginSuccessful)result).SlotData;
                isConnected = true;
            }
            else
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMessage = $"Failed to connect to {ArchipelagoData.Data.hostName}: ";
                errorMessage = failure.Errors.Aggregate(errorMessage, (current, error) => current + $"\n    {error}");

                ArchipelagoModPlugin.Log.LogError(errorMessage);
                for (int i = 0; i < failure.Errors.Length; i++)
                {
                    Singleton<ArchipelagoUI>.Instance.LogError(failure.Errors[i]);
                }

                Disconnect();
            }

            onConnectAttemptDone?.Invoke(result);

            isConnecting = false;
        }

        private static void SessionSocketClosed(string reason)
        {
            ArchipelagoModPlugin.Log.LogInfo($"Connection lost: {reason}");
            if (session != null)
                Disconnect();
        }

        private static void SessionErrorReceived(Exception e, string message)
        {
            if (Singleton<ArchipelagoUI>.Instance == null || ArchipelagoModPlugin.Log == null) return;

            Singleton<ArchipelagoUI>.Instance.LogError(message);
            ArchipelagoModPlugin.Log.LogError($"Archipelago error: {message}");
            if (session != null)
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

            ItemInfo nextItem = helper.DequeueItem();
            InscryptionItemInfo matchedItem = ArchipelagoData.Data.itemsUnaccountedFor.FirstOrDefault(x => IsSameItem(x, nextItem));

            if (matchedItem == null)
            {
                // This item is new
                InscryptionItemInfo newItemInfo = new InscryptionItemInfo((APItem)(nextItem.ItemId - ArchipelagoManager.ID_OFFSET), nextItem.ItemName, nextItem.ItemId, nextItem.LocationId, nextItem.Player.Slot, nextItem.Player.Name);
                
                ArchipelagoData.Data.receivedItems.Add(newItemInfo);

                onNewItemReceived?.Invoke(newItemInfo);
            }
            else
            {
                ArchipelagoData.Data.itemsUnaccountedFor.Remove(matchedItem);

                onProcessedItemReceived?.Invoke(matchedItem);
            }
        }

        private static bool IsSameItem(InscryptionItemInfo left, ItemInfo right)
        {
            return left.ItemId == right.ItemId
                && left.LocationId == right.LocationId
                && left.PlayerSlot == right.Player.Slot;
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
