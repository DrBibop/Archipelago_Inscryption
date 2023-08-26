using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Helpers;
using BepInEx;
using DiskCardGame;
using GBC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archipelago_Inscryption.Archipelago
{
    internal static class DeathLinkManager
    {
        public static DeathLinkService DeathLinkService;
        internal static bool receivedDeath;
        private static int nbDeathsSent;

        internal static void Init()
        {
            ArchipelagoModPlugin.Log.LogMessage($"DeathLink is set to: " + ArchipelagoClient.serverData.deathlink.ToString());
            DeathLinkService.OnDeathLinkReceived += ReceiveDeathLink;
            if (ArchipelagoClient.serverData.deathlink)
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

            if (SaveManager.saveFile.IsPart1 && Singleton<GameFlowManager>.Instance != null && ProgressionData.LearnedMechanic(MechanicsConcept.LosingLife))
            {
                if (Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.CardBattle)
                {
                    if (Singleton<TurnManager>.Instance.IsSetupPhase)
                        yield return new WaitUntil(() => !Singleton<TurnManager>.Instance.IsSetupPhase);

                    yield return Singleton<TurnManager>.Instance.CleanupPhase();
                }

                while (RunState.Run.playerLives > 0)
                    yield return Singleton<CandleHolder>.Instance.BlowOutCandleSequence();
                yield return RandomizerHelper.PrePlayerDeathSequence(Singleton<Part1GameFlowManager>.Instance);
            }
            else if (SaveManager.saveFile.IsPart2 && Singleton<PlayerMovementController>.Instance != null)
            {
                if (SaveManager.SaveFile.currentScene != "GBC_Starting_Island")
                {
                    SaveManager.SaveFile.currentScene = "GBC_Starting_Island";
                    SaveData.Data.overworldNode = "StartingIsland";
                    SaveData.Data.overworldIndoorPosition = -Vector3.up;
                    LoadingScreenManager.LoadScene(SaveManager.SaveFile.currentScene);
                }
            }
            else if (SaveManager.saveFile.IsPart3 && Singleton<GameFlowManager>.Instance != null)
            {
                if (Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.CardBattle)
                {
                    if (Singleton<TurnManager>.Instance.IsSetupPhase)
                        yield return new WaitUntil(() => !Singleton<TurnManager>.Instance.IsSetupPhase);

                    yield return Singleton<TurnManager>.Instance.CleanupPhase();
                    yield return new WaitUntil(() => Part3SaveData.Data.playerPos == Part3SaveData.Data.checkpointPos);
                }
                else
                {
                    yield return new WaitUntil(() => Singleton<GameMap>.Instance.FullyUnrolled);
                    yield return Singleton<Part3GameFlowManager>.Instance.PlayerRespawnSequence();
                }
            }

            receivedDeath = false;
        }

        static public void SendDeathLink()
        {
            if (!ArchipelagoClient.serverData.deathlink || receivedDeath)
                return;
            nbDeathsSent++;
            ArchipelagoModPlugin.Log.LogMessage("Sharing death with your friends...");
            var alias = ArchipelagoClient.session.Players.GetPlayerAliasAndName(ArchipelagoClient.session.ConnectionInfo.Slot);
            int i = UnityEngine.Random.Range(0, 2);
            string cause;
            if (i == 0)
                cause = " died because of skill issue";
            if (i == 1)
                cause = " is not good enough";
            else
                cause = " tried their hardest but ultimately failed";
            DeathLinkService.SendDeathLink(new DeathLink(ArchipelagoClient.serverData.slotName, alias + cause));
        }
    }
}

