using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
public class SafeDetonateCore : MonoBehaviour {

    public KMSelectable detonateSelectable, disarmSelectable;
    public KMBombModule modSelf;
    public KMAudio mAudio;
    public KMGameCommands commands;
    public KMBombInfo bombInfo;
    bool hasDisarmed = false, isPressedDisarm, isPressedDetonator;

    Vector3 startPosDisarm, startPosDetonate;

	// Use this for initialization
	void Start () {

        disarmSelectable.OnInteract += delegate {
            isPressedDisarm = true;
            mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, disarmSelectable.transform);
            return false;
        };
        disarmSelectable.OnInteractEnded += delegate {
            mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, disarmSelectable.transform);
            isPressedDisarm = false;
            hasDisarmed = true;
            modSelf.HandlePass();
        };
        detonateSelectable.OnInteract += delegate {
            isPressedDetonator = true;
            mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, disarmSelectable.transform);
            return false;
        };
        detonateSelectable.OnInteractEnded += delegate {
            mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, disarmSelectable.transform);
            isPressedDetonator = false;
            if (!hasDisarmed)
                RequestDetonation();
        };
        startPosDisarm = disarmSelectable.transform.localPosition;
        startPosDetonate = detonateSelectable.transform.localPosition;
    }

    // Update is called once per frame
    float percentDisarm = 1f, percentDetonate = 1f;
    void Update()
    {
        if (!isPressedDisarm)
        {
            percentDisarm = Mathf.Min(percentDisarm + Time.deltaTime, 1);
        }
        else
            percentDisarm = Mathf.Max(percentDisarm - Time.deltaTime, 0.8f);
        disarmSelectable.transform.localPosition = new Vector3(startPosDisarm.x, startPosDisarm.y * percentDisarm, startPosDisarm.z);
        if (!isPressedDetonator)
        {
            percentDetonate = Mathf.Min(percentDetonate + Time.deltaTime, 1);
        }
        else
            percentDetonate = Mathf.Max(percentDetonate - Time.deltaTime, 0.8f);
        detonateSelectable.transform.localPosition = new Vector3(startPosDetonate.x, startPosDetonate.y * percentDetonate, startPosDetonate.z);
    }
    void RequestDetonation()
    {
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
                    var strikeCnt = script.GetValue<int>("NumStrikesToLose");
                    script.SetValue("NumStrikes", strikeCnt - 1);
                    // Disabled temporary for bundling reasons.
                    /*
                    StrikeSource strikeSource = new StrikeSource
                    {
                        ComponentType = Assets.Scripts.Missions.ComponentTypeEnum.Mod,
                        InteractionType = InteractionTypeEnum.Other,
                        Time = bombInfo.GetTime(),
                        ComponentName = "Instant Detonation"
                    };
                    
                    var recordManager = GetComponent("RecordManager");
                    if (recordManager == null)
                    {
                        Debug.LogFormat("can't find record manager");   
                        return;
                    }
                    recordManager.CallMethod("RecordStrike", strikeSource);
                    */
                    // Section to cause the detonation handler.
                    var compSelf = GetComponent("BombComponent");
                    if (compSelf != null)
                    {
                        modSelf.HandleStrike();
                        //compSelf.CallMethod("OnStrike", compSelf);
                    }
                }
                else
                    Debug.LogFormat("can't find script component bomb");
            }
            else
                Debug.LogFormat("can't find bomb component");
        }
    }
}
