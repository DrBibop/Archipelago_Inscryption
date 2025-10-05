using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using BepInEx;
using BepInEx.Logging;
using DiskCardGame;
using HarmonyLib;
using System.Reflection;

namespace Archipelago_Inscryption
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class ArchipelagoModPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "ballininc.inscryption.archipelagomod";
        internal const string PluginName = "ArchipelagoMod";
        internal const string PluginVersion = "1.0.3";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Harmony harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            AssetsManager.LoadAssets();
            ArchipelagoManager.Init();

            // To remove the lag spike when obtaining a card during the connection screen
            ScriptableObjectLoader<CardInfo>.LoadData();
        }
    }
}
