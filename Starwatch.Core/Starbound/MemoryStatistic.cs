using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Starwatch.Starbound
{
    public struct MemoryUsage
    {
        public long WorkingSet, PeakWorkingSet, MaxWorkingSet;
        public MemoryUsage(Process process)
        {
            if (process != null)
            {
                WorkingSet = process.WorkingSet64;
                PeakWorkingSet = process.PeakWorkingSet64;
                MaxWorkingSet = process.MaxWorkingSet.ToInt64();
            }
            else
            {
                WorkingSet = 0;
                PeakWorkingSet = 0;
                MaxWorkingSet = 0;
            }
        }
    }
}
