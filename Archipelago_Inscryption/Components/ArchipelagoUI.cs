using Archipelago.MultiClient.Net.Models;
using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using BepInEx;
using DiskCardGame;
using EasyFeedback.APIs;
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
                } 
                catch (Exception e)
                {
                    ArchipelagoModPlugin.Log.LogError("Failed to create save directory: " + e.Message);
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

                string dataContent = "";

                try 
                {
                    dataContent = File.ReadAllText(dataPath);
                }
                catch (Exception e)
                {
                    ArchipelagoModPlugin.Log.LogError("Failed to load save data from " + dirInfo.Name + ": " + e.Message);
                    continue;
                }

                ArchipelagoData loadedData = null;

                try
                {
                    loadedData = JsonConvert.DeserializeObject<ArchipelagoData>(dataContent);
                }
                catch
                {
                    loadedData = null;
                }

                if (loadedData != null)
                {
                    loadedData.itemsUnaccountedFor = new List<InscryptionItemInfo>(loadedData.receivedItems);
                    foreach (var cI in loadedData.customCardInfos)
                    {
                        CardModificationInfo m = new CardModificationInfo();
                        m.singletonId = cI.SingletonId;
                        m.nameReplacement = cI.NameReplacement;
                        m.attackAdjustment = cI.AttackAdjustment;
                        m.healthAdjustment = cI.HealthAdjustment;
                        m.energyCostAdjustment = cI.EnergyCostAdjustment;
                        m.abilities = cI.Abilities;
                        BuildACardPortraitInfo portraitInfo = new BuildACardPortraitInfo();
                        portraitInfo.spriteIndices = cI.SpriteIndices;
                        m.buildACardPortraitInfo = portraitInfo;
                        loadedData.customCardsModsAct3.Add(m);
                    }
                    try
                    {
                        dataList.Add(File.GetLastWriteTime(dataPath), (dirInfo.Name, loadedData));
                    }
                    catch (Exception e)
                    {
                        ArchipelagoModPlugin.Log.LogError("Failed to add save entry " + dirInfo.Name + ": " + e.Message);
                        continue;
                    }
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
            entry.Init(entryName, lastSaveTime, data.playerCount, data.completedChecks.Count, data.totalLocationsCount, data.receivedItems.Count, data.totalItemsCount, goalCount, data.goalType, data.skipEpilogue, data.version);

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
            ArchipelagoData.Data.version = ArchipelagoData.currentVersion;
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

            SaveManager.LoadFromFile();

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

                postConnectText.text = "Connected";
                yield return new WaitForSeconds(0.75f);

                ArchipelagoManager.InitializeFromServer();

                postConnectText.text = "Collecting items...";

                canProcessItems = true;

                yield return null;

                yield return new WaitUntil(() => itemTimer <= 0);

                ArchipelagoManager.VerifyAllItems();

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

                SaveManager.SaveToFile(false);
            }
            else
            {
                postConnectScreen.SetActive(false);
                connectScreen.SetActive(true);
            }
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
