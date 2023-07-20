using Archipelago.MultiClient.Net;
using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using EasyFeedback;
using InscryptionAPI.Saves;
using System;
using System.ComponentModel.Design;
using UnityEngine;

namespace Archipelago_Inscryption.Helpers
{
    internal static class UIHelper
    {
        internal static InputField CreateInputField(GameObject prefab, Transform parent, string name, string label, string defaultContent, float yPosition, int characterLimit, bool censor = false)
        {
            GameObject inputFieldInstance = GameObject.Instantiate(prefab);
            inputFieldInstance.transform.SetParent(parent);
            inputFieldInstance.transform.ResetTransform();
            inputFieldInstance.transform.localPosition = new Vector3(0, yPosition, 0);
            inputFieldInstance.name = name;
            InputField inputField = inputFieldInstance.GetComponent<InputField>();
            inputField.Label = label;
            inputField.Text = defaultContent;
            inputField.CharacterLimit = characterLimit;
            inputField.Censor = censor;

            return inputField;
        }

        internal static void ConnectFromMenu(Action<LoginResult> callback)
        {
            if (ArchipelagoClient.IsConnecting) return;

            string savedHostName = ModdedSaveManager.SaveData.GetValueAsObject<string>(ArchipelagoModPlugin.PluginGuid, "HostName");
            int savedPort = ModdedSaveManager.SaveData.GetValueAsInt(ArchipelagoModPlugin.PluginGuid, "Port");
            string savedSlotName = ModdedSaveManager.SaveData.GetValueAsObject<string>(ArchipelagoModPlugin.PluginGuid, "SlotName");
            string savedPassword = ModdedSaveManager.SaveData.GetValueAsObject<string>(ArchipelagoModPlugin.PluginGuid, "Password");

            if (savedHostName == null || savedHostName == "" || savedPort <= 1024 || savedPort > 65535)
            {
                Singleton<MenuController>.Instance.ResetToDefaultState();
                Singleton<ArchipelagoUI>.Instance.LogImportant("Connect to Archipelago using the settings menu.");
            }
            else
            {
                Singleton<ArchipelagoUI>.Instance.LogMessage("Connecting...");
                ArchipelagoClient.ConnectAsync(savedHostName, savedPort, savedSlotName, savedPassword, callback);
            }
        }

        internal static void OnConnectAttemptDoneFromMainMenu(LoginResult result)
        {
            MenuController menu = Singleton<MenuController>.Instance;

            if (result.Successful)
            {
                menu.StartCoroutine(menu.TransitionToGame());
            }
            else
            {
                menu.ResetToDefaultState();

                string[] errors = ((LoginFailure)result).Errors;

                for (int i = 0; i < errors.Length; i++)
                {
                    Singleton<ArchipelagoUI>.Instance.LogError(errors[i]);
                }
            }
        }

        internal static void OnConnectAttemptDoneFromChapterSelect(LoginResult result, ChapterSelectMenu menu)
        {
            if (result.Successful)
            {
                LoadSelectedChapter(menu.currentSelectedChapter);
            }
            else
            {
                string[] errors = ((LoginFailure)result).Errors;

                for (int i = 0; i < errors.Length; i++)
                {
                    Singleton<ArchipelagoUI>.Instance.LogError(errors[i]);
                }
            }
        }

        internal static void LoadSelectedChapter(int chapter)
        {
            if (FinaleDeletionWindowManager.instance != null)
            {
                GameObject.Destroy(FinaleDeletionWindowManager.instance.gameObject);
            }

            bool newGameGBC = false;

            switch (chapter)
            {
                case 1:
                    SaveManager.SaveFile.currentScene = "Part1_Cabin";
                    SaveManager.SaveFile.NewPart1Run();
                    break;
                case 2:
                    SaveManager.SaveFile.currentScene = "GBC_Starting_Island";
                    if (!StoryEventsData.EventCompleted(StoryEvent.StartScreenNewGameUsed))
                    {
                        newGameGBC = true;
                    }
                    break;
                case 3:
                    SaveManager.SaveFile.currentScene = "Part3_Cabin";
                    break;
                case 4:
                    SaveManager.SaveFile.currentScene = "Part3_Cabin";
                    SaveManager.SaveFile.part3Data.playerPos = new Part3SaveData.WorldPosition("!FINALE_CHAPTER_SELECT", 2, 1);
                    break;
                default:
                    break;
            }

            MenuController.LoadGameFromMenu(newGameGBC);
        }
    }
}
