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

    internal enum RandomizeAbilities
    {
        Disable,
        RandomizeModded,
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
}
