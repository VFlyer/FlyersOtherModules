using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class QuerySetUI : MonoBehaviour {
    public Image[] statusRenderers;
    public Text[] displayTexts;
    public int[] resultingQueryIdxStatus;
    public Color[] responseColors;
    public void UpdateStatus(char[] letters, params int[] newResult)
    {
        resultingQueryIdxStatus = newResult;
        for (var x = 0; x < statusRenderers.Length; x++)
        {
            var curResultStatus = x < resultingQueryIdxStatus.Length && resultingQueryIdxStatus != null ? resultingQueryIdxStatus[x] : -1;
            statusRenderers[x].color = curResultStatus < 0 ? Color.white : responseColors[curResultStatus];
            statusRenderers[x].fillCenter = curResultStatus >= 0;
        }
        for (var x = 0; x < displayTexts.Length; x++)
        {
            displayTexts[x].text = x >= letters.Length ? "" : letters[x].ToString();
        }
    }
    public void UpdateResult(params int[] newResult)
    {
        resultingQueryIdxStatus = newResult;
        for (var x = 0; x < statusRenderers.Length; x++)
        {
            var curResultStatus = x < resultingQueryIdxStatus.Length && resultingQueryIdxStatus != null ? resultingQueryIdxStatus[x] : -1;
            statusRenderers[x].color = curResultStatus < 0 ? Color.white : responseColors[curResultStatus];
            statusRenderers[x].fillCenter = curResultStatus >= 0;
        }
    }
    public void UpdateText(char[] letters)
    {
        for (var x = 0; x < displayTexts.Length; x++)
        {
            displayTexts[x].text = x >= letters.Length ? "" : letters[x].ToString();
        }
    }
    public void UpdateStatus(string letters = "", params int[] newResult)
    {
        UpdateStatus(letters.ToCharArray(), newResult);
    }
}
