using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarExtendedScript : MonoBehaviour {

	public MeshRenderer barRenderL, barRenderM, barRenderR;
	public float curProgress, maxProgress, progressDelta;
	public bool alwaysUpdateProgress, simulateProgress;
	public void UpdateProgress()
    {
		var expectedProgressCur = curProgress >= maxProgress ? maxProgress : curProgress;
		var expectedProgressDelta = curProgress + progressDelta >= maxProgress ? maxProgress - curProgress : progressDelta;

		var percentageCur = expectedProgressCur / maxProgress;
		var percentageDelta = expectedProgressDelta / maxProgress;
		var percentageToGo = (maxProgress - expectedProgressDelta - expectedProgressCur) / maxProgress;

		barRenderL.transform.localScale = new Vector3(percentageCur, 1, 1);
		barRenderM.transform.localScale = new Vector3(percentageDelta, 1, 1);
		barRenderR.transform.localScale = new Vector3(percentageToGo, 1, 1);
		
		barRenderL.transform.localPosition = new Vector3(5f * percentageCur - 5f, 0, 0);
		barRenderM.transform.localPosition = new Vector3(5f - 5f * percentageToGo + 5f * percentageCur - 5f, 0, 0);
		barRenderR.transform.localPosition = new Vector3(5f - 5f * percentageToGo, 0, 0);
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
