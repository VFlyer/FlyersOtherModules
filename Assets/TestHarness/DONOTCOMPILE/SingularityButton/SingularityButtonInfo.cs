using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;

public partial class SingularityButtonHandler
{
	protected sealed class SingularityButtonInfo //Lock down infomation to a single bomb, hopefully.
	{
		public List<SingularityButtonHandler> singularButtons = new List<SingularityButtonHandler>();// A collection of Singularity Button Handlers on 1 global handler.

		public List<Color> buttonColors = new List<Color>();
		public List<string> buttonLabels = new List<string>();
		public List<int> buttonDigits = new List<int>();
		private List<int> idxInputs = new List<int>();
		public List<int> inputs = new List<int>();
		public IEnumerable<int> serialNumberDigits;
		public KMBombInfo bombInfoSelf;
		public bool canDisarm = false;
		public bool isRunning = true;
		readonly string[] LogManualChallengePhrases = {
			"Some strange magic is preventing this module to log the objective... Maybe it would be better to use your senses instead.",
			"Using your eyes and taking notes would go a long way right? The logging seems to be busted at the moment.",
			"Logging busted. 0/10. Had to use eyes and notes to figure this out instead.",
			"Wait. Logging? What's that? I'm confused. Is this \"Sig\" person here to explain this?",
			"Unfortunately for you, the logs have been taken over by some strange force.",
			"Does he obscure the logging just to make this harder?",
			"This is supposed to be a manual challenge right? What's holding it back?",
			"Sig? What is this module that you gave to them?"
		};
		readonly string[] referenceListNames = new string[] { "black", "red", "green", "yellow", "blue", "magenta", "cyan", "white", "grey/gray" };
		readonly List<Color> referenceList = new List<Color>() { Color.black, Color.red, Color.green, Color.yellow, Color.blue, Color.magenta, Color.cyan, Color.white, Color.grey },
		storesColorList = new List<Color>() {
				Color.white, Color.red, Color.green,
				Color.yellow, Color.cyan, Color.grey,
				Color.blue, Color.magenta, Color.black
			},
		singleButtonColorList = new List<Color>() {
				Color.white, Color.red, Color.green,
				Color.yellow, Color.cyan, Color.grey,
				Color.blue, Color.magenta, Color.black
			},
		buttonGridColorList = new List<Color>() {
				Color.white, Color.red, Color.green,
				Color.yellow, Color.cyan, Color.blue,
				Color.magenta, Color.black
			};
		public void DisarmAll()
		{
			canDisarm = true;
		}
		public void StopAll()
		{
			isRunning = false;
		}
		public void LogAll(string text)
		{
			foreach (SingularityButtonHandler singularityButton in singularButtons)
			{
				Debug.LogFormat("[Singularity Button #{0}]: {1}", singularityButton.curmodID, text);
			}
		}
		public void LogIndividual(string text, int idx)
		{
			if (idx >= 0 && idx < singularButtons.Count)
			{
				Debug.LogFormat("[Singularity Button #{0}]: {1}", singularButtons[idx].curmodID, text);
			}
		}
		public void LogIndividual(string text, SingularityButtonHandler handler)
		{
			Debug.LogFormat("[Singularity Button #{0}]: {1}", handler.curmodID, text);
		}
		public void CauseStrikeAll()
		{
			LogAll("An incorrect set of actions caused this module to strike.");
			foreach (SingularityButtonHandler singularityButton in singularButtons)
				singularityButton.HandleStrikeSelf();
		}
		public void CauseStrikeIndividual(int idx)
		{
			if (idx >= 0 && idx < singularButtons.Count)
			{
				LogIndividual("An incorrect set of actions caused this module to strike.", idx);
				singularButtons[idx].HandleStrikeSelf();
			}
		}
		public void CauseStrikeIndividual(SingularityButtonHandler handler)
		{
			LogIndividual("An incorrect set of actions caused this module to strike.", handler);
			handler.HandleStrikeSelf();
		}
		public int CountSingularityButtons()
		{
			return singularButtons.Count();
		}
		public int getIndexOfButton(SingularityButtonHandler handler)
		{
			return singularButtons.IndexOf(handler);
		}
		public bool IsAnyButtonHeld()
		{
			return !singularButtons.TrueForAll(a => !a.isPressedMain);
		}
		public string GrabColorofButton(int idx)
		{
			if (idx < 0 || idx > singularButtons.Count) return "";
			int grabbedIndex = referenceList.IndexOf(singularButtons[idx].buttonMainRenderer.material.color);
			return grabbedIndex >= 0 && grabbedIndex < referenceListNames.Length ? referenceListNames[grabbedIndex] : "some other color";
		}
		public string GrabColorofButton(SingularityButtonHandler handler)
		{
			int grabbedIndex = referenceList.IndexOf(handler.buttonMainRenderer.material.color);
			return grabbedIndex >= 0 && grabbedIndex < referenceListNames.Length ? referenceListNames[grabbedIndex] : "some other color";
		}
		public bool IsEqualToNumberOnBomb(SingularityButtonHandler buttonHandler)
		{
			return CountSingularityButtons() == buttonHandler.bombInfo.GetModuleNames().Count(a => a.Equals("Singularity Button"));
		}
		public void HandleInteraction(int idx, int value)
		{
			idxInputs.Add(idx);
			inputs.Add(value);
		}
		public void HandleInteraction(SingularityButtonHandler handler, int value)
		{
			idxInputs.Add(singularButtons.IndexOf(handler));
			inputs.Add(value);
		}
		public void ClearAllInputs()
		{
			idxInputs.Clear();
			inputs.Clear();
		}
		// Handle Global Interaction with the module
		public void HandleButtonRelease()
		{
			int btnCount = CountSingularityButtons();
			switch (btnCount)
			{
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
				case 13:
				case 14:
				case 15:
				case 16:
				default:
					break;
			}// Empty, would need to find better uses afterwards.

		}
		public void StartSingularityButton()
		{
			if (singularButtons.Count <= 0) return;
			if (IsEqualToNumberOnBomb(singularButtons[0]))
			{
				int btnCount = CountSingularityButtons();
				switch (btnCount)
				{
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
					case 7:
					case 8:
					case 9:
					case 10:
					case 11:
					case 12:
					case 13:
					case 14:
					case 15:
					case 16:
					default:
						break;
				}// Empty, would need to find better uses afterwards.
			}
		}

