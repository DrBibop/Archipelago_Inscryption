using System;
using System.Collections.Generic;
using System.Text;

namespace Archipelago_Inscryption.Archipelago
{
    internal class InscryptionItemInfo
    {
        public APItem Item { get; private set; }
        public string ItemName { get; private set; }
        public long ItemId { get; private set; }
        public long LocationId { get; private set; }
        public int PlayerSlot { get; private set; }
        public string PlayerName { get; private set; }

        public InscryptionItemInfo(APItem item, string itemName, long itemId, long locationId, int playerSlot, string playerName)
        {
            Item = item;
            ItemName = itemName;
            ItemId = itemId;
            LocationId = locationId;
            PlayerSlot = playerSlot;
            PlayerName = playerName;
        }
    }
}
