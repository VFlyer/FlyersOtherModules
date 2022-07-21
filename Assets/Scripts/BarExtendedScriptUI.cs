using UnityEngine;

public class BarExtendedScriptUI : MonoBehaviour {

	public RectTransform baseItem, barDeltaItem, barCurItem, barToGoItem;
	public float curProgress, maxProgress, progressDelta;
	public bool alwaysUpdateProgress, simulateProgress;
	public void UpdateProgress()
    {
		var expectedProgressCur = curProgress >= maxProgress ? maxProgress : curProgress;
		var expectedProgressDelta = curProgress + progressDelta >= maxProgress ? maxProgress - curProgress : progressDelta;

		var percentageCur = expectedProgressCur / maxProgress;
		var percentageDelta = expectedProgressDelta / maxProgress;
		var percentageToGo = (maxProgress - expectedProgressDelta - expectedProgressCur) / maxProgress;
		
		barCurItem.sizeDelta = new Vector2(baseItem.sizeDelta.x, -baseItem.sizeDelta.y * (1f - percentageCur));
		barDeltaItem.sizeDelta = new Vector2(baseItem.sizeDelta.x, -baseItem.sizeDelta.y * (1f - percentageDelta));
		barToGoItem.sizeDelta = new Vector2(baseItem.sizeDelta.x, -baseItem.sizeDelta.y * (1f - percentageToGo));

		barCurItem.anchoredPosition = new Vector2(0, baseItem.sizeDelta.y / 2 * (1f - percentageCur));
		barDeltaItem.anchoredPosition = new Vector2(0, baseItem.sizeDelta.y / 2 * (1f - percentageCur) - baseItem.sizeDelta.y / 2 * (1f - percentageToGo));
		barToGoItem.anchoredPosition = new Vector2(0, - baseItem.sizeDelta.y / 2 * (1f - percentageToGo));

	}
	// Update is called once per frame
	void Update () {
		if (alwaysUpdateProgress)
			UpdateProgress();
	}
	void FixedUpdate()
    {
		if (simulateProgress)
        {
			curProgress += Time.fixedDeltaTime;
			if (curProgress >= maxProgress)
			{
				curProgress = 0;
				progressDelta += Time.fixedDeltaTime;
				if (progressDelta >= maxProgress)
				
					progressDelta = 0;
			}
        }
    }
}
