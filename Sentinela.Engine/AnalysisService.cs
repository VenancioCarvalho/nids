using System.Threading.Channels;
using nids.Sentinela.Core;// Onde está o LogEntry
namespace nids.Sentinela.Engine;

public class AnalysisService
{
    private readonly ChannelReader<LogEntry> _reader;
        
        // A Tabela de Fluxos em Memória
        // Chave: String (SourceIP:Port -> DestIP:Port)
        // Valor: O Objeto NetworkFlow
        private readonly Dictionary<string, NetworkFlow> _activeFlows = new();

        // Constantes de controle
        private const int FLOW_TIMEOUT_SECONDS = 5; // Se ficar 60s sem pacote, fecha o fluxo
        private DateTime _lastCleanupTime = DateTime.MinValue;

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

                        // A cada X pacotes ou tempo, rodamos a limpeza para economizar RAM
                        // Aqui faremos uma verificação simples baseada no tempo do pacote atual
                        if (new DateTime(entry.TimestampTicks) > _lastCleanupTime.AddSeconds(5))
                        {
                            CleanupExpiredFlows(new DateTime(entry.TimestampTicks));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
        }

        private void ProcessPacket(LogEntry entry)
        {
            // 1. Gerar a Chave do Fluxo (Unidirecional)
            // Usamos string para facilitar leitura agora, mas em prod usamos HashCode
            string key = GenerateFlowKey(entry);

            // 2. Verificar se o fluxo já existe
            if (!_activeFlows.TryGetValue(key, out var flow))
            {
                // Se não existe, cria um novo
                flow = new NetworkFlow
                {
                    FlowKey = key,
                    FirstSeen = new DateTime(entry.TimestampTicks),
                    LastSeen = new DateTime(entry.TimestampTicks),
                    PacketCount = 0,
                    TotalBytes = 0
                };
                _activeFlows.Add(key, flow);
            }

            // 3. Atualizar as estatísticas do fluxo
            flow.Update(entry);
        }

        private string GenerateFlowKey(LogEntry entry)
        {
            // Formato: SrcIP:SrcPort -> DstIP:DstPort (Proto)
            // Nota: Para NIDS real, IP deve ser convertido para string legível apenas na hora do LOG.
            // Aqui simplificamos para você visualizar.
            return $"{IntToIpString(entry.SourceIp)}:{entry.SourcePort}->{IntToIpString(entry.DestIp)}:{entry.DestPort}({entry.Protocol})";
        }

        private void CleanupExpiredFlows(DateTime currentTime)
        {
            // Encontra chaves de fluxos inativos
            var expiredKeys = new List<string>();
            
            foreach (var kvp in _activeFlows)
            {
                var flow = kvp.Value;
                if ((currentTime - flow.LastSeen).TotalSeconds > FLOW_TIMEOUT_SECONDS)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            // Remove e Exporta (Aqui entraria o ML futuramente)
            foreach (var key in expiredKeys)
            {
                var flow = _activeFlows[key];
                
                // MOMENTO DE DECISÃO: O fluxo acabou.
                // 1. Imprimir um resumo?
                // 2. Salvar num CSV para treinar IA?
                // 3. Mandar para o modelo prever se foi ataque?
                
                // Por enquanto, vamos imprimir fluxos "gordos" (com mais de 10 pacotes)
                if (flow.PacketCount > 10)
                {
                    Console.WriteLine($"[FLUXO ENCERRADO] {flow.FlowKey} | Pkts: {flow.PacketCount} | Bytes: {flow.TotalBytes} | SYN/ACK: {flow.SynCount}/{flow.AckCount}");
                }

                _activeFlows.Remove(key);
            }
            
            _lastCleanupTime = currentTime;
        }

        // Método auxiliar apenas para visualização (Log)
        private string IntToIpString(long ipInt)
        {
            try 
            {
                // Converte o long/uint de volta para bytes
                byte[] bytes = BitConverter.GetBytes((uint)ipInt);
                
                // Se sua máquina for Little Endian (Intel/AMD padrão), inverte para ler certo
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                return new System.Net.IPAddress(bytes).ToString();
            }
            catch
            {
                return "0.0.0.0";
            }
        }
    
}
