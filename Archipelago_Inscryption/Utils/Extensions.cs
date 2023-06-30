using UnityEngine;

namespace Archipelago_Inscryption.Utils
{
    internal static class Extensions
    {
        internal static void ResetTransform(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
