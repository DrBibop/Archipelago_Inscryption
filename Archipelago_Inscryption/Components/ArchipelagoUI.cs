﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class ArchipelagoUI : Singleton<ArchipelagoUI>
    {
        private const float TIME_BETWEEN_MESSAGES = 0.2f;

        internal static bool exists = false;
        private Text statusText;
        private GameObject logPrefab;
        private VerticalLayoutGroup logParent;
        private Queue<string> messageQueue = new Queue<string>();
        private float waitTimer;

        public override bool UpdateWhenPaused => true;

        private void Awake()
        {
            exists = true;
            statusText = transform.Find("Status").GetComponent<Text>();
            logPrefab = transform.Find("LogEntry").gameObject;
            logPrefab.AddComponent<LogEntry>();
            logParent = transform.Find("Logs").GetComponent<VerticalLayoutGroup>();
        }

        public override void ManagedUpdate()
        {
            if (waitTimer > 0)
            {
                waitTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (messageQueue.Count > 0)
            {
                NewLog(messageQueue.Dequeue());
                waitTimer = TIME_BETWEEN_MESSAGES;
            }
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
            GameObject newLog = Instantiate(logPrefab, logParent.transform);
            newLog.SetActive(true);
            newLog.transform.SetAsFirstSibling();
            newLog.GetComponent<LogEntry>().SetText(message);
            LayoutRebuilder.ForceRebuildLayoutImmediate(logParent.transform as RectTransform);
        }
    }
}
