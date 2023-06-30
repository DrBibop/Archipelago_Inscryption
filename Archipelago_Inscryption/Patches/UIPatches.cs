using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using Archipelago_Inscryption.Utils;
using GBC;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    class UIPatches
    {
        [HarmonyPatch(typeof(TabbedUIPanel), "Awake")]
        [HarmonyPrefix]
        static bool AddArchipelagoOptionTab(TabbedUIPanel __instance)
        {
            // Setup tab button

            Transform tab = __instance.transform.Find("MainPanel/Tabs/Tab_4");
            if (!tab) return true;

            List<GenericUIButton> tabButtons = (List<GenericUIButton>)ReflectionInfos.TabButtonsField.GetValue(__instance);
            if (tabButtons == null || tabButtons.Count == 0) return true;

            tabButtons.Add(tab.GetComponent<GenericUIButton>());

            ReflectionInfos.TabButtonsField.SetValue(__instance, tabButtons);

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
    }
}
