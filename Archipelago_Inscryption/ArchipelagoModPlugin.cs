using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Reflection;

namespace Archipelago_Inscryption
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("cyantist.inscryption.api")]
    public class ArchipelagoModPlugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "ballininc.inscryption.archipelagomod";
        internal const string PluginName = "ArchipelagoMod";
        internal const string PluginVersion = "0.0.1";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Harmony harmony = new Harmony(PluginGuid);
            HarmonyFileLog.Enabled = true;
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            AssetsManager.LoadAssets();
            ArchipelagoManager.Init();
            ArchipelagoClient.Init();
        }
    }
}
