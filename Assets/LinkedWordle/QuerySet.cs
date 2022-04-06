using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuerySet : MonoBehaviour {
    public MeshRenderer[] statusRenderers;
    public TextMesh[] displayTexts;
    public int[] resultingQueryIdxStatus;
    public Color[] responseColors;
    public void UpdateStatus(char[] letters, params int[] newResult)
    {
        resultingQueryIdxStatus = newResult;
        for (var x = 0; x < statusRenderers.Length; x++)
        {
            var curResultStatus = x < resultingQueryIdxStatus.Length ? resultingQueryIdxStatus[x] : -1;
            statusRenderers[x].material.color = curResultStatus < 0 ? Color.black : responseColors[curResultStatus];
        }
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
