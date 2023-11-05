using Archipelago_Inscryption.Archipelago;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class SaveEntry : MonoBehaviour
    {
        [SerializeField]
        private Text nameText;

        [SerializeField]
        private Text saveTimeText;

        [SerializeField]
        private Text playerCountText;

        [SerializeField]
        private Text locationsText;

        [SerializeField]
        private Text itemsText;

        [SerializeField]
        private Text goalText;

        [SerializeField]
        private UnityEngine.UI.Button playButton;

        [SerializeField]
        private UnityEngine.UI.Button deleteButton;

        internal UnityEngine.UI.Button.ButtonClickedEvent onPlay => playButton.onClick;

        internal UnityEngine.UI.Button.ButtonClickedEvent onDelete => deleteButton.onClick;

        internal void Init(string name, DateTime lastSaveTime, int playerCount, int locationsCount, int maxLocationsCount, int itemsCount, int maxItemsCount, int goalCount, Goal goalType)
        {
            nameText.text = name;
            saveTimeText.text = "Last saved: " + lastSaveTime.ToString("dd-MM-yyyy H:mm");
            playerCountText.text = "MultiWorld player count: " + (playerCount == 0 ? "?" : playerCount.ToString());
            locationsText.text = "Locations: " + locationsCount.ToString() + "/" + (maxLocationsCount == 0 ? "?" : maxLocationsCount.ToString());
            itemsText.text = "Items: " + itemsCount.ToString() + "/" + (maxItemsCount == 0 ? "?" : maxItemsCount.ToString());
            goalText.text = "Goal Progress: " + goalCount.ToString() + "/" + (goalType == Goal.COUNT ? "?" : (goalType == Goal.Act1Only ? "1" : "4"));
        }
    }
}
