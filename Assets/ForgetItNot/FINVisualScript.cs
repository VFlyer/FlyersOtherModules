using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class FINVisualScript : MonoBehaviour {

	public ForgetItNotHandler finHandler;
	public MeshRenderer backingRender;
	FlyersOtherSettings localSettings = new FlyersOtherSettings();
	const string strBase16 = "0123456789abcdef";
	// Use this for initialization
	void Start () {
		var modSettings = new ModConfig<FlyersOtherSettings>("FlyersOtherSettings");
		localSettings = modSettings.Settings;
		modSettings.Settings = localSettings;
		if (localSettings.FINUseCustomColors)
        {
			var replacementLEDMat = new Material(finHandler.statusLEDClr[0]);
			replacementLEDMat.color = GetColor(localSettings.FINLEDColor);
			finHandler.statusLEDClr[0] = replacementLEDMat;
			backingRender.material.color = GetColor(localSettings.FINBackingColor);
        }
	}
	Color GetColor(string relevantString)
    {
		var relevantStringLower = relevantString.ToLowerInvariant();
		switch (relevantStringLower)
        {
			case "white":
				return Color.white;
			case "green":
				return Color.green;
			case "gray":
				return Color.gray;
			case "yellow":
				return Color.yellow;
			case "red":
				return Color.red;
			case "cyan":
				return Color.cyan;
			case "black":
				return Color.black;
			case "blue":
				return Color.blue;
			case "magenta":
				return Color.magenta;
        }
		var rgxHash3 = Regex.Match(relevantStringLower, @"#[0123456789ABCDEF]{3}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		var rgxHash6 = Regex.Match(relevantStringLower, @"#[0123456789ABCDEF]{6}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (rgxHash6.Success)
        {
			var obtainedRgx6 = rgxHash6.Value.Substring(1);
			return new Color32((byte)(16 * strBase16.IndexOf(obtainedRgx6[0]) + strBase16.IndexOf(obtainedRgx6[1])), (byte)(16 * strBase16.IndexOf(obtainedRgx6[2]) + strBase16.IndexOf(obtainedRgx6[3])), (byte)(16 * strBase16.IndexOf(obtainedRgx6[4]) + strBase16.IndexOf(obtainedRgx6[5])), 255);
		}
		else if (rgxHash3.Success)
        {
			var obtainedRgx3 = rgxHash3.Value.Substring(1);
            return new Color32((byte)(17 * strBase16.IndexOf(obtainedRgx3[0])), (byte)(17 * strBase16.IndexOf(obtainedRgx3[1])), (byte)(17 * strBase16.IndexOf(obtainedRgx3[2])), 255);
        }
		return Color.yellow;
    }

}
