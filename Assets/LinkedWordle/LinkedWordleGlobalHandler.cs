using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class LinkedWordle
{
    public class LinkedWordleGlobalHandler
    {
        public List<LinkedWordle> wordlesAll;
        public KMBomb referenceBomb;
        public string curWordQuery;
        public int maxQueriesAllowed = 5;
        int failedAttempts;
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public LinkedWordleGlobalHandler()
        {
            wordlesAll = new List<LinkedWordle>();
            curWordQuery = "";
        }
        public bool CheckIfAllPresent()
        {
            var allLinkedWordles = referenceBomb.GetComponentsInChildren<LinkedWordle>();
            return wordlesAll.Count == allLinkedWordles.Length;
        }
        public void ResetUnsolvedWordles()
        {
            var unsolvedWordles = wordlesAll.Where(a => !a.modSolved);
            maxQueriesAllowed = 5 + unsolvedWordles.Count();
            var shuffledWordBank = Data.GenerousWordList.ToArray().Shuffle();
            var containsDupe = false;
            for (var x = 0; x < unsolvedWordles.Count(); x++)
            {
                var curUnsolvedWordle = unsolvedWordles.ElementAt(x);
                curUnsolvedWordle.allResponses.Clear();
                curUnsolvedWordle.allWordQueries.Clear();
                var nextIdx = x % shuffledWordBank.Length;
                if (x >= shuffledWordBank.Length && x % shuffledWordBank.Length == 0)
                {
                    shuffledWordBank.Shuffle();
                    if (!containsDupe)
                    {
                        foreach (LinkedWordle wordleMod in unsolvedWordles)
                            wordleMod.QuickLog("Using duplicate words at this point. This is a result of using more Linked Wordles than the word bank provides.");
                    }
                    containsDupe = true;
                }
                for (var u = 0; u < curUnsolvedWordle.allQueryVisuals.Length; u++)
                {
                    curUnsolvedWordle.allQueryVisuals[u].UpdateStatus();
                }
                curUnsolvedWordle.selectedCorrectWord = shuffledWordBank[nextIdx];
                curUnsolvedWordle.positionedIdxInput = 0;
                curUnsolvedWordle.QuickLog("Selected correct word for this instance: {0}", curUnsolvedWordle.selectedCorrectWord);
                curUnsolvedWordle.QuickLog("Maxmium guesses allowed before failure: {0}", maxQueriesAllowed);
            }
            for (var x = 0; x < unsolvedWordles.Count(); x++)
            {
                unsolvedWordles.ElementAt(x).disableImmediateSolve |= containsDupe;
                unsolvedWordles.ElementAt(x).allowInteractions = true;
            }
        }
        public void StartGlobalHandling()
        {
            if (!CheckIfAllPresent()) return;
            ResetUnsolvedWordles();
        }
        public void HandleGlobalInput(KeyIDx selectedKey)
        {
            switch (selectedKey)
            {
                case KeyIDx.KeyBack:
                    if (curWordQuery.Any())
                        curWordQuery = curWordQuery.Substring(0, curWordQuery.Length - 1);
                    UpdateRenderersUnsolved();
                    break;
                case KeyIDx.KeySub:
                    if (curWordQuery.Length == 5)
                        HandleGlobalQuery();
                    break;
                case KeyIDx.KeyUnk:
                    break;
                default:
                    //Debug.Log((int)selectedKey);
                    if (curWordQuery.Length < 5)
                        curWordQuery += alphabet[(int)selectedKey];
                    UpdateRenderersUnsolved();
                    break;
            }
        }
        protected void HandleGlobalQuery()
        {
            if (!(Data.ObscureWordList.Contains(curWordQuery) || Data.GenerousWordList.Contains(curWordQuery)))
            {
                for (var x = 0; x < wordlesAll.Count; x++)
                {
                    var curWordle = wordlesAll[x];
                    if (curWordle.modSolved) continue;
                    curWordle.HandleInvalidWord();
                }
                return;
            }
            for (var x = 0; x < wordlesAll.Count; x++)
            {
                var curWordle = wordlesAll[x];
                if (curWordle.modSolved) continue;
                curWordle.HandleQuery(curWordQuery);
            }
            curWordQuery = "";
            var unsolvedWordles = wordlesAll.Where(a => !a.modSolved);
            if (unsolvedWordles.Any() && unsolvedWordles.All(a => a.allWordQueries.Count >= maxQueriesAllowed))
            {
                unsolvedWordles.First().StartCoroutine(HandleDelayedReset());
            }
        }
        public void UpdateRenderersUnsolved()
        {
            for (var x = 0; x < wordlesAll.Count; x++)
            {
                var curWordle = wordlesAll[x];
                if (curWordle.modSolved) continue;
                if (curWordle.allWordQueries.Count > 5)
                {
                    curWordle.curIDxQueryFirstVisible = curWordle.allWordQueries.Count - 5;
                    for (var u = 0; u < curWordle.allQueryVisuals.Length - 1; u++)
                    {
                        curWordle.allQueryVisuals[u].UpdateStatus(curWordle.allWordQueries[u + curWordle.curIDxQueryFirstVisible], curWordle.allResponses[u + curWordle.curIDxQueryFirstVisible]);
                    }
                }
                curWordle.allQueryVisuals[curWordle.positionedIdxInput].UpdateStatus(curWordQuery);
                for (var a = 0; a < curWordle.allQueryVisuals[curWordle.positionedIdxInput].displayTexts.Length; a++)
                    curWordle.allQueryVisuals[curWordle.positionedIdxInput].displayTexts[a].color = Color.white;
            }
        }
        IEnumerator HandleDelayedReset()
        {
            var unsolvedWordles = wordlesAll.Where(a => !a.modSolved);
            for (var u = 0; u < unsolvedWordles.Count(); u++)
            {
                var curUnsolvedWordle = unsolvedWordles.ElementAt(u);
                curUnsolvedWordle.allowInteractions = false;
                var allQueryVisuals = curUnsolvedWordle.allQueryVisuals;
                for (var x = 0; x < curUnsolvedWordle.allQueryVisuals.Length; x++)
                {
                    var curIDxSeeResult = curUnsolvedWordle.allWordQueries.Count - curUnsolvedWordle.allQueryVisuals.Length + x;
                    curUnsolvedWordle.allQueryVisuals[x].UpdateStatus(curUnsolvedWordle.allWordQueries[curIDxSeeResult], curUnsolvedWordle.allResponses[curIDxSeeResult]);
                }
            }
            yield return null;
            var timeCooldown = 30f + 15 * failedAttempts;
            for (float t = 0f; t < 1f; t += Time.deltaTime * 2)
            {
                for (var u = 0; u < unsolvedWordles.Count(); u++)
                {
                    var curUnsolvedWordle = unsolvedWordles.ElementAt(u);
                    curUnsolvedWordle.overlayTextMesh.text = string.Format("\nGAME OVER\nThe word was\n{0}\n{1}", curUnsolvedWordle.selectedCorrectWord, string.Format("{0}:{1}", Mathf.FloorToInt(timeCooldown / 60).ToString("0"), (timeCooldown % 60).ToString("00")));
                    curUnsolvedWordle.overlayRenderer.material.color = Color.black * t * 0.5f;
                    curUnsolvedWordle.overlayTextMesh.color = new Color(1f, 1f, 1f, t);
                }
                yield return null;
            }
            for (var u = 0; u < unsolvedWordles.Count(); u++)
            {
                var curUnsolvedWordle = unsolvedWordles.ElementAt(u);
                curUnsolvedWordle.overlayRenderer.material.color = Color.black * 0.5f;
                curUnsolvedWordle.overlayTextMesh.color = Color.white;
            }
            for (float t = timeCooldown; t >= 0f; t -= Time.deltaTime)
            {
                for (var u = 0; u < unsolvedWordles.Count(); u++)
                {
                    var curUnsolvedWordle = unsolvedWordles.ElementAt(u);
                    curUnsolvedWordle.overlayTextMesh.text = string.Format("\nGAME OVER\nThe word was\n{0}\n{1}", curUnsolvedWordle.selectedCorrectWord, string.Format("{0}:{1}", Mathf.FloorToInt(t / 60).ToString("0"), (t % 60).ToString("00")));
                }
                yield return null;
            }
            failedAttempts++;
            ResetUnsolvedWordles();
            for (float t = 1f; t > 0f; t -= Time.deltaTime * 2)
            {
                for (var u = 0; u < unsolvedWordles.Count(); u++)
                {
                    var curUnsolvedWordle = unsolvedWordles.ElementAt(u);
                    curUnsolvedWordle.overlayRenderer.material.color = Color.black * t * 0.5f;
                    curUnsolvedWordle.overlayTextMesh.color = new Color(1f, 1f, 1f, t);
                }
                yield return null;
            }
            for (var u = 0; u < unsolvedWordles.Count(); u++)
            {
                var curUnsolvedWordle = unsolvedWordles.ElementAt(u);
                curUnsolvedWordle.overlayRenderer.material.color = Color.clear;
                curUnsolvedWordle.overlayTextMesh.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}
