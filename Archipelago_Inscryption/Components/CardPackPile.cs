using Archipelago_Inscryption.Archipelago;
using DiskCardGame;
using GBC;
using Pixelplacement;
using System.Collections;
using UnityEngine;

namespace Archipelago_Inscryption.Components
{
    internal class CardPackPile : MainInputInteractable
    {
        internal Vector3 topPackBasePosition;

        internal GameObject pileTop;

        bool opening = false;

        public override void OnCursorEnter()
        {
            if (!opening && isActiveAndEnabled)
                Tween.LocalPosition(pileTop.transform, topPackBasePosition + new Vector3(-0.6f, 0.4f, 0), 0.1f, 0, Tween.EaseOut);
        }

        public override void OnCursorExit()
        {
            if (!opening && isActiveAndEnabled)
                Tween.LocalPosition(pileTop.transform, topPackBasePosition, 0.1f, 0, Tween.EaseOut);
        }

        public override void OnCursorSelectEnd()
        {
            if (!opening && isActiveAndEnabled)
                StartCoroutine(OpenPack());
        }

        private IEnumerator OpenPack()
        {
            opening = true;
            Singleton<InteractionCursor>.Instance.InteractionDisabled = true;

            Tween.LocalPosition(pileTop.transform, topPackBasePosition + new Vector3(-1.5f, 1f, 0), 0.2f, 0, Tween.EaseOut);
            Tween.LocalRotation(pileTop.transform, Quaternion.Euler(0, -90, 0), 0.2f, 0, Tween.EaseOut);

            yield return new WaitForSeconds(0.5f);

            GameObject cardPileObject = Instantiate(ResourceBank.Get<GameObject>("Prefabs/Cards/CardPile"), pileTop.transform);
            CardPile pile = cardPileObject.GetComponent<CardPile>();
            pile.transform.position = pileTop.transform.position + new Vector3(0, 0.1f, 0);
            pile.SetEnabled(false);
            pile.CreateCards(3, 0f);
            pileTop.GetComponentInChildren<Animator>().Play("open", 0, 0f);

            yield return new WaitForSeconds(1.25f);

            yield return pile.DestroyCards(new Vector3(0f, 4f, -5f), -20f, 0.75f);

            Singleton<ViewManager>.Instance.SwitchToView(View.MapArial);

            yield return new WaitForSeconds(0.25f);

            Singleton<InteractionCursor>.Instance.InteractionDisabled = false;

            CardChoicesNodeData nodeData = new CardChoicesNodeData();
            nodeData.choicesType = CardChoicesType.Random;
            Singleton<GameFlowManager>.Instance.TransitionToGameState(GameState.SpecialCardSequence, nodeData);

            SaveManager.SaveFile.gbcData.packsOpened++;

            yield return new WaitForSeconds(0.25f);

            ArchipelagoManager.AvailableCardPacks--;
        }
    }
}
