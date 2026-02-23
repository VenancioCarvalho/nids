namespace nids.Sentinela.Core;

public class NetworkFlow{
        // Identificador único (A Chave do Dicionário)
        // Ex: "192.168.1.15:443 -> 10.0.0.5:55444 (6)"
        public string?
         FlowKey { get; set; }

        // --- Features Temporais ---
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        
        // Propriedade calculada para o ML 
        public double DurationSeconds => (LastSeen - FirstSeen).TotalSeconds;

        // --- Features de Volume ---
        public long PacketCount { get; set; }
        public long TotalBytes { get; set; }

        // Método para atualizar o fluxo com um novo pacote
        public void Update(LogEntry packet)
        {
            LastSeen = new DateTime(packet.TimestampTicks);
            PacketCount++;
            TotalBytes += packet.OriginalLength;
        }
}

    
