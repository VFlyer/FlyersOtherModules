using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;

public class VennDiagramCore : MonoBehaviour
{

    public KMSelectable[] diagramSelectables;
    public MeshRenderer[] diagramRenderers;
    public MeshRenderer textRenderer;
    public TextMesh statementRenderer;

    public KMBombModule modSelf;
    public KMAudio mAudio;

    string[] possibleBoolStates = { "O", "A", "B", "AB", "C", "AC", "BC", "ABC" };
    List<string> correctBoolStates = new List<string>(), pressedBoolStates = new List<string>();
    bool interactable;
    string toDisplay;

    private static int modIdCnt = 1;
    int modID;
    // Use this for initialization
    void Start()
    {
        modID = modIdCnt++;
        for (int x = 0; x < diagramSelectables.Length; x++)
        {
            var y = x;
            diagramSelectables[x].OnInteract += delegate
            {
                mAudio.PlaySoundAtTransform("tick", diagramSelectables[y].transform);
                diagramSelectables[y].AddInteractionPunch();
                if (interactable)
                    CheckCorrectStates(possibleBoolStates[y]);
                return false;
            };
        }
        modSelf.OnActivate += delegate
        {
            interactable = true;
            statementRenderer.text = toDisplay;
            StartCoroutine(HandleGlitchText());
        };
        GenerateCorrectStates();
        statementRenderer.text = string.Empty;
    }

    IEnumerator HandleGlitchText()
    {
        if (textRenderer.material.HasProperty("_MainTex"))
        {
            for (float x = 0; x <= 1; x = Mathf.Min(x + Time.deltaTime, 1f))
            {
                yield return null;
                textRenderer.material.SetTextureOffset("_MainTex", uernd.insideUnitCircle * (1 - x));
                if (x >= 1f) break;
            }
            textRenderer.material.SetTextureOffset("_MainTex", Vector2.zero);
        }
    }

    void GenerateCorrectStates()
    {
        char[] LToRLetters = new[] { 'A', 'B', 'C' }.Shuffle();
        bool[] LToRInvert = new bool[3];
        byte[] operatorIdx = new byte[2];

        // Assign if the parantheses "()" should be on the left pair or the the right pair.
        bool paranthesesOnLeft = uernd.value < 0.5f;

        // Assign if the value needs to be a complement to the previous,
        for (int x = 0; x < LToRInvert.Length; x++)
            LToRInvert[x] = uernd.value < 0.5f;

        // Assign the idxs that are going to be used in the operators.
        for (int x = 0; x < operatorIdx.Length; x++)
            operatorIdx[x] = (byte)uernd.Range(0, 3);

        string[][] startingStates = new string[3][];
        Debug.LogFormat("<Venn Diagrams #{0}> All starting states: ", modID);
        for (int x = 0; x < startingStates.Length; x++)
        {
            startingStates[x] = possibleBoolStates.Where(a => LToRInvert[x] ^ a.Contains(LToRLetters[x])).ToArray();
            Debug.LogFormat("<Venn Diagrams #{0}> [{1}]", modID, startingStates[x].Join(", "));
        }

        if (paranthesesOnLeft)
        {
            string[] operatedLeftSet = startingStates[0];
            for (int x = 0; x < operatorIdx.Length; x++)
            {
                switch (operatorIdx[x])
                {
                    case 0: // Union
                        operatedLeftSet = operatedLeftSet.Union(startingStates[x + 1]).ToArray();
                        break;
                    case 1: // Intersect
                        operatedLeftSet = operatedLeftSet.Intersect(startingStates[x + 1]).ToArray();
                        break;
                    case 2: // Relative Complement
                        operatedLeftSet = operatedLeftSet.Where(a => !startingStates[x + 1].Contains(a)).ToArray();
                        break;
                    default:
                        break;
                }
            }

            string statementToDisplay = string.Format("({0}{5} {1} {2}{6}) {3} {4}{7}", LToRLetters[0], "∪∩\\"[operatorIdx[0]], LToRLetters[1], "∪∩\\"[operatorIdx[1]], LToRLetters[2], LToRInvert[0] ? "'" : "", LToRInvert[1] ? "'" : "", LToRInvert[2] ? "'" : "");

            Debug.LogFormat("[Venn Diagrams #{0}] Statement Displayed: {1}", modID, statementToDisplay);

            toDisplay = statementToDisplay;

            correctBoolStates = operatedLeftSet.Distinct().ToList();
        }
        else
        {
            string[] operatedRightSet = startingStates[2];
            for (int x = operatorIdx.Length - 1; x >= 0; x--)
            {
                switch (operatorIdx[x])
                {
                    case 0: // Union
                        operatedRightSet = startingStates[x].Union(operatedRightSet).ToArray();
                        break;
                    case 1: // Intersect
                        operatedRightSet = startingStates[x].Intersect(operatedRightSet).ToArray();
                        break;
                    case 2: // Relative Complement
                        operatedRightSet = startingStates[x].Where(a => !operatedRightSet.Contains(a)).ToArray();
                        break;
                    default:
                        break;
                }
            }

            string statementToDisplay = string.Format("{0}{5} {1} ({2}{6} {3} {4}{7})", LToRLetters[0], "∪∩\\"[operatorIdx[0]], LToRLetters[1], "∪∩\\"[operatorIdx[1]], LToRLetters[2], LToRInvert[0] ? "'" : "", LToRInvert[1] ? "'" : "", LToRInvert[2] ? "'" : "");

            Debug.LogFormat("[Venn Diagrams #{0}] Statement Displayed: {1}", modID, statementToDisplay);

            toDisplay = statementToDisplay;

            correctBoolStates = operatedRightSet.Distinct().ToList();
        }
        Debug.LogFormat("[Venn Diagrams #{0}] Final Button States: ", modID);
        foreach (string boolState in possibleBoolStates)
        {
            Debug.LogFormat("[Venn Diagrams #{0}] {1}: {2}", modID, boolState, correctBoolStates.Contains(boolState) ? "PRESS" : "DO NOT PRESS");
        }
    }

