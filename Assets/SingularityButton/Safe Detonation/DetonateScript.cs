using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
public class DetonateScript : MonoBehaviour {
    public KMGameCommands commands;
    public KMBombInfo bombInfo;
    public void RequestSafeDetonation()
    {
        // Try to detonate this by attempting to do the following
        /* Set the max strikes to the current number of strikes, + 1.
         * Cause a strike to blow it on the next strike.
         * Have the record manager say it blew it up.
         */
        if (Application.isEditor)
        {
            var bombComponent = GetComponentInParent<KMBomb>();
            if (bombComponent != null)
            {
                var timer = bombComponent.gameObject.transform.Find("TimerModule(Clone)");
                if (timer != null)
                {
                    var script = timer.GetComponent("TimerModule");
                    if (script != null)
                        script.SetValue("ExplodedToTime", true);
                }
                else
                    Debug.LogFormat("can't find component");
            }
            else
                Debug.LogFormat("can't find component");
        }
        else
        {
            var bombComponent = GetComponentInParent<KMBomb>();
            if (bombComponent != null)
            {
                var script = bombComponent.GetComponent("Bomb");
                if (script != null)
                {
                    var curStrikes = bombInfo.GetStrikes();
                    script.SetValue("NumStrikesToLose", curStrikes + 1);
                    //script.CallMethod("Detonate");
                    commands.CauseStrike("Requested Detonation");
                    /*
                    KMBombModule modSelf = GetComponent<KMBombModule>();
                    if (modSelf != null)
                        modSelf.HandleStrike();
                    */
                }
                else
                    Debug.LogFormat("can't find script component bomb");
            }
            else
                Debug.LogFormat("can't find bomb component");
        }
    }
    public void RequestDetonation()
    {
        // Tries to detonate by spamming with strikes. One of the typical ways to blow it up.
        // (Commonly used by many modules, minus the editor part.)
        if (Application.isEditor)
        {
            var bombComponent = GetComponentInParent<KMBomb>();
            if (bombComponent != null)
            {
                var timer = bombComponent.gameObject.transform.Find("TimerModule(Clone)");
                if (timer != null)
                {
                    var script = timer.GetComponent("TimerModule");
                    if (script != null)
                        script.SetValue("ExplodedToTime", true);
                }
                else
                    Debug.LogFormat("can't find component");
            }
            else
                Debug.LogFormat("can't find component");
        }
        else
        {
            var bombComponent = GetComponentInParent<KMBomb>();
            if (bombComponent != null)
            {
                var script = bombComponent.GetComponent("Bomb");
                if (script != null)
                {
                    var maxStrikes = script.GetValue<int>("NumStrikesToLose");
                    var curStrikes = bombInfo.GetStrikes();
                    //script.SetValue("NumStrikes", strikeCnt - 1);
                    Debug.LogFormat("{0} + {1} -> {2}", maxStrikes - curStrikes, curStrikes, maxStrikes);
                    for (var x = 0; x < maxStrikes - curStrikes; x++)
                        commands.CauseStrike("Requested Detonation");
                    
                }
                else
                    Debug.LogFormat("can't find script component bomb");
            }
            else
                Debug.LogFormat("can't find bomb component");
        }
    }
}
