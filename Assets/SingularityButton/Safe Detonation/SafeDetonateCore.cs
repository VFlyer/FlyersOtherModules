using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
public class SafeDetonateCore : MonoBehaviour {

    public KMSelectable detonateSelectable, disarmSelectable;
    public KMBombModule modSelf;
    public KMAudio mAudio;
    public DetonateScript detonateHandler;
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
                detonateHandler.RequestSafeDetonation();
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
}
