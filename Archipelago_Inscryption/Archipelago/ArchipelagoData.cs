using Archipelago.MultiClient.Net.Models;
using BepInEx;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Archipelago_Inscryption.Archipelago
{

    internal class ArchipelagoData
    {
        [JsonIgnore]
        internal static string saveName = "";

        [JsonIgnore]
        internal static string saveFilePath = "";

        [JsonIgnore]
        internal static string dataFilePath = "";

        [JsonIgnore]
        internal static ArchipelagoData Data;

        [JsonProperty("hostName")]
        internal string hostName = "archipelago.gg";
        [JsonProperty("port")]
        internal int port = 38281;
        [JsonProperty("slotName")]
        internal string slotName = "";
        [JsonProperty("password")]
        internal string password = "";

        [JsonProperty("seed")]
        internal string seed = "";
        [JsonProperty("playerCount")]
        internal int playerCount = 0;
        [JsonProperty("totalLocationsCount")]
        internal int totalLocationsCount = 0;
        [JsonProperty("totalItemsCount")]
        internal int totalItemsCount = 0;
        [JsonProperty("goalType")]
        internal Goal goalType = Goal.COUNT;

        [JsonProperty("completedChecks")]
        internal List<long> completedChecks = new List<long>();
        [JsonProperty("receivedItems")]
        internal List<NetworkItem> receivedItems = new List<NetworkItem>();
        [JsonIgnore]
        internal List<NetworkItem> itemsUnaccountedFor = new List<NetworkItem>();

        [JsonProperty("availableCardPacks")]
        internal int availableCardPacks = 0;

        [JsonProperty("cabinSafeCode")]
        internal List<int> cabinSafeCode = new List<int>();
        [JsonProperty("cabinClockCode")]
        internal List<int> cabinClockCode = new List<int>();
        [JsonProperty("cabinSmallClockCode")]
        internal List<int> cabinSmallClockCode = new List<int>();
        [JsonProperty("factoryClockCode")]
        internal List<int> factoryClockCode = new List<int>();
        [JsonProperty("wizardCode1")]
        internal List<int> wizardCode1 = new List<int>();
        [JsonProperty("wizardCode2")]
        internal List<int> wizardCode2 = new List<int>();
        [JsonProperty("wizardCode3")]
        internal List<int> wizardCode3 = new List<int>();

        [JsonProperty("act1Completed")]
        internal bool act1Completed = false;
        [JsonProperty("act2Completed")]
        internal bool act2Completed = false;
        [JsonProperty("act3Completed")]
        internal bool act3Completed = false;
        [JsonProperty("epilogueCompleted")]
        internal bool epilogueCompleted = false;
        [JsonProperty("goalCompletedAndSent")]
        internal bool goalCompletedAndSent = false;

        [JsonIgnore]
        internal uint index = 0;

        internal void Reset()
        {
            availableCardPacks = 0;
            index = 0;

            completedChecks.Clear();
            receivedItems.Clear();
            itemsUnaccountedFor.Clear();

            cabinSafeCode.Clear();
            cabinClockCode.Clear();
            cabinSmallClockCode.Clear();
            factoryClockCode.Clear();

            act1Completed = false;
            act2Completed = false;
            act3Completed = false;
            epilogueCompleted = false;
            goalCompletedAndSent = false;
        }

        internal static void SaveToFile()
        {
            string json = JsonConvert.SerializeObject(Data);
            File.WriteAllText(dataFilePath, json);
        }
    }
}
