namespace Loopia.Hex
{
    /// <summary>
    /// Os estados de uma jornada: acampamento -> 4 voltas -> chefe -> fim.
    /// Vitoria e Derrota sao terminais: depois de um deles nada mais muda.
    /// </summary>
    public enum EstadoDaPartida
    {
        /// <summary>A cena ainda esta montando (mapa, bake). Gancho para uma futura fase de colocacao.</summary>
        Preparando,

        /// <summary>O Lucca anda pelo anel e as voltas contam.</summary>
        Correndo,

        /// <summary>Confronto final: as voltas pararam de contar e o chefe esta no mapa.</summary>
        Chefe,

        /// <summary>O chefe caiu.</summary>
        Vitoria,

        /// <summary>O Lucca caiu.</summary>
        Derrota,
    }
}