		public IEnumerator StartBootUpSequence()
		{
			if (singularButtons.Count <= 0) yield break;
			if (IsEqualToNumberOnBomb(singularButtons[0]))
			{
				int btnCount = CountSingularityButtons();
				LogAll("Detected this many Singularity Buttons on the bomb: " + btnCount);
				foreach (SingularityButtonHandler singularity in singularButtons)
				{
					LogIndividual(LogManualChallengePhrases.PickRandom(), singularity);
				}
				switch (btnCount)
				{
					case 1:
						{// 1 Singularity Button
							int goalValue = Random.Range(0, 10);
							int secondValue = Random.Range(0, 10);
							int[] poweroftwoindex = new int[] { 1, 2, 4 };
							int[] forbiddenColorIndex = new int[] { 8, 0, 1, 2, 4, 7 };
							Color selectedColor = singleButtonColorList[Random.Range(0, singleButtonColorList.Count)];
							singularButtons[0].buttonMainRenderer.material.color = selectedColor;
							float displayValue = 0;
							int coloridx = referenceList.IndexOf(selectedColor);
							float delayTime = 1;
							while (!canDisarm && isRunning)
							{
								if (inputs.Count >= 2)
								{
									if (
									(poweroftwoindex.Contains(coloridx) && (inputs[0] % 60).ToString().Contains(goalValue.ToString()) && (inputs[1] % 60).ToString().Contains(goalValue.ToString()) && (delayTime > 0))
									|| (coloridx == 0 && (inputs[1] % 10) == (9 - secondValue) && (delayTime <= 0))
									|| ((coloridx == 7) && (inputs[1] % 60).ToString().Contains(goalValue.ToString()) && (delayTime <= 0))
									|| (coloridx == 8 && (inputs[1] % 60).ToString().Contains(secondValue.ToString()) && (delayTime <= 0))
									|| (!forbiddenColorIndex.Contains(coloridx) && inputs[0] % 10 == goalValue && inputs[1] % 10 == secondValue && (delayTime <= 0))
									)
									{
										DisarmAll();
									}
									else
									{
										CauseStrikeIndividual(0);
										LogIndividual("The button's display has changed.", 0);
										//LogIndividual(string.Format("For reference, the button's initial display was {0} and the missing digit in the sequence was {1}", goalValue, secondValue), 0);
										goalValue = Random.Range(0, 10);
										secondValue = Random.Range(0, 10);
									}
									ClearAllInputs();
								}
								else if (inputs.Count == 1)
								{
									delayTime = Mathf.Max(delayTime - Time.deltaTime, 0);
									singularButtons[0].textDisplay.text = delayTime <= 0 && Mathf.FloorToInt(displayValue) != secondValue ? Mathf.FloorToInt(displayValue).ToString() : "";
									displayValue = (displayValue + (delayTime <= 0 ? Time.deltaTime * 10 : 0)) % 10;
								}
								else
								{
									delayTime = 1;
									singularButtons[0].textDisplay.text = coloridx >= 4 ? (9 - goalValue).ToString() : goalValue.ToString();
									displayValue = 0;
								}
								yield return new WaitForEndOfFrame();

							}
							singularButtons[0].textDisplay.text = "";
							break;
						}
					case 2:
						{// 2 Singularity Buttons
							int goalvalue = Random.Range(0, 65536);
							serialNumberDigits = singularButtons[0].bombInfo.GetSerialNumberNumbers();
							string bit2Input = "";
							string bit16Input = "";
							char[] hexdecimals = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
							int givenValue = goalvalue;
							bool[] inputPattern = new bool[4];
							int lastDigitInSerial = serialNumberDigits.Any() ? serialNumberDigits.Last() : 0;
							// Divider for Intializing
							for (int x = 0; x < inputPattern.Length; x++)
							{
								inputPattern[x] = Random.Range(0, 2) == 0;
							}
							for (int x = 0; x < 16; x++)
							{
								bit2Input += (givenValue % 2).ToString();
								givenValue /= 2;
							}
							givenValue = goalvalue;
							for (int x = 0; x < 4; x++)
							{
								bit16Input += hexdecimals[givenValue % 16];
								givenValue /= 16;
							}
							string bit2FinalInput = "";
							for (int p = 0; p < bit2Input.Length; p++)
							{
								if (inputPattern[p % 4])
								{
									bit2FinalInput += bit2Input.Substring(p, 1).Equals("0") ? "1" : "0";
								}
								else
								{
									bit2FinalInput += bit2Input.Substring(p, 1).Equals("1") ? "1" : "0";
								}
							}
							char[] displayHex = bit16Input.Reverse().ToArray();
							int representOne = Random.Range(0, 2);
							if (representOne == 1)
							{
								singularButtons[0].buttonMainRenderer.material.color = Color.black;
								singularButtons[1].buttonMainRenderer.material.color = Color.white;
							}
							else
							{
								singularButtons[0].buttonMainRenderer.material.color = Color.white;
								singularButtons[1].buttonMainRenderer.material.color = Color.black;
							}
							int goalIDX = 0;
							float curDelay = 0.4f;
							//print(inputPattern.ToList().Join(", "));
							//print(bit2Input);
							//print(bit2FinalInput);
							//print(lastDigitInSerial % 2 == 0);
							LogAll(string.Format("Not all logging has been obscured yet. The module is logging the final answer: {0}. Figure out how to get that and how to input it.", bit2FinalInput));
							// Display
							while (!canDisarm && isRunning)
							{
								if (inputs.Count == 0 && goalIDX <= 0)
								{
									if (curDelay <= 0)
										curDelay = 10f;
									else
										curDelay -= Time.deltaTime;
									if (curDelay > 9f)
									{
										singularButtons[0].buttonBacking.material.color = Color.white;
										singularButtons[1].buttonBacking.material.color = Color.white;
									}
									else if (curDelay > 7f && curDelay <= 8f)
									{
										if (!inputPattern[0])
											singularButtons[0].textDisplay.text = displayHex[0].ToString();
										else
											singularButtons[1].textDisplay.text = displayHex[0].ToString();
									}
									else if (curDelay > 5f && curDelay <= 6f)
									{
										if (!inputPattern[1])
											singularButtons[0].textDisplay.text = displayHex[1].ToString();
										else
											singularButtons[1].textDisplay.text = displayHex[1].ToString();
									}
									else if (curDelay > 3f && curDelay <= 4f)
									{
										if (!inputPattern[2])
											singularButtons[0].textDisplay.text = displayHex[2].ToString();
										else
											singularButtons[1].textDisplay.text = displayHex[2].ToString();
									}
									else if (curDelay > 1f && curDelay <= 2f)
									{
										if (!inputPattern[3])
											singularButtons[0].textDisplay.text = displayHex[3].ToString();
										else
											singularButtons[1].textDisplay.text = displayHex[3].ToString();
									}
									else
									{
										singularButtons[0].textDisplay.text = "";
										singularButtons[1].textDisplay.text = "";
										singularButtons[0].buttonBacking.material.color = Color.black;
										singularButtons[1].buttonBacking.material.color = Color.black;
									}
								}
								else
								{
									singularButtons[0].textDisplay.text = "";
									singularButtons[1].textDisplay.text = "";
									singularButtons[0].buttonBacking.material.color = Color.black;
									singularButtons[1].buttonBacking.material.color = Color.black;
									if (inputs.Count == 2)
									{
										if (Mathf.Abs(inputs[0] - inputs[1]) < 5 || Mathf.Abs(inputs[1] - inputs[0] + 60) < 5 || Mathf.Abs(inputs[0] - inputs[1] + 60) < 5)
										{
											if (idxInputs[0] == representOne)
											{
												if (bit2FinalInput[goalIDX].Equals('1') && inputs[0] % 2 == (lastDigitInSerial + 1) % 2)
													goalIDX++;
												else
												{
													CauseStrikeIndividual(idxInputs[0]);
													goalIDX = 0;
												}
											}
											else
											{
												if (bit2FinalInput[goalIDX].Equals('0') && inputs[0] % 2 == lastDigitInSerial % 2)
													goalIDX++;
												else
												{
													CauseStrikeIndividual(idxInputs[0]);
													goalIDX = 0;
												}
											}
										}
										else
										{
											goalIDX = 0;
											LogAll("The inputs have been cleared.");
										}
										if (goalIDX >= bit2FinalInput.Length)
										{
											DisarmAll();
										}
										ClearAllInputs();
									}
								}
								yield return new WaitForEndOfFrame();
							}
							break;
						}
					case 3:
						{

							Dictionary<int, Color[,]> colorPatternIndexes = new Dictionary<int, Color[,]>() {
								{ 0, new Color[,]
								{
									{ Color.magenta, Color.red, Color.yellow },
									{ Color.red, Color.yellow, Color.green },
									{ Color.yellow, Color.green, Color.cyan },
									{ Color.green, Color.cyan, Color.blue },
									{ Color.cyan, Color.blue, Color.magenta },
									{ Color.blue, Color.magenta, Color.red },
								}
								},


							};

							// Display
							while (!canDisarm && isRunning)
							{
								float curDelay = 0.4f;
								if (inputs.Count == 0)
								{
									if (curDelay <= 0)
										curDelay = 10f;
									else
										curDelay -= Time.deltaTime;
									if (curDelay > 9f)
									{
										singularButtons[0].buttonBacking.material.color = Color.white;
										singularButtons[1].buttonBacking.material.color = Color.white;
									}
									else if (curDelay > 7f && curDelay <= 8f)
									{

									}
									else if (curDelay > 5f && curDelay <= 6f)
									{
									}
									else if (curDelay > 3f && curDelay <= 4f)
									{
									}
									else if (curDelay > 1f && curDelay <= 2f)
									{
									}
									else
									{
										singularButtons[0].textDisplay.text = "";
										singularButtons[1].textDisplay.text = "";
										singularButtons[0].buttonBacking.material.color = Color.black;
										singularButtons[1].buttonBacking.material.color = Color.black;
									}
								}
								else
								{
									singularButtons[0].textDisplay.text = "";
									singularButtons[1].textDisplay.text = "";
									singularButtons[0].buttonBacking.material.color = Color.black;
									singularButtons[1].buttonBacking.material.color = Color.black;
									if (inputs.Count == 2)
									{
										ClearAllInputs();
									}
								}
								yield return new WaitForEndOfFrame();
							}
							break;
						}
					case 4:
					case 5:
					case 6:
					case 7:
					case 8:
					case 9:
					case 10:
						{
							break;
						}
					case 11:
					case 12:
					case 13:
					case 14:
					case 15:
						{

							break;
						}
					case 16:
					default:
						break;
				}
			}
			yield return null;
		}
	}
}
