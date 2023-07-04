using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using InscryptionAPI.Card;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Archipelago_Inscryption.Helpers
{
    internal static class RandomizerHelper
    {
        private static DiscoverableCheckInteractable[] paintingChecks;

        private static readonly string[] checkCardLeshyDialog =
        {
            "This... does not belong here.",
            "What creature could this be?",
            "I don't remember leaving this card there.",
            "How strange...",
            "I believe this belongs to someone.",
            "I don't recognize this...",
            "Perhaps this can be useful to someone.",
            "This is not of this world... What could it be?"
        };

        private static readonly string[] checkCardP03Dialog =
        {
            "Huh? I didn't code that one in.",
            "How did that end up there?",
            "Wait, this doesn't belong in Botopia...",
            "I don't remember printing that.",
            "That's not mine...",
            "Did you print this one yourself?",
            "I'm sure this could be of use to someone, right?",
            "That's weird... Don't let it distract you, though."
        };

        internal static DiscoverableCheckInteractable CreateDiscoverableCardCheck(GameObject originalObject, APCheck check, bool destroyOriginal, StoryEvent activeStoryFlag = StoryEvent.NUM_EVENTS)
        {
            if (!ArchipelagoManager.HasCompletedCheck(check))
            {
                GameObject objectToFollow;
                SelectableCard originalSelectableCard = originalObject.GetComponentInChildren<SelectableCard>(true);
                if (originalSelectableCard != null) 
                    objectToFollow = originalSelectableCard.gameObject;
                else 
                    objectToFollow = originalObject;

                GameObject newCheckCard = new GameObject("DiscoverableCheck_" + check.ToString());
                newCheckCard.transform.SetParent(originalObject.transform.parent);
                newCheckCard.transform.position = objectToFollow.transform.position;
                newCheckCard.transform.rotation = objectToFollow.transform.rotation;
                newCheckCard.transform.localScale = 
                    originalSelectableCard ? 
                    Vector3.Scale(originalObject.transform.localScale, originalSelectableCard.transform.localScale) 
                    : originalObject.transform.localScale;
                newCheckCard.AddComponent<BoxCollider>().size = originalObject.GetComponent<BoxCollider>().size;

                float closeUpDistance = 2.2f;
                Vector3 closeUpEulers = Vector3.zero;
                float closeUpVerticalOffset = 0f;

                DiscoverableObjectInteractable originalCardInteractable = originalObject.GetComponent<DiscoverableObjectInteractable>();

                if (originalCardInteractable)
                {
                    closeUpDistance = originalCardInteractable.closeUpDistance;
                    closeUpEulers = originalCardInteractable.closeUpEulers;
                    closeUpVerticalOffset = originalCardInteractable.closeUpVerticalOffset;
                }

                CheckInfo checkInfo = ArchipelagoManager.GetCheckInfo(check);

                CardInfo info = ScriptableObject.CreateInstance<CardInfo>();
                info.SetNames("ArchipelagoCheck_" + check.ToString(), checkInfo.itemName);
                info.SetHideStats();
                string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                info.SetPortrait(modPath + "\\CardPortraits\\archi_portrait.png");

                DiscoverableCheckInteractable newCardInteractable = newCheckCard.AddComponent<DiscoverableCheckInteractable>();

                newCardInteractable.check = check;
                newCardInteractable.closeUpDistance = closeUpDistance;
                newCardInteractable.closeUpEulers = closeUpEulers;
                newCardInteractable.closeUpVerticalOffset = closeUpVerticalOffset;
                string[] discoverTextDialogs = SaveManager.SaveFile.IsPart1 ? checkCardLeshyDialog : checkCardP03Dialog;
                newCardInteractable.onDiscoverText = discoverTextDialogs[Random.Range(0, discoverTextDialogs.Length)];
                newCardInteractable.storyEvent = StoryEvent.NUM_EVENTS;
                newCardInteractable.requireStoryEventToAddToDeck = false;
                GameObject newCard = Object.Instantiate(AssetsManager.selectableCardPrefab, newCheckCard.transform);
                newCard.name = "ArchipelagoCheckCard_" + check.ToString();
                newCard.transform.ResetTransform();
                newCardInteractable.card = newCard.GetComponent<SelectableCard>();
                newCardInteractable.card.SetInfo(info);

                if (activeStoryFlag < StoryEvent.NUM_EVENTS)
                {
                    ActiveIfStoryFlag storyFlagCondition = newCardInteractable.gameObject.AddComponent<ActiveIfStoryFlag>();
                    storyFlagCondition.targetObject = newCard;
                    storyFlagCondition.checkConditionEveryFrame = true;
                    storyFlagCondition.activeIfConditionMet = true;
                    storyFlagCondition.storyFlag = activeStoryFlag;
                }

                if (destroyOriginal)
                    Object.Destroy(originalObject);

                return newCardInteractable;
            }
            else
            {
                if (destroyOriginal)
                    Object.Destroy(originalObject);

                return null;
            }
            
        }

        internal static void CreateWizardEyeCheck(EyeballInteractable wizardEye)
        {
            GameObject reference = new GameObject();
            reference.transform.SetParent(wizardEye.transform.parent);
            reference.transform.position = wizardEye.transform.position;
            reference.transform.localEulerAngles = new Vector3(90, 0, 0);
            reference.transform.localScale = Vector3.one * 0.7114f;
            reference.AddComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.4f);

            DiscoverableCheckInteractable checkCard = CreateDiscoverableCardCheck(reference, APCheck.CabinMagnificusEye, true);
        }

        internal static void SetPaintingRewards(DiscoverableCheckInteractable card1, DiscoverableCheckInteractable card2, DiscoverableCheckInteractable card3)
        {
            paintingChecks = new DiscoverableCheckInteractable[] { card1, card2, card3 };
        }

        internal static void ClaimPaintingCheck(int rewardIndex)
        {
            paintingChecks[rewardIndex].Discover();
        }
    }
}
