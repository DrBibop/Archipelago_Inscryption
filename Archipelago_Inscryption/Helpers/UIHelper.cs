﻿using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using GBC;
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

        internal static void LoadSelectedChapter(int chapter)
        {
            SaveManager.LoadFromFile();

            if (FinaleDeletionWindowManager.instance != null)
            {
                GameObject.Destroy(FinaleDeletionWindowManager.instance.gameObject);
            }

            StoryEventsData.EraseEvent(StoryEvent.FullGameCompleted);
            if (chapter != 4)
                StoryEventsData.EraseEvent(StoryEvent.Part3Completed);

            switch (chapter)
            {
                case 1:
                    ScriptableObjectLoader<CardInfo>.AllData.Find(x => x.name == "Hrokkall").temple = CardTemple.Tech;
                    SaveManager.SaveFile.currentScene = "Part1_Cabin";
                    SaveManager.SaveFile.NewPart1Run();
                    break;
                case 2:
                    ScriptableObjectLoader<CardInfo>.AllData.Find(x => x.name == "Hrokkall").temple = CardTemple.Nature;
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

                        SaveManager.SaveFile.currentScene = StoryEventsData.EventCompleted(StoryEvent.GBCIntroCompleted) ? "GBC_WorldMap" : "GBC_Starting_Island";
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
                        Part3SaveData.Data.playerPos.worldId == "StartingArea" || 
                        Part3SaveData.Data.playerPos.worldId == "!FINALE_CHAPTER_SELECT")
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

            LoadingScreenManager.LoadScene(SaveManager.SaveFile.currentScene);
        }

        internal static void GoToChapterSelect()
        {
            Singleton<MenuController>.Instance.ResetToDefaultState();
            Singleton<VideoCameraRig>.Instance.EnterChapterSelect();
        }

        internal static void UpdateChapterButtons()
        {
            ChapterSelectMenu menu = Singleton<VideoCameraRig>.Instance.chapterSelectMenu;

            if (ArchipelagoOptions.goal != Goal.AllActsAnyOrder)
            {
                menu.transform.Find("Chapter_Row/ChapterSelectItemUI (2)").gameObject.SetActive(ArchipelagoOptions.goal != Goal.Act1Only && StoryEventsData.EventCompleted(StoryEvent.StartScreenNewGameUnlocked));
                menu.transform.Find("Chapter_Row/ChapterSelectItemUI (3)").gameObject.SetActive(ArchipelagoOptions.goal != Goal.Act1Only && StoryEventsData.EventCompleted(StoryEvent.Part2Completed));
            }
            menu.transform.Find("Chapter_Row/ChapterSelectItemUI (4)").gameObject.SetActive(ArchipelagoData.Data.act1Completed && ArchipelagoData.Data.act2Completed && ArchipelagoData.Data.act3Completed);
        }
    }
}
