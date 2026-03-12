/**
using System;
using SharpPcap;

var devices = CaptureDeviceList.Instance;
foreach (var dev in devices)
    Console.WriteLine("{0}\n", dev.ToString());
**/