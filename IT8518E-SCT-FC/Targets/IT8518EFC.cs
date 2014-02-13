﻿using SCTFanControl.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SCTFanControl
{
    class IT8518EFanControl: EmbeddedController, ITarget
    {
        private float fanSpeed = 0;
        private int avgTemp = 0;
        private int gpuTemp = 0;
        private int cpuTemp = 0;
        private int pchTemp = 0;
        private bool biosControl = true;

        int[] tempsBuffer = new int[16];
        int bufferIndex = 0;
        int dataCount = 0;

        public float GetTemperature()
        {
            gpuTemp = ReadEC(0x56);
            cpuTemp = ReadEC(0x5A);
            pchTemp = ReadEC(0x5B);

            if (gpuTemp != 0) 
            {
                float gcMax = Math.Max(gpuTemp, cpuTemp);
                return (float)Math.Max(gcMax, pchTemp);
            }
            else // intel-only boards do not report gpu temp
            {
                return (float)Math.Max(cpuTemp, pchTemp);
            }
        }

        public int GetAverageTemp()
        {
            if (dataCount < tempsBuffer.Length) dataCount++; // if array isn't filled with data yet - incr on each loop till length of array
            if (bufferIndex >= tempsBuffer.Length) bufferIndex = 0; // if index is same number as array length - reset index to 0
            tempsBuffer[bufferIndex] = ReadEC(0x5A); // read CPU die temp from EC as it's the only relevant temp for fan control
                                                     // if you'd like to rely on all the temps store GetTemperature() here
            bufferIndex++; //increment index for next loop
            avgTemp = tempsBuffer.Sum()/dataCount; // compute average from sum of array elements divided by datacount
            return avgTemp;
        }

        public int GetAcStatus()
        {
            int acin = ReadEC(0x40) & 1; // check if we are running on AC power.. 
            return acin;
        }

        public void SetFanSpeed(int tsafe, int ttrip, int speed)
        {
            int flvl = ReadEC(0x63); // check what is the current fan level
            
            // fan control only on ac power
            if (GetAcStatus() != 0)
            {
                if (biosControl) // if automatic control is enabled
                {
                    if (speed > 0) // if not using silent mode
                    {
                        if (avgTemp <= tsafe) // if temp is below or equal safe temp
                        {
                            // if fan is running at level 1 speeds
                            if (flvl == 0x01) WriteEC(0x63, 0x00); // make fan drop speed gradually
                            // if fan speed dropped below requested steady speed
                            if (fanSpeed <= speed && fanSpeed != 0) SetBiosControl(false); // enable manul mode and lock fan at desired steady speed
                        }
                    }

                    if (speed == 0) // if silent mode requested
                    {
                        // if temp is below or equal safe temp and fan has turned off
                        if (avgTemp <= tsafe && fanSpeed == 0) SetBiosControl(false); // enable manual mode and lock fan in off position
                    }
                }
                else // if fan is in manual mode
                {
                    // and temp has reached trip point
                    if (avgTemp >= ttrip) SetBiosControl(true); // re-enabled automatic control
                }
            }
            else
            {
                // if suddenly running on battery and manual mode set - restore auto control
                if (!biosControl) SetBiosControl(true);
            }
        }

        public float GetFanSpeed()
        {
            fanSpeed = ReadEC(0x68) << 8 | ReadEC(0x69); // read fan speed, rpm as WORD
            return fanSpeed;
        }

        public void SetBiosControl(Boolean toggle)
        {
            if (toggle) WriteEC(0x60, 0x40); // enabled EC automatic control
            else WriteEC(0x60, 0x00); // enable manual control
            biosControl = toggle; // toggle control state
        }

        public bool GetBiosControl()
        {
            if (ReadEC(0x60) == 0x40) biosControl = true; // if TCTL bit is set - auto mode enabled
            else biosControl = false; // otherwise it's disabled
            return biosControl;
        }

        public string Information
        {
            get
            {
                if (gpuTemp != 0) // if laptop has amd or nvidia board - report gpu temp too
                {
                    return String.Format("CPU:{0:0}°C, GPU:{1:0}°C, PCH:{2:0}°C, FAN:{3:0}rpm", cpuTemp, gpuTemp, pchTemp, fanSpeed);
                }
                else
                {
                    return String.Format("CPU:{0:0}°C, PCH:{1:0}°C, FAN:{2:0}rpm", cpuTemp, pchTemp, fanSpeed);
                }
            }
        }

    }
}
