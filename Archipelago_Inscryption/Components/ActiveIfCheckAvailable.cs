using Archipelago_Inscryption.Archipelago;
using DiskCardGame;

namespace Archipelago_Inscryption.Components
{
    internal class ActiveIfCheckAvailable : ActiveIfCondition
    {
        internal APCheck check;

        public override bool ConditionIsMet()
        {
            return !ArchipelagoManager.HasCompletedCheck(check);
        }
    }
}
