namespace IdleDataCenter
{
    /// <summary>Qualquer coisa da faixa que reage a clique. Precisa de um Collider2D no mesmo objeto.</summary>
    public interface IClicavel
    {
        /// <summary>Quando vários estão sob o cursor, ganha o de maior ordem (o que está na frente).</summary>
        int Ordem { get; }
        void Clicar();
    }
}
