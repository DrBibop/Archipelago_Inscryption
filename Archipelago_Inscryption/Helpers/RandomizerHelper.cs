using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using GBC;
using InscryptionAPI.Card;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using static GBC.DialogueSpeaker;

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

        private static readonly Dictionary<Character, APCheck> npcCheckPairs = new Dictionary<Character, APCheck>()
        {
            { Character.Angler,                             APCheck.GBCBattleAngler },
            { Character.Prospector,                         APCheck.GBCBattleProspector },
            { Character.Trader,                             APCheck.GBCBattleTrapper },
            { Character.Trapper,                            APCheck.GBCBattleTrapper },
            { Character.Leshy,                              APCheck.GBCBossLeshy },
            { Character.GhoulRoyal,                         APCheck.GBCBattleRoyal },
            { Character.GhoulBriar,                         APCheck.GBCBattleKaycee },
            { Character.GhoulSawyer,                        APCheck.GBCBattleSawyer },
            { Character.Grimora,                            APCheck.GBCBossGrimora },
            { Character.GreenWizard,                        APCheck.GBCBattleGoobert },
            { Character.OrangeWizard,                       APCheck.GBCBattlePikeMage },
            { Character.BlueWizard,                         APCheck.GBCBattleLonelyWizard },
            { Character.Magnificus,                         APCheck.GBCBossMagnificus },
            { Character.Inspector,                          APCheck.GBCBattleInspector },
            { Character.Smelter,                            APCheck.GBCBattleMelter },
            { Character.Dredger,                            APCheck.GBCBattleDredger },
            { Character.P03,                                APCheck.GBCBossP03 }
        };

        private static readonly Dictionary<string, APCheck> gbcContainerCheckPair = new Dictionary<string, APCheck>()
        {
            { "GBC_Docks/Room/Objects/Chest/ContainerVolume",                                           APCheck.GBCDockChest },
            { "GBC_Temple_Nature/Temple/OutdoorsCentral/Chest_NaturePack/ContainerVolume",              APCheck.GBCForestChest },
            { "GBC_Temple_Nature/Temple/Meadow/Objects/Chest_NaturePack/ContainerVolume",               APCheck.GBCForestBurrowChest },
            { "GBC_Temple_Nature/Temple/Cabin/Objects/SliderPuzzleContainer",                           APCheck.GBCCabinDrawer },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Casket_CardPack (1)/ContainerVolume",          APCheck.GBCCryptCasket1 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Casket_CardPack/ContainerVolume",              APCheck.GBCCryptCasket2 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Casket_Piece/ContainerVolume",                 APCheck.GBCEpitaphPiece7 },
            { "GBC_Temple_Wizard/Temple/Floor_1/Chest_WizardPack/ContainerVolume",                      APCheck.GBCTowerChest1 },
            { "GBC_Temple_Wizard/Temple/Floor_2/Objects/Chest_WizardPack (1)/ContainerVolume",          APCheck.GBCTowerChest2 },
            { "GBC_Temple_Wizard/Temple/Floor_3/Objects/Chest_Card",                                    APCheck.GBCTowerChest3 },
            { "GBC_Temple_Tech/Temple/--- MainRoom ---/Objects/TechSliderPuzzleContainer",              APCheck.GBCFactoryDrawer1 },
            { "GBC_Temple_Tech/Temple/--- MainRoom ---/Objects/TechSliderPuzzleContainer (1)",          APCheck.GBCFactoryDrawer2 },
            { "GBC_Temple_Tech/Temple/--- AssemblyRoom ---/Objects/Chest_TechPack/ContainerVolume",     APCheck.GBCFactoryChest1 },
            { "GBC_Temple_Tech/Temple/--- AssemblyRoom ---/Objects/Chest_TechPack (1)/ContainerVolume", APCheck.GBCFactoryChest2 },
            { "GBC_Temple_Tech/Temple/--- DredgingRoom ---/Objects/Chest_TechPack/ContainerVolume",     APCheck.GBCFactoryChest3 },
            { "GBC_Temple_Tech/Temple/--- DredgingRoom ---/Objects/Chest_TechPack (1)/ContainerVolume", APCheck.GBCFactoryChest4 },
        };

        private static readonly Dictionary<string, APCheck> gbcEpitaphCheckPair = new Dictionary<string, APCheck>()
        {
            { "Objects",                                    APCheck.GBCEpitaphPiece1 },
            { "EpitaphPieceVolume (1)",                     APCheck.GBCEpitaphPiece2 },
            { "OverworldGhoulNPC_Sawyer",                   APCheck.GBCEpitaphPiece3 },
            { "OverworldGhoulNPC_Royal",                    APCheck.GBCEpitaphPiece4 },
            { "EpitaphPieceVolume (2)",                     APCheck.GBCEpitaphPiece5 },
            { "OverworldGhoulNPC_Briar",                    APCheck.GBCEpitaphPiece6 },
            { "MirrorRoom",                                 APCheck.GBCEpitaphPiece9 },
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

                CardInfo info = GenerateCardInfo(check);

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

        internal static CardInfo GenerateCardInfo(APCheck check)
        {
            CheckInfo checkInfo = ArchipelagoManager.GetCheckInfo(check);

            CardInfo info = ScriptableObject.CreateInstance<CardInfo>();
            info.SetNames("ArchipelagoCheck_" + check.ToString(), checkInfo.itemName);
            info.SetHideStats();
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            info.SetPortrait(modPath + "\\CardPortraits\\archi_portrait.png");
            info.SetPixelPortrait(modPath + "\\CardPortraits\\archi_portrait_gbc.png");
            string description =
                checkInfo.recipientName == ArchipelagoClient.serverData.slotName ?
                $"A misplaced item from your world. Collect this card to receive {checkInfo.itemName}." :
                $"An item from another world. Collecting this card will send {checkInfo.itemName} to {checkInfo.recipientName}.";
            info.description = description;
            return info;
        }

        internal static APCheck GetCheckGainedFromNPC(Character npcCharacter)
        {
            if (npcCheckPairs.TryGetValue(npcCharacter, out APCheck check))
                return check;
            else
                return APCheck.COUNT;
        }

        internal static IEnumerator CombatRewardCheckSequence(CardBattleNPC npc)
        {
            if (npc.gainPacks != null)
            {
                npc.gainPacks = null;
                APCheck check = GetCheckGainedFromNPC(npc.DialogueSpeaker.characterId);
                yield return GiveGBCCheckSequence(check);
            }
        }

        internal static IEnumerator PrePlayerDeathSequence(Part1GameFlowManager manager)
        {
            ArchipelagoModPlugin.Log.LogMessage("Rip bozo");
            yield return manager.KillPlayerSequence();
        }

        internal static void GiveContainerCheck(ContainerVolume instance)
        {
            string containerPath = instance.transform.GetPath();
            string key = $"{SceneLoader.ActiveSceneName}/{containerPath}";
            if (gbcContainerCheckPair.TryGetValue(key, out APCheck check) && !ArchipelagoManager.HasCompletedCheck(check))
            {
                CustomCoroutine.Instance.StartCoroutine(GiveGBCCheckSequence(check));
            }
        }

        internal static IEnumerator GiveGBCCheckSequence(APCheck check)
        {
            CardInfo card = GenerateCardInfo(check);
            if (!ArchipelagoManager.HasCompletedCheck(check))
            {
                Singleton<PlayerMovementController>.Instance.SetEnabled(false);
                yield return SingleCardGainUI.instance.GainCard(card, true);
                Singleton<PlayerMovementController>.Instance.SetEnabled(true);
                SaveManager.SaveToFile(true);
            }
            else
            {
                yield return null;
            }
        }

        internal static string GetCardGainedMessage(CardInfo info)
        {
            if (info.name.Contains("Archipelago"))
                return "The card was sent to its rightful owner.";
            else
                return "The card was added to your collection.";
        }
    }
}
