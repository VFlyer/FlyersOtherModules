using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGeneratorSampler : MonoBehaviour {

	public GridDisplayer gridRenderer;
	public Maze selectedMaze;
	public Color[] highlightColors;
	// Use this for initialization
	void Start () {
		selectedMaze = new MazeHuntAndKill(5, 5, true, true, true);
		
		StartCoroutine(GenerateMazeXTimes(1));
	}

	IEnumerator GenerateMazeXTimes(int repeatCount = 1)
    {
		for (int x = 0; x < repeatCount; x++)
		{
			selectedMaze.MoveToNewPosition(selectedMaze.GetLength() / 2, selectedMaze.GetWidth() / 2);
			selectedMaze.FillMaze();
			yield return selectedMaze.AnimateGeneratedMaze(0.2f);
		}
    }

	void UpdateDisplay()
    {
        for (int x = 0; x < gridRenderer.rowRenderers.Length; x++)
        {
			for (int y = 0; y < Mathf.Min(gridRenderer.rowRenderers[x].wallRenderers.Length, gridRenderer.rowRenderers[x].canRender.Length); y++)
				gridRenderer.rowRenderers[x].wallRenderers[y].enabled = gridRenderer.rowRenderers[x].canRender[y];

		}
    }

	// Update is called once per frame
	void Update () {
        for (int x = 0; x < selectedMaze.maze.GetLength(0); x++)
        {
            for (int y = 0; y < selectedMaze.maze.GetLength(1); y++)
            {
				if (x + 1 < selectedMaze.maze.GetLength(0))
					gridRenderer.rowRenderers[y * 2].canRender[x * 2 + 1] = !selectedMaze.maze[x, y].Contains("R");
                
				if (x - 1 >= 0)
					gridRenderer.rowRenderers[y * 2].canRender[x * 2 - 1] = !selectedMaze.maze[x, y].Contains("L");
				
				if (y - 1 >= 0)
					gridRenderer.rowRenderers[y * 2 - 1].canRender[x * 2] = !selectedMaze.maze[x, y].Contains("U");
				
				if (y + 1 < selectedMaze.maze.GetLength(1))
					gridRenderer.rowRenderers[y * 2 + 1].canRender[x * 2] = !selectedMaze.maze[x, y].Contains("D");

				gridRenderer.rowRenderers[y * 2].canRender[x * 2] = selectedMaze.GetState() && selectedMaze.GetCurX() == x && selectedMaze.GetCurY() == y;
				gridRenderer.rowRenderers[y * 2].wallRenderers[x * 2].material.color = selectedMaze.GetState() && selectedMaze.GetCurX() == x && selectedMaze.GetCurY() == y ? highlightColors[1] : highlightColors[0];
			}
        }
		for (int x = 1; x < gridRenderer.rowRenderers.Length; x += 2)
        {
			for (int y = 1; y < gridRenderer.rowRenderers[x].canRender.Length; y += 2)
			{
				gridRenderer.rowRenderers[x].canRender[y] = true;
			}
		}
		UpdateDisplay();
	}
}
