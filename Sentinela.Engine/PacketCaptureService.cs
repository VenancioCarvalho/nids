namespace nids.Sentinela.Engine;
using System.Threading.Channels;
using System.Net;
using nids.Sentinela.Core;
using SharpPcap;


public class PacketCaptureService{

    private readonly ChannelWriter<LogEntry> writer;

    public PacketCaptureService(ChannelWriter<LogEntry> channel)
    {
        writer = channel;
    }
    
    public void OnPacketArrival(object sender, PacketCapture e){
        uint protocol = 0;
        ushort srcPort = 0;
        ushort dstPort = 0;

        var rawPacket = e.GetPacket(); // Captura o pacote bruto
        var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
        
        // Tenta extrair informações do protocolo IP(Camada 3)
        var ipPacket = packet.Extract<PacketDotNet.IPPacket>();
        if (ipPacket == null) return; 
        uint srcIp = IpToUint(ipPacket.SourceAddress); // Converte o ip para um inteiro de 32 bits.
        uint dstIp = IpToUint(ipPacket.DestinationAddress);
        
        // Tenta extrair informações dos protocolos TCP/UDP (camada 4)
        var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
        var udpPacket = packet.Extract<PacketDotNet.UdpPacket>();

        // Lógica para pegar portas e flags dependendo do protocolo
        if (tcpPacket != null)
        {   
            protocol = 6;
            srcPort = tcpPacket.SourcePort;
            dstPort = tcpPacket.DestinationPort;
        }
        else if (udpPacket != null)
        {   
            protocol = 17;
            srcPort = udpPacket.SourcePort;
            dstPort = udpPacket.DestinationPort;
        }

        // 3. Criação do Struct (Na Stack - Rápido)
        var logEntry = new LogEntry
        {
            TimestampTicks = e.Header.Timeval.Date.Ticks, // Usamos Ticks (long)
            SourceIp = srcIp,
            DestIp = dstIp,
            SourcePort = srcPort,
            DestPort = dstPort,
            OriginalLength = rawPacket.Data.Length,
            Protocol= protocol
        };
        
        // Se a fila estiver cheia, ele retorna false e descarta o pacote.
        writer.TryWrite(logEntry);
    }
    

    private static uint IpToUint(IPAddress ipAddress){
        byte[] bytes = ipAddress.GetAddressBytes(); 
        // Inverte bytes se a arquitetura do processador for Little Endian (padrão Intel/AMD)
        // para manter a ordem matemática correta do IP
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt32(bytes, 0);
    }

}// fim classe
