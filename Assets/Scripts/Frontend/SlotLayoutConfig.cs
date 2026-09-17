using UnityEngine;

[CreateAssetMenu(fileName = "SlotLayoutConfig", menuName = "Slot/SlotLayoutConfig")]
public class SlotLayoutConfig : ScriptableObject
{
    public int reels = 5;
    public int rows = 3;

    public int GetFlatIndex(int reel, int row)
    {
        return row * reels + reel;
    }

    public (int reel, int row) GetReelRowFromFlat(int flatIndex)
    {
        int reel = flatIndex % reels;
        int row = flatIndex / reels;
        return (reel, row);
    }
}
