using Archipelago.MultiClient.Net;
using Archipelago_Inscryption.Archipelago;
using DiskCardGame;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class ArchipelagoOptionsMenu : ManagedBehaviour
    {
        private InputField hostNameField;
        private InputField portField;
        private InputField slotNameField;
        private InputField passwordField;

        private GameObject statusBox;
        private Text statusText;

        private MainInputInteractable button;
        private Text buttonText;

        internal void SetFields(InputField hostNameField, InputField portField, InputField slotNameField, InputField passwordField, GameObject statusBox, MainInputInteractable button)
        {
            this.hostNameField = hostNameField;
            this.portField = portField;
            this.slotNameField = slotNameField;
            this.passwordField = passwordField;
            this.statusBox = statusBox;
            statusText = statusBox.GetComponentInChildren<Text>();
            this.button = button;
            buttonText = button.GetComponentInChildren<Text>();
            button.CursorSelectEnded = OnConnectButtonPressed;

            hostNameField.Text = ArchipelagoData.Data.hostName;
            portField.Text = ArchipelagoData.Data.port.ToString();
            slotNameField.Text = ArchipelagoData.Data.slotName;
            passwordField.Text = ArchipelagoData.Data.password;

            statusBox.SetActive(false);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            ArchipelagoClient.onConnectAttemptDone += OnConnectAttemptDone;
            UpdateButton(ArchipelagoClient.IsConnected);
        }

        private void OnDisable()
        {
            ArchipelagoClient.onConnectAttemptDone -= OnConnectAttemptDone;
        }

        private void OnConnectButtonPressed(MainInputInteractable button)
        {
            if (ArchipelagoClient.IsConnecting) return;

            statusBox.SetActive(true);

            if (int.TryParse(portField.Text, out int port) && port > 1024 && port <= 65535)
            {
                statusText.text = "CONNECTING...";
                ArchipelagoClient.ConnectAsync(hostNameField.Text, port, slotNameField.Text, passwordField.Text);
            }
            else
            {
                statusText.text = "INVALID PORT";
            }
        }

        private void OnConnectAttemptDone(LoginResult result)
        {
            if (result.Successful)
            {
                statusText.text = "CONNECTION SUCCESSFUL!";

                UpdateButton(true);
            }
            else
            {
                statusText.text = "CONNECTION FAILED. CHECK LOGS.";
                string[] errors = ((LoginFailure)result).Errors;

                for (int i  = 0; i < errors.Length; i++)
                {
                    Singleton<ArchipelagoUI>.Instance.LogError(errors[i]);
                }
            }
        }

        private void OnDisconnectButtonPressed(MainInputInteractable button)
        {
            ArchipelagoClient.Disconnect();

            statusText.text = "DISCONNECTED";

            UpdateButton(false);
        }

        private void UpdateButton(bool isConnected)
        {
            if (isConnected)
            {
                button.CursorSelectEnded = OnDisconnectButtonPressed;
                buttonText.text = "DISCONNECT";
            }
            else
            {
                button.CursorSelectEnded = OnConnectButtonPressed;
                buttonText.text = "CONNECT";
            }
        }
    }
}
