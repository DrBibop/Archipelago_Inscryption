using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Assets;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using DiskCardGame;
using GBC;
using Pixelplacement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static GBC.DialogueSpeaker;

namespace Archipelago_Inscryption.Helpers
{
    internal static class RandomizerHelper
    {
        private static DiscoverableCheckInteractable[] paintingChecks;

        private static int randomSeed = UnityEngine.Random.Range(1, 500);

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
            "Huh? What even is this?",
            "How did that end up there?",
            "Wait, this doesn't belong in Botopia...",
            "I don't remember printing that.",
            "That's not mine...",
            "I'd be embarassed to give that to anybody.",
            "This looks completely useless.",
            "That's weird... Don't let it distract you, though."
        };
        /*
        private static readonly string[] checkCardTraderDialog =
        {
            "A fine reward for your first pelt, don't you think?",
            "Quite mysterious, but surely worth the pelt.",
            "Here's another strange one. Do you know what this is?",
            "I am still unsure where these cards come from.",
            "This is the last one I found."
        };*/

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

        private static readonly Dictionary<string, APCheck> gbcObjectCheckPair = new Dictionary<string, APCheck>()
        {
            { "GBC_Docks/Room/Objects/Chest/ContainerVolume",                                           APCheck.GBCDockChest },
            { "GBC_Temple_Nature/Temple/OutdoorsCentral/Chest_NaturePack/ContainerVolume",              APCheck.GBCForestChest },
            { "GBC_Temple_Nature/Temple/Meadow/Objects/Chest_NaturePack/ContainerVolume",               APCheck.GBCForestBurrowChest },
            { "GBC_Temple_Nature/Temple/Cabin/Objects/SliderPuzzleContainer",                           APCheck.GBCCabinDrawer },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Casket_CardPack (1)/ContainerVolume",          APCheck.GBCCryptCasket1 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Casket_CardPack/ContainerVolume",              APCheck.GBCCryptCasket2 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/EpitaphPieceVolume",                           APCheck.GBCEpitaphPiece1 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/EpitaphPieceVolume (1)",                       APCheck.GBCEpitaphPiece2 },
            { "GBC_Temple_Undead/Temple/MainRoom/OverworldGhoulNPC_Sawyer",                             APCheck.GBCEpitaphPiece3 },
            { "GBC_Temple_Undead/Temple/BasementRoom/EpitaphPieceVolume (2)",                           APCheck.GBCEpitaphPiece4 },
            { "GBC_Temple_Undead/Temple/MainRoom/OverworldGhoulNPC_Royal",                              APCheck.GBCEpitaphPiece5 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Casket_Piece/ContainerVolume",                 APCheck.GBCEpitaphPiece6 },
            { "GBC_Temple_Undead/Temple/MirrorRoom/EpitaphPieceVolume",                                 APCheck.GBCEpitaphPiece7 },
            { "GBC_Temple_Undead/Temple/MainRoom/OverworldGhoulNPC_Briar",                              APCheck.GBCEpitaphPiece8 },
            { "GBC_Temple_Undead/Temple/MainRoom/Objects/Well/ContainerVolume",                         APCheck.GBCEpitaphPiece9 },
            { "GBC_Temple_Wizard/Temple/Floor_1/Chest_WizardPack/ContainerVolume",                      APCheck.GBCTowerChest1 },
            { "GBC_Temple_Wizard/Temple/Floor_2/Objects/Chest_WizardPack (1)/ContainerVolume",          APCheck.GBCTowerChest2 },
            { "GBC_Temple_Wizard/Temple/Floor_3/Objects/Chest_Card/ContainerVolume",                    APCheck.GBCTowerChest3 },
            { "GBC_Temple_Tech/Temple/--- MainRoom ---/Objects/TechSliderPuzzleContainer",              APCheck.GBCFactoryDrawer1 },
            { "GBC_Temple_Tech/Temple/--- MainRoom ---/Objects/TechSliderPuzzleContainer (1)",          APCheck.GBCFactoryDrawer2 },
            { "GBC_Temple_Tech/Temple/--- AssemblyRoom ---/Objects/Chest_TechPack/ContainerVolume",     APCheck.GBCFactoryChest1 },
            { "GBC_Temple_Tech/Temple/--- AssemblyRoom ---/Objects/Chest_TechPack (1)/ContainerVolume", APCheck.GBCFactoryChest2 },
            { "GBC_Temple_Tech/Temple/--- DredgingRoom ---/Objects/Chest_TechPack/ContainerVolume",     APCheck.GBCFactoryChest3 },
            { "GBC_Temple_Tech/Temple/--- DredgingRoom ---/Objects/Chest_TechPack (1)/ContainerVolume", APCheck.GBCFactoryChest4 },
        };

