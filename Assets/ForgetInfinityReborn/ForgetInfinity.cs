using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System;

public class ForgetInfinity : MonoBehaviour {
	public KMSelectable[] Buttons;
	public KMSelectable BackSpaceButton;
	public KMBombModule ModSelf;
	public KMBombInfo Info;
	public TextMesh ScreenStages;
    public TextMesh ScreenStatus;

    private List<string> ignoredModuleNames;

	
	

    private bool solved = false, inFinale = false, hasStarted = false, isRecapturing = false, autosolvable = false;

    public List<List<int>> stages = new List<List<int>>();
    public List<List<int>> solution = new List<List<int>>();

    private int stagestoGenerate = 1;
    private int currentStage = 0;

    private bool containsFI = false;
    private int batteryCount = 0;

    
    public int delay = 0;

	private int lastThing = 0;
    private static int modID = 1;
    private static int curModID;

	// Use this for initialization
	void Awake() {
		if (ignoredModuleNames == null)
			ignoredModuleNames = GetComponent<KMBossModule>().GetIgnoredModules("Forget Infinity", new string[]{
                "14",
                "Bamboozling Time Keeper",
                "Cookie Jars",
                "Cruel Purgatory",
                "Divided Squares",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Hogwarts",
                "Organization",
                "Purgatory",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ültimate Custom Night",
                "The Very Annoying Button"
            }).ToList();
        //"Forget" Modules in this list, Simon's Stages, Souvenir, Tallordered Keys: Requires this module to be solved without Boss Module Manager
        //Time Keeper, Timing is Everything, Turn The Key: Bomb Timer Sensitive that can stall bombs
        //
        //Forget Infinity: DON'T HANG BOMBS WITH DUPLICATES OF THIS
        curModID = modID++;
	}
    void Start()
    {
        ModSelf.OnActivate += delegate
        {
            List<string> modNames = Info.GetSolvableModuleNames().Where(a => !ignoredModuleNames.Contains(a)).ToList();
            if (modNames.Count > 1)
            {
                if (GetComponent<KMBossModule>().GetIgnoredModules("Organization").Contains("Forget Infinity") || !Info.GetSolvableModuleNames().Contains("Organization"))
                {

                    hasStarted = true;
                }
                else
                { // Implement Failsafe to enforce this module to be solvable if Forget Infinity is NOT ignored by Organization AND Organization is present on the bomb.
                    Debug.LogFormat("[Forget Infinity {0}]: Organization is present AND not ignoring Forget Infinity! This module can be auto-solved by pressing any button.", curModID);
                    autosolvable = true;
                }
            }
            else
            {
                Debug.LogFormat("[Forget Infinity {0}]: No stages can be generated, the module can be auto-solved by pressing any button.",curModID);
                autosolvable = true;
            }
        };
        BackSpaceButton.OnInteract += delegate
        {
            if (inFinale)
            {

            }
            else if (autosolvable)
            {
                ModSelf.HandlePass();
            }
            else
            {
                ModSelf.HandleStrike();
            }
            return false;
        };
    }
    int curdelay = 0;
    void Update()
    {
        if (hasStarted)
        { 

        }
    }
    // Twitch Plays support

    public readonly string TwitchHelpMessage = "Enter the sequence with \"!{0} press 1 2 3 4 5...\". Submit with \"!{0} submit\". Reset with \"!{0} reset\".";

    public KMSelectable[] ProcessTwitchCommand(string cmd)
    {
        return null;
    }
}
