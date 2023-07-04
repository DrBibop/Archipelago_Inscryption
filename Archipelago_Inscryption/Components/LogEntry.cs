using UnityEngine;
using UnityEngine.UI;

namespace Archipelago_Inscryption.Components
{
    internal class LogEntry : ManagedBehaviour
    {
        private const float DURATION = 8f;
        private const float FADEOUT_DURATION = 2f;

        private Text text;
        private float creationTime;
        private bool fading = false;

        private void Awake()
        {
            text = GetComponent<Text>();
        }

        private void Start()
        {
            creationTime = Time.realtimeSinceStartup;
        }

        internal void SetText(string message)
        {
            text.text = message;
        }

        public override void ManagedUpdate()
        {
            float timeSinceCreation = Time.realtimeSinceStartup - creationTime;

            if (!fading && timeSinceCreation >= DURATION - FADEOUT_DURATION ) 
            {
                fading = true;

                Color newColor = text.color;
                newColor.a = 0;
                text.CrossFadeColor(newColor, FADEOUT_DURATION, true, true);
            }

            if (timeSinceCreation > DURATION)
            {
                Destroy(gameObject);
            }
        }
    }
}
