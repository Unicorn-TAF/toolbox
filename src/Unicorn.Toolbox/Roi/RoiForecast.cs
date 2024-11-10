using System.Collections.Generic;

namespace Unicorn.Toolbox.Roi;

public sealed class RoiForecast
{
    public RoiForecast()
    {
        Series.Add(new RoiEntry());
    }

    public List<RoiEntry> Series { get; } = new List<RoiEntry>();

    public int ZeroReachIndex { get; private set; } = -1;

    public void AddEntry(RoiEntry entry)
    {
        Series.Add(entry);

        if (ZeroReachIndex == -1 && entry.ROI > 0)
        {
            ZeroReachIndex = Series.Count - 1;
        }
    }
}
