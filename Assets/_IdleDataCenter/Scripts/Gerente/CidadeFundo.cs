using System;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// A cidade atrás da sala no modo gerente, conforme o relógio do computador: céu de dia, pôr do sol,
    /// noite com lua e estrelas, e janelas dos prédios que acendem ao anoitecer.
    /// Desenhada em baixa resolução numa PixelCanvas e redesenhada só a cada poucos minutos.
    /// </summary>
    public class CidadeFundo
    {
        public const int Largura = 480, Altura = 300;

        struct Predio { public int x, w, h, camada; public int semente; }

        readonly PixelCanvas tela = new PixelCanvas(Largura, Altura);
        readonly Predio[] predios;
        int desenhadoEm = -1;   // minuto do dia do último desenho (arredondado)

        public Texture2D Textura => tela.Textura;

        /// <summary>Para testes: força uma hora do dia (0 a 24); negativo usa o relógio.</summary>
        public float HoraFixa = -1;

        public CidadeFundo()
        {
            var sorteio = new System.Random(7);
            var lista = new System.Collections.Generic.List<Predio>();
            for (int camada = 0; camada < 2; camada++)
            {
                int x = -6;
                while (x < Largura)
                {
                    int w = 14 + sorteio.Next(24), h = camada == 0 ? 70 + sorteio.Next(80) : 40 + sorteio.Next(50);
                    lista.Add(new Predio { x = x, w = w, h = h, camada = camada, semente = sorteio.Next() });
                    x += w + (camada == 0 ? 3 : 2);
                }
            }
            predios = lista.ToArray();
        }

        float Hora => HoraFixa >= 0 ? HoraFixa : (float)DateTime.Now.TimeOfDay.TotalHours;

        /// <summary>Redesenha se a hora mudou o bastante (a cada 5 minutos).</summary>
        public void Atualizar()
        {
            float hora = Hora;
            int bloco = Mathf.FloorToInt(hora * 12);
            if (bloco == desenhadoEm) return;
            desenhadoEm = bloco;
            Desenhar(hora);
        }

        // cores do céu (topo, horizonte) por hora do dia
        static readonly (float hora, string topo, string horizonte)[] Ceu =
        {
            (0f, "0b1330", "1d2a55"), (5f, "0e1636", "2a2f5c"), (6f, "3a4f8f", "f0a070"), (7.5f, "5f9ae0", "bfe0f5"),
            (16.5f, "4f8ad8", "b5d8f0"), (18f, "40407f", "ff9a5c"), (19.3f, "1f2150", "8a4a6a"), (20.5f, "0d1433", "25305e"), (24f, "0b1330", "1d2a55"),
        };

        static Color CorDoCeu(float hora, bool topo)
        {
            for (int i = 0; i < Ceu.Length - 1; i++)
                if (hora >= Ceu[i].hora && hora <= Ceu[i + 1].hora)
                {
                    float t = Mathf.InverseLerp(Ceu[i].hora, Ceu[i + 1].hora, hora);
                    return Color.Lerp(IsoGui.Cor(topo ? Ceu[i].topo : Ceu[i].horizonte), IsoGui.Cor(topo ? Ceu[i + 1].topo : Ceu[i + 1].horizonte), t);
                }
            return IsoGui.Cor(topo ? Ceu[0].topo : Ceu[0].horizonte);
        }

        /// <summary>0 = dia claro, 1 = noite fechada.</summary>
        static float Escuridao(float hora)
        {
            if (hora < 5f || hora > 20.5f) return 1;
            if (hora < 7.5f) return 1 - Mathf.InverseLerp(5f, 7.5f, hora);
            if (hora > 17.5f) return Mathf.InverseLerp(17.5f, 20.5f, hora);
            return 0;
        }

        void Desenhar(float hora)
        {
            float noite = Escuridao(hora);
            Color topo = CorDoCeu(hora, true), horizonte = CorDoCeu(hora, false);
            for (int y = 0; y < Altura; y += 2)
                tela.Ret(0, y, Largura, 2, (Color32)Color.Lerp(topo, horizonte, Mathf.Pow(y / (float)Altura, 1.4f)));

            // estrelas
            if (noite > 0.5f)
            {
                var s = new System.Random(3);
                for (int i = 0; i < 70; i++) tela.Pixel(s.Next(Largura), s.Next(Altura / 2), (Color32)Color.Lerp(horizonte, Color.white, 0.4f + 0.6f * (noite - 0.5f) * 2 * (float)s.NextDouble()));
            }

            // sol (6h às 18h30) ou lua, num arco da esquerda para a direita
            bool dia = hora >= 6f && hora < 18.5f;
            float f = dia ? Mathf.InverseLerp(6f, 18.5f, hora) : Mathf.InverseLerp(0, 11.5f, hora >= 18.5f ? hora - 18.5f : hora + 5.5f);
            int cx = Mathf.RoundToInt(Mathf.Lerp(40, Largura - 40, f)), cy = Mathf.RoundToInt(Altura * 0.45f - Mathf.Sin(f * Mathf.PI) * Altura * 0.33f);
            if (dia)
            {
                var brilho = Color.Lerp(IsoGui.Cor("fff2b0"), IsoGui.Cor("ff9a50"), 1 - Mathf.Sin(f * Mathf.PI));
                tela.Circulo(cx, cy, 11, (Color32)Color.Lerp(horizonte, brilho, 0.35f));
                tela.Circulo(cx, cy, 8, (Color32)brilho);
            }
            else
            {
                tela.Circulo(cx, cy, 7, (Color32)IsoGui.Cor("f4efd8"));
                tela.Circulo(cx + 3, cy - 2, 6, (Color32)topo);   // lua crescente
            }

            // prédios: os do fundo mais claros (névoa), os da frente mais escuros; janelas acendem à noite
            foreach (var p in predios)
            {
                var r = new System.Random(p.semente);
                bool fundo = p.camada == 0;
                var cor = Color.Lerp(fundo ? Color.Lerp(horizonte, IsoGui.Cor("1a2448"), 0.55f) : IsoGui.Cor("141d3c"),
                                     fundo ? IsoGui.Cor("7d93b8") : IsoGui.Cor("51648c"), 1 - noite);
                int topoY = Altura - p.h - (fundo ? 20 : 0);
                tela.Ret(p.x, topoY, p.w, Altura - topoY, (Color32)cor);
                if (!fundo && r.Next(3) == 0) tela.Ret(p.x + p.w / 2 - 1, topoY - 6, 2, 6, (Color32)cor);   // antena
                var luz = Color.Lerp(Color.Lerp(cor, IsoGui.Cor("c9d8ee"), 0.35f), IsoGui.Cor(fundo ? "8a93c0" : "ffd98a"), noite);
                for (int y = topoY + 4; y < Altura - 4; y += 6)
                    for (int x = p.x + 3; x < p.x + p.w - 3; x += 5)
                        if (r.NextDouble() < 0.25f + noite * 0.25f) tela.Ret(x, y, 2, 3, (Color32)luz);
            }
            tela.Ret(0, Altura - 6, Largura, 6, (Color32)Color.Lerp(IsoGui.Cor("0c1328"), IsoGui.Cor("3d4a63"), 1 - noite));
            tela.Aplicar();
        }
    }
}
