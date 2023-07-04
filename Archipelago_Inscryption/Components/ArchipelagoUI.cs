using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class ArchipelagoUI : Singleton<ArchipelagoUI>
    {
        internal static bool exists = false;
        private Text statusText;
        private GameObject logPrefab;
        private Transform logParent;

        private void Awake()
        {
            exists = true;
            statusText = transform.Find("Status").GetComponent<Text>();
            logPrefab = transform.Find("LogEntry").gameObject;
            logPrefab.AddComponent<LogEntry>();
            logParent = transform.Find("Logs");
        }

        internal void UpdateConnectionStatus(bool connected)
        {
            statusText.text = "Archipelago Status: " + (connected ? "<color=green>Connected</color>" : "<color=red>Not Connected</color>");
        }

        internal void LogMessage(string message)
        {
            NewLog(message);
        }

        internal void LogImportant(string message) 
        {
            NewLog("<color=yellow>" + message + "</color>"); 
        }

        internal void LogError(string message)
        {
            NewLog("<color=red>" + message + "</color>");
        }

        private void NewLog(string message)
        {
            GameObject newLog = Instantiate(logPrefab, logParent);
            newLog.SetActive(true);
            newLog.transform.SetAsFirstSibling();
            newLog.GetComponent<LogEntry>().SetText(message);
        }
    }
}
