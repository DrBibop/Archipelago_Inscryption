using Archipelago_Inscryption.Archipelago;
using DiskCardGame;

namespace Archipelago_Inscryption.Components
{
    internal class DiscoverableCheckInteractable : DiscoverableCardInteractable
    {
        internal APCheck check;

        internal SelectableCard card;

        public override void UnlockObject()
        {
            ArchipelagoManager.SendCheck(check);
        }

        public override void OnCursorSelectStart()
        {
            if (!requireStoryEventToAddToDeck || StoryEventsData.EventCompleted(requiredStoryEvent)) 
            {
                base.OnCursorSelectStart();
            }
            else
            {
                card.Anim.PlayRiffleSound();
            }
        }
    }
}
