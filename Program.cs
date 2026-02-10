using System;
using SharpPcap;
using System.Threading.Channels;
using nids.Sentinela.Core;
using nids.Sentinela.Engine;

//Nome da placa de rede
string nomeDaMinhaInterface = @"\Device\NPF_{EF3FDC02-A092-4E0C-A436-EA19432E74E0}";

// Configuração do Channel (O Tubo)
// Capacidade de 50.000 pacotes. Se encher, descarta o mais novo para não travar a RAM.
var channelOptions = new BoundedChannelOptions(50000)
{
    FullMode = BoundedChannelFullMode.DropWrite,
    SingleWriter = true, // Só o PacketCaptureService escreve
    SingleReader = true  // Só o AnalysisService lê
};

var trafficChannel = Channel.CreateBounded<LogEntry>(channelOptions);


// Instanciando os Serviços (Injeção de Dependência Manual)
var captureService = new PacketCaptureService(trafficChannel.Writer); // Produtor
var analysisService = new AnalysisService(trafficChannel.Reader); // Consumidor



CaptureDeviceList devices = CaptureDeviceList.Instance;
ICaptureDevice device = devices.First(d => d.Name == nomeDaMinhaInterface);

device.OnPacketArrival += captureService.OnPacketArrival;

// Iniciando a captura

device.Open(DeviceModes.Promiscuous, 1000);
Console.WriteLine($"\n-- SENTINELA INICIADO na interface: {nomeDaMinhaInterface} --");
Console.WriteLine("Pressione [ENTER] para parar.\n");

// Iniciando as Threads
// Iniciamos a captura (Driver -> CaptureService -> Channel)
device.StartCapture();

// Iniciamos a análise (Channel -> AnalysisService -> Console) em uma Task separada
using var cts = new CancellationTokenSource();
var analysisTask = Task.Run(() => analysisService.StartAnalysisLoopAsync(cts.Token));

// Mantém o programa rodando até o usuário dar Enter
Console.ReadLine();

// 6. Encerramento Gracioso
Console.WriteLine("Parando captura...");
device.StopCapture();
device.Close();

Console.WriteLine("Parando análise...");
cts.Cancel(); // Avisa o loop do AnalysisService para parar
await analysisTask; // Espera terminar

Console.WriteLine("Sentinela encerrado.");
