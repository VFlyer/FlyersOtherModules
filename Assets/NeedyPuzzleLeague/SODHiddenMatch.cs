using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SODHiddenMatch : MonoBehaviour {

	public MeshFilter[] quadrantMeshes = new MeshFilter[4];
	public MeshRenderer[] quadrantRenderers = new MeshRenderer[4];

	public Mesh[] possibleMeshes;
	public Color[] possibleColors;

	private int[] idxGoalMeshes = new int[4], idxGoalColors = new int[4],
		idxCurrentMeshes = new int[4], idxCurrentColors = new int[4];

	public void GeneratePuzzle()
	{
		for (int x = 0; x < 4; x++)
		{
			idxCurrentMeshes[x] = 1;
			idxCurrentColors[x] = 0;

			idxGoalMeshes[x] = Random.Range(0, possibleMeshes.Length);
			idxGoalColors[x] = Random.Range(1, possibleColors.Length);

		}
	}

	public bool IsAllCorrect()
	{
		bool output = true;
		for (int x = 0; x < 4; x++)
		{
			switch (idxGoalMeshes[x])
			{
				case 0:
					output &= idxGoalMeshes[x] == idxCurrentMeshes[x];
					break;
				default:
					output &= idxGoalMeshes[x] == idxCurrentMeshes[x] && idxGoalColors[x] == idxGoalMeshes[x];
					break;
			}
		}

		return output;
	}

	public void AssignMeshIdx(int idx, int value)
	{
		if (idx < 0 || idx >= 4) return;

		idxCurrentMeshes[idx] = PMod(value, possibleMeshes.Length);
	}
	public void AssignColorIdx(int idx, int value)
	{
		if (idx < 0 || idx >= 4) return;

		idxCurrentColors[idx] = PMod(value, possibleColors.Length);
	}

	int PMod(int value, int divisor)
    {
		if (divisor <= 0) return 0;
		return ((value % divisor) + divisor) % divisor;
    }

	void RenderCurrentMesh()
    {
		for (var x = 0; x < quadrantMeshes.Length; x++)
        {
			quadrantMeshes[x].mesh = possibleMeshes.ElementAtOrDefault(idxCurrentMeshes[x]);
            quadrantRenderers[x].material.color = possibleColors.ElementAtOrDefault(idxCurrentColors[x]);
		}
    }
	void RenderGoalMesh()
	{
		for (var x = 0; x < quadrantMeshes.Length; x++)
		{
			quadrantMeshes[x].mesh = possibleMeshes.ElementAtOrDefault(idxGoalMeshes[x]);
			quadrantRenderers[x].material.color = possibleColors.ElementAtOrDefault(idxGoalColors[x]);
		}
	}
}
