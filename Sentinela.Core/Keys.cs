namespace nids.Sentinela.Core;

// 1. Chave do Host (Foca apenas em quem está enviando)
public readonly record struct HostKey(uint SourceIp);

// 2. Chave do Canal (Foca na conversa entre duas máquinas)
public readonly record struct ChannelKey(uint SourceIp, uint DestIp);

// 3. Chave do Socket (Foca na aplicação específica)
public readonly record struct SocketKey(
    uint SourceIp, 
    ushort SourcePort, 
    uint DestIp, 
    ushort DestPort, 
    uint Protocol
);