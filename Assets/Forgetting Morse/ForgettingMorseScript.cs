using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForgettingMorseScript : MonoBehaviour {

	public KMBossModule bossHandler;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMSelectable[] keyboardBtns;
	public TextMesh shortTxt, longTxt;

	static int modIDCnt;
	int moduleID;
	bool focused, moduleSolved, activated;

	private static Dictionary<char, string> chrMorse = new Dictionary<char, string> {
		{ 'A', ".-" }, { 'B', "-..." }, { 'C', "-.-." }, { 'D', "-.." },
		{ 'E', "." }, { 'F', "..-." }, { 'G', "--." }, { 'H', "...." },
		{ 'I', ".." }, { 'J', ".---" }, { 'K', "-.-" }, { 'L', ".-.." },
		{ 'M', "--" }, { 'N', "-." }, { 'O', "---" }, { 'P', ".--." },
		{ 'Q', "--.-" }, { 'R', ".-." }, { 'S', "..." }, { 'T', "-" },
		{ 'U', "..-" }, { 'V', "...-" }, { 'W', ".--" }, { 'X', "-..-" },
		{ 'Y', "-.--" }, { 'Z', "--.." },
		{ '0', "-----" }, { '1', ".----" }, { '2', "..---" },
		{ '3', "...--" }, { '4', "....-" }, { '5', "....." },
		{ '6', "-...." }, { '7', "--..." }, { '8', "---.." },
		{ '9', "----." },
	};
	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[Forgetting Morse #{0}] {1}", moduleID, string.Format(value, args));
	}
	void QuickLogDebug(string value, params object[] args)
	{
		Debug.LogFormat("<Forgetting Morse #{0}> {1}", moduleID, string.Format(value, args));
	}
	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		modSelf.OnActivate += ActivateModule;
	}
	void ActivateModule()
    {
		activated = true;
    }

	// Update is called once per frame
	void Update () {
		
	}
}
