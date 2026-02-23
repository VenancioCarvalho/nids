using System.Threading.Channels;
using nids.Sentinela.Core;// Onde está o LogEntry
namespace nids.Sentinela.Engine;

public class AnalysisService
{
    private readonly ChannelReader<LogEntry> _reader;
    
    // As 3 Tabelas Hash (Dicionários) para os níveis hierárquicos do Kitsune
    private readonly Dictionary<HostKey, FeatureTracker> _hostTable = new();
    private readonly Dictionary<ChannelKey, FeatureTracker> _channelTable = new();
    private readonly Dictionary<SocketKey, FeatureTracker> _socketTable = new();


    // Constantes de controle
    private readonly double[] _lambdas = { 5, 3, 1, 0.1, 0.01 };

    public AnalysisService(ChannelReader<LogEntry> reader)
    {
        _reader = reader;
    }

    public async Task StartAnalysisLoopAsync(CancellationToken ct)
    { 
        Console.WriteLine("[AnalysisService] Motor de Fluxos Iniciado.");

        try
        {
            while (await _reader.WaitToReadAsync(ct))
            {
                while (_reader.TryRead(out LogEntry entry))
                {
                    ProcessPacket(entry);

                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[AnalysisService] Processamento cancelado pelo usuário.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CRÍTICO] Ocorreu um erro ao processar o pacote: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
        }
    }

    private void ProcessPacket(LogEntry entry)
    {

        long currentTimeMs = entry.TimestampTicks / TimeSpan.TicksPerMillisecond;
        double packetSize = entry.OriginalLength;

        // 1. Geração Imediata das 3 Chaves (Zero Alocação de Memória Heap!)
        var hostKey = new HostKey(entry.SourceIp);
        var channelKey = new ChannelKey(entry.SourceIp, entry.DestIp);
        var socketKey = new SocketKey(
            entry.SourceIp, 
            entry.SourcePort, 
            entry.DestIp, 
            entry.DestPort, 
            entry.Protocol
        );

        var hostContext = AtualizarContexto(_hostTable, hostKey, currentTimeMs, packetSize);
        var channelContext = AtualizarContexto(_channelTable, channelKey, currentTimeMs, packetSize);
        var socketContext = AtualizarContexto(_socketTable, socketKey, currentTimeMs, packetSize);

    }

    private  FeatureTracker AtualizarContexto<TKey>(
    Dictionary<TKey, FeatureTracker> tabela, 
    TKey chave, 
    long currentTimeMs, 
    double packetSize) where TKey : struct
    {
        // Se a chave ainda não existe na tabela, criamos a gaveta nova
        if (!tabela.TryGetValue(chave, out var contexto))
        {
            contexto = new FeatureTracker(_lambdas, currentTimeMs);
            tabela.Add(chave, contexto);
        }

        // O coração do sistema: Aplica o decaimento exponencial e injeta o novo pacote
        contexto.ExtractUpdate(currentTimeMs, packetSize);

        // Devolvemos o contexto já com os Trios (W, LS, SS) atualizados
        return contexto;
    }


    public void ImprimirEstadoDasTabelas()
    {
        Console.WriteLine("\n=======================================================");
        Console.WriteLine("    INSPEÇÃO DE MEMÓRIA DAS TABELAS HASH (KITSUNE)     ");
        Console.WriteLine("=======================================================");
        Console.WriteLine($"Total de Hosts: {_hostTable.Count}");
        Console.WriteLine($"Total de Canais: {_channelTable.Count}");
        Console.WriteLine($"Total de Sockets: {_socketTable.Count}");
        Console.WriteLine("-------------------------------------------------------\n");

        // Vamos imprimir apenas os 5 primeiros Canais para não inundar o console
        int limiteConsole = 5;
        int contador = 0;

        Console.WriteLine("--- AMOSTRA DA TABELA DE CANAIS (A -> B) ---");
        foreach (var kvp in _channelTable)
        {
            if (contador >= limiteConsole) break;

            var chave = kvp.Key;
            var contexto = kvp.Value;

            // Convertendo IP de Int para String só para visualização
            string ipOrigem = new System.Net.IPAddress(BitConverter.GetBytes(chave.SourceIp).Reverse().ToArray()).ToString();
            string ipDestino = new System.Net.IPAddress(BitConverter.GetBytes(chave.DestIp).Reverse().ToArray()).ToString();

            Console.WriteLine($"\n[Canal] {ipOrigem} -> {ipDestino}");
            Console.WriteLine($"Último Pacote Visto (ms): {contexto.LastSeenMilliseconds}");

            // Vamos olhar o que está guardado na 1ª Janela de Tempo (ex: Lambda 0)
            var trioTamanho = contexto.SizeStats[0];
            
            Console.WriteLine("   Váriaveis de Estado Armazenadas na RAM:");
            Console.WriteLine($"     -> Peso (w):  {trioTamanho.W:F4}");
            Console.WriteLine($"     -> Soma (LS): {trioTamanho.LS:F4}");
            Console.WriteLine($"     -> Quad (SS): {trioTamanho.SS:F4}");
            
            Console.WriteLine("   Estatísticas Calculadas Sob Demanda:");
            Console.WriteLine($"     -> Média:     {trioTamanho.Mean:F2} bytes");
            Console.WriteLine($"     -> Variância: {trioTamanho.Variance:F2}");

            contador++;
        }
        
        Console.WriteLine("\n=======================================================");
    }

}
       

