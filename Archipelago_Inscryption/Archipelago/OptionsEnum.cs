using System;
using System.Collections.Generic;
using System.Text;

namespace Archipelago_Inscryption.Archipelago
{
    internal enum RandomizeDeck
    {
        Disable,
        RandomizeType,
        RandomizeAll,
        COUNT
    }

    internal enum RandomizeSigils
    {
        Disable,
        RandomizeAddons,
        RandomizeAll,
        COUNT
    }

    internal enum OptionalDeathCard
    {
        Disable,
        Enable,
        EnableOnlyOnDeathLink,
        COUNT
    }

    internal enum Goal
    {
        AllActsInOrder,
        AllActsAnyOrder,
        Act1Only,
        COUNT
    }

    internal enum Act1DeathLink
    {
        Sacrificed,
        CandleExtinguished,
        COUNT
    }

    internal enum EpitaphPiecesRandomization
    {
        AllPieces,
        Groups,
        AsOneItem,
        COUNT
    }
}