    void CheckCorrectStates(string givenValue)
    {
        if (!correctBoolStates.Any())
        {
            Debug.LogFormat("[Venn Diagrams #{0}] Failsafe triggered! Solving module and negating other checks.", modID);
            modSelf.HandlePass();
            pressedBoolStates = possibleBoolStates.ToList();
            interactable = false;
            return;
        }
        if (!pressedBoolStates.Contains(givenValue))
        {
            pressedBoolStates.Add(givenValue);
            if (correctBoolStates.Contains(givenValue))
            {
                Debug.LogFormat("[Venn Diagrams #{0}] Correctly selected region {1}", modID, givenValue);
                if (pressedBoolStates.Where(a => correctBoolStates.Contains(a)).OrderBy(a => a).SequenceEqual(correctBoolStates.OrderBy(a => a)))
                {
                    Debug.LogFormat("[Venn Diagrams #{0}] That's all of them! Module solved.", modID);
                    modSelf.HandlePass();
                    pressedBoolStates = possibleBoolStates.ToList();
                    interactable = false;
                }
            }
            else
            {
                Debug.LogFormat("[Venn Diagrams #{0}] Strike! Incorrectly selected region {1}", modID, givenValue);
                modSelf.HandleStrike();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int x = 0; x < diagramRenderers.Length; x++)
        {
            if (pressedBoolStates.Contains(possibleBoolStates[x]))
            {
                diagramRenderers[x].enabled = true;
                diagramRenderers[x].material.color = correctBoolStates.Contains(possibleBoolStates[x]) ? new Color(0.5f, 1f, 0.5f) : new Color(1, 0.5f, 0.5f);
            }
            else
            {
                diagramRenderers[x].enabled = false;
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        List<string> remainingCorrectPresses = correctBoolStates.Where(a => !pressedBoolStates.Contains(a)).ToList();

        Debug.LogFormat("[Venn Diagrams #{0}] Requesting force solve viva TP Handler.", modID);
        if (interactable)
        {
            if (!remainingCorrectPresses.Any())
                diagramSelectables.PickRandom().OnInteract();
            else
                foreach (string valuePress in remainingCorrectPresses)
                {
                    int idx = Array.IndexOf(possibleBoolStates, valuePress);
                    if (idx != -1)
                    {
                        diagramSelectables[idx].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
        }
    }
    readonly string TwitchHelpMessage = "\"!{0} o AB BC ABC c\" to select that given region. \"press\" is optional. Possible regions to select are O, A, B, C, AB, AC, BC, ABC";

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (!interactable)
        {
            yield return "sendtochaterror The module is not allowing inputs to process. Chances are, the module is already solved or the module has not yet started.";
            yield break;
        }

        if (command.ToLower().StartsWith("press"))
            command = command.Substring(5).Trim();
        List<KMSelectable> pressesToMake = new List<KMSelectable>();
        string[] possibleRegions = command.ToUpperInvariant().Split();
        foreach (string aRegion in possibleRegions)
        {
            if (possibleBoolStates.Contains(aRegion))
                pressesToMake.Add(diagramSelectables[Array.IndexOf(possibleBoolStates, aRegion)]);
            else
            {
                yield return string.Format("sendtochaterror The diagram does not have a region \"{0}\"",aRegion);
                yield break;
            }
        }
        var hasStruck = false;
        for (int x = 0; x < pressesToMake.Count && !hasStruck && interactable; x++)
        {
            yield return null;
            if (!(correctBoolStates.Contains(possibleRegions[x]) || pressedBoolStates.Contains(possibleRegions[x])))
                hasStruck = true;
            pressesToMake[x].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

}
