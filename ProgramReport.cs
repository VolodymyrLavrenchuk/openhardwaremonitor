/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>
 
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenHardwareMonitor.Hardware;
using Newtonsoft.Json;


namespace OpenHardwareMonitorReport {

  public class Config
  {
    public string ConfigFile = "sensors.json";
    public Dictionary<String, String[]> Sensors;
  }

  class Program {

    static string GetSensorsValues(IVisitor visitor, bool genReport = false)
    {

      string ret = "";
      Computer computer = new Computer();

      computer.CPUEnabled = true;
      computer.FanControllerEnabled = false;
      computer.GPUEnabled = false;
      computer.HDDEnabled = false;
      computer.MainboardEnabled = true;
      computer.RAMEnabled = true;

      computer.Open();
      computer.Accept(visitor);
      if (genReport)
      {
        ret = computer.GetReport();
      }
      
      computer.Close();

      return ret;
    }

    static bool GetValidSensorsValues(Config cnfg, Dictionary<String, String[]> sensors)
    {
      cnfg.Sensors = sensors.ToDictionary(entry => entry.Key, entry => (String[])entry.Value.Clone());
      IVisitor sensorsVisitor = new SensorVisitor(cnfg);

      GetSensorsValues(sensorsVisitor);

      foreach (KeyValuePair<string, string[]> curSensor in cnfg.Sensors)
      {
        for (int i = 0; i < curSensor.Value.Length; i++)
        {
          try
          {
            Convert.ToDouble(curSensor.Value[i]);
            curSensor.Value[i] = curSensor.Value[i].Replace(",", ".");
          }
          catch (FormatException)
          {
            return false;
          }
        }
      }

      return true;
    }

    static void Main(string[] args) {

      if (args.Length > 0 && File.Exists(args[0])) {

        Dictionary<String, String[]> sensors;

        using (StreamReader r = new StreamReader(args[0]))
        {
          sensors = JsonConvert.DeserializeObject<Dictionary<String, String[]>>(r.ReadToEnd());
        }

        Config cnfg = new Config();

        while (!GetValidSensorsValues(cnfg, sensors))
        {
          System.Threading.Thread.Sleep(1000);
        }

        foreach (KeyValuePair<string, string[]> curSensor in cnfg.Sensors)
        {
          Console.Out.Write(curSensor.Key + " ");
          Console.Out.WriteLine(string.Join(":", curSensor.Value));
        }
      }
      else
      {
        Console.Out.Write(GetSensorsValues(new UpdateVisitor(), true));
      }
    }
  }

  public class SensorVisitor : IVisitor {
    Config m_config;

    public SensorVisitor(Config config)
    {
      m_config = config;
    }
    public void VisitComputer(IComputer computer)
    {
      computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
      hardware.Update();
      foreach (IHardware subHardware in hardware.SubHardware)
        subHardware.Accept(this);

      hardware.Traverse(this);
    }


    public void VisitSensor(ISensor sensor)
    {
      foreach (KeyValuePair<string, string[]> curSensor in m_config.Sensors)
      {
        int iIndex = Array.IndexOf(curSensor.Value, sensor.Identifier.ToString());
        if (iIndex >= 0)
        {
          m_config.Sensors[curSensor.Key][iIndex] = sensor.Value.ToString();
        }
      }
    }

    public void VisitParameter(IParameter parameter) {}
  }
  public class UpdateVisitor : IVisitor {

    public void VisitComputer(IComputer computer) {
      computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware) {
      hardware.Update();
      foreach (IHardware subHardware in hardware.SubHardware)
        subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
  }
}
