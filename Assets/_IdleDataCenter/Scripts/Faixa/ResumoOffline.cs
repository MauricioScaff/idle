using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Aviso "Enquanto você estava fora", mostrado ao abrir o jogo: quanto tempo passou, quanto rendeu
    /// e o que o técnico consertou. Fica acima da faixa, na escala do painel, e some no "OK" ou sozinho.
    /// </summary>
    public class ResumoOffline : MonoBehaviour, IClicavel
    {
        public const int Largura = 236, Altura = 62;
        const float Duracao = 25f;

        Faixa faixa;
        Economia economia;
        PixelCanvas tela;
        SpriteRenderer sr;
        BoxCollider2D colisor;
        RectInt botaoOk;
        Vector2Int cursor = new Vector2Int(-1, -1);
        float fechaEm, proximoDesenho;

        public bool Aberto { get; private set; }
        public int Ordem => 21;

        public void Iniciar(Faixa faixa, Economia economia)
        {
            this.faixa = faixa;
            this.economia = economia;
            tela = new PixelCanvas(Largura, Altura);
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = tela.Sprite;
            sr.sortingOrder = 31;
            colisor = gameObject.AddComponent<BoxCollider2D>();
            colisor.size = new Vector2(Largura, Altura);
            colisor.offset = new Vector2(Largura / 2f, Altura / 2f);
            botaoOk = new RectInt(Largura - 50, Altura - 20, 42, 14);
            Exibir(false);
        }

        void Exibir(bool sim)
        {
            Aberto = sim;
            sr.enabled = sim;
            colisor.enabled = sim;
        }

        public void Mostrar()
        {
            Exibir(true);
            fechaEm = Time.time + Duracao;
            Desenhar();
        }

        public void Fechar()
        {
            if (!Aberto) return;
            Exibir(false);
            faixa.AoFecharResumo();
        }

        Vector2Int ParaLocal(Vector2 mundo)
        {
            float s = transform.localScale.x;
            return new Vector2Int(Mathf.FloorToInt((mundo.x - transform.position.x) / s), Altura - 1 - Mathf.FloorToInt((mundo.y - transform.position.y) / s));
        }

        public void DefinirCursor(Vector2 mundo) => cursor = ParaLocal(mundo);

        public void ClicarEm(Vector2 mundo)
        {
            Sons.Tique();
            Fechar(); // qualquer clique no aviso fecha (o "OK" é só o convite)
        }

        public void Clicar() { }
        public void DefinirDestaque(bool ligado) { if (!ligado) cursor = new Vector2Int(-1, -1); }

        void Update()
        {
            if (!Aberto) return;
            if (Time.time >= fechaEm) { Fechar(); return; }
            if (Time.time < proximoDesenho) return;
            proximoDesenho = Time.time + 0.25f;
            Desenhar();
        }

        void Desenhar()
        {
            var C = (System.Func<string, Color32>)PixelCanvas.C;
            tela.Limpar(C("#1b1a2e"));
            tela.Ret(1, 1, Largura - 2, Altura - 2, C("#20233d"));
            tela.Ret(1, 1, Largura - 2, 12, C("#2f3263"));
            tela.Texto("Enquanto você estava fora", 6, 4, C("#ffd65c"));
            string tempo = FormatarTempo(economia.SegundosFora);
            tela.Texto(tempo, Largura - 6 - PixelCanvas.LarguraTexto(tempo), 4, C("#a9c7ff"), false);

            tela.Texto("+R$ " + Faixa.Formatar(economia.GanhoFora), 6, 18, C("#ffd65c"), true, 2);
            tela.Texto("(50% da receita normal)", 6, 31, C("#7d82ad"), false);
            string conserto = economia.ConsertadosFora == 0 ? "Nenhum servidor travou."
                            : economia.ConsertadosFora == 1 ? "O técnico consertou 1 servidor."
                            : $"O técnico consertou {economia.ConsertadosFora} servidores.";
            tela.Texto(conserto, 6, 40, C("#6fd36f"), false);
            if (economia.PassouDoLimite) tela.Texto("Rendeu só as primeiras 12 h.", 6, 49, C("#ff9fae"), false);

            bool sobre = botaoOk.Contains(cursor);
            tela.Ret(botaoOk.x, botaoOk.y, botaoOk.width, botaoOk.height, C("#1b1a2e"));
            tela.Ret(botaoOk.x + 1, botaoOk.y + 1, botaoOk.width - 2, botaoOk.height - 2, C(sobre ? "#8ae88a" : "#6fd36f"));
            tela.Texto("OK", botaoOk.x + (botaoOk.width - PixelCanvas.LarguraTexto("OK")) / 2, botaoOk.y + 5, C("#1b1a2e"), false);

            // barrinha mostrando quanto falta para sumir sozinho
            float resta = Mathf.Clamp01((fechaEm - Time.time) / Duracao);
            tela.Ret(1, Altura - 3, Mathf.RoundToInt((Largura - 2) * resta), 2, C("#3a3f7a"));
            tela.Aplicar();
        }

        static string FormatarTempo(double s)
        {
            int min = (int)(s / 60), h = min / 60, d = h / 24;
            if (d > 0) return $"{d}d {h % 24}h";
            if (h > 0) return $"{h}h {min % 60}min";
            return $"{min}min";
        }
    }
}
