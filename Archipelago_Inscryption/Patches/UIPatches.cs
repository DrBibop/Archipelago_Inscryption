using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    internal class UIPatches
    {
        [HarmonyPatch(typeof(TabbedUIPanel), "Awake")]
        [HarmonyPrefix]
        static bool AddArchipelagoOptionTab(TabbedUIPanel __instance)
        {
            if (!__instance.gameObject.name.Contains("Options")) return true;

            // Setup tab button

            Transform tab = __instance.transform.Find("MainPanel/Tabs/Tab_4");
            if (!tab) return true;

            __instance.tabButtons.Add(tab.GetComponent<GenericUIButton>());

            tab.gameObject.SetActive(true);

            SpriteRenderer tabIcon = tab.transform.Find("Icon").GetComponent<SpriteRenderer>();

            tabIcon.sprite = AssetsManager.archiSettingsTabSprite;

            // Setup tab content

            Transform tabGroup = __instance.transform.Find("MainPanel/TabGroup_");
            if (!tabGroup) return true;

            tabGroup.gameObject.name = "TabGroup_Archipelago";

            GameObject inputFieldPrefab = new GameObject("InputField_");
            inputFieldPrefab.transform.ResetTransform();

            BoxCollider2D collider = inputFieldPrefab.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.86f, 0.16f);

            GameObject labelPrefab = __instance.transform.Find("MainPanel/TabGroup_Gameplay/IncrementalSlider_TextSpeed/Title").gameObject;

            GameObject inputFieldLabel = Object.Instantiate(labelPrefab, inputFieldPrefab.transform);
            inputFieldLabel.name = "Title";
            inputFieldLabel.transform.localPosition = labelPrefab.transform.localPosition;

            GameObject fieldContentPrefab = __instance.transform.Find("MainPanel/TabGroup_Gameplay/IncrementalField_Language/TextFrame").gameObject;

            GameObject inputFieldContent = Object.Instantiate(fieldContentPrefab, inputFieldPrefab.transform);
            inputFieldContent.name = "TextFrame";
            inputFieldContent.transform.localPosition = fieldContentPrefab.transform.localPosition;

            inputFieldContent.GetComponent<SpriteRenderer>().sprite = AssetsManager.inputFieldSprite;

            Text inputFieldText = inputFieldContent.GetComponentInChildren<Text>(true);
            inputFieldText.rectTransform.offsetMin = new Vector2(-88, -25);
            inputFieldText.rectTransform.offsetMax = new Vector2(88, 25);
            inputFieldText.alignment = TextAnchor.MiddleLeft;

            inputFieldPrefab.AddComponent<Components.InputField>();

            Components.InputField hostNameField = UIHelper.CreateInputField(inputFieldPrefab, tabGroup, "InputField_HostName", "HOST NAME", "archipelago.gg", 0.74f, 100);
            Components.InputField portField = UIHelper.CreateInputField(inputFieldPrefab, tabGroup, "InputField_Port", "PORT", "", 0.4f, 5);
            Components.InputField slotNameField = UIHelper.CreateInputField(inputFieldPrefab, tabGroup, "InputField_SlotName", "SLOT NAME", "", 0.06f, 16);
            Components.InputField passwordField = UIHelper.CreateInputField(inputFieldPrefab, tabGroup, "InputField_Pass", "PASSWORD", "", -0.28f, 100, true);

            Object.Destroy(inputFieldPrefab);

            GameObject statusBox = Object.Instantiate(fieldContentPrefab, tabGroup);
            statusBox.transform.localPosition = new Vector3(0, -0.6f, 0);
            statusBox.GetComponent<SpriteRenderer>().enabled = false;

            GameObject buttonPrefab = __instance.transform.Find("MainPanel/TabGroup_Gameplay/Button_ApplyGraphics").gameObject;
            GameObject connectButton = Object.Instantiate(buttonPrefab, tabGroup.transform);
            connectButton.name = "Button_Connect";
            connectButton.GetComponentInChildren<Text>().text = "CONNECT";

            ArchipelagoOptionsMenu archipelagoMenu = tabGroup.gameObject.AddComponent<ArchipelagoOptionsMenu>();
            archipelagoMenu.SetFields(hostNameField, portField, slotNameField, passwordField, statusBox, connectButton.GetComponent<GenericUIButton>());

            return true;
        }

        [HarmonyPatch(typeof(MenuController), "Start")]
        [HarmonyPostfix]
        static void CreateStatusAndLogsUI()
        {
            if (ArchipelagoUI.exists) return;

            GameObject uiObj = Object.Instantiate(AssetsManager.archipelagoUIPrefab);
            uiObj.AddComponent<ArchipelagoUI>();
            Object.DontDestroyOnLoad(uiObj);
        }

        [HarmonyPatch(typeof(MenuController), "OnStartGameCardReachedSlot")]
        [HarmonyPrefix]
        static bool PreventPlayIfNotConnected(MenuController __instance)
        {
            if (!ArchipelagoClient.IsConnected)
            {
                UIHelper.ConnectFromMenu(UIHelper.OnConnectAttemptDoneFromMainMenu);

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(GenericUIButton), "UpdateInputButton")]
        [HarmonyPrefix]
        static bool PreventButtonUpdateIfInputFieldSelected()
        {
            return !Components.InputField.IsAnySelected;
        }

        [HarmonyPatch(typeof(GenericUIButton), "UpdateInputKey")]
        [HarmonyPrefix]
        static bool PreventKeyUpdateIfInputFieldSelected()
        {
            return !Components.InputField.IsAnySelected;
        }

        [HarmonyPatch(typeof(DeckBuildingUI), "Start")]
        [HarmonyPostfix]
        static void CreateCardPackButton(DeckBuildingUI __instance)
        {
            GameObject newButtonObject = Object.Instantiate(__instance.autoCompleteButton.gameObject);
            newButtonObject.name = "OpenCardPackButton";
            newButtonObject.transform.SetParent(__instance.transform);
            newButtonObject.transform.localPosition = new Vector3(-1.66f, 0.66f, 0f);
            newButtonObject.GetComponent<BoxCollider2D>().size = new Vector2(0.49f, 0.68f);

            GenericUIButton newButton = newButtonObject.GetComponent<GenericUIButton>();
            newButton.defaultSprite = AssetsManager.packButtonSprites[0];
            newButton.hoveringSprite = AssetsManager.packButtonSprites[1];
            newButton.downSprite = AssetsManager.packButtonSprites[2];
            newButton.disabledSprite = AssetsManager.packButtonSprites[3];

            newButtonObject.GetComponent<SpriteRenderer>().sprite = newButton.defaultSprite;

            Object.Destroy(newButtonObject.transform.GetChild(0).gameObject);

            newButton.CursorSelectEnded = (x => CustomCoroutine.instance.StartCoroutine(RandomizerHelper.OnPackButtonPressed(x)));

            RandomizerHelper.packButton = newButton;
            RandomizerHelper.UpdatePackButtonEnabled();
        }

        [HarmonyPatch(typeof(ChapterSelectMenu), "OnChapterConfirmed")]
        [HarmonyPrefix]
        static bool ChooseChapterWithSameSaveFile(ChapterSelectMenu __instance)
        {
            __instance.confirmPrompt.SetActive(true);

            if (!ArchipelagoClient.IsConnected)
            {
                UIHelper.ConnectFromMenu(result => UIHelper.OnConnectAttemptDoneFromChapterSelect(result, __instance));
            }
            else
            {
                UIHelper.LoadSelectedChapter(__instance.currentSelectedChapter);
            }

            return false;
        }

        [HarmonyPatch(typeof(ChapterSelectMenu), "OnChapterSelected")]
        [HarmonyPrefix]
        static bool ReplaceChapterSelectPromptText(ChapterSelectMenu __instance, int chapter)
        {
            if (chapter == 1)
                __instance.confirmPromptText.text = "Start a new act 1 run?";
            else if (chapter == 2)
                __instance.confirmPromptText.text = StoryEventsData.EventCompleted(StoryEvent.StartScreenNewGameUsed) ? "Continue act 2?" : "Start act 2?";
            else if (chapter == 3)
                __instance.confirmPromptText.text = $"Continue act 3?";

            return true;
        }

        [HarmonyPatch(typeof(ChapterSelectMenu), "Start")]
        [HarmonyPostfix]
        static void DisableCertainButtonsFromChapterSelect(ChapterSelectMenu __instance)
        {
            __instance.transform.Find("Clips_Row").gameObject.SetActive(false);
            __instance.transform.Find("Chapter_Row/ChapterSelectItemUI").gameObject.SetActive(false);
            if (!StoryEventsData.EventCompleted(StoryEvent.StartScreenNewGameUnlocked))
                __instance.transform.Find("Chapter_Row/ChapterSelectItemUI (2)").gameObject.SetActive(false);
            if (!StoryEventsData.EventCompleted(StoryEvent.Part2Completed))
                __instance.transform.Find("Chapter_Row/ChapterSelectItemUI (3)").gameObject.SetActive(false);
            if (!StoryEventsData.EventCompleted(StoryEvent.Part3Completed))
                __instance.transform.Find("Chapter_Row/ChapterSelectItemUI (4)").gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(MenuController), "OnCardReachedSlot")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ReplaceNewGameWithChapterSelect(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int index = codes.FindIndex(x => x.Calls(AccessTools.Method(typeof(StoryEventsData), "EventCompleted")));

            codes.RemoveRange(index, 49);

            var newCodes = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIHelper), "GoToChapterSelect"))
            };

            codes.InsertRange(index, newCodes);

            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(MenuCard), "Awake")]
        [HarmonyPostfix]
        static void ReplaceNewGameText(MenuCard __instance)
        {
            if (__instance.MenuAction == MenuAction.NewGame)
            {
                __instance.titleSprite = null;
                __instance.lockedTitleSprite = null;
                __instance.titleLocId = "";
                __instance.titleText = "CHAPTER SELECT";
            }
        }
    }
}
