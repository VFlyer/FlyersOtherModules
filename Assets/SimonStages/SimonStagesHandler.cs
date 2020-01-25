using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimonStagesHandler : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;

    public LightInformation[] lightDevices;
    public IndicatorInformation[] indicatorLights;
    public TextMesh indicatorText;
    public Color[] lightDeviceColorOptions;
    public Material[] lightBaseOptions;
    public string[] lightTextOptions;
    public string[] lightNameOptions;
    public AudioClip[] sfxOptions;
    private List<int> chosenIndices = new List<int>();
    private List<int> chosenIndices2 = new List<int>();

    public int currentLevel = 0;

    public List<int> sequences = new List<int>();
    public List<string> solutionNames = new List<string>();
    public List<int> sequenceLengths = new List<int>();
    public List<int> solveLengths = new List<int>();
    public List<int> startLocation = new List<int>();
    public List<int> solutionStartLocation = new List<int>();
    private int lastStartPosition = 0;
    private int lastSolutionLocation = 0;
    public List<int> currentSequence = new List<int>();
    public List<string> currentSequenceNames = new List<string>();
    public List<int> currentSolution = new List<int>();
    public List<string> currentSolutionNames = new List<string>();
    public List<int> indicatorColour = new List<int>();
    public List<string> indicatorLetter = new List<string>();
    private int indicator = 0;
    string result = "";

    public List<int> clearLights = new List<int>();
    public List<int> completeLights = new List<int>();
    public List<int> absoluteLevelPosition = new List<int>();

    private int totalPresses = 0;
    private int stagePresses = 0;
    public int increaser = 0;
    public List<bool> lightsSolved = new List<bool>();
    public List<bool> stagesSolved = new List<bool>();

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;
    private bool moduleLocked = true;
    private bool reverse = false;
    private bool gameOn = false;
    private bool secondAttempt;
    private bool checking;
    private bool canPlaySound = false;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (LightInformation button in lightDevices)
        {
            LightInformation pressedButton = button;
            button.connectedButton.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
        }
    }
    void GenerateSequence()
    {
        absoluteLevelPosition.Add(absoluteLevelPosition.Last() + 1);
        currentSequence.Clear();
        currentSequenceNames.Clear();
        stagesSolved.Add(false);
        startLocation.Add(lastStartPosition);
        int sequenceLength = Random.Range(3, 6);
        sequenceLengths.Add(sequenceLength);
        for (int i = 0; i < sequenceLength; i++)
        {
            int sequenceColour = Random.Range(0, 10);
            sequences.Add(sequenceColour);
            currentSequence.Add(sequenceColour);
            currentSequenceNames.Add(lightDevices[sequenceColour].colorName);
        }
        currentLevel++;
        indicator = Random.Range(0, 10);
        indicatorColour.Add(indicator);
        lastStartPosition = sequenceLength + startLocation[currentLevel - 1];
        Debug.LogFormat("[Simon Stages #{0}]", moduleId, currentLevel);
        Debug.LogFormat("[Simon Stages #{0}] Sequence #{1}: {2}.", moduleId, currentLevel, string.Join(", ", currentSequenceNames.Select((x) => x).ToArray()));
        Debug.LogFormat("[Simon Stages #{0}] Indicator #{1}: {2}.", moduleId, currentLevel, indicatorLights[indicator].colorName);
        CalculateSolution();
    }

    void CalculateSolution()
    {
        if (indicatorLights[indicator].colorName == "red")
        {
            for (int i = 0; i < currentSequence.Count; i++)
            {
                solutionNames.Add(lightDevices[currentSequence[i]].colorName);
                currentSolution.Add(currentSequence[i]);
                currentSolutionNames.Add(lightDevices[currentSequence[i]].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "blue")
        {
            for (int i = currentSequence.Count - 1; i >= 0; i--)
            {
                solutionNames.Add(lightDevices[currentSequence[i]].colorName);
                currentSolution.Add(currentSequence[i]);
                currentSolutionNames.Add(lightDevices[currentSequence[i]].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "yellow")
        {
            for (int i = 0; i < 2; i++)
            {
                solutionNames.Add(lightDevices[currentSequence[i]].colorName);
                currentSolution.Add(currentSequence[i]);
                currentSolutionNames.Add(lightDevices[currentSequence[i]].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "orange")
        {
            for (int i = 1; i >= 0; i--)
            {
                solutionNames.Add(lightDevices[currentSequence[i]].colorName);
                currentSolution.Add(currentSequence[i]);
                currentSolutionNames.Add(lightDevices[currentSequence[i]].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "magenta")
        {
            for (int i = currentSequence.Count - 2; i < currentSequence.Count; i++)
            {
                solutionNames.Add(lightDevices[currentSequence[i]].colorName);
                currentSolution.Add(currentSequence[i]);
                currentSolutionNames.Add(lightDevices[currentSequence[i]].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "green")
        {
            for (int i = currentSequence.Count - 1; i >= (currentSequence.Count - 2); i--)
            {
                solutionNames.Add(lightDevices[currentSequence[i]].colorName);
                currentSolution.Add(currentSequence[i]);
                currentSolutionNames.Add(lightDevices[currentSequence[i]].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "pink")
        {
            for (int i = 0; i < currentSequence.Count; i++)
            {
                solutionNames.Add(lightDevices[(5 + currentSequence[i]) % 10].colorName);
                currentSolution.Add(lightDevices[(5 + currentSequence[i]) % 10].colorIndex);
                currentSolutionNames.Add(lightDevices[(5 + currentSequence[i]) % 10].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "lime")
        {
            for (int i = currentSequence.Count - 1; i >= 0; i--)
            {
                solutionNames.Add(lightDevices[(5 + currentSequence[i]) % 10].colorName);
                currentSolution.Add(lightDevices[(5 + currentSequence[i]) % 10].colorIndex);
                currentSolutionNames.Add(lightDevices[(5 + currentSequence[i]) % 10].colorName);
            }
        }
        else if (indicatorLights[indicator].colorName == "cyan")
        {
            int last = currentSequence.Count - 1;
            solutionNames.Add(lightDevices[(5 + currentSequence[0]) % 10].colorName);
            solutionNames.Add(lightDevices[(5 + currentSequence[last]) % 10].colorName);
            currentSolution.Add(lightDevices[(5 + currentSequence[0]) % 10].colorIndex);
            currentSolution.Add(lightDevices[(5 + currentSequence[last]) % 10].colorIndex);
            currentSolutionNames.Add(lightDevices[(5 + currentSequence[0]) % 10].colorName);
            currentSolutionNames.Add(lightDevices[(5 + currentSequence[last]) % 10].colorName);
        }
        else if (indicatorLights[indicator].colorName == "white")
        {
            solutionNames.Add(lightDevices[(5 + currentSequence[2]) % 10].colorName);
            solutionNames.Add(lightDevices[(5 + currentSequence[1]) % 10].colorName);
            currentSolution.Add(lightDevices[(5 + currentSequence[2]) % 10].colorIndex);
            currentSolution.Add(lightDevices[(5 + currentSequence[1]) % 10].colorIndex);
            currentSolutionNames.Add(lightDevices[(5 + currentSequence[2]) % 10].colorName);
            currentSolutionNames.Add(lightDevices[(5 + currentSequence[1]) % 10].colorName);
        }
        solutionStartLocation.Add(lastSolutionLocation);
        lastSolutionLocation = currentSolutionNames.Count + lastSolutionLocation;
        solveLengths.Add(currentSolution.Count);
        for (int i = 0; i < currentSolution.Count; i++)
        {
            lightsSolved.Add(false);
        }
        Debug.LogFormat("[Simon Stages #{0}] Solution #{1}: {2}.", moduleId, currentLevel, string.Join(", ", currentSolutionNames.Select((x) => x).ToArray()));
        currentSolution.Clear();
        currentSolutionNames.Clear();
        indicatorLetter.Add(lightTextOptions[indicatorLights[indicatorColour[currentLevel - 1]].colorIndex]);
    }

    void Start()
    {
        absoluteLevelPosition.Add(0);
        indicatorText.text = "";
        moduleLocked = true;
        foreach (LightInformation device in lightDevices)
        {
            device.colorIndex = Random.Range(0, 10);
            while (chosenIndices.Contains(device.colorIndex))
            {
                device.colorIndex = Random.Range(0, 10);
            }
            chosenIndices.Add(device.colorIndex);

            device.soundIndex = Random.Range(0, 10);
            while (chosenIndices2.Contains(device.soundIndex))
            {
                device.soundIndex = Random.Range(0, 10);
            }
            chosenIndices2.Add(device.soundIndex);

            float scalar = transform.lossyScale.x;
            device.ledGlow.range *= scalar;
            device.ledGlow.color = lightDeviceColorOptions[device.colorIndex];
            device.colorBase.material = lightBaseOptions[device.colorIndex];
            device.lightText.text = lightTextOptions[device.colorIndex];
            device.colorName = lightNameOptions[device.colorIndex];
            device.connectedSound = sfxOptions[device.soundIndex];
            device.ledGlow.enabled = false;
        }

        Debug.LogFormat("[Simon Stages #{0}] The arrangement of colors is: {1} // {2}", moduleId,
            string.Join(", ", lightDevices.Take(5).Select(ld => ld.colorName).ToArray()),
            string.Join(", ", lightDevices.Skip(5).Select(ld => ld.colorName).ToArray()));

        chosenIndices.Clear();
        chosenIndices2.Clear();

        foreach (IndicatorInformation indic in indicatorLights)
        {
            indic.colorIndex = Random.Range(0, 10);
            while (chosenIndices.Contains(indic.colorIndex))
            {
                indic.colorIndex = Random.Range(0, 10);
            }
            chosenIndices.Add(indic.colorIndex);

            float scalar = transform.lossyScale.x;
            indic.glow.range *= scalar;
            indic.glow.color = lightDeviceColorOptions[indic.colorIndex];
            indic.colorName = lightNameOptions[indic.colorIndex];
            indic.glow.enabled = false;
        }
        chosenIndices.Clear();
        StartCoroutine(StartFlash());
    }

    IEnumerator StartFlash()
    {
        Audio.PlaySoundAtTransform("scaryRiff", transform);
        int index = 0;
        int iterations = 0;
        int currentIdx = 0;
        while (iterations < 5)
        {
            lightDevices[9 - index].greyBase.enabled = false;
            lightDevices[9 - index].ledGlow.enabled = true;
            lightDevices[index].greyBase.enabled = false;
            lightDevices[index].ledGlow.enabled = true;
            indicatorLights[currentIdx].glow.enabled = true;
            indicatorText.text = lightTextOptions[indicatorLights[currentIdx].colorIndex];
            yield return new WaitForSeconds(0.05f);
            lightDevices[9 - index].greyBase.enabled = true;
            lightDevices[9 - index].ledGlow.enabled = false;
            lightDevices[index].greyBase.enabled = true;
            lightDevices[index].ledGlow.enabled = false;
            indicatorLights[currentIdx].glow.enabled = false;
            yield return new WaitForSeconds(0.025f);
            if (index < 10 && !reverse)
            {
                index++;
            }
            if (index == 5)
            {
                reverse = true;
                index = 4;
            }

            if (index >= 0 && reverse)
            {
                index--;
            }
            if (index < 0 && reverse)
            {
                reverse = false;
                iterations++;
                index = 1;
            }
            currentIdx = (currentIdx+1)%10;
        }
        indicatorText.text = "";
        int counter = 0;
        while (!gameOn)
        {
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = false;
                lightDevices[i].ledGlow.enabled = true;
                indicatorLights[i].glow.enabled = true;
            }
            yield return new WaitForSeconds(0.05f);
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = true;
                lightDevices[i].ledGlow.enabled = false;
                indicatorLights[i].glow.enabled = false;
            }
            yield return new WaitForSeconds(0.05f);
            counter++;
            if (counter >= 30)
            {
                moduleLocked = false;
                gameOn = true;
                GenerateSequence();
            }
        }
    }

    public void ButtonPress(LightInformation device)
    {
        if (moduleSolved || moduleLocked || !gameOn || checking)
        {
            return;
        }
        StopCoroutine(RepeatSequence());
        canPlaySound = true;
        moduleLocked = true;
        if (totalPresses == 0)
            Debug.LogFormat("[Simon Stages #{0}] SEQUENCE {1} RESPONSE:", moduleId, absoluteLevelPosition[0] + 1, result);
        Audio.PlaySoundAtTransform(device.connectedSound.name, transform);
        StartCoroutine(PressFlash(device));

        if (device.colorName == solutionNames[totalPresses])
        {
            lightsSolved[totalPresses] = true;
            clearLights.Add(totalPresses);
            Debug.LogFormat("[Simon Stages #{0}] You pressed {1}. That is correct.", moduleId, device.colorName);
        }
        else
        {
            clearLights.Clear();
            lightsSolved[totalPresses] = false;
            Debug.LogFormat("[Simon Stages #{0}] You pressed {1}. That is incorrect.", moduleId, device.colorName);
        }
        stagePresses++;
        totalPresses++;
        if (stagePresses == solveLengths[increaser])
        {
            device.connectedButton.AddInteractionPunch();
            stagePresses = 0;
            for (int i = solutionStartLocation[increaser]; i <= solutionStartLocation[increaser] + solveLengths[increaser] - 1; i++)
            {
                if (!lightsSolved[i])
                {
                    stagesSolved[increaser] = false;
                    break;
                }
                else
                {
                    stagesSolved[increaser] = true;
                }
            }
            if (stagesSolved[increaser])
            {
                result = "correct";
                for (int i = 0; i < clearLights.Count; i++)
                {
                    completeLights.Add(clearLights[i]);
                }
            }
            else
            {
                result = "incorrect";
            }
            clearLights.Clear();
            Debug.LogFormat("[Simon Stages #{0}] END OF SEQUENCE {1}. The given sequence of inputs are {2}.", moduleId, absoluteLevelPosition[increaser] + 1, result);
            increaser++;
            if (totalPresses < solutionNames.Count)
            {
                Debug.LogFormat("[Simon Stages #{0}] SEQUENCE {1} RESPONSE:", moduleId, absoluteLevelPosition[increaser] + 1, result);
            }
        }
        else
        {
            device.connectedButton.AddInteractionPunch(0.25f);
        }

        if (totalPresses >= solutionNames.Count)
        {
            moduleLocked = true;
            checking = true;
            CheckEndGame();
        }
    }
    void CheckEndGame()
    {
        for (int i = 0; i < stagesSolved.Count; i++)
        {
            if (!stagesSolved[i])
            {
                secondAttempt = true;
                break;
            }
            else
            {
                secondAttempt = false;
            }
        }

        int endGameCheck = stagesSolved.Count;
        if (endGameCheck == 1)
        {
            if (stagesSolved[0])
            {
                secondAttempt = false;
            }
        }

        if (!secondAttempt)
        {
            if (currentLevel >= 5) // If the defuser has successfully done 5 stages on the module.
            {
                GetComponent<KMBombModule>().HandlePass();
                Debug.LogFormat("[Simon Stages #{0}] Inputs correct. Module disarmed.", moduleId);
                moduleSolved = true;
                StartCoroutine(SolveLights());
            }
            else
            {
                Debug.LogFormat("[Simon Stages #{0}] Inputs correct. However the module wants more.", moduleId);
                totalPresses = 0;
                stagePresses = 0;
                increaser = 0;
                isRepeating = false;
                GenerateSequence();
                moduleLocked = true;
                clearLights.Clear();
            }
        }
        else
        {
            totalPresses = 0;
            stagePresses = 0;
            increaser = 0;
            Debug.LogFormat("[Simon Stages #{0}] Strike! Your full sequence was incorrect.", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        checking = false;
    }
    bool isRepeating = false;
    IEnumerator RepeatSequence()
    {
        isRepeating = true;
        moduleLocked = true;
        yield return new WaitForSeconds(2f);
        foreach (LightInformation device in lightDevices)
        {
            device.ledGlow.enabled = false;
            device.greyBase.enabled = true;
        }
        int lastLevel = currentLevel;
        int j = 0;
        int k = 0;
        for (int i = 0; i < sequences.Count; i++)
        {
                
            indicatorText.text = indicatorLetter[j];
            indicatorLights[indicatorColour[j]].glow.enabled = true;
            if (canPlaySound)
            {
                Audio.PlaySoundAtTransform(lightDevices[sequences[i]].connectedSound.name, transform);
            }
            if (totalPresses > 0)
            {
                break;
            }
            lightDevices[sequences[i]].ledGlow.enabled = true;
            lightDevices[sequences[i]].greyBase.enabled = false;
            yield return new WaitForSeconds(0.5f);
            if (totalPresses > 0)
            {
                break;
            }
            lightDevices[sequences[i]].ledGlow.enabled = false;
            lightDevices[sequences[i]].greyBase.enabled = true;
            if (totalPresses > 0)
            {
                break;
            }
            yield return new WaitForSeconds(0.25f);
            if (sequenceLengths[j] - 1 == k)
            {
                j++;
                k = 0;
                foreach (IndicatorInformation indic in indicatorLights)
                {
                    indic.glow.enabled = false;
                }
            }
            else
            {
                k++;
            }
        }
        foreach (LightInformation device in lightDevices)
        {
            device.ledGlow.enabled = false;
            device.greyBase.enabled = true;
        }
        indicatorText.text = "";
        j = 0;
        foreach (IndicatorInformation indic in indicatorLights)
        {
            indic.glow.enabled = false;
        }
        moduleLocked = false;
        yield return new WaitForSeconds(5f);
        if (totalPresses <= 0 && isRepeating && lastLevel == currentLevel)
        {
            isRepeating = false;
        }
    }

    void Update()
    {
        if (totalPresses <= 0 && !isRepeating && gameOn && !moduleLocked)
            StartCoroutine(RepeatSequence());
    }

    IEnumerator PressFlash(LightInformation device)
    {
        device.greyBase.enabled = false;
        device.ledGlow.enabled = true;
        yield return new WaitForSeconds(0.4f);
        device.greyBase.enabled = true;
        device.ledGlow.enabled = false;
        moduleLocked = false;
    }

    IEnumerator SolveLights()
    {
        Audio.PlaySoundAtTransform("solveRiff", transform);
        int solveCounter = 0;
        while (solveCounter < 2)
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = false;
                lightDevices[i].ledGlow.enabled = true;
                indicatorLights[i].glow.enabled = true;
            }
            yield return new WaitForSeconds(1f);
            for (int i = 0; i <= 9; i++)
            {
                lightDevices[i].greyBase.enabled = true;
                lightDevices[i].ledGlow.enabled = false;
                indicatorLights[i].glow.enabled = false;
            }
            solveCounter++;
        }
        yield return new WaitForSeconds(1f);
        for (int i = 0; i <= 9; i++)
        {
            lightDevices[i].greyBase.enabled = false;
            lightDevices[i].ledGlow.enabled = true;
            indicatorLights[i].glow.enabled = true;
        }
    }
}
