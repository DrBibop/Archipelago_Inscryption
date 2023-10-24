using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Models;
using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Helpers;
using BepInEx;
using DiskCardGame;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class ArchipelagoUI : Singleton<ArchipelagoUI>
    {
        private const float TIME_BETWEEN_MESSAGES = 0.2f;

        private readonly string savesPath = Path.Combine(Paths.GameRootPath, "ArchipelagoSaveFiles");

        private readonly char[] illegalCharacters = new char[] { '\\', '/', '*', '?', ':', '\"', '<', '>', '|' };

        private const string SAVE_FILE_NAME = "SaveFile-Archipelago.gwsave";

        private const string DATA_FILE_NAME = "ArchipelagoData.json";

        internal static bool exists = false;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private GameObject logPrefab;

        [SerializeField]
        private RectTransform logParent;


        [SerializeField]
        private GameObject saveSelectScreen;

        [SerializeField]
        private GameObject saveNameScreen;

        [SerializeField]
        private GameObject connectScreen;

        [SerializeField]
        private GameObject postConnectScreen;

        [SerializeField]
        private GameObject yesNoPrompt;


        [SerializeField]
        private RectTransform saveSelectViewport;

        [SerializeField]
        private UnityEngine.UI.InputField saveNameInputField;

        [SerializeField]
        private UnityEngine.UI.InputField hostNameInputField;

        [SerializeField]
        private UnityEngine.UI.InputField portInputField;

        [SerializeField]
        private UnityEngine.UI.InputField slotNameInputField;

        [SerializeField]
        private UnityEngine.UI.InputField passwordInputField;

        [SerializeField]
        private Text postConnectText;

        [SerializeField]
        private Text promptMessageText;

        [SerializeField]
        private UnityEngine.UI.Button confirmNameButton;

        [SerializeField]
        private UnityEngine.UI.Button connectButton;

        internal StartScreenController startScreen;

        private Queue<string> messageQueue = new Queue<string>();
        private float logTimer;
        private float saveTimer;
        private float itemTimer;
        private bool canProcessItems;
        private SortedList<DateTime, (string, ArchipelagoData)> dataList = new SortedList<DateTime, (string, ArchipelagoData)>();
        private Dictionary<string, GameObject> saveUIEntries = new Dictionary<string, GameObject>();

        private Action yesCallback;
        private Action noCallback;
        private GameObject screenUnderPrompt;

        public override bool UpdateWhenPaused => true;

        private void Awake()
        {
            exists = true;
            InitializeSaves();
            Singleton<InteractionCursor>.Instance.gameObject.SetActive(false);
            Cursor.visible = true;
        }

        private void InitializeSaves()
        {
            if (!Directory.Exists(savesPath))
            {
                try
                {
                    Directory.CreateDirectory(savesPath);

                    string oldSavePath = Path.Combine(Paths.GameRootPath, SAVE_FILE_NAME);
                    string oldDataPath = Path.Combine(Paths.GameRootPath, DATA_FILE_NAME);

                    if (File.Exists(oldSavePath) && File.Exists(oldDataPath))
                    {
                        ArchipelagoModPlugin.Log.LogInfo("Save file from previous version detected. Adjusting file to new save system...");
                        string legacyPath = Path.Combine(savesPath, "Legacy Save File");
                        Directory.CreateDirectory(legacyPath);
                        File.Move(oldSavePath, Path.Combine(legacyPath, SAVE_FILE_NAME));
                        File.Move(oldDataPath, Path.Combine(legacyPath, DATA_FILE_NAME));
                    }
                } 
                catch (Exception e)
                {
                    ArchipelagoModPlugin.Log.LogError("Failed to initialize new save system: " + e.Message);
                    return;
                }
            }

            string[] directories = Directory.GetDirectories(savesPath);

            for (int i = 0; i < directories.Length; i++)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directories[i]);

                string savePath = Path.Combine(directories[i], SAVE_FILE_NAME);
                string dataPath = Path.Combine(directories[i], DATA_FILE_NAME);

                if (!File.Exists(savePath) || !File.Exists(dataPath)) continue;

                string dataContent = File.ReadAllText(dataPath);
                ArchipelagoData loadedData = JsonConvert.DeserializeObject<ArchipelagoData>(dataContent);

                if (loadedData != null)
                {
                    loadedData.itemsUnaccountedFor = new List<NetworkItem>(loadedData.receivedItems);
                    dataList.Add(File.GetLastWriteTime(dataPath), (dirInfo.Name, loadedData));
                }
                else
                {
                    ArchipelagoModPlugin.Log.LogError("Failed to load data from save file \"" + dirInfo.Name + "\". The file might be corrupted or is unsupported by this version of the mod.");
                }
            }

            foreach (var dataEntry in dataList)
            {
                CreateEntryInScrollview(dataEntry.Key, dataEntry.Value.Item1, dataEntry.Value.Item2);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(saveSelectViewport);
        }

        private void CreateEntryInScrollview(DateTime lastSaveTime, string entryName, ArchipelagoData data)
        {
            GameObject entryIntance = Instantiate(AssetsManager.saveEntryPrefab, saveSelectViewport);
            entryIntance.transform.SetAsFirstSibling();

            SaveEntry entry = entryIntance.GetComponent<SaveEntry>();
            int goalCount = (data.act1Completed ? 1: 0) + (data.act2Completed ? 1 : 0) + (data.act3Completed ? 1 : 0) + (data.epilogueCompleted ? 1 : 0);
            entry.Init(entryName, lastSaveTime, data.playerCount, data.completedChecks.Count, data.totalLocationsCount, data.receivedItems.Count, data.totalItemsCount, goalCount, data.goalType);

            entry.onPlay.AddListener(delegate { OnSaveFileSelected(entryName); });
            entry.onDelete.AddListener(delegate { OnSaveFileDeleted(entryName); });

            saveUIEntries.Add(entryName, entryIntance);
        }

        public override void ManagedUpdate()
        {
            if (saveNameScreen.activeSelf)
            {
                confirmNameButton.interactable = saveNameInputField.text != "" && !saveUIEntries.ContainsKey(saveNameInputField.text) && !illegalCharacters.Any(c => saveNameInputField.text.Contains(c));
            }
            else if (connectScreen.activeSelf)
            {
                connectButton.interactable = int.TryParse(portInputField.text, out int port) && port > 1024 && port <= 65535;
            }

            if (itemTimer > 0)
            {
                itemTimer -= Time.unscaledDeltaTime;
            }

            if (canProcessItems && itemTimer <= 0 && ArchipelagoManager.ProcessNextItem())
            {
                itemTimer = 0.2f;
            }

            if (saveTimer > 0)
            {
                saveTimer -= Time.unscaledDeltaTime;

                if (saveTimer <= 0)
                    SaveManager.SaveToFile(false);
            }

            if (logTimer > 0)
            {
                logTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (messageQueue.Count > 0)
            {
                NewLog(messageQueue.Dequeue());
                logTimer = TIME_BETWEEN_MESSAGES;
            }
        }

        internal void QueueSave()
        {
            saveTimer = 0.5f;
        }

        internal void UpdateConnectionStatus(bool connected)
        {
            statusText.text = "Archipelago Status: " + (connected ? "<color=green>Connected</color>" : "<color=red>Not Connected</color>");
        }

        internal void LogMessage(string message)
        {
            messageQueue.Enqueue(message);
        }

        internal void LogImportant(string message) 
        {
            messageQueue.Enqueue("<color=yellow>" + message + "</color>"); 
        }

        internal void LogError(string message)
        {
            messageQueue.Enqueue("<color=red>" + message + "</color>");
        }

        private void NewLog(string message)
        {
            GameObject newLog = Instantiate(logPrefab, logParent);
            newLog.SetActive(true);
            newLog.transform.SetAsFirstSibling();
            newLog.GetComponent<LogEntry>().SetText(message);
            LayoutRebuilder.ForceRebuildLayoutImmediate(logParent);
        }

        private void ShowPrompt(string message, Action yesCallback, Action noCallback, GameObject screenUnderPrompt)
        {
            screenUnderPrompt.SetActive(false);
            yesNoPrompt.SetActive(true);
            promptMessageText.text = message;
            this.yesCallback = yesCallback;
            this.noCallback = noCallback;
            this.screenUnderPrompt = screenUnderPrompt;
        }

        public void OnSaveFileSelected(string saveName)
        {
            saveSelectScreen.SetActive(false);
            connectScreen.SetActive(true);

            ArchipelagoData selectedData = dataList.FirstOrDefault((pair) => pair.Value.Item1 == saveName).Value.Item2;
            ArchipelagoData.saveFilePath = Path.Combine(savesPath, saveName, SAVE_FILE_NAME);
            ArchipelagoData.dataFilePath = Path.Combine(savesPath, saveName, DATA_FILE_NAME);
            ArchipelagoData.saveName = saveName;

            UpdateConnectScreenTexts(selectedData);
        }

        private void UpdateConnectScreenTexts(ArchipelagoData data)
        {
            ArchipelagoData.Data = data;

            hostNameInputField.text = data.hostName;
            portInputField.text = data.port.ToString();
            slotNameInputField.text = data.slotName;
            passwordInputField.text = data.password;
        }

        public void OnSaveFileDeleted(string saveName)
        {
            ShowPrompt("Delete " + saveName + "?", () => DeleteSaveFile(saveName), null, saveSelectScreen);
        }

        private void DeleteSaveFile(string saveName)
        {
            Directory.Delete(Path.Combine(savesPath, saveName), true);
            DateTime keyToDelete = dataList.FirstOrDefault(pair => pair.Value.Item1 == saveName).Key;
            dataList.Remove(keyToDelete);
            if (saveUIEntries.TryGetValue(saveName, out GameObject entry))
            {
                Destroy(entry);
                saveUIEntries.Remove(saveName);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(saveSelectViewport);
        }

        public void OnNewSaveFile()
        {
            saveSelectScreen.SetActive(false);
            saveNameScreen.SetActive(true);
            saveNameInputField.text = "";
        }

        public void OnBackButtonPressed()
        {
            if (saveNameScreen.activeSelf)
            {
                saveSelectScreen.SetActive(true);
                saveNameScreen.SetActive(false);
            }
            else if (connectScreen.activeSelf)
            {
                connectScreen.SetActive(false);
                saveSelectScreen.SetActive(true);
            }
        }

        public void OnFileNameConfirmed()
        {
            saveNameScreen.SetActive(false);
            connectScreen.SetActive(true);

            UpdateConnectScreenTexts(new ArchipelagoData());

            string saveName = saveNameInputField.text;
            string savePath = Path.Combine(savesPath, saveName);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            ArchipelagoData.saveFilePath = Path.Combine(savePath, SAVE_FILE_NAME);
            ArchipelagoData.dataFilePath = Path.Combine(savePath, DATA_FILE_NAME);
            ArchipelagoData.saveName = saveName;
        }

        public void OnConnectButtonPressed()
        {
            connectScreen.SetActive(false);
            ArchipelagoClient.ConnectAsync(hostNameInputField.text, int.Parse(portInputField.text), slotNameInputField.text, passwordInputField.text);
            StartCoroutine(PostConnect());
            
        }

        private IEnumerator PostConnect()
        {
            postConnectScreen.SetActive(true);
            postConnectText.text = "Connecting...";

            yield return new WaitUntil(() => !ArchipelagoClient.IsConnecting);

            if (ArchipelagoClient.IsConnected)
            {
                if (ArchipelagoData.Data.seed != "" && ArchipelagoClient.session.RoomState.Seed != ArchipelagoData.Data.seed)
                {
                    ShowPrompt("Seed mismatch! This save file isn't associated to the connected MultiWorld. Proceed anyway?", null, () => connectScreen.SetActive(true), postConnectScreen);
                    
                    yield return new WaitUntil(() => postConnectScreen.activeSelf);

                    if (connectScreen.activeSelf)
                    {
                        ArchipelagoClient.Disconnect();
                        postConnectScreen.SetActive(false);
                        yield break;
                    }
                }

                SaveManager.LoadFromFile();

                postConnectText.text = "Connected";
                yield return new WaitForSeconds(0.75f);

                OnConnectionSuccessful();

                postConnectText.text = "Collecting items...";

                canProcessItems = true;

                yield return null;

                yield return new WaitUntil(() => itemTimer <= 0);

                postConnectScreen.SetActive(false);
                startScreen.gameObject.SetActive(true);
                startScreen.Start();

                if (ArchipelagoOptions.goal == Goal.AllActsAnyOrder)
                {
                    MenuCard chapterSelectCard = startScreen.menu.cards.First(c => c.MenuAction == MenuAction.NewGame);

                    if (chapterSelectCard != null)
                    {
                        chapterSelectCard.lockBeforeStoryEvent = false;
                        chapterSelectCard.SetGlitchedSpriteShown(false);
                    }
                }

                SaveManager.SaveToFile();
            }
            else
            {
                postConnectScreen.SetActive(false);
                connectScreen.SetActive(true);
            }
        }

        private void OnConnectionSuccessful()
        {
            if (ArchipelagoClient.slotData.TryGetValue("deathlink", out var deathlink))
                ArchipelagoOptions.deathlink = Convert.ToInt32(deathlink) != 0;
            if (ArchipelagoClient.slotData.TryGetValue("optional_death_card", out var optionalDeathCard))
                ArchipelagoOptions.optionalDeathCard = (OptionalDeathCard)Convert.ToInt32(optionalDeathCard);
            if (ArchipelagoClient.slotData.TryGetValue("goal", out var goal))
                ArchipelagoOptions.goal = (Goal)Convert.ToInt32(goal);
            if (ArchipelagoClient.slotData.TryGetValue("randomize_codes", out var randomizeCodes))
                ArchipelagoOptions.randomizeCodes = Convert.ToInt32(randomizeCodes) != 0;
            if (ArchipelagoClient.slotData.TryGetValue("randomize_deck", out var randomizeDeck))
                ArchipelagoOptions.randomizeDeck = (RandomizeDeck)Convert.ToInt32(randomizeDeck);
            if (ArchipelagoClient.slotData.TryGetValue("randomize_abilities", out var randomizeAbilities))
                ArchipelagoOptions.randomizeAbilities = (RandomizeAbilities)Convert.ToInt32(randomizeAbilities);
            if (ArchipelagoClient.slotData.TryGetValue("skip_tutorial", out var skipTutorial))
                ArchipelagoOptions.skipTutorial = Convert.ToInt32(skipTutorial) != 0;

            ArchipelagoData.Data.seed = ArchipelagoClient.session.RoomState.Seed;
            ArchipelagoData.Data.playerCount = ArchipelagoClient.session.Players.AllPlayers.Count() - 1;
            ArchipelagoData.Data.totalLocationsCount = ArchipelagoClient.session.Locations.AllLocations.Count();
            ArchipelagoData.Data.totalItemsCount = ArchipelagoData.Data.totalLocationsCount;
            ArchipelagoData.Data.goalType = ArchipelagoOptions.goal;

            DeathLinkManager.DeathLinkService = ArchipelagoClient.session.CreateDeathLinkService();
            DeathLinkManager.Init();

            if (ArchipelagoOptions.randomizeCodes && ArchipelagoData.Data.cabinClockCode.Count <= 0)
            {
                int seed = int.Parse(ArchipelagoClient.session.RoomState.Seed.Substring(ArchipelagoClient.session.RoomState.Seed.Length - 6)) + 20 * ArchipelagoClient.session.ConnectionInfo.Slot;

                ArchipelagoOptions.RandomizeCodes(seed);
            }

            if (ArchipelagoOptions.skipTutorial && !StoryEventsData.EventCompleted(StoryEvent.TutorialRun3Completed))
                ArchipelagoOptions.SkipTutorial();

            ArchipelagoManager.ScoutChecks();
            ArchipelagoManager.VerifyGoalCompletion();
            ArchipelagoClient.SendChecksToServerAsync();
        }

        public void OnQuitButtonPressed()
        {
            ShowPrompt("Quit Inscryption?", () => Application.Quit(), null, saveSelectScreen);
        }

        public void OnPomptYes()
        {
            yesNoPrompt.SetActive(false);
            yesCallback?.Invoke();
            screenUnderPrompt.SetActive(true);
        }

        public void OnPromptNo()
        {
            yesNoPrompt.SetActive(false);
            noCallback?.Invoke();
            screenUnderPrompt.SetActive(true);
        }
    }
}