        internal static GenericUIButton packButton;

        private static GameObject packPile;
        private static List<GameObject> packs = new List<GameObject>();
        private static bool doDeathCard = true;

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
                newCardInteractable.onDiscoverText = info.description;
                newCardInteractable.storyEvent = StoryEvent.NUM_EVENTS;
                newCardInteractable.requireStoryEventToAddToDeck = false;
                GameObject newCard = GameObject.Instantiate(SaveManager.SaveFile.IsPart3 ? AssetsManager.selectableDiskCardPrefab : AssetsManager.selectableCardPrefab, newCheckCard.transform);
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
                    GameObject.Destroy(originalObject);

                return newCardInteractable;
            }
            else
            {
                if (destroyOriginal)
                    GameObject.Destroy(originalObject);

                return null;
            }
            
        }

        internal static HoloMapNode CreateHoloMapNodeCheck(GameObject originalNodeObject, APCheck check)
        {
            HoloMapNode originalNode = originalNodeObject.GetComponent<HoloMapNode>();

            HoloMapNode newNode = null;

            if (!ArchipelagoManager.HasCompletedCheck(check))
            {
                GameObject newNodeObject = GameObject.Instantiate(AssetsManager.cardChoiceHoloNodePrefab, originalNodeObject.transform.parent);
                newNodeObject.transform.localPosition = originalNodeObject.transform.localPosition;
                newNodeObject.transform.localRotation = originalNodeObject.transform.localRotation;

                newNode = newNodeObject.GetComponent<HoloMapNode>();
                newNode.nodeId = originalNode.nodeId;
                newNode.fixedChoices = new List<CardInfo>() { GenerateCardInfo(check) };
                newNodeObject.transform.Find("RendererParent/Renderer").GetComponent<MeshFilter>().sharedMesh = AssetsManager.checkCardHoloNodeMesh;
                Renderer rendererToDelete = newNode.nodeRenderers[1];
                newNode.nodeRenderers.RemoveAt(1);
                GameObject.Destroy(rendererToDelete.gameObject);
                newNode.AssignNodeData();

                if (!originalNodeObject.activeSelf)
                    newNodeObject.SetActive(false);

                Singleton<MapNodeManager>.Instance.nodes.Add(newNode);

                HoloMapShopNode shopNode = originalNode.GetComponentInParent<HoloMapShopNode>();

                if (shopNode)
                {
                    shopNode.nodeToBuy = newNode;
                    newNode.defaultColor = new Color(1f, 0.5725f, 0.149f);
                    foreach (Renderer renderer in newNode.nodeRenderers)
                    {
                        renderer.material.SetColor("_MainColor", newNode.defaultColor);
                    }
                    GameObject.Destroy(newNodeObject.GetComponent<BoxCollider>());
                }
            }

            if (Singleton<MapNodeManager>.Instance.nodes.Contains(originalNode))
                Singleton<MapNodeManager>.Instance.nodes.Remove(originalNode);

            GameObject.Destroy(originalNodeObject);

            return newNode;
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
            info.name = "ArchipelagoCheck_" + check.ToString();
            info.displayedName = checkInfo.itemName;
            info.hideAttackAndHealth = true;
            info.portraitTex = AssetsManager.cardPortraitSprite;
            info.pixelPortrait = AssetsManager.cardPixelPortraitSprite;
            string[] discoverTextDialogs = SaveManager.SaveFile.IsPart3 ? checkCardP03Dialog : checkCardLeshyDialog;
            info.description = discoverTextDialogs[UnityEngine.Random.Range(0, discoverTextDialogs.Length)];
            return info;
        }

        internal static CardInfo GenerateCardInfoWithName(string name, string description)
        {
            CardInfo info = ScriptableObject.CreateInstance<CardInfo>();
            info.name = "Archipelago_" + name;
            info.displayedName = name;
            info.hideAttackAndHealth = true;
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            info.portraitTex = AssetsManager.cardPortraitSprite;
            info.pixelPortrait = AssetsManager.cardPixelPortraitSprite;
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

        internal static void GiveObjectRelatedCheck(GameObject instance)
        {
            string objectPath = instance.transform.GetPath();
            string key = $"{SceneLoader.ActiveSceneName}/{objectPath}";
            if (gbcObjectCheckPair.TryGetValue(key, out APCheck check) && !ArchipelagoManager.HasCompletedCheck(check))
            {
                GiveGBCCheck(check);
            }
        }

        internal static void GiveGBCCheck(APCheck check)
        {
            CustomCoroutine.Instance.StartCoroutine(GiveGBCCheckSequence(check));
        }

        internal static IEnumerator GiveGBCCheckSequence(APCheck check)
        {
            CardInfo card = GenerateCardInfo(check);
            if (!ArchipelagoManager.HasCompletedCheck(check))
            {
                Singleton<PlayerMovementController>.Instance.SetEnabled(false);
                yield return SingleCardGainUI.instance.GainCard(card, true);
                Singleton<PlayerMovementController>.Instance.SetEnabled(true);
                Singleton<ArchipelagoUI>.Instance.QueueSave();
            }
            else
            {
                yield return null;
            }
        }

        internal static void GiveGBCCheckWithMessage(APCheck check, string message)
        {
            CustomCoroutine.instance.StartCoroutine(GiveGBCCheckWithMessageSequence(check, message));
        }

        internal static IEnumerator GiveGBCCheckWithMessageSequence(APCheck check, string message)
        {
            Singleton<PlayerMovementController>.Instance.SetEnabled(false);
            yield return Singleton<TextBox>.Instance.ShowUntilInput(message, TextBox.Style.Nature);
            yield return new WaitForSeconds(0.25f);
            yield return GiveGBCCheckSequence(check);
            if (!Singleton<PlayerMovementController>.Instance.enabled)
                Singleton<PlayerMovementController>.Instance.SetEnabled(true);
        }

        internal static string GetCardGainedMessage(CardInfo info)
        {
            if (info.name.Contains("Archipelago"))
                return "The card was sent to its rightful owner.";
            else
                return "The card was added to your collection.";
        }

        internal static IEnumerator OnPackButtonPressed()
        {
            PauseMenu.instance.SetPaused(false);
            PauseMenu.pausingDisabled = true;

            if (Singleton<PlayerMovementController>.Instance != null)
                Singleton<PlayerMovementController>.Instance.SetEnabled(false);

            yield return new WaitForSeconds(0.25f);

            bool result = false;
            TextBox.Prompt prompt = new TextBox.Prompt("Open a pack", "Cancel", option => result = (option == 0));
            yield return Singleton<TextBox>.Instance.ShowUntilInput($"You have {ArchipelagoData.Data.availableCardPacks} card pack{(ArchipelagoData.Data.availableCardPacks > 1 ? "s" : "")} available.", TextBox.Style.Neutral, null, TextBox.ScreenPosition.ForceTop, 0, true, false, prompt);
            if (result)
            {
                ArchipelagoData.Data.availableCardPacks--;
                yield return PackOpeningUI.instance.OpenPack((CardTemple)UnityEngine.Random.Range(0, (int)CardTemple.NUM_TEMPLES));
                SaveManager.SaveToFile();
            }

            yield return new WaitForSeconds(0.05f);

            UpdatePackButtonEnabled();

            if (Singleton<PlayerMovementController>.Instance != null)
                Singleton<PlayerMovementController>.Instance.SetEnabled(true);

            PauseMenu.instance.SetPaused(true);
            PauseMenu.instance.menuController.PlayMenuCardImmediate((PauseMenu.instance as GBCPauseMenu).modifyDeckCard);
            PauseMenu.pausingDisabled = false;
        }

        internal static void UpdatePackButtonEnabled()
        {
            if (packButton == null) return;

            packButton.SetEnabled(ArchipelagoData.Data.availableCardPacks > 0 && SceneLoader.ActiveSceneName != "GBC_WorldMap");
        }

        internal static void SpawnPackPile(DeckReviewSequencer instance)
        {
            packPile = new GameObject("PackPile");
            packPile.transform.SetParent(instance.transform);
            packPile.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            packPile.transform.localEulerAngles = new Vector3(0, 90, 0);
            packPile.AddComponent<BoxCollider>().size = new Vector3(1.2f, 0.1f, 2.2f);

            for (int i = 0; i < ArchipelagoData.Data.availableCardPacks; i++)
            {
                GameObject pack = GameObject.Instantiate(AssetsManager.cardPackPrefab, packPile.transform);
                pack.transform.localPosition = new Vector3(-10, 0.1f * i, 0);
                Tween.LocalPosition(pack.transform, new Vector3(0, 0.1f * i, 0), 0.20f, 0.02f * i, Tween.EaseOut);
                packs.Add(pack);
            }

            CardPackPile pileScript = packPile.AddComponent<CardPackPile>();
            pileScript.topPackBasePosition = new Vector3(0, 0.1f * (ArchipelagoData.Data.availableCardPacks - 1), 0);
            pileScript.pileTop = packs.Last();
        }

        internal static void DestroyPackPile()
        {
            if (packPile == null) return;
            packPile.GetComponent<CardPackPile>().enabled = false;
            int i = 0;
            int packsCount = packs.Count;
            foreach (GameObject pack in packs)
            {
                Tween.LocalPosition(pack.transform, new Vector3(-10, 0.1f * i, 0), 0.20f, 0.02f * (packsCount - i - 1), Tween.EaseIn, Tween.LoopType.None, null, () => GameObject.Destroy(pack));

                i++;
            }
            packs.Clear();
            GameObject.Destroy(packPile, 1);
            packPile = null;
        }

        internal static IEnumerator PrePlayerDeathSequence(Part1GameFlowManager manager)
        {
            if (!DeathLinkManager.receivedDeath)
                DeathLinkManager.SendDeathLink();
            if ((!DeathLinkManager.receivedDeath && ArchipelagoOptions.optionalDeathCard == OptionalDeathCard.EnableOnlyOnDeathLink)
                || ArchipelagoOptions.optionalDeathCard == OptionalDeathCard.Disable)
            {
                Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, true);
                yield return manager.KillPlayerSequence();
                yield break;
            }
            if (Singleton<GameMap>.Instance.FullyUnrolled)
                Singleton<GameMap>.Instance.HideMapImmediate();
            yield return Singleton<TextDisplayer>.Instance.ShowUntilInput("Choose if you want to create a new deathcard");
            CardChoicesNodeData choice = new CardChoicesNodeData();
            choice.gemifyChoices = true;
            CardChoice c1 = new CardChoice();
            c1.CardInfo = GenerateCardInfoWithName("Yes", "You will restart the game normally");
            CardChoice c2 = new CardChoice();
            c2.CardInfo = GenerateCardInfoWithName("No", "You will restart without creating a new death card");
            choice.overrideChoices = new List<CardChoice> {c1, c2};
            Singleton<ViewManager>.Instance.SwitchToView(View.BoardCentered, false, true);
            yield return Singleton<CardSingleChoicesSequencer>.Instance.CardSelectionSequence(choice);
            Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, true);
            if (Singleton<CardSingleChoicesSequencer>.Instance.chosenReward.Info.name == "Archipelago_Yes")
                doDeathCard = true;
            else
                doDeathCard = false;
            yield return manager.KillPlayerSequence();
        }

        internal static IEnumerator LeshySaysMessage(string message)
        {
            yield return Singleton<TextDisplayer>.Instance.ShowMessage(message);
        }

        internal static void AfterPlayerDeathSequence()
        {
            if (doDeathCard)
                SceneLoader.Load("Part1_Sanctum");
            else
            {
                SaveManager.SaveFile.NewPart1Run();
                SceneLoader.Load("Part1_Cabin");
            }
        }

        internal static IEnumerator TraderPeltRewardCheckSequence(TraderMaskInteractable instance)
        {
            APCheck check = APCheck.FactoryTrader1 + Part3SaveData.Data.collectedTarots.Count;
            if (ArchipelagoManager.HasCompletedCheck(check))
            {
                Part3SaveData.Data.collectedTarots.Add(instance.GetAvailableTarotTypes().First());
                yield break; 
            }

            Singleton<InteractionCursor>.Instance.InteractionDisabled = false;
            Singleton<ViewManager>.Instance.OffsetPosition(new Vector3(0f, -2f, 8f), 0.25f);
            Singleton<ViewManager>.Instance.OffsetRotation(new Vector3(50f, 0f, 0f), 0.25f);

            yield return new WaitForSeconds(0.1f);

            GameObject reference = new GameObject("Ref");
            reference.transform.SetParent(instance.cardsParent);
            reference.transform.ResetTransform();
            reference.transform.localScale = Vector3.one * 0.5f;
            reference.AddComponent<BoxCollider>().size = new Vector3(1.2f, 1.8f, 0.4f);

            DiscoverableCheckInteractable checkCard = CreateDiscoverableCardCheck(reference, check, true);
            checkCard.closeUpDistance *= 1.5f;
            Vector3 targetPos = checkCard.transform.position;
            checkCard.transform.position += new Vector3(0, 3, 1);
            Tween.Position(checkCard.transform, targetPos, 0.25f, 0f, Tween.EaseIn);
            checkCard.SetEnabled(false);

            yield return new WaitForSeconds(0.25f);

            checkCard.SetEnabled(true);

            yield return new WaitUntil(() => checkCard.Discovering);
            yield return new WaitUntil(() => !checkCard.Discovering);

            Part3SaveData.Data.collectedTarots.Add(instance.GetAvailableTarotTypes().First());

            yield return new WaitForSeconds(0.75f);

            Singleton<ViewManager>.Instance.OffsetPosition(Vector3.zero, 0.25f);
            Singleton<ViewManager>.Instance.OffsetRotation(new Vector3(10f, 0f, 0f), 0.25f);
        }

        internal static IEnumerator BlowOutOneOrAllCandles(bool fromBoss)
        {
            if (DeathLinkManager.receivedDeath)
            {
                if (Singleton<GameMap>.Instance.FullyUnrolled)
                    Singleton<GameMap>.Instance.HideMapImmediate();
                while (RunState.Run.playerLives > 0)
                {
                    yield return Singleton<CandleHolder>.Instance.BlowOutCandleSequence(fromBoss);
                }
            }
            else
            {
                yield return Singleton<CandleHolder>.Instance.BlowOutCandleSequence(fromBoss);
            }
        }

        internal static List<CardInfo> GetAllDeathCards()
        {
            List<CardInfo> list = new List<CardInfo>();
            List<CardModificationInfo> choosableDeathcardMods = SaveManager.SaveFile.GetChoosableDeathcardMods();
            if (choosableDeathcardMods.Count > 0)
            {
                foreach (CardModificationInfo deathcardMod in choosableDeathcardMods)
                    list.Add(CardLoader.CreateDeathCard(deathcardMod));
            }
            else
            {
                CardModificationInfo cardModificationInfo2 = new CardModificationInfo();
                cardModificationInfo2.nameReplacement = "Luke Carder";
                cardModificationInfo2.deathCardInfo = new DeathCardInfo(CompositeFigurine.FigurineType.Gravedigger, 0, 0);
                cardModificationInfo2.attackAdjustment = 4;
                cardModificationInfo2.healthAdjustment = 4;
                list.Add(CardLoader.CreateDeathCard(cardModificationInfo2));
            }
            return list;
        }

        internal static CardInfo RandomRareCardInAct1(int seed)
        {
            List<CardInfo> cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll((CardInfo x) => x.metaCategories.Contains(CardMetaCategory.Rare)
            && x.temple == CardTemple.Nature && x.portraitTex != null && !x.metaCategories.Contains(CardMetaCategory.AscensionUnlock) && ConceptProgressionTree.Tree.CardUnlocked(x, false)
            && (ArchipelagoManager.HasItem(APItem.GreatKrakenCard) || x.name != "Kraken"));
            return CardLoader.GetDistinctCardsFromPool(seed++, 1, cardsInfoRandomPool).First();
        }

        internal static void OnlyPutOneTalkingCardInDeckAct1(List<string> newCardsIds, ref int seed, ref CardInfo card, List<CardInfo> cardsInfoRandomPool)
        {
            if (newCardsIds.Contains("Stoat_Talking"))
            {
                while (card.name == "Stoat_Talking")
                    card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
            }
            if (newCardsIds.Contains("Stinkbug_Talking"))
            {
                while (card.name == "Stinkbug_Talking")
                    card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
            }
            if (newCardsIds.Contains("Wolf_Talking"))
            {
                while (card.name == "Wolf_Talking")
                    card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
            }
            if (newCardsIds.Contains("CagedWolf"))
            {
                while (card.name == "CagedWolf")
                    card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
            }
        }

        internal static void UpdateItemsWhenDoneDiscovering(DiscoverableCheckInteractable discoveringCard)
        {
            CustomCoroutine.Instance.StartCoroutine(UpdateItemsWhenDoneDiscoveringSequence(discoveringCard));
        }

        private static IEnumerator UpdateItemsWhenDoneDiscoveringSequence(DiscoverableCheckInteractable card)
        {
            yield return new WaitUntil(() => !card.Discovering);
            Singleton<ItemsManager>.Instance.UpdateItems();
        }

        internal static void OldDataOpened()
        {
            ArchipelagoData.Data.epilogueCompleted = true;
            ArchipelagoManager.VerifyGoalCompletion();
        }
    }
}
