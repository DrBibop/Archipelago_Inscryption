using Archipelago_Inscryption.Archipelago;
using UnityEngine;

namespace Archipelago_Inscryption.Components
{
    internal class ActivateOnItemReceived : ManagedBehaviour
    {
        internal GameObject targetObject;

        internal APItem item;

        internal void Init(GameObject targetObject, APItem item)
        {
            this.targetObject = targetObject;
            this.item = item;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            ArchipelagoManager.onItemReceived += OnItemReceived;
        }

        public void OnDisable()
        {
            ArchipelagoManager.onItemReceived -= OnItemReceived;
        }

        private void OnItemReceived(APItem itemReceived)
        {
            if (item == itemReceived)
                targetObject.SetActive(true);
        }
    }
}
