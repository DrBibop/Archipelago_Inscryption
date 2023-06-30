using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
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

        internal static bool IsConnecting => isConnecting;
        internal static bool IsConnected => isConnected;

        private const string ArchipelagoVersion = "0.4.1";

        private delegate void OnConnectAttempt(LoginResult result);

        private static bool isConnecting = false;
        private static bool isConnected = false;
        private static Data serverData;

        private static ArchipelagoSession session;

        internal static bool ConnectAsync(string hostName, int port, string slotName, string password)
        {
            if (isConnecting || isConnected) return false;

            serverData = new Data(hostName, port, slotName, password);

            isConnecting = true;

            ArchipelagoModPlugin.Log.LogInfo($"Connecting to {serverData.hostName}:{serverData.port} as {serverData.slotName}...");

            ThreadPool.QueueUserWorkItem(_ => Connect(OnConnected));

            return true;
        }

        private static ArchipelagoSession CreateSession()
        {
            var session = ArchipelagoSessionFactory.CreateSession(serverData.hostName, serverData.port);
            session.MessageLog.OnMessageReceived += OnMessageReceived;
            session.Socket.ErrorReceived += SessionErrorReceived;
            session.Socket.SocketClosed += SessionSocketClosed;
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

        internal static void Disconnect()
        {
            ArchipelagoModPlugin.Log.LogInfo("Disconnecting from server...");
            session?.Socket.DisconnectAsync();
            session = null;
            isConnected = false;
            isConnecting = false;
        }

        private static void OnConnected(LoginResult result)
        {
            if (onConnectAttemptDone != null)
            {
                onConnectAttemptDone(result);
            }
        }

        private static void SessionSocketClosed(string reason)
        {
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
            ArchipelagoModPlugin.Log.LogInfo(message.ToString());
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
            public List<string> completedChecks;
            public List<string> receivedItems;

            public Data(
                string hostName                     = "archipelago.gg",
                int port                            = 38281,
                string slotName                     = "",
                string password                     = "",
                bool deathlink                      = false,
                string seed                         = "Unknown",
                Dictionary<string, object> slotData = null,
                List<string> completedChecks        = null,
                List<string> receivedItems          = null
                )
            {
                this.hostName           = hostName;
                this.port               = port;
                this.slotName           = slotName;
                this.password           = password;
                this.deathlink          = deathlink;
                this.seed               = seed;
                this.slotData           = slotData == null ? new Dictionary<string, object>() : slotData;
                this.completedChecks    = completedChecks == null ? new List<string>() : completedChecks;
                this.receivedItems      = receivedItems == null ? new List<string>() : receivedItems;
            }
        }
    }
}
