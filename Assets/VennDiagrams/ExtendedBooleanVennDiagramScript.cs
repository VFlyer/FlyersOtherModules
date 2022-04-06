using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExtendedBooleanVennDiagramScript : MonoBehaviour {

	public KMSelectable[] buttonSelectables;
	public KMBombModule modSelf;
    public KMAudio mAudio;
	public MeshRenderer[] buttonRenderers;
    public MeshRenderer textRenderer;
    public TextMesh equationDisplay;

    List<int> goalPressIdxes, currentPressIdxes;
    bool calculatedSolution, modSolved;
    static int modIDCnt = 1;
    string expressionToDisplay;
    int curModID;
	// Use this for initialization
	void Start () {
        curModID = modIDCnt++;
        modSelf.OnActivate += GenerateSolution;
        for (var x = 0; x < buttonSelectables.Length; x++)
        {
            var y = x;
            buttonSelectables[x].OnInteract += delegate {
                buttonSelectables[y].AddInteractionPunch(0.25f);
                mAudio.PlaySoundAtTransform("tick", buttonSelectables[y].transform);
                ProcessInput(y);
                return false;
            };
        }
        equationDisplay.text = "";
	}
    void SolveModule()
    {
        StartCoroutine(GlitchText());
        modSolved = true;
        modSelf.HandlePass();
        foreach (int incorrectIdx in Enumerable.Range(0, 32).Except(goalPressIdxes).Except(currentPressIdxes))
        {
            buttonRenderers[incorrectIdx].material.color = Color.red * 0.5f + Color.white * 0.5f;
        }
    }

    void ProcessInput(int idx)
    {
        if (modSolved || !calculatedSolution || currentPressIdxes.Contains(idx)) return;
        currentPressIdxes.Add(idx);
        if (goalPressIdxes.Contains(idx))
        {
            Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] Correctly pressed {1}.", curModID, idx == 0 ? "O" : new [] {
                idx % 2 == 1 ? "A" : "",
                idx / 2 % 2 == 1 ? "B" : "",
                idx / 4 % 2 == 1 ? "C" : "",
                idx / 8 % 2 == 1 ? "D" : "",
                idx / 16 % 2 == 1 ? "E" : ""
                }.Join(""));
            if (currentPressIdxes.Intersect(goalPressIdxes).Count() == goalPressIdxes.Count)
            {
                Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] That's all of them. Module disarmed.", curModID);
                SolveModule();

            }
            buttonRenderers[idx].material.color = Color.green * 0.5f + Color.white * 0.5f;
        }
        else if (!goalPressIdxes.Any())
        {
            Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] There were no correct buttons. Disarming automatically.", curModID);
            SolveModule();
            return;
        }
        else
        {
            Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] Strike! {1} is not one of the correct buttons.",
                curModID, idx == 0 ? "O" : new[] {
                idx % 2 == 1 ? "A" : "",
                idx / 2 % 2 == 1 ? "B" : "",
                idx / 4 % 2 == 1 ? "C" : "",
                idx / 8 % 2 == 1 ? "D" : "",
                idx / 16 % 2 == 1 ? "E" : ""
                }.Join(""));
            buttonRenderers[idx].material.color = Color.red * 0.5f + Color.white * 0.5f;
            modSelf.HandleStrike();
        }
    }

	void GenerateSolution()
    {
        var idxMerges = new List<int[]>();
        var idxOperatorSets = new List<int>();
        goalPressIdxes = new List<int>();
        currentPressIdxes = new List<int>();

        for (var x = 0; x < 4; x++)
        {
            var startOffsetIdx = Random.Range(0, x + 1);
            idxMerges.Add(Enumerable.Range(startOffsetIdx, 2).ToArray());
            idxOperatorSets.Add(Random.Range(0, 8));
        }

        var separateStrings = new List<string>() { "A", "B", "C", "D", "E" };
        var operatorSets = new[] { " \u22C0 ", " \u22C1 ", " \u22BB ", " | ", " \u2193 ", " \u2194 ", " \u2192 ", " \u2190 " };
        for (var y = idxMerges.Count - 1; y >= 0; y--)
        {
            var idxCurSet = idxMerges[y];
            var idxCurOp = idxOperatorSets[y];

            separateStrings[idxCurSet.First()] = separateStrings[idxCurSet.First()] + operatorSets[idxCurOp] + separateStrings[idxCurSet.Last()];
            if (y > 0)
            {
                separateStrings[idxCurSet.First()] = "(" + separateStrings[idxCurSet.First()] + ")";
            }
            separateStrings.RemoveAt(idxCurSet.Last());
            //Debug.Log(separateStrings.Join());
        }
        var finalEquation = separateStrings.FirstOrDefault().Trim();
        expressionToDisplay = finalEquation;
        equationDisplay.text = expressionToDisplay;
        Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] Generated Expression: {1}", curModID, finalEquation);
        Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] Visualization of buttons to press:", curModID);
        for (var x = 0; x < 32; x++)
        {
            var boolStatements = new List<bool>
            { x % 2 == 1, x / 2 % 2 == 1, x / 4 % 2 == 1, x / 8 % 2 == 1, x / 16 % 2 == 1 };
            //Debug.Log(x);
            for (var y = idxMerges.Count - 1; y >= 0; y--)
            {
                var idxCurSet = idxMerges[y];
                var idxCurOp = idxOperatorSets[y];

                boolStatements[idxCurSet.First()] = EvaluateExpression(boolStatements[idxCurSet.First()], boolStatements[idxCurSet.Last()], idxCurOp);
                boolStatements.RemoveAt(idxCurSet.Last());
                //Debug.Log(boolStatements.Join());
            }
            if (boolStatements.FirstOrDefault())
                goalPressIdxes.Add(x);
        }
        Debug.LogFormat("[Extended Boolean Venn Diagram #{0}] Final buttons to press ({2} total button{3}): {1}", curModID,
            goalPressIdxes.Select(a => a == 0 ? "O" : new[]
        {
            a % 2 == 1 ? "A" : "",
            a / 2 % 2 == 1 ? "B" : "",
            a / 4 % 2 == 1 ? "C" : "",
            a / 8 % 2 == 1 ? "D" : "",
            a / 16 % 2 == 1 ? "E" : ""
        }.Join("")).Join(", "), goalPressIdxes.Count, goalPressIdxes.Count == 1 ? "" : "s");
        calculatedSolution = true;
        StartCoroutine(GlitchTextToStabalize());
    }
    IEnumerator GlitchTextToStabalize()
    {
        var matToModify = textRenderer.material;
        for (float x = 0; x < 1f; x += Time.deltaTime)
        {
            yield return null;
            matToModify.mainTextureOffset = Random.insideUnitCircle * (1f - x) + Vector2.one * x;
        }
        matToModify.mainTextureOffset = Vector2.zero;
    }
    IEnumerator GlitchText()
    {
        var matToModify = textRenderer.material;
        var lastColor = equationDisplay.color;
        for (float x = 0; x < 1f; x += Time.deltaTime)
        {
            yield return null;
            matToModify.mainTextureOffset = Random.insideUnitCircle * x;
            equationDisplay.color = lastColor * (1f - x);
            equationDisplay.text = expressionToDisplay.Select(a => Random.value <= x ? '*' : a).Join("");
        }
        matToModify.mainTextureOffset = Vector2.zero;
        equationDisplay.text = "";
    }

	bool EvaluateExpression(bool valueA, bool valueB, int idxOperator)
    {
        switch (idxOperator)
        {
            case 0: // AND
                return valueA && valueB;
            case 1: // OR
                return valueA || valueB;
            case 2: // XOR
                return valueA ^ valueB;
            case 3: // NAND
                return !(valueA && valueB);
            case 4: // NOR
                return !(valueA || valueB);
            case 5: // XNOR
                return !valueA ^ valueB;
            case 6: // ->
                return !valueA || valueB;
            case 7: // <-
                return valueA || !valueB;
        }
		return valueA;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (!calculatedSolution)
            yield return true;
        if (goalPressIdxes.Any())
        {
            foreach (int idx in goalPressIdxes.Except(currentPressIdxes))
            {
                buttonSelectables[idx].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            buttonSelectables.PickRandom().OnInteract();
        }
    }
    static List<string> possiblePresses;
#pragma warning disable IDE0051 // Remove unused private members
    readonly string TwitchHelpMessage = "Select that given region with :\"!{0} o AB BC ABC c\". \"press\" is optional. "+
        "Possible regions to select are O, A, B, C, AB, AC, BC, ABC, D, AD, BD, CD, ABD, ACD, BCD, ABCD, E, AE, BE, CE, ABE, ACE, BCE, ABCE, DE, ADE, BDE, CDE, ABDE, ACDE, BCDE, ABCDE. " +
        "Refer to the manual for where A, B, C, D, E are located";
#pragma warning restore IDE0051 // Remove unused private members
    IEnumerator ProcessTwitchCommand(string cmd)
    {
        var usedCmd = cmd;
        if (Application.isEditor)
            usedCmd = usedCmd.Trim();
        if (possiblePresses == null)
        {
            possiblePresses = Enumerable.Range(0, 32).Select(
                a => a == 0 ? "O" : new[]
            {
                a % 2 == 1 ? "A" : "",
                a / 2 % 2 == 1 ? "B" : "",
                a / 4 % 2 == 1 ? "C" : "",
                a / 8 % 2 == 1 ? "D" : "",
                a / 16 % 2 == 1 ? "E" : ""
            }.Join("")).ToList();
        }
        if (usedCmd.ToLower().StartsWith("press"))
            usedCmd = usedCmd.Substring(5).Trim();
        List<int> pressesToMake = new List<int>();
        string[] possibleRegions = usedCmd.ToUpperInvariant().Split();
        foreach (string aRegion in possibleRegions)
        {
            if (possiblePresses.Contains(aRegion))
                pressesToMake.Add(possiblePresses.IndexOf(aRegion));
            else
            {
                yield return string.Format("sendtochaterror The diagram does not have a region \"{0}\"", aRegion);
                yield break;
            }
        }
        var hasStruck = false;
        for (int x = 0; x < pressesToMake.Count && !hasStruck && calculatedSolution; x++)
        {
            yield return null;
            if (!(goalPressIdxes.Contains(pressesToMake[x]) || currentPressIdxes.Contains(pressesToMake[x])))
                hasStruck = true;
            buttonSelectables[pressesToMake[x]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
