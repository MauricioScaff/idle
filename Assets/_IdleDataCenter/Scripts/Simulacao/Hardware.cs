using System;

namespace IdleDataCenter.Simulacao
{
    public static partial class Catalogo
    {
        /// <summary>Até aqui (segundos de jogo) os servidores estão na garantia; depois falham mais.</summary>
        public const double FimDaGarantia = 3600, FimDaVida = 7200;
        public const double FalhasForaDaGarantia = 2, FalhasNoFimDaVida = 3;
        /// <summary>O refresh troca o parque todo por hardware novo: custa alguns minutos de receita (mínimo R$ 500).</summary>
        public const double SegundosDoRefresh = 120, RefreshMinimo = 3000;
    }

    /// <summary>
    /// Ciclo de vida do hardware: as torres e os servidores 1U envelhecem enquanto o jogo roda. Na garantia (1 h) tudo
    /// normal; fora da garantia travam 2x mais; no fim da vida (2 h), 3x. O refresh troca o parque por hardware novo e
    /// zera a idade. (Racks cheios, containers e nós não envelhecem: são máquinas de outra geração, alugadas com suporte.)
    /// </summary>
    public partial class Economia
    {
        /// <summary>A garantia venceu (true) ou o hardware chegou ao fim da vida (false).</summary>
        public event Action<bool> HardwareEnvelheceu;

        public double IdadeDosServidores => Estado.idadeServidores;
        public bool ForaDaGarantia => Estado.idadeServidores >= Catalogo.FimDaGarantia;
        public bool FimDaVida => Estado.idadeServidores >= Catalogo.FimDaVida;

        /// <summary>Quantas vezes mais os servidores travam por causa da idade.</summary>
        public double FatorIdade => FimDaVida ? Catalogo.FalhasNoFimDaVida : ForaDaGarantia ? Catalogo.FalhasForaDaGarantia : 1;

        public double CustoDoRefresh => Math.Max(Catalogo.RefreshMinimo, Math.Round(ReceitaPorSegundo * Catalogo.SegundosDoRefresh));
        public bool PodeFazerRefresh => ForaDaGarantia && Estado.dinheiro >= CustoDoRefresh;

        void AvancarIdade(double segundos)
        {
            if (TotalServidores == 0) return;
            bool garantiaAntes = ForaDaGarantia, vidaAntes = FimDaVida;
            Estado.idadeServidores += segundos;
            if (!garantiaAntes && ForaDaGarantia) HardwareEnvelheceu?.Invoke(true);
            else if (!vidaAntes && FimDaVida) HardwareEnvelheceu?.Invoke(false);
        }

        /// <summary>Troca o parque por hardware novo. Retorna se deu (precisa estar fora da garantia e ter o dinheiro).</summary>
        public bool FazerRefresh()
        {
            if (!PodeFazerRefresh) return false;
            Estado.dinheiro -= CustoDoRefresh;
            Estado.idadeServidores = 0;
            Conquistar(Catalogo.ConquistaRefresh);
            return true;
        }
    }
}
