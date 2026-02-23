
namespace nids.Sentinela.Core;

public class FeatureTracker
{
    // O relógio mestre dessa conexão
    public long LastSeenMilliseconds { get; private set; }

    // As janelas de tempo do Kitsune (ex: 100ms, 500ms, 1.5s, 10s, 1min)
    // Representadas aqui como lambdas para a fórmula de decaimento
    private readonly double[] _lambdas;

    // Arrays de structs (O C# aloca isso de forma contígua na memória, super rápido)
    // Tamanho do array = Quantidade de lambdas (5)
    public StatTrio[] SizeStats { get; }
    public StatTrio[] JitterStats { get; }

    // Construtor: Chamado APENAS quando uma chave nova aparece na rede
    public FeatureTracker(double[] lambdas, long currentMilliseconds)
    {
        _lambdas = lambdas;
        LastSeenMilliseconds = currentMilliseconds;
        
        SizeStats = new StatTrio[lambdas.Length];
        JitterStats = new StatTrio[lambdas.Length];
    }

    // Método chamado para CADA PACOTE que pertence a esta chave
    public void ExtractUpdate(long currentMilliseconds, double packetSize)
    {
        // 1. Calcula o Delta T em segundos
        double deltaT = (currentMilliseconds - LastSeenMilliseconds) / 1000.0;
        
        // Proteção contra pacotes que chegam fora de ordem (Time Drift)
        if (deltaT < 0) deltaT = 0; 

        // O Jitter (Variação de tempo) é literalmente o próprio Delta T
        double jitter = deltaT; 

        // 2. Itera sobre as 5 janelas de tempo simultaneamente
        for (int i = 0; i < _lambdas.Length; i++)
        {
            // Fórmula base do artigo: d = 2^(-lambda * deltaT)
            double decayFactor = Math.Pow(2, -_lambdas[i] * deltaT);

            // 3. Atualiza os Trios! (Isso executa a struct StatTrio)
            SizeStats[i].Update(packetSize, decayFactor);
            JitterStats[i].Update(jitter, decayFactor);
        }

        // 4. Atualiza o relógio mestre para o próximo pacote
        LastSeenMilliseconds = currentMilliseconds;
    }
}
