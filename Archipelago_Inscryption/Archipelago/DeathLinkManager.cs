using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using DiskCardGame;
using GBC;
using System.Collections;
using UnityEngine;

namespace Archipelago_Inscryption.Archipelago
{
    internal static class DeathLinkManager
    {
        public static DeathLinkService DeathLinkService;
        internal static bool receivedDeath;

        internal static void Init()
        {
            ArchipelagoModPlugin.Log.LogInfo($"DeathLink is set to {ArchipelagoOptions.deathlink}");
            DeathLinkService.OnDeathLinkReceived += ReceiveDeathLink;
            if (ArchipelagoOptions.deathlink)
                DeathLinkService.EnableDeathLink();
            else
                DeathLinkService.DisableDeathLink();
        }

        static void ReceiveDeathLink(DeathLink deathLink)
        {
            if (receivedDeath == true)
                return;
            receivedDeath = true;
            string message = $"Received DeathLink from: {deathLink.Source} due to {deathLink.Cause}";
            ArchipelagoModPlugin.Log.LogMessage(message);
            Singleton<ArchipelagoUI>.Instance.LogMessage(message);
            Singleton<ArchipelagoUI>.Instance.StartCoroutine(ApplyDeathLink());
        }

        static IEnumerator ApplyDeathLink()
        {
            if (Singleton<TextDisplayer>.Instance != null && Singleton<TextDisplayer>.Instance.PlayingEvent)
                yield return new WaitUntil(() => !Singleton<TextDisplayer>.Instance.PlayingEvent);

            if (Singleton<MapNodeManager>.Instance != null && Singleton<MapNodeManager>.Instance.MovingNodes)
                yield return new WaitUntil(() => !Singleton<MapNodeManager>.Instance.MovingNodes);

            if (Singleton<InteractionCursor>.Instance != null && Singleton<InteractionCursor>.Instance.InteractionDisabled == true)
                yield return new WaitUntil(() => !Singleton<InteractionCursor>.Instance.InteractionDisabled);

            if (Singleton<FirstPersonController>.Instance != null && Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.FirstPerson3D)
                yield return Singleton<GameFlowManager>.Instance.DoTransitionSequence(GameState.Map, null);

            if (PauseMenu.instance && PauseMenu.instance.Paused)
                yield return new WaitUntil(() => !PauseMenu.instance.Paused);

            if (SaveManager.saveFile.IsPart1 && Singleton<GameFlowManager>.Instance != null && ProgressionData.LearnedMechanic(MechanicsConcept.LosingLife))
            {
                PauseMenu.pausingDisabled = true;

                RunState finishedRun = RunState.Run;

                if (Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.CardBattle)
                {
                    int prevLives = RunState.Run.playerLives;
                    yield return new WaitUntil(() => Singleton<TurnManager>.Instance.IsPlayerTurn || Singleton<TurnManager>.Instance.GameIsOver());
                    Singleton<TurnManager>.Instance.PlayerSurrendered = true;

                    if (ArchipelagoOptions.act1DeathLinkBehaviour == Act1DeathLink.Sacrificed)
                        yield return new WaitUntil(() => RunState.Run.playerLives == 0);
                    else
                        yield return new WaitUntil(() => RunState.Run.playerLives == prevLives - 1);
                }
                else
                {
                    if (ArchipelagoOptions.act1DeathLinkBehaviour == Act1DeathLink.Sacrificed)
                    {
                        while (RunState.Run.playerLives > 0)
                            yield return Singleton<CandleHolder>.Instance.BlowOutCandleSequence();
                        yield return RandomizerHelper.PrePlayerDeathSequence(Singleton<Part1GameFlowManager>.Instance);
                    }
                    else
                    {
                        yield return Singleton<CandleHolder>.Instance.BlowOutCandleSequence();
                    }
                }

                PauseMenu.pausingDisabled = false;

                if (ArchipelagoOptions.act1DeathLinkBehaviour == Act1DeathLink.Sacrificed)
                    yield return new WaitUntil(() => RunState.Run != finishedRun);
            }
            else if (SaveManager.saveFile.IsPart2)
            {
                if (SceneLoader.ActiveSceneName != "GBC_Starting_Island" && SceneLoader.ActiveSceneName != "GBC_WorldMap")
                {
                    if (GBCEncounterManager.Instance != null && GBCEncounterManager.Instance.EncounterOccurring)
                    {
                        yield return new WaitUntil(() => Singleton<TurnManager>.Instance != null && (Singleton<TurnManager>.Instance.IsPlayerTurn || Singleton<TurnManager>.Instance.GameIsOver()));
                        Singleton<TurnManager>.Instance.PlayerSurrendered = true;
                        yield return new WaitUntil(() => !GBCEncounterManager.Instance.EncounterOccurring);
                    }

                    SaveData.Data.natureTemple.roomId = "OutdoorsCentral";
                    SaveData.Data.natureTemple.cameraPosition = Vector2.zero;
                    SaveData.Data.undeadTemple.roomId = "MainRoom";
                    SaveData.Data.undeadTemple.cameraPosition = Vector2.zero;
                    SaveData.Data.techTemple.roomId = "--- MainRoom ---";
                    SaveData.Data.techTemple.cameraPosition = Vector2.zero;
                    SaveData.Data.wizardTemple.roomId = "Floor_1";
                    SaveData.Data.wizardTemple.cameraPosition = Vector2.zero;

                    SaveManager.SaveFile.currentScene = "GBC_WorldMap";
                    SaveData.Data.overworldNode = "StartingIsland";
                    LoadingScreenManager.LoadScene(SaveManager.SaveFile.currentScene);
                }
            }
            else if (SaveManager.saveFile.IsPart3 && Singleton<GameFlowManager>.Instance != null)
            {
                PauseMenu.pausingDisabled = true;

                if (Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.CardBattle)
                {
                    yield return new WaitUntil(() => Singleton<TurnManager>.Instance.IsPlayerTurn);
                    Singleton<TurnManager>.Instance.PlayerSurrendered = true;
                    yield return new WaitUntil(() => Part3SaveData.Data.playerLives < Part3SaveData.Data.playerMaxLives);
                    yield return new WaitUntil(() => Part3SaveData.Data.playerLives == Part3SaveData.Data.playerMaxLives);
                }
                else
                {
                    yield return new WaitUntil(() => Singleton<GameMap>.Instance.FullyUnrolled);
                    yield return Singleton<Part3GameFlowManager>.Instance.PlayerRespawnSequence();
                }

                PauseMenu.pausingDisabled = false;
            }

            receivedDeath = false;
        }

        static public void SendDeathLink()
        {
            if (!ArchipelagoOptions.deathlink || receivedDeath)
                return;
            ArchipelagoModPlugin.Log.LogMessage("Sharing death with your friends...");
            var alias = ArchipelagoClient.session.Players.GetPlayerAliasAndName(ArchipelagoClient.session.ConnectionInfo.Slot);
            int i = UnityEngine.Random.Range(0, 2);
            string cause;
            if (i == 0)
                cause = " skill issue";
            if (i == 1)
                cause = " lack of skill";
            else
                cause = " ineptitude";
            DeathLinkService.SendDeathLink(new DeathLink(ArchipelagoClient.GetPlayerName(ArchipelagoClient.session.ConnectionInfo.Slot), alias + cause));
        }
    }
}

