﻿using UnityEngine;

namespace Archipelago_Inscryption.Assets
{
    internal static class AssetsManager
    {
        private static AssetBundle assetBundle;

        internal static Sprite archiSettingsTabSprite;
        internal static Sprite inputFieldSprite;
        internal static Sprite[] packButtonSprites;

        internal static Texture2D boonTableTex;
        internal static Texture2D[] smallClockClueTexs;

        internal static GameObject cardPackPrefab;
        internal static GameObject selectableCardPrefab;
        internal static GameObject selectableDiskCardPrefab;
        internal static GameObject archipelagoUIPrefab;
        internal static GameObject cardChoiceHoloNodePrefab;
        internal static GameObject clockCluesPrefab;
        internal static GameObject smallClockCluePrefab;

        internal static Mesh checkCardHoloNodeMesh;

        internal static void LoadAssets()
        {
            assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.archiassets);

            if (!assetBundle)
            {
                ArchipelagoModPlugin.Log.LogError("The asset bundle couldn't be loaded.");

                return;
            }

            Texture2D archipelagoTabTex = assetBundle.LoadAsset<Texture2D>("ArchipelagoTab");
            Texture2D inputFieldTex = assetBundle.LoadAsset<Texture2D>("InputFieldImage");

            archiSettingsTabSprite = GenerateSprite(archipelagoTabTex);
            inputFieldSprite = GenerateSprite(inputFieldTex);

            packButtonSprites = assetBundle.LoadAssetWithSubAssets<Sprite>("GBCCardPackButton");

            boonTableTex = assetBundle.LoadAsset<Texture2D>("BoonTableEdited");
            smallClockClueTexs = new Texture2D[12];
            for (int i = 0; i < 12; i++)
            {
                smallClockClueTexs[i] = assetBundle.LoadAsset<Texture2D>("SmallClockClue_" + i.ToString());
            }

            cardPackPrefab = ResourceBank.Get<GameObject>("prefabs/cards/specificcardmodels/CardPack");
            selectableCardPrefab = ResourceBank.Get<GameObject>("prefabs/cards/SelectableCard");
            selectableDiskCardPrefab = ResourceBank.Get<GameObject>("prefabs/cards/SelectableCard_Part3");
            cardChoiceHoloNodePrefab = ResourceBank.Get<GameObject>("prefabs/map/mapnodespart3/CardChoiceNode3D");

            clockCluesPrefab = assetBundle.LoadAsset<GameObject>("ClockHeadClues");
            archipelagoUIPrefab = assetBundle.LoadAsset<GameObject>("ArchipelagoUI");
            smallClockCluePrefab = assetBundle.LoadAsset<GameObject>("SmallClockClue");

            checkCardHoloNodeMesh = assetBundle.LoadAsset<Mesh>("CheckCard_mesh");
        }

        private static Sprite GenerateSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }
}
