using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace Archipelago_Inscryption.Patches
{
    [HarmonyPatch]
    internal class DeckRandomizerPatches
    {
        [HarmonyPatch(typeof(MapNode), "OnArriveAtNode")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct1(MapNode __instance)
        {
            if ((ArchipelagoOptions.randomizeDeck == RandomizeDeck.Disable || ArchipelagoOptions.randomizeDeck == RandomizeDeck.StarterOnly) && ArchipelagoOptions.randomizeSigils == RandomizeSigils.Disable)
                return true;

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType || ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPool = RandomizerHelper.GenerateCardPoolAct1();
                List<string> newCardsIds = new List<string>();

                foreach (CardInfo c in RunState.Run.playerDeck.Cards)
                {
                    CardInfo card = c;
                    if (c.name == "CagedWolf")
                    {
                        newCardsIds.Add(c.name);
                        newCards.Add(c);
                        continue;
                    }
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.HasTrait(Trait.Pelt))
                        {
                            newCardsIds.Add(c.name);
                            newCards.Add(c);
                            continue;
                        }
                        else if (c.metaCategories.Contains(CardMetaCategory.Rare))
                        {
                            card = RandomizerHelper.RandomRareCardInAct1(seed++);
                        }
                        else
                        {
                            card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];

                            RandomizerHelper.RemoveUniqueAct1CardIfApplicable(ref cardsInfoRandomPool, ref card);
                        }
                    }
                    else
                    {
                        card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];

                        RandomizerHelper.RemoveUniqueAct1CardIfApplicable(ref cardsInfoRandomPool, ref card);
                    }

                    if (!card.mods.Any(x => x.deathCardInfo != null))
                        card = (CardInfo)card.Clone();

                    foreach (Ability ability in c.Abilities)
                    {
                        if (!ProgressionData.LearnedAbility(ability))
                        {
                            ProgressionData.SetAbilityLearned(ability);
                        }
                    }

                    foreach (CardModificationInfo mod in c.Mods)
                    {
                        if (mod.deathCardInfo != null)
                        {
                            continue;
                        }

                        card.mods.Add(mod);
                    }

                    card.decals = c.decals;
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }

                RunState.Run.playerDeck.CardInfos = newCards;
                RunState.Run.playerDeck.cardIds = newCardsIds;
            }

            if (ArchipelagoOptions.randomizeSigils != RandomizeSigils.Disable)
            {
                foreach (CardInfo c in RunState.Run.playerDeck.Cards)
                {
                    List<AbilityInfo> learnedAbilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(
                        x => x.metaCategories.Contains(AbilityMetaCategory.Part1Modular)
                        && x.metaCategories.Contains(AbilityMetaCategory.Part1Rulebook)
                        && x.ability != Ability.RandomAbility
                        && x.ability != Ability.CreateEgg
                        && x.ability != Ability.HydraEgg);

                    foreach (CardModificationInfo mod in c.Mods)
                    {
                        if (mod.deathCardInfo != null)
                        {
                            continue;
                        }
                        if (mod.fromCardMerge)
                        {
                            if (mod.abilities.Count > 0)
                            {
                                int abilityCount = mod.abilities.Count;
                                mod.abilities = new List<Ability>();
                                for (int l = 0; l < abilityCount; l++)
                                {
                                    learnedAbilities.RemoveAll(x => c.HasAbility(x.ability));
                                    if (learnedAbilities.Count > 0)
                                    {
                                        mod.abilities.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                                    }
                                }
                            }
                        }
                    }

                    if (ArchipelagoOptions.randomizeSigils == RandomizeSigils.RandomizeAll)
                    {
                        CardModificationInfo deathCardMod = c.Mods.FirstOrDefault(m => m.deathCardInfo != null);
                        bool isDeathCard = deathCardMod != null;

                        int abilityCount = isDeathCard ? deathCardMod.abilities.Count : c.abilities.Count;

                        if (isDeathCard)
                            deathCardMod.abilities = new List<Ability>();
                        else
                            c.abilities = new List<Ability>();

                        for (int t = 0; t < abilityCount; t++)
                        {
                            learnedAbilities.RemoveAll(x => c.HasAbility(x.ability));
                            if (learnedAbilities.Count > 0)
                            {
                                if (isDeathCard)
                                    deathCardMod.abilities.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                                else
                                    c.abilities.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                            }
                        }
                    }
                }
            }

            RunState.Run.playerDeck.UpdateModDictionary();

            return true;
        }

        [HarmonyPatch(typeof(GBCEncounterManager), "StartEncounter")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct2()
        {
            if ((ArchipelagoOptions.randomizeDeck == RandomizeDeck.Disable || ArchipelagoOptions.randomizeDeck == RandomizeDeck.StarterOnly) && ArchipelagoOptions.randomizeSigils == RandomizeSigils.Disable)
                return true;

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType || ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPoolAll = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolNature = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolNatureRare = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolUndead = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolUndeadRare = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolTech = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolTechRare = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolWizard = new List<CardInfo>();
                List<CardInfo> cardsInfoRandomPoolWizardRare = new List<CardInfo>();
                int cardAdded = 0;
                cardsInfoRandomPoolAll = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.metaCategories.Contains(CardMetaCategory.GBCPlayable) && x.pixelPortrait != null);

                if (!ArchipelagoManager.HasItem(APItem.GreatKrakenCard))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "Kraken");
                }
                if (!ArchipelagoManager.HasItem(APItem.BoneLordHorn))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "BonelordHorn");
                }
                if (!ArchipelagoManager.HasItem(APItem.DrownedSoulCard))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "DrownedSoul");
                }
                if (!ArchipelagoManager.HasItem(APItem.SalmonCard))
                {
                    cardsInfoRandomPoolAll.RemoveAll(c => c.name == "Salmon");
                }

                if (!ArchipelagoManager.HasCompletedCheck(APCheck.GBCAncientObol))
                {
                    CardInfo obolLeft = CardLoader.GetCardByName("CoinLeft");
                    newCards.Add(obolLeft);
                    newCardsIds.Add(obolLeft.name);
                    CardInfo obolRight = CardLoader.GetCardByName("CoinRight");
                    newCards.Add(obolRight);
                    newCardsIds.Add(obolRight.name);
                    cardAdded += 2;
                }
                if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                {
                    cardsInfoRandomPoolNature = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Nature);
                    cardsInfoRandomPoolNatureRare = cardsInfoRandomPoolNature.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolNature = cardsInfoRandomPoolNature.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolUndead = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Undead);
                    cardsInfoRandomPoolUndeadRare = cardsInfoRandomPoolUndead.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolUndead = cardsInfoRandomPoolUndead.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolTech = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Tech);
                    cardsInfoRandomPoolTechRare = cardsInfoRandomPoolTech.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolTech = cardsInfoRandomPoolTech.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolWizard = cardsInfoRandomPoolAll.FindAll(x => x.temple == CardTemple.Wizard);
                    cardsInfoRandomPoolWizardRare = cardsInfoRandomPoolWizard.FindAll(x => x.metaCategories.Contains(CardMetaCategory.Rare));
                    cardsInfoRandomPoolWizard = cardsInfoRandomPoolWizard.FindAll(x => !x.metaCategories.Contains(CardMetaCategory.Rare));

                    cardsInfoRandomPoolNature.RemoveAll(x => x.name == "Squirrel");
                    cardsInfoRandomPoolUndead.RemoveAll(x => x.name == "Skeleton");
                    cardsInfoRandomPoolWizard.RemoveAll(x => x.name == "MoxSapphire" || x.name == "MoxRuby" || x.name == "MoxEmerald");
                }

                foreach (var c in SaveData.Data.deck.Cards)
                {
                    if (cardAdded > 0)
                    {
                        cardAdded--;
                        continue;
                    }
                    CardInfo card = c;
                    if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                    {
                        if (c.metaCategories.Contains(CardMetaCategory.Rare))
                        {
                            switch (c.temple)
                            {
                                case CardTemple.Nature:
                                    card = cardsInfoRandomPoolNatureRare[SeededRandom.Range(0, cardsInfoRandomPoolNatureRare.Count, seed++)];
                                    break;
                                case CardTemple.Undead:
                                    card = cardsInfoRandomPoolUndeadRare[SeededRandom.Range(0, cardsInfoRandomPoolUndeadRare.Count, seed++)];
                                    break;
                                case CardTemple.Tech:
                                    card = cardsInfoRandomPoolTechRare[SeededRandom.Range(0, cardsInfoRandomPoolTechRare.Count, seed++)];
                                    break;
                                case CardTemple.Wizard:
                                    card = cardsInfoRandomPoolWizardRare[SeededRandom.Range(0, cardsInfoRandomPoolWizardRare.Count, seed++)];
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (c.name != "Squirrel" && c.name != "Skeleton" && c.name != "MoxSapphire" && c.name != "MoxRuby" && c.name != "MoxEmerald")
                        {
                            switch (c.temple)
                            {
                                case CardTemple.Nature:
                                    card = cardsInfoRandomPoolNature[SeededRandom.Range(0, cardsInfoRandomPoolNature.Count, seed++)];
                                    break;
                                case CardTemple.Undead:
                                    card = cardsInfoRandomPoolUndead[SeededRandom.Range(0, cardsInfoRandomPoolUndead.Count, seed++)];
                                    break;
                                case CardTemple.Tech:
                                    card = cardsInfoRandomPoolTech[SeededRandom.Range(0, cardsInfoRandomPoolTech.Count, seed++)];
                                    break;
                                case CardTemple.Wizard:
                                    card = cardsInfoRandomPoolWizard[SeededRandom.Range(0, cardsInfoRandomPoolWizard.Count, seed++)];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        card = cardsInfoRandomPoolAll[SeededRandom.Range(0, cardsInfoRandomPoolAll.Count, seed++)];
                    }

                    card = (CardInfo)card.Clone();

                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                SaveData.Data.deck.CardInfos = newCards;
                SaveData.Data.deck.cardIds = newCardsIds;
            }

            if (ArchipelagoOptions.randomizeSigils == RandomizeSigils.RandomizeAll)
            {
                foreach (CardInfo card in SaveData.Data.deck.Cards)
                {
                    List<AbilityInfo> learnedAbilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(x => x.pixelIcon != null
                    && x.ability != Ability.ActivatedSacrificeDrawCards && x.ability != Ability.CreateEgg
                    && x.ability != Ability.HydraEgg && x.ability != Ability.Tutor);

                    int baseAbilityCount = card.abilities.Count;

                    card.abilities = new List<Ability>();

                    for (int t = 0; t < baseAbilityCount; t++)
                    {
                        learnedAbilities.RemoveAll(x => card.HasAbility(x.ability));
                        if (learnedAbilities.Count > 0)
                        {
                            AbilityInfo randomAbility = learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)];
                            card.abilities.Add(randomAbility.ability);

                            if (randomAbility.activated)
                                learnedAbilities.RemoveAll(x => x.activated);

                            if (randomAbility.conduit)
                                learnedAbilities.RemoveAll(x => x.conduit);
                        }
                    }
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(HoloMapNode), "OnSelected")]
        [HarmonyPrefix]
        static bool RandomizeDeckAct3()
        {
            if ((ArchipelagoOptions.randomizeDeck == RandomizeDeck.Disable || ArchipelagoOptions.randomizeDeck == RandomizeDeck.StarterOnly) && ArchipelagoOptions.randomizeSigils == RandomizeSigils.Disable)
                return true;

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType || ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeAll)
            {
                List<CardInfo> newCards = new List<CardInfo>();
                List<string> newCardsIds = new List<string>();
                List<CardInfo> cardsInfoRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.temple == CardTemple.Tech && x.portraitTex != null
                                                     && x.name != "!BOUNTYHUNTER_BASE" && x.name != "Librarian" && !x.name.Contains("EmptyVessel")
                                                     && x.name != "!MYCOCARD_BASE" && x.name != "CaptiveFile" && x.name != "!BUILDACARD_BASE");
                cardsInfoRandomPool.AddRange(RandomizerHelper.GetAllCustomCards());
                List<CardInfo> cardsInfoRandomGemPool = cardsInfoRandomPool;
                List<CardInfo> cardsInfoRandomConduitPool = cardsInfoRandomPool;
                if (ArchipelagoOptions.randomizeDeck == RandomizeDeck.RandomizeType)
                {
                    cardsInfoRandomConduitPool = cardsInfoRandomPool.FindAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell"));
                    cardsInfoRandomGemPool = cardsInfoRandomPool.FindAll(x => x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                    cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell") || x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                }
                else
                {
                    if (!StoryEventsData.EventCompleted(StoryEvent.GemsModuleFetched))
                        cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Sentinel") || x.name.Contains("Gem"));
                    if (!Part3SaveData.Data.sideDeckAbilities.Contains(Ability.ConduitNull))
                        cardsInfoRandomPool.RemoveAll(x => x.name.Contains("Conduit") || x.name.Contains("Cell"));
                }
                if (ArchipelagoManager.HasItem(APItem.LonelyWizbotCard))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("BlueMage_Talking"));
                if (ArchipelagoManager.HasItem(APItem.FishbotCard))
                    cardsInfoRandomPool.Add(CardLoader.GetCardByName("Angler_Talking"));
                if (!ArchipelagoManager.HasItem(APItem.Ourobot))
                    cardsInfoRandomPool.RemoveAll(x => x.name == "Ouroboros_Part3");
                foreach (CardInfo c in Part3SaveData.Data.deck.Cards)
                {
                    CardInfo card = c;
                    if (card.name == "!MYCOCARD_BASE" && card.mods.Count > 0)
                    {
                        card.mods.Remove(card.mods.First());
                    }

                    int abilityCount = 0;
                    do
                    {
                        card = RandomizerHelper.RandomizeOneCardAct3(ref seed, ref cardsInfoRandomPool, ref cardsInfoRandomGemPool, ref cardsInfoRandomConduitPool, c);
                        if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                            abilityCount = card.mods[0].abilities.Count;
                        else
                            abilityCount = card.abilities.Count;
                        foreach (var modCurrent in c.Mods)
                        {
                            if (modCurrent.buildACardPortraitInfo != null)
                                continue;
                            if (modCurrent.abilities.Count > 0)
                            {
                                foreach (var ability in modCurrent.abilities)
                                    abilityCount++;
                            }
                        }
                    } while (abilityCount > 4);

                    foreach (var modCurrent in c.Mods)
                    {
                        if (modCurrent.buildACardPortraitInfo != null)
                        {
                            continue;
                        }

                        card.mods.Add(modCurrent);
                    }

                    card.decals = c.decals;
                    newCardsIds.Add(card.name);
                    newCards.Add(card);
                }
                Part3SaveData.Data.deck.CardInfos = newCards;
                Part3SaveData.Data.deck.cardIds = newCardsIds;
            }

            if (ArchipelagoOptions.randomizeSigils != RandomizeSigils.Disable)
            {
                foreach (CardInfo card in Part3SaveData.Data.deck.Cards)
                {
                    List<AbilityInfo> learnedAbilities = ScriptableObjectLoader<AbilityInfo>.allData.FindAll(x => x.metaCategories.Contains(AbilityMetaCategory.Part3Modular));
                    foreach (var modCurrent in card.Mods)
                    {
                        if (modCurrent.buildACardPortraitInfo != null)
                        {
                            continue;
                        }

                        if (ArchipelagoOptions.randomizeSigils != RandomizeSigils.Disable)
                        {
                            if (modCurrent.abilities.Count > 0)
                            {
                                int moddedAbilityCount = modCurrent.abilities.Count;
                                if (modCurrent.abilities.Contains(Ability.PermaDeath))
                                    modCurrent.attackAdjustment--;
                                modCurrent.abilities = new List<Ability>();
                                for (int l = 0; l < moddedAbilityCount; l++)
                                {
                                    learnedAbilities.RemoveAll(x => card.HasAbility(x.ability));
                                    if (learnedAbilities.Count > 0)
                                    {
                                        Ability ab = learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability;
                                        modCurrent.abilities.Add(ab);

                                        if (ab == Ability.PermaDeath)
                                            modCurrent.attackAdjustment++;
                                    }
                                }
                            }
                        }
                    }

                    if (ArchipelagoOptions.randomizeSigils == RandomizeSigils.RandomizeAll)
                    {
                        List<Ability> abilityList = (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE") ? card.mods.First().abilities : card.abilities;
                        int baseAbilityCount = abilityList.Count;

                        if (abilityList.Contains(Ability.PermaDeath))
                        {
                            if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                                card.mods.First().attackAdjustment--;
                            else
                                card.baseAttack--;
                        }

                        if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                            card.mods.First().abilities = new List<Ability>();
                        else
                            card.abilities = new List<Ability>();

                        abilityList = (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE") ? card.mods.First().abilities : card.abilities;

                        for (int t = 0; t < baseAbilityCount; t++)
                        {
                            learnedAbilities.RemoveAll(x => card.HasAbility(x.ability));
                            if (learnedAbilities.Count > 0)
                            {
                                abilityList.Add(learnedAbilities[SeededRandom.Range(0, learnedAbilities.Count, seed++)].ability);
                            }
                        }

                        if (abilityList.Contains(Ability.PermaDeath))
                        {
                            if (card.name == "!BUILDACARD_BASE" || card.name == "!MYCOCARD_BASE")
                                card.mods.First().attackAdjustment++;
                            else
                                card.baseAttack++;
                        }
                    }
                }
            }

            Part3SaveData.Data.deck.UpdateModDictionary();

            return true;
        }

        [HarmonyPatch(typeof(DeckInfo), "InitializeAsPlayerDeck")]
        [HarmonyPrefix]
        static bool RandomizeStarterDeckAct1(DeckInfo __instance)
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.StarterOnly)
                return true;

            int nbCardsToAdd = 4;

            if (StoryEventsData.EventCompleted(StoryEvent.CageCardDiscovered) && !StoryEventsData.EventCompleted(StoryEvent.WolfCageBroken))
            {
                __instance.AddCard(CardLoader.GetCardByName("CagedWolf"));
                nbCardsToAdd--;
            }

            List<CardInfo> cardsInfoRandomPool = RandomizerHelper.GenerateCardPoolAct1();

            int seed = SaveManager.SaveFile.GetCurrentRandomSeed();

            for (int i = 0; i < nbCardsToAdd; i++)
            {
                CardInfo card = cardsInfoRandomPool[SeededRandom.Range(0, cardsInfoRandomPool.Count, seed++)];
                cardsInfoRandomPool.Remove(card);
                __instance.AddCard(card.Mods.Any(x => x.deathCardInfo != null) ? card : CardLoader.Clone(card));
            }

            return false;
        }

        [HarmonyPatch(typeof(StarterDecks), "GetDeck")]
        [HarmonyPrefix]
        static bool RandomizeStarterDeckAct2(CardTemple temple, ref List<string> __result)
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.StarterOnly)
                return true;

            __result = new List<string>();
            List<CardInfo> cardsRandomPool = ScriptableObjectLoader<CardInfo>.AllData.FindAll(x => x.metaCategories.Contains(CardMetaCategory.GBCPlayable)
                                         && ConceptProgressionTree.Tree.CardUnlocked(x, false) && x.pixelPortrait != null && !x.metaCategories.Contains(CardMetaCategory.Rare));

            cardsRandomPool.RemoveAll(c => c.name == "Kraken");
            cardsRandomPool.RemoveAll(c => c.name == "BonelordHorn");
            cardsRandomPool.RemoveAll(c => c.name == "DrownedSoul");
            cardsRandomPool.RemoveAll(c => c.name == "Salmon");

            switch (temple)
            {
                case CardTemple.Nature:
                    for (int i = 0; i < 7; i++)
                    {
                        __result.Add("Squirrel");
                    }
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Nature && x.name != "Squirrel");
                    break;
                case CardTemple.Undead:
                    for (int i = 0; i < 7; i++)
                    {
                        __result.Add("Skeleton");
                    }
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Undead && x.name != "Skeleton");
                    break;
                case CardTemple.Tech:
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Tech);
                    break;
                case CardTemple.Wizard:
                    for (int i = 0; i < 3; i++)
                    {
                        __result.Add("MoxSapphire");
                        __result.Add("MoxRuby");
                        __result.Add("MoxEmerald");
                    }
                    cardsRandomPool = cardsRandomPool.FindAll(x => x.temple == CardTemple.Wizard && x.name != "MoxSapphire" && x.name != "MoxRuby" && x.name != "MoxEmerald");
                    break;
                default:
                    __result = StarterDecks.NATURE_STARTER;
                    break;
            }

            int seed = SaveManager.saveFile.GetCurrentRandomSeed();

            while (__result.Count < 20)
            {
                __result.Add(cardsRandomPool[SeededRandom.Range(0, cardsRandomPool.Count, seed++)].name);
            }

            return false;
        }

        [HarmonyPatch(typeof(Part3GameFlowManager), "SceneSpecificInitialization")]
        [HarmonyPrefix]
        static bool RandomizeStarterDeckAct3()
        {
            if (ArchipelagoOptions.randomizeDeck != RandomizeDeck.StarterOnly || StoryEventsData.EventCompleted(StoryEvent.Part3Intro))
                return true;

            Part3SaveData.Data.deck.RemoveCardByName("BatteryBot");
            Part3SaveData.Data.deck.RemoveCardByName("Shieldbot");
            Part3SaveData.Data.deck.RemoveCardByName("Sniper");
            Part3SaveData.Data.deck.RemoveCardByName("CloserBot");

            List<CardInfo> randomCards = new List<CardInfo>();
            int randomSeed = SaveManager.SaveFile.GetCurrentRandomSeed();
            for (int i = 0; i < 4; i++)
            {
                CardInfo card = CardLoader.GetRandomChoosableCard(randomSeed++, CardTemple.Tech);
                while (randomCards.Exists(x => x.name == card.name))
                {
                    card = CardLoader.GetRandomChoosableCard(randomSeed++, CardTemple.Tech);
                }

                randomCards.Add(card);
            }

            foreach (CardInfo card in randomCards)
            {
                Part3SaveData.Data.deck.AddCard(card);
            }

            return true;
        }
    }
}
