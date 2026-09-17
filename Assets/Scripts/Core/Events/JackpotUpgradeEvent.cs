using System.Collections.Generic;
using UnityEngine;

public class JackpotUpgradeEvent : SlotEvent
{
    public List<int> jackpotPositions;

    public JackpotUpgradeEvent(List<int> positions)
    {
        this.jackpotPositions = positions;
    }
}
