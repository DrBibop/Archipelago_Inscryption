using Archipelago.MultiClient.Net;
using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using GBC;
using System;
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

            if (ArchipelagoData.Data.hostName == "" || ArchipelagoData.Data.port <= 1024 || ArchipelagoData.Data.port > 65535)
            {
                Singleton<MenuController>.Instance.ResetToDefaultState();
                Singleton<ArchipelagoUI>.Instance.LogImportant("Connect to Archipelago using the settings menu.");
            }
            else
            {
                Singleton<ArchipelagoUI>.Instance.LogMessage("Connecting...");
                ArchipelagoClient.ConnectAsync(ArchipelagoData.Data.hostName, ArchipelagoData.Data.port, ArchipelagoData.Data.slotName, ArchipelagoData.Data.password, callback);
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
            SaveManager.LoadFromFile();

            if (FinaleDeletionWindowManager.instance != null)
            {
                GameObject.Destroy(FinaleDeletionWindowManager.instance.gameObject);
            }

            switch (chapter)
            {
                case 1:
                    SaveManager.SaveFile.currentScene = "Part1_Cabin";
                    SaveManager.SaveFile.NewPart1Run();
                    break;
                case 2:
                    StoryEventsData.EraseEvent(StoryEvent.GBCUndeadFinaleChosen);
                    StoryEventsData.EraseEvent(StoryEvent.GBCNatureFinaleChosen);
                    StoryEventsData.EraseEvent(StoryEvent.GBCTechFinaleChosen);
                    StoryEventsData.EraseEvent(StoryEvent.GBCWizardFinaleChosen);
                    if (StoryEventsData.EventCompleted(StoryEvent.StartScreenNewGameUsed))
                    {
                        // Reset temple rooms to default entrance
                        SaveData.Data.natureTemple.roomId = "OutdoorsCentral";
                        SaveData.Data.natureTemple.cameraPosition = Vector2.zero;
                        SaveData.Data.undeadTemple.roomId = "MainRoom";
                        SaveData.Data.undeadTemple.cameraPosition = Vector2.zero;
                        SaveData.Data.techTemple.roomId = "--- MainRoom ---";
                        SaveData.Data.techTemple.cameraPosition = Vector2.zero;
                        SaveData.Data.wizardTemple.roomId = "Floor_1";
                        SaveData.Data.wizardTemple.cameraPosition = Vector2.zero;

                        SaveManager.SaveFile.currentScene = "GBC_WorldMap";
                        SaveData.Data.overworldNode = "StartingIsland";
                    }
                    else
                    {
                        SaveManager.SaveFile.currentScene = "GBC_Intro";
                    }
                    break;
                case 3:
                    if (StoryEventsData.EventCompleted(StoryEvent.ArchivistDefeated) &&
                        StoryEventsData.EventCompleted(StoryEvent.PhotographerDefeated) && 
                        StoryEventsData.EventCompleted(StoryEvent.TelegrapherDefeated) && 
                        StoryEventsData.EventCompleted(StoryEvent.CanvasDefeated) &&
                        Part3SaveData.Data.playerPos.worldId == "StartingArea")
                        Part3SaveData.Data.playerPos = new Part3SaveData.WorldPosition("NorthNeutralPath", 2, 1);
                    SaveManager.SaveFile.currentScene = "Part3_Cabin";
                    break;
                case 4:
                    SaveManager.SaveFile.currentScene = "Part3_Cabin";
                    SaveManager.SaveFile.part3Data.playerPos = new Part3SaveData.WorldPosition("!FINALE_CHAPTER_SELECT", 2, 1);
                    break;
                default:
                    break;
            }

            StoryEventsData.EraseEvent(StoryEvent.FullGameCompleted);
            StoryEventsData.EraseEvent(StoryEvent.Part3Completed);

            LoadingScreenManager.LoadScene(SaveManager.SaveFile.currentScene);
        }

        internal static void GoToChapterSelect()
        {
            Singleton<MenuController>.Instance.ResetToDefaultState();
            Singleton<VideoCameraRig>.Instance.EnterChapterSelect();
        }
    }
}
