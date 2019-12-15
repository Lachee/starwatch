/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
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
