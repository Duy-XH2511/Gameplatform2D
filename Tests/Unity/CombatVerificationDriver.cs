using System;
using System.Collections;
using UnityEngine;

// Runtime coroutine driver for the temporary Unity project.
public sealed class CombatVerificationDriver : MonoBehaviour
{
    public void Run(IEnumerator verification, Action<int, string> finished)
    {
        StartCoroutine(Execute(verification, finished));
    }

    private IEnumerator Execute(IEnumerator verification, Action<int, string> finished)
    {
        while (true)
        {
            bool next = false;
            Exception failure = null;
            try { next = verification.MoveNext(); }
            catch (Exception error) { failure = error; }
            if (failure != null)
            {
                finished(1, "FAIL: " + failure);
                yield break;
            }
            if (!next)
            {
                finished(0, "PASS: all Unity combat integration checks.");
                yield break;
            }
            yield return verification.Current;
        }
    }
}
