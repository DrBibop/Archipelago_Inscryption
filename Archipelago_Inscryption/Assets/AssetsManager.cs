using UnityEngine;

namespace Archipelago_Inscryption.Assets
{
    internal static class AssetsManager
    {
        private static AssetBundle assetBundle;

        internal static Sprite archiSettingsTabSprite;
        internal static Sprite inputFieldSprite;
        internal static Sprite[] packButtonSprites;
        internal static Sprite editedNatureFloorSprite;
        internal static Sprite cardPortraitSprite;
        internal static Sprite cardPixelPortraitSprite;

        internal static Texture2D boonTableTex;
        internal static Texture2D[] smallClockClueTexs;
        internal static Texture2D[] factoryClockClueTexs;

        internal static GameObject cardPackPrefab;
        internal static GameObject selectableCardPrefab;
        internal static GameObject selectableDiskCardPrefab;
        internal static GameObject archipelagoUIPrefab;
        internal static GameObject cardChoiceHoloNodePrefab;
        internal static GameObject clockCluesPrefab;
        internal static GameObject smallClockCluePrefab;
        internal static GameObject gbcSafeCluePrefab;

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
            Texture2D editedNatureFloorTex = assetBundle.LoadAsset<Texture2D>("nature_temple_floor_edited");
            Texture2D cardPortraitTex = assetBundle.LoadAsset<Texture2D>("archi_portrait");
            Texture2D cardPixelPortraitTex = assetBundle.LoadAsset<Texture2D>("archi_portrait_gbc");

            archiSettingsTabSprite = GenerateSprite(archipelagoTabTex);
            inputFieldSprite = GenerateSprite(inputFieldTex);
            editedNatureFloorSprite = GenerateSprite(editedNatureFloorTex);
            cardPortraitSprite = GenerateSprite(cardPortraitTex);
            cardPixelPortraitSprite = GenerateSprite(cardPixelPortraitTex);

            packButtonSprites = assetBundle.LoadAssetWithSubAssets<Sprite>("GBCCardPackButton");

            boonTableTex = assetBundle.LoadAsset<Texture2D>("BoonTableEdited");
            smallClockClueTexs = new Texture2D[12];
            for (int i = 0; i < 12; i++)
            {
                smallClockClueTexs[i] = assetBundle.LoadAsset<Texture2D>("SmallClockClue_" + i.ToString());
            }
            factoryClockClueTexs = new Texture2D[12];
            for (int i = 0; i < 12; i++)
            {
                factoryClockClueTexs[i] = assetBundle.LoadAsset<Texture2D>("FactoryClockClue_" + i.ToString());
            }

            cardPackPrefab = ResourceBank.Get<GameObject>("prefabs/cards/specificcardmodels/CardPack");
            selectableCardPrefab = ResourceBank.Get<GameObject>("prefabs/cards/SelectableCard");
            selectableDiskCardPrefab = ResourceBank.Get<GameObject>("prefabs/cards/SelectableCard_Part3");
            cardChoiceHoloNodePrefab = ResourceBank.Get<GameObject>("prefabs/map/mapnodespart3/CardChoiceNode3D");

            clockCluesPrefab = assetBundle.LoadAsset<GameObject>("ClockHeadClues");
            archipelagoUIPrefab = assetBundle.LoadAsset<GameObject>("ArchipelagoUI");
            smallClockCluePrefab = assetBundle.LoadAsset<GameObject>("SmallClockClue");
            gbcSafeCluePrefab = assetBundle.LoadAsset<GameObject>("GBCSafeClue");

            checkCardHoloNodeMesh = assetBundle.LoadAsset<Mesh>("CheckCard_mesh");
        }

        private static Sprite GenerateSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }
}
