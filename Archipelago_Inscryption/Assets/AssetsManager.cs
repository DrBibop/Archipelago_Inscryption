﻿using UnityEngine;

namespace Archipelago_Inscryption.Assets
{
    internal static class AssetsManager
    {
        private static AssetBundle assetBundle;

        internal static Sprite archiSettingsTabSprite;
        internal static Sprite inputFieldSprite;
        internal static Sprite[] packButtonSprites;

        internal static GameObject cardPackPrefab;
        internal static GameObject selectableCardPrefab;
        internal static GameObject archipelagoUIPrefab;

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

            cardPackPrefab = ResourceBank.Get<GameObject>("Prefabs/Cards/SpecificCardModels/CardPack");
            selectableCardPrefab = ResourceBank.Get<GameObject>("prefabs/cards/SelectableCard");

            archipelagoUIPrefab = assetBundle.LoadAsset<GameObject>("ArchipelagoUI");
        }

        private static Sprite GenerateSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }
}
