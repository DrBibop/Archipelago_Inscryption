using System.Collections;
using UnityEngine;

namespace Archipelago_Inscryption.Utils
{
    /// <summary>
    /// Made by Peeling in the Unity Forums: https://forum.unity.com/threads/crashed-coroutine-remains-non-null-is-there-any-way-to-detect-this.1548821/
    /// </summary>
    public class WaitForFailsafeCoroutine : CustomYieldInstruction
    {
        public WaitForFailsafeCoroutine(FailsafeCoroutine toWrap)
        {
            wrapped = toWrap;
        }

        public FailsafeCoroutine wrapped;
        public override bool keepWaiting => wrapped.IsRunning;
    }

    public class FailsafeCoroutine : IEnumerator
    {

        public static WaitForFailsafeCoroutine Start(MonoBehaviour host, System.Collections.IEnumerator corot, FailsafeCoroutine.CompletionCallback callback, bool propagateCrash = true)
        {
            var fsc = new FailsafeCoroutine(corot, callback, propagateCrash);
            host.StartCoroutine(fsc);
            return new WaitForFailsafeCoroutine(fsc);
        }


        public object Current { get => current; }
        public bool IsRunning { get => running; }
        public bool HasCrashed { get => crashed; }

        public delegate void CompletionCallback(bool success);

        private CompletionCallback onCompletion;
        private object current = null;
        private FailsafeCoroutine nested = null;
        private IEnumerator enumerator = null;

        private bool running;
        private bool crashed;
        private bool propagateCrash;

        private FailsafeCoroutine(IEnumerator corot, CompletionCallback callback, bool propagateCrash)
        {
            onCompletion = callback;
            enumerator = corot;
            crashed = false;
            running = true;
            this.propagateCrash = propagateCrash;
        }

        bool Finish()
        {
            running = false;
            onCompletion?.Invoke(true);
            return false;
        }

        bool Crash(string message)
        {
            Debug.Log(enumerator + " " + message);
            running = false;
            crashed = true;
            onCompletion?.Invoke(false);
            return false;
        }

        public bool MoveNext()
        {
            // Check the health of any FailsafeCoroutines we are waiting on
            if (nested != null && nested.crashed)
            {
                if (propagateCrash)
                {
                    Crash("Nested FailsafeCoroutine crashed - propagating...");
                    return false;
                }
                else
                {
                    Debug.Log(enumerator + " Nested FailsafeCoroutine crashed - continuing");
                }
                nested = null;
            }

            // Attempt to pump our own coroutine
            try
            {
                if (!enumerator.MoveNext())
                {
                    return Finish();
                }
            }
            catch (System.Exception e)
            {
                // Coroutine has crashed. Log and abort execution.
                return Crash("Failsafe Coroutine Crashed : " + e.Message + " \n " + e.StackTrace);
            }

            nested = null;

            if (enumerator.Current is YieldInstruction y)
            {
                //Adopt yieldinstructions unmodified
                if (enumerator.Current is Coroutine c)
                {
                    Debug.LogWarning("*** Failsafe coroutine " + enumerator + " relies on the success of an unsafe coroutine ***");
                }
                current = y;
            }
            else if (enumerator.Current is WaitForFailsafeCoroutine wffsc)
            {
                // We are now waiting on another independently pumped FailsafeCoroutine
                current = wffsc;
                nested = wffsc.wrapped;
            }
            else if (enumerator.Current is FailsafeCoroutine f)
            {
                // we are now waiting on a dependent FailsafeCoroutine.
                current = nested = f;
            }
            else if (enumerator.Current is IEnumerator i)
            {
                // Wrap any other enumerators in a FailsafeCoroutine to ensure their safe handling.
                current = nested = new FailsafeCoroutine(i, null, true);
            }
            else
            {
                // We are not equipped to handle this eventuality; pass it on.
                current = enumerator.Current;
            }

            return true;
        }

        public void Reset()
        {
            if (enumerator != null) enumerator.Reset();
        }
    }
}