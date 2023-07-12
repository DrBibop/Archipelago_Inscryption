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

        internal static void SetLayerRecursive(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;

            foreach (Transform t in gameObject.transform)
            {
                t.gameObject.SetLayerRecursive(layer);
            }
        }

        internal static string GetPath(this Transform transform)
        {
            return (transform.parent ? transform.parent.GetPath() + "/" : "") + transform.name;
        }
    }
}
