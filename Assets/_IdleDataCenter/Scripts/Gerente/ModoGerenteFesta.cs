using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Festa nas compras: confete estoura de onde se clicou e, se o equipamento aparece na sala, de cima dele também. A
    /// primeira unidade de cada coisa ganha a faixa "Novo!"; completar o nível máximo, a faixa "Completo!".
    /// </summary>
    public partial class ModoGerente
    {
        struct Confete { public Vector2 pos, vel; public Color cor; public float nasceu, giro; }

        readonly List<Confete> confetes = new List<Confete>();
        string novidadeTitulo, novidadeNome;
        float novidadeDesde = -10;
        static readonly string[] CoresDoConfete = { "ffd65c", "5cff8a", "5cc8ff", "ff8cc6", "b48cff", "ffa53c" };

        void FestaDaCompra(string id)
        {
            if (!Aberto) return;
            var def = Catalogo.Buscar(id);
            // de onde se clicou (o botão da loja ou o "+" da sala)
            if (Event.current != null) Estourar(Event.current.mousePosition, 22, 1f);
            // e de cima do equipamento, se ele está na sala que aparece agora
            var lugar = VistaAtual == SalaIso.Vista.Sala || EmArea ? salaIso?.LugarDaCompra(id) : null;
            if (lugar.HasValue) Estourar(NaTela(lugar.Value), 34, 1.3f);

            if (E.Nivel(id) == 1) { novidadeTitulo = "Novo!"; novidadeNome = NomeLongo(id); novidadeDesde = Time.unscaledTime; }
            else if (E.NoMaximo(id) && !def.Gerador) { novidadeTitulo = "Completo!"; novidadeNome = NomeLongo(id) + " no nível máximo"; novidadeDesde = Time.unscaledTime; }
        }

        void Estourar(Vector2 centro, int quantos, float forca)
        {
            float agora = Time.unscaledTime;
            for (int i = 0; i < quantos; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2), v = Random.Range(120f, 320f) * forca;
                confetes.Add(new Confete
                {
                    pos = centro, vel = new Vector2(Mathf.Cos(ang) * v, Mathf.Sin(ang) * v * 0.7f - 180 * forca),
                    cor = IsoGui.Cor(CoresDoConfete[i % CoresDoConfete.Length]), nasceu = agora, giro = Random.Range(0f, 6f),
                });
            }
        }

        /// <summary>Desenha e anima o confete (cai com gravidade e some) e a faixa da novidade.</summary>
        void Confetes()
        {
            float agora = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (int i = confetes.Count - 1; i >= 0; i--)
            {
                var c = confetes[i];
                float idade = agora - c.nasceu;
                if (idade > 1.6f) { confetes.RemoveAt(i); continue; }
                // o OnGUI roda mais de uma vez por quadro: só anda no Repaint
                if (Event.current.type == EventType.Repaint)
                {
                    c.vel.y += 520 * dt;
                    c.vel *= 1 - 1.2f * dt;
                    c.pos += c.vel * dt;
                    confetes[i] = c;
                }
                var cor = c.cor; cor.a = Mathf.Clamp01(1.6f - idade);
                float largura = 3 + 2 * Mathf.Abs(Mathf.Sin(idade * 9 + c.giro));   // o papelzinho girando
                ui.Ret(new Rect(c.pos.x, c.pos.y, largura, 5), cor);
            }

            float t = agora - novidadeDesde;
            if (t > 2.6f || novidadeTitulo == null) return;
            float a = Mathf.Clamp01(Mathf.Min(t * 6, (2.6f - t) * 3));
            float largura2 = Mathf.Max(ui.Largura(novidadeNome, 3), ui.Largura(novidadeTitulo, 4)) + 60;
            float sobe = (1 - Mathf.Clamp01(t * 5)) * 16;
            var r = new Rect(W / 2 - largura2 / 2, 196 + sobe, largura2, 76);
            var fundo = IsoGui.Cor("173a2e"); fundo.a = a;
            var borda = IsoGui.Verde; borda.a = a;
            ui.Caixa(r, fundo, borda);
            var ouro = Ouro; ouro.a = a;
            var branco = IsoGui.Branco; branco.a = a;
            ui.Texto(novidadeTitulo, r.center.x, r.y + 12, ouro, 4, true);
            ui.Texto(novidadeNome, r.center.x, r.y + 46, branco, 3, true);
        }
    }
}
