using System.Collections.Generic;
using UnityEngine;

public class WildMeterEvent : SlotEvent
{
    public List<int> symbolPositions;

    public WildMeterEvent(List<int> positions)
    {
        this.symbolPositions = positions;
    }
}
