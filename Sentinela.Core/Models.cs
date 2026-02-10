namespace nids.Sentinela.Core;
using System.Net;

public readonly record struct LogEntry
    {
        public long TimestampTicks { get; init; } // Usar Ticks é mais leve que o objeto DateTime
        
        // IPs como inteiros 
        public uint SourceIp { get; init; }
        public uint DestIp { get; init; }
        
        // Portas como inteiros curtos 
        public ushort SourcePort { get; init; }
        public ushort DestPort { get; init; }
        
        // Flags
        public bool Synchronize { get; init; }
        public bool Acknowledgment { get; init; }
        public bool Finished { get; init; }
        public bool Reset { get; init; }
        
        public int OriginalLength { get; init; }

        // Só usamos isso na hora de exibir, não é usado na fase de processamento.
        public string SourceIpString => new IPAddress(BitConverter.GetBytes(SourceIp).Reverse().ToArray()).ToString();
    }