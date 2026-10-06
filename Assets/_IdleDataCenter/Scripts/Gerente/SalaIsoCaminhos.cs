using System.Collections.Generic;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Caminhos de verdade nas salas que declaram obstáculos (quarto e escritório): cada móvel ocupa um retângulo do piso
    /// (em casas), a pessoa procura o caminho numa grade fina (A*, com diagonais e sem cortar quina), com folga dos móveis,
    /// e o trajeto é esticado (só os pontos onde ela precisa virar). Assim ninguém atravessa móvel nem anda raspando neles.
    /// Atrás dos móveis altos não sobra corredor: a pessoa (desenhada por cima da ilustração) nunca fica "atrás" de algo.
    /// </summary>
    public partial class SalaIso
    {
        /// <summary>Um móvel: o retângulo do piso que ele ocupa (gx0, gy0, gx1, gy1 em casas) e a altura dele na imagem (pixels).</summary>
        struct Obstaculo { public Rect piso; public float altura; }

        /// <summary>Os móveis da sala, montados a cada desenho.</summary>
        readonly List<Obstaculo> obstaculos = new List<Obstaculo>();

        /// <summary>O corpo de uma pessoa na imagem, a partir dos pés (largura e altura em pixels): o que pode encostar num móvel.</summary>
        const float LarguraDoCorpo = 22, AlturaDoCorpo = 46;

        /// <summary>Teste: pinta na imagem as casas bloqueadas (ExportarSalas -obstaculos).</summary>
        public bool MostrarObstaculos { get; set; }

        const float PassoDaGrade = 0.2f;    // tamanho da casinha da busca, em casas
        const float Folga = 0.18f;          // distância mínima dos móveis
        const float FolgaDaParede = 0.3f;   // e das paredes / da borda do piso

        bool UsaCaminhos => obstaculos.Count > 0 && areaAtual == null;

        /// <summary>Um móvel no piso (em casas); altura: quantos pixels ele sobe na imagem a partir do chão (0 = rente ao chão).</summary>
        void Bloquear(float gx0, float gy0, float gx1, float gy1, float altura = 0) =>
            obstaculos.Add(new Obstaculo { piso = Rect.MinMaxRect(gx0, gy0, gx1, gy1), altura = altura });

        /// <summary>
        /// A pessoa pode ficar aqui? Não pode estar dentro de um móvel (com folga), nem atrás de um móvel encostando nele na
        /// imagem: as pessoas são desenhadas por cima da ilustração, então quem está atrás pareceria em cima dele.
        /// </summary>
        bool Livre(Vector2 g)
        {
            float n = Sala.casas;
            if (g.x < FolgaDaParede || g.y < FolgaDaParede || g.x > n - FolgaDaParede || g.y > n - FolgaDaParede) return false;
            foreach (var o in obstaculos)
            {
                var r = o.piso;
                if (g.x > r.xMin - Folga && g.x < r.xMax + Folga && g.y > r.yMin - Folga && g.y < r.yMax + Folga) return false;
                // atrás do móvel (no isométrico: menor nos dois eixos que a frente dele) e encostando nele na imagem
                if (o.altura <= 0 || g.x >= r.xMax || g.y >= r.yMax) continue;
                if (CorpoEncosta(PontoQuebrado(g.x, g.y), Contorno(o))) return false;
            }
            return true;
        }

        /// <summary>
        /// O contorno do móvel na imagem: uma caixa isométrica vista de cima vira um hexágono (o canto do fundo no alto, os
        /// cantos da esquerda e da direita subindo pela altura, o canto da frente no chão). Em ordem, para o teste de dentro.
        /// </summary>
        Vector2[] Contorno(Obstaculo o)
        {
            var r = o.piso;
            var fundo = PontoQuebrado(r.xMin, r.yMin); var direita = PontoQuebrado(r.xMax, r.yMin);
            var frente = PontoQuebrado(r.xMax, r.yMax); var esquerda = PontoQuebrado(r.xMin, r.yMax);
            var h = new Vector2(0, o.altura);
            return new[] { fundo - h, direita - h, direita, frente, esquerda, esquerda - h };
        }

        /// <summary>Algum ponto do corpo (pés p, largura e altura da pessoa) cai dentro do contorno do móvel?</summary>
        static bool CorpoEncosta(Vector2 p, Vector2[] contorno)
        {
            for (int i = 0; i <= 2; i++)
                for (int j = 0; j <= 3; j++)
                    if (Dentro(new Vector2(p.x + (i - 1) * LarguraDoCorpo / 2, p.y - j * AlturaDoCorpo / 3), contorno)) return true;
            return false;
        }

        /// <summary>Ponto dentro de um polígono convexo (vértices em ordem, horária ou anti-horária).</summary>
        static bool Dentro(Vector2 q, Vector2[] poligono)
        {
            int sinal = 0;
            for (int i = 0; i < poligono.Length; i++)
            {
                var a = poligono[i]; var b = poligono[(i + 1) % poligono.Length];
                float cruz = (b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x);
                if (Mathf.Abs(cruz) < 1e-4f) continue;
                int s = cruz > 0 ? 1 : -1;
                if (sinal == 0) sinal = s; else if (s != sinal) return false;
            }
            return true;
        }

        int Celulas => Mathf.CeilToInt(Sala.casas / PassoDaGrade);
        Vector2 Centro(int i, int j) => new Vector2((i + 0.5f) * PassoDaGrade, (j + 0.5f) * PassoDaGrade);

        /// <summary>A casinha livre mais perto de um ponto (o próprio ponto pode estar dentro da folga de um móvel).</summary>
        Vector2Int MaisPertoLivre(Vector2 g, Trabalhador quem = null)
        {
            int n = Celulas;
            // onde os outros estão ou vão parar: ninguém para em cima de ninguém (0,6 casa de distância)
            var ocupados = new List<Vector2>();
            if (quem != null) foreach (var o in pessoas.Values) if (o != quem) ocupados.Add(Destino(o));
            var melhor = new Vector2Int(-1, -1);
            float dist = float.MaxValue;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var c = Centro(i, j);
                    if (!Livre(c)) continue;
                    bool perto = false;
                    foreach (var o in ocupados) if ((o - c).sqrMagnitude < 0.36f) { perto = true; break; }
                    if (perto) continue;
                    float d = (c - g).sqrMagnitude;
                    if (d < dist) { dist = d; melhor = new Vector2Int(i, j); }
                }
            return melhor;
        }

        /// <summary>Dá para ir em linha reta de a até b sem entrar na folga de nenhum móvel?</summary>
        bool LinhaLivre(Vector2 a, Vector2 b)
        {
            int passos = Mathf.CeilToInt((b - a).magnitude / 0.08f);
            for (int k = 1; k < passos; k++) if (!Livre(Vector2.Lerp(a, b, k / (float)passos))) return false;
            return true;
        }

        /// <summary>O caminho até o destino (A* na grade fina, depois esticado). Destino dentro de móvel: o ponto livre mais perto.</summary>
        void RotaLivre(Trabalhador p, Vector2 destino)
        {
            p.caminho.Clear();
            bool destinoLivre = Livre(destino);
            var fim = MaisPertoLivre(destino, p);   // sem parar em cima de outra pessoa
            bool destinoOcupado = false;
            foreach (var o in pessoas.Values) if (o != p && (Destino(o) - destino).sqrMagnitude < 0.36f) destinoOcupado = true;
            var inicio = MaisPertoLivre(p.pos);
            if (fim.x < 0 || inicio.x < 0) { p.caminho.Add(destino); return; }

            int n = Celulas;
            var custo = new float[n, n];
            var veio = new Vector2Int[n, n];
            var fechado = new bool[n, n];
            for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) custo[i, j] = float.MaxValue;
            var aberta = new List<Vector2Int> { inicio };
            custo[inicio.x, inicio.y] = 0;
            veio[inicio.x, inicio.y] = inicio;
            float H(Vector2Int c) { int dx = Mathf.Abs(c.x - fim.x), dy = Mathf.Abs(c.y - fim.y); return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy); }
            bool achou = false;
            while (aberta.Count > 0)
            {
                int melhor = 0;
                for (int k = 1; k < aberta.Count; k++)
                    if (custo[aberta[k].x, aberta[k].y] + H(aberta[k]) < custo[aberta[melhor].x, aberta[melhor].y] + H(aberta[melhor])) melhor = k;
                var c = aberta[melhor];
                aberta.RemoveAt(melhor);
                if (fechado[c.x, c.y]) continue;
                fechado[c.x, c.y] = true;
                if (c == fim) { achou = true; break; }
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var v = new Vector2Int(c.x + dx, c.y + dy);
                        if (v.x < 0 || v.y < 0 || v.x >= n || v.y >= n || fechado[v.x, v.y] || !Livre(Centro(v.x, v.y))) continue;
                        // diagonal só se as duas casinhas do lado estão livres (não corta a quina do móvel)
                        if (dx != 0 && dy != 0 && (!Livre(Centro(c.x + dx, c.y)) || !Livre(Centro(c.x, c.y + dy)))) continue;
                        float novo = custo[c.x, c.y] + (dx != 0 && dy != 0 ? 1.414f : 1f);
                        if (novo >= custo[v.x, v.y]) continue;
                        custo[v.x, v.y] = novo;
                        veio[v.x, v.y] = c;
                        aberta.Add(v);
                    }
            }
            if (!achou) { p.caminho.Add(destino); return; }

            // de trás para a frente, e depois só os pontos de virada (o resto é linha reta livre)
            var pontos = new List<Vector2>();
            for (var c = fim; c != inicio; c = veio[c.x, c.y]) pontos.Add(Centro(c.x, c.y));
            pontos.Reverse();
            if (destinoLivre && !destinoOcupado) { if (pontos.Count > 0) pontos[pontos.Count - 1] = destino; else pontos.Add(destino); }
            var atual = p.pos;
            int i0 = 0;
            while (i0 < pontos.Count)
            {
                int longe = i0;
                for (int k = pontos.Count - 1; k > i0; k--) if (LinhaLivre(atual, pontos[k])) { longe = k; break; }
                p.caminho.Add(pontos[longe]);
                atual = pontos[longe];
                i0 = longe + 1;
            }
        }

        /// <summary>Teste: as casinhas bloqueadas em vermelho translúcido.</summary>
        void DesenharObstaculos()
        {
            if (!MostrarObstaculos) return;
            int n = UsaCaminhos ? Celulas : 0;
            var vermelho = new Color32(255, 40, 60, 90);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var c = Centro(i, j);
                    if (Livre(c)) continue;
                    var p = IP(c.x, c.y);
                    tela.Ret(p.x - 1, p.y - 1, 3, 2, vermelho);
                }
            // e os caminhos da mesa do técnico até cada chamado e ponto de ronda (pontinhos amarelos)
            var destinos = new List<Vector2>();
            foreach (var l in lugaresDeChamado) destinos.Add(l.grade);
            foreach (var r in pontosDeRonda) destinos.Add(r.lugar);
            if (tecnicoConsertando) destinos.Add(lugarDoConserto);
            foreach (var d in destinos)
            {
                var teste = new Trabalhador { pos = Mesa.lugar };
                Rota(teste, d);   // a rota que a sala usa de verdade (A* com obstáculos, ou os corredores)
                var a = Mesa.lugar;
                foreach (var b in teste.caminho)
                {
                    for (float u = 0; u <= 1; u += 0.05f) { var q = IP(Mathf.Lerp(a.x, b.x, u), Mathf.Lerp(a.y, b.y, u)); tela.Ret(q.x, q.y, 1, 1, new Color32(255, 214, 92, 255)); }
                    a = b;
                }
                var parada = teste.caminho.Count > 0 ? teste.caminho[teste.caminho.Count - 1] : d;   // onde ela para de verdade
                if (UsaCaminhos && !Livre(parada)) { var c = MaisPertoLivre(parada); if (c.x >= 0) parada = Centro(c.x, c.y); }
                var fim = IP(parada.x, parada.y);
                tela.Ret(fim.x - 1, fim.y - 1, 3, 3, new Color32(80, 255, 140, 255));
                // o técnico parado ali, para ver se ele fica em cima de algum móvel
                var boneco = Pose("tecnico", "se", false, 0);
                if (boneco != null) { int pes = Pes(boneco); tela.Imagem(boneco.px, boneco.w, boneco.h, fim.x - boneco.w / 2, fim.y - pes); }
            }
        }
    }
}
