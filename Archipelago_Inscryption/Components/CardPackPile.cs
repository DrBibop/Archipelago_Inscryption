using Archipelago_Inscryption.Archipelago;
using Archipelago_Inscryption.Helpers;
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
            if (pileTop == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!opening && isActiveAndEnabled)
                Tween.LocalPosition(pileTop.transform, topPackBasePosition + new Vector3(-0.6f, 0.4f, 0), 0.1f, 0, Tween.EaseOut);
        }

        public override void OnCursorExit()
        {
            if (pileTop == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!opening && isActiveAndEnabled)
                Tween.LocalPosition(pileTop.transform, topPackBasePosition, 0.1f, 0, Tween.EaseOut);
        }

        public override void OnCursorSelectEnd()
        {
            if (pileTop == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!opening && isActiveAndEnabled)
                StartCoroutine(OpenPack());
        }

        private IEnumerator OpenPack()
        {
            opening = true;
            Singleton<InteractionCursor>.Instance.InteractionDisabled = true;
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;

            Tween.LocalPosition(pileTop.transform, topPackBasePosition + new Vector3(-1.5f, 1f, 0), 0.2f, 0, Tween.EaseOut);
            Tween.LocalRotation(pileTop.transform, Quaternion.Euler(0, -90, 0), 0.2f, 0, Tween.EaseOut);

            yield return new WaitForSeconds(0.5f);

            GameObject cardPileObject = Instantiate(ResourceBank.Get<GameObject>(SaveManager.SaveFile.IsPart3 ? "Prefabs/Cards/CardPile_Part3" : "Prefabs/Cards/CardPile"), pileTop.transform);
            CardPile pile = cardPileObject.GetComponent<CardPile>();
            pile.transform.position = pileTop.transform.position + new Vector3(0, SaveManager.SaveFile.IsPart3 ? -0.05f : 0.1f, 0);
            pile.SetEnabled(false);
            pile.CreateCards(3, 0f);
            pileTop.GetComponentInChildren<Animator>().Play("open", 0, 0f);

            yield return new WaitForSeconds(1.25f);

            yield return pile.DestroyCards(new Vector3(0f, 4f, -5f), -20f, 0.75f);

            Singleton<ViewManager>.Instance.SwitchToView(View.MapArial);

            yield return new WaitForSeconds(0.25f);

            Singleton<InteractionCursor>.Instance.InteractionDisabled = false;

            SaveManager.SaveFile.gbcData.packsOpened++;
            ArchipelagoManager.AvailableCardPacks--;

            RandomizerHelper.DestroyPackPile();

            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;

            CardChoicesNodeData nodeData = new CardChoicesNodeData();
            nodeData.choicesType = CardChoicesType.Random;
            Singleton<GameFlowManager>.Instance.TransitionToGameState(GameState.SpecialCardSequence, nodeData);

            yield return new WaitForSeconds(0.25f);
        }
    }
}
