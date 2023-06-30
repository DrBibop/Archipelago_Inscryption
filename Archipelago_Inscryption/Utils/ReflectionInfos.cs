using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Reflection;

namespace Archipelago_Inscryption.Utils
{
    internal static class ReflectionInfos
    {
        // Fields

        internal static FieldInfo TabButtonsField = AccessTools.Field(typeof(TabbedUIPanel), "tabButtons");

        // Methods
    }
}
