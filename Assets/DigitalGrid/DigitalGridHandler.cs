using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using uernd = UnityEngine.Random;

public class DigitalGridHandler : MonoBehaviour {

	public KMSelectable[] gridSelectables;
	public MeshRenderer[] gridRenderers;
	public TextMesh[] gridText;
	public KMSelectable checkBtn, resetBtn;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMColorblindMode colorblindMode;
	public KMAudio mAudio;

	bool interactable, colorblindDetected;
	int[] idxColors = new int[25], digitsGrid = new int[25];
	bool[] correctPresses = new bool[25], currentPresses = new bool[25];
	Color[] possibleColors = {
		Color.red,
		Color.green,
		Color.blue,
		Color.cyan,
		Color.yellow,
		Color.magenta,
		Color.white
	};
	bool[] invertTextColor = {
			false,
			false,
			true,
			false,
			false,
			false,
			false
		};
	private static int modIDCnt = 1;
	int modIDLog;
	void Awake()
    {
		try
        {
			colorblindDetected = colorblindMode.ColorblindModeActive;
        }
		catch
		{
			colorblindDetected = false;
		}
		
    }
	// Use this for initialization
	void Start () {
		modIDLog = modIDCnt++;
		modSelf.OnActivate += delegate {
			ResetModule();
			DisplayGrid();
		};
        for (int x = 0; x < gridSelectables.Length; x++)
        {
			int y = x;
			gridSelectables[x].OnInteract += delegate {
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, gridSelectables[y].transform);
				mAudio.PlaySoundAtTransform("tick", gridSelectables[y].transform);
				if (interactable)
				{
					currentPresses[y] = true;
					DisplayGrid();
				}
				return false;
			};
			gridText[x].text = "";
        }
		resetBtn.OnInteract += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			resetBtn.AddInteractionPunch();
			if (interactable)
            {
				for (int x = 0; x < currentPresses.Length; x++)
					currentPresses[x] = false;
				DisplayGrid();
            }
			return false;
		};
		checkBtn.OnInteract += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			checkBtn.AddInteractionPunch();
			if (interactable)
				CheckAnswer();
			return false;
		};
	}
	void DisplayGrid()
    {
        for (int x = 0; x < 25; x++)
        {
			if (currentPresses[x])
			{
				gridRenderers[x].material.color = invertTextColor[idxColors[x]] ? Color.white : Color.black;
                gridText[x].text = digitsGrid[x].ToString() + (colorblindDetected ? "RGBCYMW"[idxColors[x]].ToString() : "");
				gridText[x].characterSize = colorblindDetected ? 0.1f : 0.17f;
				gridText[x].color = possibleColors[idxColors[x]];
			}
			else
			{
				gridRenderers[x].material.color = possibleColors[idxColors[x]];
				gridText[x].text = digitsGrid[x].ToString() + (colorblindDetected ? "RGBCYMW"[idxColors[x]].ToString() : "");
				gridText[x].characterSize = colorblindDetected ? 0.1f : 0.17f;
				gridText[x].color = invertTextColor[idxColors[x]] ? Color.white : Color.black;
			}
        }
    }

	int DigitalRoot(int curNum)
    {
		int output = curNum * 1;
		while (output > 9)
        {
			output = output / 10 + output % 10;
        }
		return output;
    }

	bool ContainsSameColorOnRowOrCol()
    {
        for (int x = 0; x < 5; x++)
        {
			List<int> oneRow = new List<int>(), oneCol = new List<int>();
            for (int y = 0; y < 25; y++)
            {
				if (y / 5 == x)
					oneRow.Add(idxColors[y]);
				if (y % 5 == x)
					oneCol.Add(idxColors[y]);
			}
			if (oneRow.Distinct().Count() == 1) return true;
			if (oneCol.Distinct().Count() == 1) return true;
        }
		return false;
    }
	bool ContainsDistinctColorOnRowOrCol()
	{
		for (int x = 0; x < 5; x++)
		{
			List<int> oneRow = new List<int>(), oneCol = new List<int>();
			for (int y = 0; y < 25; y++)
			{
				if (y / 5 == x)
					oneRow.Add(idxColors[y]);
				if (y % 5 == x)
					oneCol.Add(idxColors[y]);
			}
			if (oneRow.Distinct().Count() == 5) return true;
			if (oneCol.Distinct().Count() == 5) return true;
		}
		return false;
	}
	void ResetModule()
    {
        for (int x = 0; x < 25; x++) // Generate a new set of digits and colors and reset current presses.
        {
			idxColors[x] = uernd.Range(0, possibleColors.Length);
			digitsGrid[x] = uernd.Range(0, 10);
			correctPresses[x] = false;
			currentPresses[x] = false;
        }
		Debug.LogFormat("[Digital Grid #{0}]: The grid is now:", modIDLog);
		List<string> logGrid = new List<string>();
		for (int x = 0; x < correctPresses.Length; x++)
		{
			logGrid.Add("RGBCYMW"[idxColors[x]].ToString() + digitsGrid[x].ToString());
			if (logGrid.Count == 5)
			{
				Debug.LogFormat("[Digital Grid #{0}]: {1}", modIDLog, logGrid.Join());
				logGrid.Clear();
			}
		}

		//Step 1
		int curVal = digitsGrid.Sum();
		Debug.LogFormat("[Digital Grid #{0}]: Sum of digits on the module: {1}", modIDLog, curVal);
		curVal = DigitalRoot(curVal);
		Debug.LogFormat("[Digital Grid #{0}]: After digital root from Step 1: {1}", modIDLog, curVal);
		// Step 2
		int sumSerialNo = bombInfo.GetSerialNumberNumbers().Sum();
		Debug.LogFormat("[Digital Grid #{0}]: Sum of digits on the serial number: {1}", modIDLog, sumSerialNo);
		curVal = DigitalRoot(curVal + sumSerialNo);
		Debug.LogFormat("[Digital Grid #{0}]: After digital root from Step 2: {1}", modIDLog, curVal);
		// Step 3
		int sumRedNum = 0;
        for (int x = 0; x < 25; x++)
        {
			if (idxColors[x] == 0)
            {
				sumRedNum += digitsGrid[x];
            }
        }
		sumRedNum = DigitalRoot(sumRedNum);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root of sum of all red numbers on the module: {1}", modIDLog, sumRedNum);
		curVal = DigitalRoot(curVal + sumRedNum);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root after step 3: {1}", modIDLog, curVal);
		// Step 4
		int portPlateCnt = bombInfo.GetPortPlateCount();
		Debug.LogFormat("[Digital Grid #{0}]:Port Plates detected: {1}", modIDLog, portPlateCnt);
		curVal = DigitalRoot(curVal + portPlateCnt);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root after step 4: {1}", modIDLog, curVal);
		// Step 5
		int sumYellowGreen = 0;
		for (int x = 0; x < 25; x++)
		{
			if (new[] { 1, 4 }.Contains( idxColors[x]))
			{
				sumYellowGreen += digitsGrid[x];
			}
		}
		sumYellowGreen = DigitalRoot(sumYellowGreen);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root of sum of all green and yellow numbers on the module: {1}", modIDLog, sumYellowGreen);
		curVal = DigitalRoot(curVal + sumYellowGreen);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root after step 5: {1}", modIDLog, curVal);
		// Step 6
		int productBatHolders = bombInfo.GetBatteryCount() * bombInfo.GetBatteryHolderCount();
		Debug.LogFormat("[Digital Grid #{0}]: Product of the number of battery holders and batteries: {1}", modIDLog, productBatHolders);
		curVal = DigitalRoot(curVal + productBatHolders);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root after step 6: {1}", modIDLog, curVal);
		int whiteNumCnt = idxColors.Count(a => a == 6);
		Debug.LogFormat("[Digital Grid #{0}]: Number of white numbers on the module: {1}", modIDLog, whiteNumCnt);
		curVal = DigitalRoot(curVal + whiteNumCnt);
		Debug.LogFormat("[Digital Grid #{0}]: Digital Root after step 7: {1}", modIDLog, curVal);

		char selectedLetter = '?';

		char[,] conditionTable = {
			{ 'G','G','G','G','G','G','G','G','G','G', },
			{ 'D','C','E','D','F','F','C','A','B','A', },
			{ 'A','D','B','D','B','D','B','D','B','D', },
			{ 'B','C','A','E','B','A','F','D','C','E', },
			{ 'F','B','E','D','F','A','C','E','B','D', },
			{ 'C','A','F','A','C','D','E','A','E','F', },
			{ 'G','G','G','G','G','G','G','G','G','G', },
			{ 'E','F','B','D','B','D','B','D','B','D', },
		};
		int cyanSum = 0;
		for (int x = 0; x < 25; x++)
		{
			if (idxColors[x] == 3)
			{
				cyanSum += digitsGrid[x];
			}
		}
		bool[] conditionsSatsfied = {
			ContainsSameColorOnRowOrCol(), // At least 1 row or column contains exactly one color
			idxColors.Count(a => a == 5) % 2 == 0, // An even number of magenta numbers
			idxColors.Count(a => a == 2) > idxColors.Count(a => a == 4), // More blue numbers than yellow numbers
			!idxColors.Any(a => a == 0), // No red numbers
			DigitalRoot(cyanSum) % 2 == 0, // Digital Root of all of the cyan numbers is even
			idxColors.Count(a => a == 6) == 2, // exactly 2 white numbers
			ContainsDistinctColorOnRowOrCol(), // At least one column or row all have different colors.
			true // Otherwise...
		};
        for (int y = 0; y < conditionsSatsfied.Length; y++)
        {
			if (conditionsSatsfied[y])
            {
				selectedLetter = conditionTable[y, curVal];
				switch (selectedLetter)
                {
					case 'A':
                        {
							int[] serialNoDigits = bombInfo.GetSerialNumberNumbers().ToArray();
                            for (int x = 0; x < 25; x++)
                            {
								if (serialNoDigits.Contains(digitsGrid[x]) || idxColors[x] == 4) // All yellow numbers, and all numbers that appear in the serial no.
									correctPresses[x] = true;
                            }
							break;
                        }
					case 'B':
                        {
                            for (int x = 0; x < 25; x++)
                            {
                                if (digitsGrid[x] % 2 == ((x / 5) + 1) % 2) // All odd numbers on rows 1, 3, 5 and all even numbers on rows 2, 4
                                    correctPresses[x] = true;
                            }
                            break;
                        }
					case 'C':
						{
							for (int x = 0; x < 25; x++)
							{
                                if (new[] { 0, 1, 2 }.Contains(idxColors[x]))// All red, green, blue numbers
									correctPresses[x] = true;
							}
							break;
						}
					case 'D':
						{
							for (int x = 0; x < 25; x++)
							{
								if (new[] { 3, 4, 5 }.Contains(idxColors[x]))// All cyan, yellow, magenta numbers
									correctPresses[x] = true;
							}
							break;
						}
					case 'E':
						{
							for (int x = 0; x < 25; x++)
							{
                                if (digitsGrid[x] % 2 == 1 && !new[] { 0, 6 }.Contains(idxColors[x])) // All even numbers not red or white
                                    correctPresses[x] = true;
							}
							break;
						}
					case 'F':
						{
							for (int x = 0; x < 25; x++)
							{
								if (digitsGrid[x] % 2 == 1 && !new[] { 3, 5 }.Contains(idxColors[x])) // All odd numbers not cyan or magenta
									correctPresses[x] = true;
							}
							break;
						}
					case 'G':
                        {
							switch (y)
							{
								case 0:
									{
										for (int x = 0; x < 5; x++)
										{
											List<int> oneRow = new List<int>(), oneCol = new List<int>();
											for (int idx = 0; idx < 25; idx++)
											{
												if (idx / 5 == x)
													oneRow.Add(idx);
												if (idx % 5 == x)
													oneCol.Add(idx);
											}
											if (oneRow.Select(a => idxColors[a]).Distinct().Count() == 1)
											{
												foreach (int oneIdx in oneRow)
												{
													correctPresses[oneIdx] = true;
												}
											}
											if (oneCol.Select(a => idxColors[a]).Distinct().Count() == 1)
											{
												foreach (int oneIdx in oneCol)
												{
													correctPresses[oneIdx] = true;
												}
											}
										}
										break;
									}
								case 6:
									{
										for (int x = 0; x < 5; x++)
										{
											List<int> oneRow = new List<int>(), oneCol = new List<int>();
											for (int idx = 0; idx < 25; idx++)
											{
												if (idx / 5 == x)
													oneRow.Add(idx);
												if (idx % 5 == x)
													oneCol.Add(idx);
											}
											if (oneRow.Select(a => idxColors[a]).Distinct().Count() == 5)
											{
												foreach (int oneIdx in oneRow)
                                                {
													correctPresses[oneIdx] = true;
                                                }
											}
											if (oneCol.Select(a => idxColors[a]).Distinct().Count() == 5)
											{
												foreach (int oneIdx in oneCol)
												{
													correctPresses[oneIdx] = true;
												}
											}
										}
										break;
									}
							}
							break;
                        }
				}
				Debug.LogFormat("[Digital Grid #{0}]: First Applied Row: {1}", modIDLog, y + 1);
				Debug.LogFormat("[Digital Grid #{0}]: Resulting Letter: {1}", modIDLog, selectedLetter);
				break;
            }
        }
		Debug.LogFormat("[Digital Grid #{0}]: Correct Presses ('O' denotes must press, 'X' denotes do not press):", modIDLog);
		List<string> logCorrectPresses = new List<string>();
		for (int x = 0; x < correctPresses.Length; x++)
        {
			logCorrectPresses.Add(correctPresses[x] ? "O" : "X");
			if (logCorrectPresses.Count == 5)
			{
				Debug.LogFormat("[Digital Grid #{0}]: {1}", modIDLog, logCorrectPresses.Join(""));
				logCorrectPresses.Clear();
			}
		}
		
		interactable = true;
    }
	void CheckAnswer()
    {
		interactable = false;
		Debug.LogFormat("[Digital Grid #{0}]: Presses Submitted:", modIDLog);
		List<string> logCorrectPresses = new List<string>();
		for (int x = 0; x < currentPresses.Length; x++)
		{
			logCorrectPresses.Add(currentPresses[x] ? "O" : "X");
			if (logCorrectPresses.Count == 5)
			{
				Debug.LogFormat("[Digital Grid #{0}]: {1}", modIDLog, logCorrectPresses.Join(""));
				logCorrectPresses.Clear();
			}
		}
		if (currentPresses.SequenceEqual(correctPresses))
        {
			Debug.LogFormat("[Digital Grid #{0}]: No errors detected. Module solved.", modIDLog);
			StartCoroutine(HandleSolveAnim());
			modSelf.HandlePass();
        }
		else
        {
			Debug.LogFormat("[Digital Grid #{0}]: Strike! Incorrect presses:", modIDLog);
			List<string> logInCorrectPresses = new List<string>();
			for (int x = 0; x < currentPresses.Length; x++)
			{
				logCorrectPresses.Add(currentPresses[x] != correctPresses[x] ? "!" : "-");
				if (logCorrectPresses.Count == 5)
				{
					Debug.LogFormat("[Digital Grid #{0}]: {1}", modIDLog, logCorrectPresses.Join(""));
					logCorrectPresses.Clear();
				}
			}
			modSelf.HandleStrike();
			StartCoroutine(HandleStrikeAnim());
        }
    }
	IEnumerator HandleSolveAnim()
    {
        for (int x = 0; x < 25; x++)
			gridText[x].text = "";
		mAudio.PlaySoundAtTransform("correctALT2", transform);
        for (int y = 0; y < 4; y++)
        {
			for (int x = 0; x < gridRenderers.Length; x++)
            {
				gridRenderers[x].material.color = Color.green;
			}
			yield return new WaitForSeconds(0.2f);
			for (int x = 0; x < gridRenderers.Length; x++)
			{
				gridRenderers[x].material.color = Color.black;
			}
			yield return new WaitForSeconds(0.2f);
		}
		for (int x = 0; x < gridRenderers.Length; x++)
		{
			gridRenderers[x].material.color = Color.black;
		}
		yield return null;
    }
	IEnumerator HandleStrikeAnim()
    {
		for (int x = 0; x < 25; x++)
			gridText[x].text = "";
		for (int y = 0; y < 2; y++)
		{
			for (int x = 0; x < gridRenderers.Length; x++)
			{
				gridRenderers[x].material.color = Color.red;
			}
			yield return new WaitForSeconds(0.2f);
			for (int x = 0; x < gridRenderers.Length; x++)
			{
				gridRenderers[x].material.color = Color.black;
			}
			yield return new WaitForSeconds(0.2f);
		}
		yield return null;
		ResetModule();
		DisplayGrid();
	}


	// Update is called once per frame
	void Update () {

	}

	// Twitch Plays Handling
	IEnumerator TwitchHandleForcedSolve()
    {
		Debug.LogFormat("[Digital Grid #{0}]: Force solve requested viva TP handler.", modIDLog);
		while (!interactable)
			yield return true;
		while (!currentPresses.SequenceEqual(correctPresses))
        {
			yield return null;
			resetBtn.OnInteract();
			for (int x = 0; x < correctPresses.Length; x++)
            {
				if (correctPresses[x])
				{
					yield return null;
					gridSelectables[x].OnInteract();
				}
            }
        }
		yield return null;
		checkBtn.OnInteract();
		yield return true;
	}
