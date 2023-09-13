using Archipelago_Inscryption.Archipelago;
using DiskCardGame;
using System.Collections;
using UnityEngine;

namespace Archipelago_Inscryption.Components
{
    internal class DiscoverableCheckInteractable : DiscoverableCardInteractable
    {
        internal APCheck check;

        internal SelectableCard card;

        public override void UnlockObject()
        {
            StartCoroutine(UnlockAfterDiscard());
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

        private IEnumerator UnlockAfterDiscard()
        {
            yield return new WaitUntil(() => Discovering);
            yield return new WaitUntil(() => !Discovering);
            ArchipelagoManager.SendCheck(check);
        }
    }
}
