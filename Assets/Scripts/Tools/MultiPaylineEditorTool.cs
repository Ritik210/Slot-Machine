#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MultiPaylineEditorTool : MonoBehaviour
{
    public int rows = 3;
    public int columns = 5;
    public int totalLines = 3; // how many lines you want to create at once

    [System.Serializable]
    public class Payline
    {
        public string name = "Line";
        public List<int> positions = new();
        public string indexString = "";
    }

    public List<Payline> paylines = new();

    [HideInInspector]
    public bool[,,] lineSelections; // [line, row, column]
}

[CustomEditor(typeof(MultiPaylineEditorTool))]
public class MultiPaylineEditorToolEditor : Editor
{
    private MultiPaylineEditorTool tool;

    public override void OnInspectorGUI()
    {
        tool = (MultiPaylineEditorTool)target;

        tool.rows = Mathf.Clamp(EditorGUILayout.IntField("Rows", tool.rows), 1, 10);
        tool.columns = Mathf.Clamp(EditorGUILayout.IntField("Columns", tool.columns), 1, 10);
        tool.totalLines = Mathf.Clamp(EditorGUILayout.IntField("Total Lines", tool.totalLines), 1, 125);

        // Init selection array if needed
        if (tool.lineSelections == null ||
            tool.lineSelections.GetLength(0) != tool.totalLines ||
            tool.lineSelections.GetLength(1) != tool.rows ||
            tool.lineSelections.GetLength(2) != tool.columns)
        {
            tool.lineSelections = new bool[tool.totalLines, tool.rows, tool.columns];
        }

        GUILayout.Space(10);

        for (int line = 0; line < tool.totalLines; line++)
        {
            GUILayout.Label($"Line {line + 1}", EditorStyles.boldLabel);
            for (int row = 0; row < tool.rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < tool.columns; col++)
                {
                    bool prev = tool.lineSelections[line, row, col];
                    bool toggle = GUILayout.Toggle(prev, "", GUILayout.Width(30));

                    if (toggle && !prev)
                    {
                        for (int r = 0; r < tool.rows; r++)
                            tool.lineSelections[line, r, col] = false;

                        tool.lineSelections[line, row, col] = true;
                    }
                    else if (!toggle)
                    {
                        tool.lineSelections[line, row, col] = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
        }

        if (GUILayout.Button("Generate Paylines"))
        {
            tool.paylines.Clear();

            for (int line = 0; line < tool.totalLines; line++)
            {
                var newLine = new MultiPaylineEditorTool.Payline();
                newLine.name = $"Line {line + 1}";

                for (int col = 0; col < tool.columns; col++)
                {
                    for (int row = 0; row < tool.rows; row++)
                    {
                        if (tool.lineSelections[line, row, col])
                        {
                            int index = row * tool.columns + col;
                            newLine.positions.Add(index);
                            break; // only 1 per column
                        }
                    }
                }

                newLine.indexString = string.Join(",", newLine.positions);
                tool.paylines.Add(newLine);
            }

            EditorUtility.SetDirty(tool);
        }

        if (GUILayout.Button("Clear All"))
        {
            tool.paylines.Clear();
            tool.lineSelections = new bool[tool.totalLines, tool.rows, tool.columns];
            EditorUtility.SetDirty(tool);
        }

        GUILayout.Space(10);
        GUILayout.Label("Resulting Paylines:");
        string finalOutput = "";
        foreach (var line in tool.paylines)
        {
            //GUILayout.Label($"{line.name}: {line.indexString}");
            finalOutput += $"{line.indexString}~";
        }

        EditorGUILayout.TextArea(finalOutput, GUILayout.Height(100));
    }
}
#endif
