using Archipelago_Inscryption.Assets;
using DiskCardGame;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archipelago_Inscryption.Archipelago
{
    internal class ArchipelagoOptions
    {
        internal static bool deathlink = false;
        internal static Act1DeathLink act1DeathLinkBehaviour = Act1DeathLink.Sacrificed;
        internal static OptionalDeathCard optionalDeathCard = OptionalDeathCard.Disable;
        internal static bool randomizeCodes = false;
        internal static RandomizeDeck randomizeDeck = RandomizeDeck.Disable;
        internal static RandomizeAbilities randomizeAbilities = RandomizeAbilities.Disable;
        internal static Goal goal;
        internal static bool skipTutorial = false;
        internal static EpitaphPiecesRandomization epitaphPiecesRandomization = EpitaphPiecesRandomization.AllPieces;

        private static Sprite[] wizardClues;
        private static GameObject[] floor1Clues;
        private static GameObject[] floor2Clues;
        private static GameObject[] floor3Clues;


        internal static void SetupRandomizedCodes()
        {
            Sprite[] wizardSprites = Resources.LoadAll<Sprite>("art/gbc/temples/wizard/wizard_temple_objects");

            wizardClues = new Sprite[]
            {
                wizardSprites.First(x => x.name == "wizard_temple_objects_23"),
                wizardSprites.First(x => x.name == "wizard_temple_objects_22"),
                wizardSprites.First(x => x.name == "wizard_temple_objects_21"),
                AssetsManager.wizardTowerSubmergeClueSprite,
                wizardSprites.First(x => x.name == "wizard_temple_objects_51"),
                AssetsManager.wizardTowerLensClueSprite,
                wizardSprites.First(x => x.name == "wizard_temple_objects_55")
            };

            floor1Clues = new GameObject[]
            {
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F1_1"),
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F1_2"),
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F1_3")
            };

            for (int i = 0; i < floor1Clues.Length; i++)
            {
                floor1Clues[i].transform.Find("icon").GetComponent<SpriteRenderer>().sprite = wizardClues[ArchipelagoData.Data.wizardCode1[i]];
            }

            floor2Clues = new GameObject[]
            {
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F2_1"),
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F2_2"),
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F2_3")
            };

            for (int i = 0; i < floor2Clues.Length; i++)
            {
                floor2Clues[i].transform.Find("icon").GetComponent<SpriteRenderer>().sprite = wizardClues[ArchipelagoData.Data.wizardCode2[i]];
            }

            floor3Clues = new GameObject[]
            {
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F3_1"),
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F3_2"),
                Resources.Load<GameObject>("prefabs/gbcui/textadditive/WizardMarking_F3_3")
            };

            for (int i = 0; i < floor3Clues.Length; i++)
            {
                floor3Clues[i].transform.Find("icon").GetComponent<SpriteRenderer>().sprite = wizardClues[ArchipelagoData.Data.wizardCode3[i]];
            }
        }

        internal static void SetClueSprite(SpriteRenderer renderer, int floorIndex, int codeIndex)
        {
            renderer.sprite = wizardClues[floorIndex == 0 ? ArchipelagoData.Data.wizardCode1[codeIndex] : (floorIndex == 1 ? ArchipelagoData.Data.wizardCode2[codeIndex] : ArchipelagoData.Data.wizardCode3[codeIndex])];
        }

        internal static void RandomizeCodes(int seed)
        {
            List<int> cabinSafeCode = new List<int>();
            do
            {
                int number = SeededRandom.Range(0, 9, seed++);
                if (!cabinSafeCode.Contains(number))
                    cabinSafeCode.Add(number);
            } while (cabinSafeCode.Count < 3);
            ArchipelagoData.Data.cabinSafeCode = cabinSafeCode;

            List<int> cabinClockCode = new List<int>();
            do
            {
                int number = SeededRandom.Range(0, 11, seed++);
                if (!cabinClockCode.Contains(number))
                    cabinClockCode.Add(number);
            } while (cabinClockCode.Count < 3);
            ArchipelagoData.Data.cabinClockCode = cabinClockCode;

            ArchipelagoData.Data.cabinSmallClockCode = new List<int> { 0, 0, SeededRandom.Range(0, 11, seed++) };

            ArchipelagoData.Data.factoryClockCode = new List<int> { 0, 0, SeededRandom.Range(0, 11, seed++) };

            ArchipelagoData.Data.wizardCode1 = new List<int> { SeededRandom.Range(0, 6, seed++), SeededRandom.Range(0, 6, seed++), SeededRandom.Range(0, 6, seed++) };
            ArchipelagoData.Data.wizardCode2 = new List<int> { SeededRandom.Range(0, 6, seed++), SeededRandom.Range(0, 6, seed++), SeededRandom.Range(0, 6, seed++) };
            ArchipelagoData.Data.wizardCode3 = new List<int> { SeededRandom.Range(0, 6, seed++), SeededRandom.Range(0, 6, seed++), SeededRandom.Range(0, 6, seed++) };
        }

        internal static void SkipTutorial()
        {
            StoryEventsData.SetEventCompleted(StoryEvent.BasicTutorialCompleted, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.TutorialRunCompleted, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.TutorialRun2Completed, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.TutorialRun3Completed, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.BonesTutorialCompleted, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.StoatIntroduction, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.StoatIntroduction2, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.StoatIntroduction3, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.StoatSaysFindWolf, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.FigurineFetched, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.LeshyLostCamera, false, false);
            StoryEventsData.SetEventCompleted(StoryEvent.WoodcarverMet, false, false);

            ProgressionData.SetMechanicLearned(MechanicsConcept.SacrificingNotPermanent);
            ProgressionData.SetMechanicLearned(MechanicsConcept.EndingTurn);
            ProgressionData.SetMechanicLearned(MechanicsConcept.LosingLife);
            ProgressionData.SetMechanicLearned(MechanicsConcept.OpponentQueue);
            ProgressionData.SetMechanicLearned(MechanicsConcept.CardChoice);
            ProgressionData.SetMechanicLearned(MechanicsConcept.IntermediateCards);
            ProgressionData.SetMechanicLearned(MechanicsConcept.FirstPersonNavigation);
            ProgressionData.SetMechanicLearned(MechanicsConcept.DeathCardCreation);
            ProgressionData.SetMechanicLearned(MechanicsConcept.GainConsumables);
            ProgressionData.SetMechanicLearned(MechanicsConcept.CardMerging);
            ProgressionData.SetMechanicLearned(MechanicsConcept.DeathCardSelection);
            ProgressionData.SetMechanicLearned(MechanicsConcept.Bones);
            ProgressionData.SetMechanicLearned(MechanicsConcept.CostBasedCardChoice);
            ProgressionData.SetMechanicLearned(MechanicsConcept.AdvancedCards);
            ProgressionData.SetMechanicLearned(MechanicsConcept.OpponentTotems);
            ProgressionData.SetMechanicLearned(MechanicsConcept.BuyingPelts);
            ProgressionData.SetMechanicLearned(MechanicsConcept.TradingPelts);
            ProgressionData.SetMechanicLearned(MechanicsConcept.OverkillDamage);
            ProgressionData.SetMechanicLearned(MechanicsConcept.BuildingTotems);
            ProgressionData.SetMechanicLearned(MechanicsConcept.TribeBasedCardChoice);
            ProgressionData.SetMechanicLearned(MechanicsConcept.Rulebook);
            ProgressionData.SetMechanicLearned(MechanicsConcept.RulebookPageFlipping);
            ProgressionData.SetMechanicLearned(MechanicsConcept.AltInput);
            ProgressionData.SetMechanicLearned(MechanicsConcept.ViewHand);
            ProgressionData.SetMechanicLearned(MechanicsConcept.ViewQueue);
            ProgressionData.SetMechanicLearned(MechanicsConcept.BossSuddenDeath);
            ProgressionData.SetMechanicLearned(MechanicsConcept.GetUpFromTableAnyTime);

            ProgressionData.SetAbilityLearned(Ability.Evolve);
            ProgressionData.SetAbilityLearned(Ability.Flying);
            ProgressionData.SetAbilityLearned(Ability.Deathtouch);

            ProgressionData.SetConsumableIntroduced("SquirrelBottle");
            ProgressionData.SetConsumableIntroduced("Pliers");

            SaveManager.SaveFile.RefreshOilPaintingPuzzle();
            SaveManager.SaveFile.ResetPart1Run();
        }
    }
}
