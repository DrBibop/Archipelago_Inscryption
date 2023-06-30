using Archipelago.MultiClient.Net;
using DiskCardGame;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Archipelago
{
    internal class ArchipelagoOptionsMenu : ManagedBehaviour
    {
        private Components.InputField hostNameField;
        private Components.InputField portField;
        private Components.InputField slotNameField;
        private Components.InputField passwordField;

        private GameObject statusBox;
        private Text statusText;

        private MainInputInteractable button;
        private Text buttonText;

        internal void SetFields(Components.InputField hostNameField, Components.InputField portField, Components.InputField slotNameField, Components.InputField passwordField, GameObject statusBox, MainInputInteractable button)
        {
            this.hostNameField = hostNameField;
            this.portField = portField;
            this.slotNameField = slotNameField;
            this.passwordField = passwordField;
            this.statusBox = statusBox;
            this.statusText = statusBox.GetComponentInChildren<Text>();
            this.button = button;
            this.buttonText = button.GetComponentInChildren<Text>();
            button.CursorSelectEnded = OnConnectButtonPressed;

            statusBox.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ArchipelagoClient.onConnectAttemptDone += OnConnectAttemptDone;
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

                button.CursorSelectEnded = OnDisconnectButtonPressed;

                buttonText.text = "DISCONNECT";
            }
            else
            {
                statusText.text = "CONNECTION FAILED. CHECK LOGS.";
            }
        }

        private void OnDisconnectButtonPressed(MainInputInteractable button)
        {
            ArchipelagoClient.Disconnect();

            statusText.text = "DISCONNECTED";

            button.CursorSelectEnded = OnConnectButtonPressed;

            buttonText.text = "CONNECT";
        }
    }
}
