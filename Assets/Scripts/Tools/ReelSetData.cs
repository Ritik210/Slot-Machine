using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TextAreaItem
{
    [TextArea(10, 100)]
    public string text;
}

[CreateAssetMenu(menuName = "Slot/Reel Set Data")]
public class ReelSetData : ScriptableObject
{
    public int numberOfReels = 5;
    public List<TextAreaItem> reels = new List<TextAreaItem>();
}