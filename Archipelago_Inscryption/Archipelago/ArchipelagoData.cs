using Archipelago.MultiClient.Net.Models;
using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Archipelago_Inscryption.Archipelago
{

    internal class ArchipelagoData
    {
        [JsonIgnore]
        private static readonly string dataFilePath = Path.Combine(Paths.GameRootPath, "ArchipelagoData.json");

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

        internal static void LoadData()
        {
            if (File.Exists(dataFilePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(dataFilePath);
                    Data = JsonConvert.DeserializeObject<ArchipelagoData>(fileContent);
                    Data.itemsUnaccountedFor = new List<NetworkItem>(Data.receivedItems);
                }
                catch (Exception e) 
                {
                    ArchipelagoModPlugin.Log.LogError("Error while loading ArchipelagoData.json: " + e.Message);
                }
            }
            else
            {
                Data = new ArchipelagoData();
            }
        }

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
