using Archipelago.MultiClient.Net;
using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using EasyFeedback;
using InscryptionAPI.Saves;
using System.ComponentModel.Design;
using UnityEngine;

namespace Archipelago_Inscryption.Helpers
{
    internal static class UIHelper
    {
        internal static InputField CreateInputField(GameObject prefab, Transform parent, string name, string label, string defaultContent, float yPosition, int characterLimit, bool censor = false)
        {
            GameObject inputFieldInstance = Object.Instantiate(prefab);
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

        internal static void ConnectFromMainMenu()
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
                ArchipelagoClient.onConnectAttemptDone += OnConnectAttemptDone;
                ArchipelagoClient.ConnectAsync(savedHostName, savedPort, savedSlotName, savedPassword);
            }
        }

        private static void OnConnectAttemptDone(LoginResult result)
        {
            ArchipelagoClient.onConnectAttemptDone -= OnConnectAttemptDone;

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
    }
}
