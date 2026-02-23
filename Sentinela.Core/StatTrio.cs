namespace nids.Sentinela.Core;

// Usamos 'struct' normal (não readonly) porque os valores vão ser atualizados 
// dentro dos arrays da classe principal, garantindo zero alocação de memória Heap.
public struct StatTrio
{
    public double W;  // Peso
    public double LS; // Soma Linear
    public double SS; // Soma Quadrática

    // O método "Coração" do Decaimento
    public void Update(double value, double decayFactor)
    {
        W = (W * decayFactor) + 1;
        LS = (LS * decayFactor) + value;
        SS = (SS * decayFactor) + (value * value);
    }

    // Propriedades calculadas em tempo real (O(1)) para a Inteligência Artificial
    public readonly double Mean => W > 0 ? LS / W : 0;
    
    // Usamos Math.Max para evitar que imprecisões de ponto flutuante gerem variância negativa
    public readonly double Variance => W > 0 ? Math.Max(0, (SS / W) - (Mean * Mean)) : 0;
}
