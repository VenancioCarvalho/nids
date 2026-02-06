using System;
using nids.Sentinela.Engine;
using SharpPcap;

//Nome da placa de rede
string nomeDaMinhaInterface = @"\Device\NPF_{EF3FDC02-A092-4E0C-A436-EA19432E74E0}";

CaptureDeviceList devices = CaptureDeviceList.Instance;
Analizador anl = new Analizador();

ICaptureDevice device = devices.First(d => d.Name == nomeDaMinhaInterface);

device.OnPacketArrival += anl.OnPacketArrival;

device.Open(DeviceModes.Promiscuous, 1000);
device.StartCapture();

Console.WriteLine("=== MONITORAMENTO ATIVO ===");
Console.WriteLine("O Sentinela está ouvindo... Pressione ENTER para parar e fechar.");

//  O BLOQUEIO (Essencial para o código não fechar sozinho)
Console.ReadLine(); 

//  PARADA LIMPA (Executa após o Enter)
device.StopCapture();
device.Close();

Console.WriteLine("Monitoramento encerrado com segurança.");

