namespace Loopia.Hex.Tests
{
    /// <summary>Carta so de teste com disponibilidade controlada (simula Ilha alta sem/com Mola).</summary>
    public class CartaDeTesteControlavel : PlataformaDef
    {
        public bool disponivel;

        public override bool DisponivelParaCompra(MapaDePlataformas mapa) => disponivel;
    }
}
