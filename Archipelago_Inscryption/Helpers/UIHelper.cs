using Archipelago_Inscryption.Components;
using Archipelago_Inscryption.Utils;
using UnityEngine;

namespace Archipelago_Inscryption.Helpers
{
    internal static class UIHelper
    {
        internal static InputField CreateInputField(GameObject prefab, Transform parent, string name, string label, string defaultContent, float yPosition, int characterLimit, bool censor = false)
        {
            GameObject inputFieldInstance = Object.Instantiate(prefab);
            inputFieldInstance.transform.SetParent(parent);
            inputFieldInstance.transform.ResetTransform();
            inputFieldInstance.transform.localPosition = new Vector3(0, yPosition, 0);
            inputFieldInstance.name = name;
            InputField inputField = inputFieldInstance.GetComponent<InputField>();
            inputField.Label = label;
            inputField.Text = defaultContent;
            inputField.CharacterLimit = characterLimit;
            inputField.Censor = censor;

            return inputField;
        }
    }
}