#pragma warning disable IDE0051 // Remove unused private members
	readonly string TwitchHelpMessage = "Toggle selected numbers with \"!{0} press A1 B2 C3...\" \"press\" is optional. Columns are labeled A-E from left to right, rows are numbered 1-5 from top to bottom. Submit the current state with \"!{0} submit\" or with specific buttons pressed with \"!{0} submit A1 B2 C3...\". Reset buttons pressed with \"!{0} reset\" Toggle colorblind mode with \"!{0} colorblind/colourblind\"";
	bool TwitchPlaysActive;
#pragma warning restore IDE0051 // Remove unused private members
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (!interactable)
        {
			yield return "sendtochaterror The module cannot be interacted right now. Wait a bit until you can interact with it again.";
			yield break;
        }
		if (Regex.IsMatch(cmd, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			colorblindDetected = !colorblindDetected;
			DisplayGrid();
        }
		else if (Regex.IsMatch(cmd, @"^reset$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			resetBtn.OnInteract();
        }
		else if (Regex.IsMatch(cmd, @"^submit(\s[abcde][12345])+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			string[] coordinates = cmd.ToLowerInvariant().Split();
			bool[] requestedPresses = new bool[25];
			for (int x = 1; x < coordinates.Length; x++)
            {
                string colCmdRange = "abcde", rowCmdRange = "12345";
                if (!coordinates[x].RegexMatch(@"^[abcde][12345]$"))
                {
                    yield break;
                }
                int rowIdx = rowCmdRange.IndexOf(coordinates[x][1]), colIdx = colCmdRange.IndexOf(coordinates[x][0]);
				if (rowIdx > -1 && colIdx > -1)
					requestedPresses[rowIdx * 5 + colIdx] = true;
            }
			
			while (!requestedPresses.SequenceEqual(currentPresses))
			{
				yield return null;
				resetBtn.OnInteract();
				for (int x = 0; x < requestedPresses.Length; x++)
				{
					if (requestedPresses[x] && !currentPresses[x])
					{
						yield return null;
						gridSelectables[x].OnInteract();
					}
					yield return new WaitForSeconds(0.1f);
				}
			}
            yield return null;
			checkBtn.OnInteract();
		}
		else if (Regex.IsMatch(cmd, @"^submit$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			checkBtn.OnInteract();
		}
		else if (Regex.IsMatch(cmd, @"^(press\s)?[abcde][12345](\s[abcde][12345])*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			string lowerCmd = cmd.ToLowerInvariant();
			string[] coordinates = lowerCmd.StartsWith("press") ? lowerCmd.Substring(5).Trim().Split() : lowerCmd.Split();
			bool[] requestedPresses = new bool[25];
			for (int x = 0; x < coordinates.Length; x++)
			{
				string colCmdRange = "abcde", rowCmdRange = "12345";
				if (!coordinates[x].RegexMatch(@"^[abcde][12345]$"))
				{
					yield break;
				}
				int rowIdx = rowCmdRange.IndexOf(coordinates[x][1]), colIdx = colCmdRange.IndexOf(coordinates[x][0]);
				if (rowIdx > -1 && colIdx > -1)
					requestedPresses[rowIdx * 5 + colIdx] = true;
			}
			for (int x = 0; x < requestedPresses.Length; x++)
			{
				if (requestedPresses[x])
				{
					yield return null;
					gridSelectables[x].OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
				
			}
		}
		yield break;
    }

}
