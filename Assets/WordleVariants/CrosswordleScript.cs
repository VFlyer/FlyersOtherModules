using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CrosswordleScript : MonoBehaviour {
	public QuerySetUI[] displayResults, displayInitials;
	public KMSelectable[] keyboardSelectables, letterSelectables;
	public KMSelectable checkSelectable;
	// Use this for initialization
	void Start () {
        GeneratePuzzle();
	}
	void GeneratePuzzle()
    {
		var allowedWords = Data.GenerousWordList.ToList();
        var answer = allowedWords.PickRandom();
        var selectedWords = new List<string>();
		selectedWords.Add(answer);
        allowedWords.Remove(answer);
        var wordResponse = new Dictionary<string, int[]>();
        foreach (var word in allowedWords)
            wordResponse.Add(word, GetResult(word, answer));
        var limit = 5;
        var disallowedLetters = new List<char>();
        while (selectedWords.Count < 4)
        {
            var allowedQuery = allowedWords.Where(a => wordResponse[a].Count(b => b > 0) >= limit && a.All(b => !disallowedLetters.Contains(b)));
            if (allowedQuery.Any())
            {
                tryagain:
                var pickedWord = allowedQuery.PickRandom();
                var lettersNotInAnswer = pickedWord.Except(answer).Distinct();
                selectedWords.Add(pickedWord);
                disallowedLetters.AddRange(lettersNotInAnswer);
            }
            limit--;
        }
        foreach (var word in selectedWords)
            Debug.LogFormat("{0}: {1}", word, GetResult(word, answer).Join(""));
    }
	int[] GetResult(string query, string answer)
    {
		if (query.Length != answer.Length)
			return null;
        var output = new int[query.Length];
        var distinctLettersInCorrectWord = answer.Distinct();
        foreach (char distinctLetter in distinctLettersInCorrectWord)
        {
            var idxesMatchCurLetterInCorrectWord = Enumerable.Range(0, answer.Length).Where(a => answer[a] == distinctLetter);
            var idxesMatchCurLetterInSelectedWord = Enumerable.Range(0, query.Length).Where(a => query[a] == distinctLetter);
            var idxesMatchBoth = idxesMatchCurLetterInSelectedWord.Intersect(idxesMatchCurLetterInCorrectWord);
            foreach (int idxMatch in idxesMatchBoth)
                output[idxMatch] = 2;
            var idxesMatchExcluded = idxesMatchCurLetterInSelectedWord.Except(idxesMatchCurLetterInCorrectWord).Take(idxesMatchCurLetterInCorrectWord.Count() - idxesMatchBoth.Count());
            foreach (int curSet in idxesMatchExcluded)
                output[curSet] = 1;
        }
        return output;
    }
}
