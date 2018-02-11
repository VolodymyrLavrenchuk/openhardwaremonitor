/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>
 
*/

using System;
using System.IO;
using System.Collections.Generic;
using OpenHardwareMonitor.Hardware;
using Newtonsoft.Json;


namespace OpenHardwareMonitorReport {

  public class Config {
    public string ConfigFile = "sensors.json";
    public Dictionary<String, String[]> Sensors;
    public Dictionary<String, String[]> SensorValues;
  }

  class Program {
    static void Main(string[] args) {

      Computer computer = new Computer();
      Config cnfg = new Config();

      computer.CPUEnabled = true;
      computer.FanControllerEnabled = false;
      computer.GPUEnabled = false ;
      computer.HDDEnabled = false;
      computer.MainboardEnabled = true;
      computer.RAMEnabled = true;

      computer.Open();

      if (File.Exists(cnfg.ConfigFile))
      {
        using (StreamReader r = new StreamReader(cnfg.ConfigFile))
        {
          cnfg.Sensors = JsonConvert.DeserializeObject<Dictionary<String, String[]>>(r.ReadToEnd());
        }

        cnfg.SensorValues = new Dictionary<string, string[]>(cnfg.Sensors);
        computer.Accept(new SensorVisitor(cnfg));
        foreach (KeyValuePair<string, string[]> curSensor in cnfg.SensorValues)
        {
          Console.Out.Write(curSensor.Key + ":");
          Console.Out.WriteLine(string.Join(":", curSensor.Value));
        }
      }
      else
      {
        computer.Accept(new UpdateVisitor());
        Console.Out.Write(computer.GetReport());
      }

      computer.Close();
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
          m_config.SensorValues[curSensor.Key][iIndex] = sensor.Value.ToString().Replace(",",".");
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
