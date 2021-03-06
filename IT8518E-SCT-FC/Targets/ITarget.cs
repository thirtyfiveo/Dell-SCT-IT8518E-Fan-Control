﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCTFanControl.Targets
{
    interface ITarget
    {
        int GetTemperature();
        int GetAverageTemp(); //added for evaluation purposes
        int[] GetAcStatus();
        void SetBiosControl(Boolean b);
        bool GetBiosControl();        
        void SetFanSpeed(int tsafe, int ttrip, int speed);
        int GetFanSpeed();
        string Information { get; }
    }
}
