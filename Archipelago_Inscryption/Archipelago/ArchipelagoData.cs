using DiskCardGame;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Archipelago_Inscryption.Archipelago
{

    internal class ArchipelagoData
    {
        [JsonIgnore]
        internal static int currentVersion = 1;

        [JsonIgnore]
        internal static string saveName = "";

        [JsonIgnore]
        internal static string saveFilePath = "";

        [JsonIgnore]
        internal static string dataFilePath = "";

        [JsonIgnore]
        internal static ArchipelagoData Data;

        [JsonProperty("version")]
        internal int version = 0;

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
        [JsonProperty("skipEpilogue")]
        internal bool skipEpilogue = false;

        [JsonProperty("completedChecks")]
        internal List<long> completedChecks = new List<long>();
        [JsonProperty("receivedItems")]
        internal List<InscryptionItemInfo> receivedItems = new List<InscryptionItemInfo>();
        [JsonIgnore]
        internal List<InscryptionItemInfo> itemsUnaccountedFor = new List<InscryptionItemInfo>();

        [JsonProperty("customCardInfos")]
        internal List<CustomCardInfo> customCardInfos = new List<CustomCardInfo>();
        [JsonProperty("mycoCardInfo")]
        internal CustomCardInfo mycoCardInfo;
        [JsonIgnore]
        internal List<CardModificationInfo> customCardsModsAct3 = new List<CardModificationInfo>();
        [JsonIgnore]
        internal CardModificationInfo mycoCardMod = null;

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

        internal static void SaveToFile()
        {
            string json = JsonConvert.SerializeObject(Data);
            File.WriteAllText(dataFilePath, json);
        }
    }

    internal struct CustomCardInfo
    {
        [JsonProperty("singletonId")]
        public string SingletonId { get; set; }

        [JsonProperty("nameReplacement")]
        public string NameReplacement { get; set; }

        [JsonProperty("attackAdjustment")]
        public int AttackAdjustment { get; set; }

        [JsonProperty("healthAdjustment")]
        public int HealthAdjustment { get; set; }

        [JsonProperty("energyCostAdjustment")]
        public int EnergyCostAdjustment { get; set; }

        [JsonProperty("abilities")]
        public List<Ability> Abilities { get; set; }

        [JsonProperty("spriteIndices")] 
        public int[] SpriteIndices { get; set; }

        public CustomCardInfo(string singletonId, string nameReplacement, int attackAdjustment, int healthAdjustment, int energyCostAdjustment, List<Ability> abilities, int[] spriteIndices)
        {
            SingletonId = singletonId;
            NameReplacement = nameReplacement;
            AttackAdjustment = attackAdjustment;
            HealthAdjustment = healthAdjustment;
            EnergyCostAdjustment = energyCostAdjustment;
            Abilities = abilities;
            SpriteIndices = spriteIndices;
        }
    }
}
