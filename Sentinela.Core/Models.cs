namespace nids.Sentinela.Core;
using System.Net;

public readonly record struct LogEntry
    {
        // 1. O Relógio (Obrigatório para o Decaimento)
        public long TimestampTicks { get; init; } 
        
        // 2. Identificação (Obrigatório para gerar as Chaves Hash)
        public uint SourceIp { get; init; }
        public uint DestIp { get; init; }
        public ushort SourcePort { get; init; }
        public ushort DestPort { get; init; }
        public uint Protocol { get; init; } // byte é suficiente (ex: 6 para TCP, 17 para UDP)
        
        // 3. A Característica Física (Obrigatório para as Médias/Variâncias)
        public int OriginalLength { get; init; }

        // Só usamos isso na hora de exibir, não é usado na fase de processamento.
        public string SourceIpString => new IPAddress(BitConverter.GetBytes(SourceIp).Reverse().ToArray()).ToString();
    }