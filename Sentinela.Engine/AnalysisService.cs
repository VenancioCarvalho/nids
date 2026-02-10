using System.Threading.Channels;
using nids.Sentinela.Core;// Onde está o LogEntry
namespace nids.Sentinela.Engine;

public class AnalysisService
{
    private readonly ChannelReader<LogEntry> _reader;

    // Aqui iniciamos o consumidor(leitor) do canal
    public AnalysisService(ChannelReader<LogEntry> reader)
    {
        _reader = reader;
    }

    public async Task StartAnalysisLoopAsync(CancellationToken ct)
    {
        Console.WriteLine("[AnalysisService] Iniciando motor de análise...");

        try
        {
            // Loop infinito assíncrono: Se não tem dados, ele dorme  e não gasta CPU.
            while (await _reader.WaitToReadAsync(ct))
            {
                while (_reader.TryRead(out LogEntry entry))
                {
                    // AQUI SERÁ O LUGAR DA IA NO FUTURO

                    string srcIp = new System.Net.IPAddress(BitConverter.GetBytes(inverterIp(entry.SourceIp))).ToString();
                    
                    Console.WriteLine($"[ALERTA] Pacote Capturado! {srcIp} -> Porta {entry.DestPort} [{entry.OriginalLength} bytes]");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[AnalysisService] Parando...");
        }
    }

    // Inverte o IP
    private uint inverterIp(uint ip)
    {
        if (BitConverter.IsLittleEndian)
        {
            byte[] bytes = BitConverter.GetBytes(ip);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
        return ip;
    }
}
