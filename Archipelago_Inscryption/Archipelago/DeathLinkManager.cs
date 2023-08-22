using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
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
        private static readonly List<DeathLink> deathLinks = new List<DeathLink>();
        private static System.Random random;
        internal static bool receivedDeath;
        private static int nbDeathsSent;

        static void Init()
        {
            DeathLinkService = ArchipelagoClient.session.CreateDeathLinkService();
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
            deathLinks.Add(deathLink);
            Console.WriteLine($"Received DeathLink from: {deathLink.Source} due to {deathLink.Cause}");
            if (Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.CardBattle)
                Singleton<TurnManager>.Instance.GameEnded = true;
            else
            {
                CustomCoroutine.Instance.StartCoroutine("ReceiveDeathLinkCoroutine");
                if (SaveManager.saveFile.IsPart1)
                {
                    CustomCoroutine.Instance.StartCoroutine("BlowOutCandleSequence");
                }
                if (SaveManager.saveFile.IsPart2)
                {
                    SaveManager.SaveFile.currentScene = "GBC_Starting_Island";
                    SaveData.Data.overworldNode = "StartingIsland";
                    SaveData.Data.overworldIndoorPosition = -Vector3.up;
                    LoadingScreenManager.LoadScene(SaveManager.SaveFile.currentScene);
                }
                if (SaveManager.saveFile.IsPart3)
                {
                    CustomCoroutine.Instance.StartCoroutine("PlayerRespawnSequence");
                }
            }

            receivedDeath = false;
        }

        static IEnumerator ReceiveDeathLinkNotInCombatCoroutine()
        {
            yield return new WaitUntil(() => Singleton<GameFlowManager>.Instance.CurrentGameState == GameState.Map);
        }

        static public void SendDeathLink()
        {
            if (!ArchipelagoClient.serverData.deathlink || receivedDeath)
                return;
            nbDeathsSent++;
            Console.WriteLine("Sharing death with your friends...");
            var alias = ArchipelagoClient.session.Players.GetPlayerAliasAndName(ArchipelagoClient.session.ConnectionInfo.Slot);
            int i = random.Next(0, 3);
            string cause;
            if (i == 0)
                cause = " died because of skill issue";
            if (i == 1)
                cause = " is not good enough";
            else
                cause = " tried their hardest but ultimately failed";

            DeathLinkService.SendDeathLink(new DeathLink(ArchipelagoClient.serverData.slotName, alias + cause));
        }

        static public void KillPlayer()
        {
            if (deathLinks.Count > 0)
                receivedDeath = true;
            if (!receivedDeath)
                return;
            string cause = deathLinks[0].Cause;
            if (cause.IsNullOrWhiteSpace())
            {
                cause = deathLinks[0].Source + " is dead : rip bozo";
            }
            ArchipelagoManager.KillPlayer();
            ArchipelagoModPlugin.Log.LogMessage(deathLinks[0].Source + cause);
            deathLinks.RemoveAt(0);
            receivedDeath = false;
        }


    }
}
