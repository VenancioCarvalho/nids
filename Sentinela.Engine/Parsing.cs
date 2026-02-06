
namespace nids.Sentinela.Engine;
using SharpPcap;
public class Analizador{

    public DateTime? StartTimestamp = null;
    public DateTime LastPacketTimestamp;
   
    public readonly struct LogEntry
{   
    public DateTime Timestamp { get; }
    public string SourceAddress { get; }
    public string DestinationAddress { get; }
    public string SourcePort { get; }
    public string DestinationPort { get; }
    public bool Synchronize { get; }
    public bool Acknowledgment { get; }
    public bool Finished { get; }
    public bool Reset { get; }

    public LogEntry(
        DateTime timestamp, 
        string sourceAddress, 
        string destinationAddress, 
        string sourcePort, 
        string destinationPort, 
        bool synchronize, 
        bool acknowledgment, 
        bool finished, 
        bool reset)
    {
        Timestamp = timestamp;
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        SourcePort = sourcePort;
        DestinationPort = destinationPort;
        Synchronize = synchronize;
        Acknowledgment = acknowledgment;
        Finished = finished;
        Reset = reset;
    }
}


    public void OnPacketArrival(object sender, PacketCapture e)
    {
        var rawPacket = e.GetPacket();
        DateTime ts = rawPacket.Timeval.Date;

        // Atualiza métricas de tempo
        if (StartTimestamp == null) StartTimestamp = ts;
        LastPacketTimestamp = ts;

        var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
        // Tenta extrair a camada IP (IPv4 ou IPv6)
        var ipPacket = packet.Extract<PacketDotNet.IPPacket>();

        if (ipPacket != null){
        
            Console.WriteLine($"TS: {ts:HH:mm:ss.fff}");
            Console.WriteLine($"Origem: {ipPacket.SourceAddress} -> Destino: {ipPacket.DestinationAddress}");
            
            // Tenta extrair a camada de Transporte (TCP ou UDP)
            var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
            //tamanho do payload
            
            if (tcpPacket != null){
                Console.WriteLine($"Porta TCP: {tcpPacket.SourcePort} -> {tcpPacket.DestinationPort}");
                Console.WriteLine($"Flags: SYN: {tcpPacket.Synchronize}, ACK: {tcpPacket.Acknowledgment}, FIN: {tcpPacket.Finished}, RST: {tcpPacket.Reset}");
            }
        }
    }
}