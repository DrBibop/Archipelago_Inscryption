using DiskCardGame;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class InputField : MainInputInteractable
    {
        public static bool IsAnySelected 
        {
            get
            {
                foreach (InputField field in allInputFields)
                {
                    if (field.keyboardInput.enabled) return true;
                }

                return false;
            }
        }

        private static List<InputField> allInputFields = new List<InputField>();

        internal string Label
        {
            get { return label.text; }
            set { label.text = value; }
        }

        internal string Text
        {
            get { return realText; }
            set 
            { 
                realText = value; 
                text.text = (censor ? new string('*', value.Length) : value); ;
                keyboardInput.KeyboardInput = value;
            }
        }

        internal int CharacterLimit
        {
            get { return keyboardInput.maxInputLength; }
            set { keyboardInput.maxInputLength = value; }
        }

        internal bool Censor
        {
            get { return censor; }
            set { censor = value; }
        }

        public override bool CollisionIs2D => true;

        private KeyboardInputHandler keyboardInput;
        private Text label;
        private Text text;
        private string realText;
        private bool censor;

        private bool isPointerInside = false;

        private void Awake()
        {
            keyboardInput = GetComponent<KeyboardInputHandler>();

            if (keyboardInput == null )
                keyboardInput = gameObject.AddComponent<KeyboardInputHandler>();

            keyboardInput.allowPasteClipboard = true;
            keyboardInput.maxInputLength = 30;
            keyboardInput.enabled = false;
            keyboardInput.EnterPressed += OnEnterPressed;

            label = transform.Find("Title/Text").GetComponent<Text>();
            text = transform.Find("TextFrame/Text/Text").GetComponent<Text>();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            allInputFields.Add(this);
        }

        private void OnDisable()
        {
            allInputFields.Remove(this);
        }

        private void OnEnterPressed()
        {
            keyboardInput.enabled = false;
        }

        public override void ManagedUpdate()
        {
            base.ManagedUpdate();

            if (InputButtons.GetButtonDown(Button.Select))
            {
                if (isPointerInside)
                {
                    keyboardInput.enabled = true;
                }
                else
                {
                    keyboardInput.enabled = false;
                }
            }

            if (!keyboardInput.enabled)
            {
                text.text = (censor ? new string('*', realText.Length) : realText);
                return;
            }

            realText = keyboardInput.KeyboardInput;

            bool showTextCursor = ((int)(Time.timeSinceLevelLoad * 2)) % 2 == 0;

            text.text = (censor ? new string('*', realText.Length) : realText) + (showTextCursor ? "" : "|");
        }

        public override void OnCursorEnter()
        {
            isPointerInside = true;
        }

        public override void OnCursorExit()
        {
            isPointerInside = false;
        }
    }
}
