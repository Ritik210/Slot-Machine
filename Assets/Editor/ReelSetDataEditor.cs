using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ReelSetData))]
public class ReelSetDataEditor : Editor
{
    private List<string> transposedReelStrings = new List<string>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw serialized fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("numberOfReels"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("reels"), includeChildren: true);

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear All"))
        {
            transposedReelStrings.Clear();
        }

        // Parse Button
        if (GUILayout.Button("Parse and Transpose Reels"))
        {
            ReelSetData data = (ReelSetData)target;
            transposedReelStrings.Clear();

            for (int i = 0; i < data.reels.Count; i++)
            {
                string rawText = data.reels[i].text.Trim();

                // Split into rows
                string[] rows = rawText.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

                // Split each row by tab only
                List<string[]> splitRows = new List<string[]>();
                foreach (var row in rows)
                {
                    string[] cells = row.Split(new[] { '\t' }, System.StringSplitOptions.None);
                    splitRows.Add(cells);
                }

                // Find max columns
                int maxColumns = 0;
                foreach (var r in splitRows)
                    if (r.Length > maxColumns)
                        maxColumns = r.Length;

                // Transpose
                List<List<string>> columns = new List<List<string>>();
                for (int col = 0; col < maxColumns; col++)
                {
                    List<string> columnSymbols = new List<string>();
                    for (int row = 0; row < splitRows.Count; row++)
                    {
                        if (col < splitRows[row].Length)
                        {
                            string symbol = splitRows[row][col].Trim();
                            if (string.IsNullOrEmpty(symbol))
                                columnSymbols.Add("");
                            else
                                columnSymbols.Add($"\"{symbol}\"");
                        }
                        else
                        {
                            columnSymbols.Add("");
                        }
                    }
                    columns.Add(columnSymbols);
                }

                // Build output string
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"// Transposed Reel Set {i}");
                for (int col = 0; col < columns.Count; col++)
                {
                    sb.AppendLine($"<Reel>{string.Join(",",columns[col])}</Reel>");
                }

                transposedReelStrings.Add(sb.ToString());
            }
        }

        // Display transposed output
        if (transposedReelStrings.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transposed Output", EditorStyles.boldLabel);

            foreach (var text in transposedReelStrings)
            {
                EditorGUILayout.TextArea(text, GUILayout.Height(150));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}