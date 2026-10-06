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
        /// <summary>Retângulos do piso ocupados (gx0, gy0, gx1, gy1 em casas), montados a cada desenho.</summary>
        readonly List<Rect> obstaculos = new List<Rect>();

        /// <summary>Teste: pinta na imagem as casas bloqueadas (ExportarSalas -obstaculos).</summary>
        public bool MostrarObstaculos { get; set; }

        const float PassoDaGrade = 0.2f;    // tamanho da casinha da busca, em casas
        const float Folga = 0.18f;          // distância mínima dos móveis
        const float FolgaDaParede = 0.3f;   // e das paredes / da borda do piso

        bool UsaCaminhos => obstaculos.Count > 0 && areaAtual == null;

        void Bloquear(float gx0, float gy0, float gx1, float gy1) => obstaculos.Add(Rect.MinMaxRect(gx0, gy0, gx1, gy1));

        bool Livre(Vector2 g)
        {
            float n = Sala.casas;
            if (g.x < FolgaDaParede || g.y < FolgaDaParede || g.x > n - FolgaDaParede || g.y > n - FolgaDaParede) return false;
            foreach (var o in obstaculos)
                if (g.x > o.xMin - Folga && g.x < o.xMax + Folga && g.y > o.yMin - Folga && g.y < o.yMax + Folga) return false;
            return true;
        }

        int Celulas => Mathf.CeilToInt(Sala.casas / PassoDaGrade);
        Vector2 Centro(int i, int j) => new Vector2((i + 0.5f) * PassoDaGrade, (j + 0.5f) * PassoDaGrade);

        /// <summary>A casinha livre mais perto de um ponto (o próprio ponto pode estar dentro da folga de um móvel).</summary>
        Vector2Int MaisPertoLivre(Vector2 g)
        {
            int n = Celulas;
            var melhor = new Vector2Int(-1, -1);
            float dist = float.MaxValue;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var c = Centro(i, j);
                    if (!Livre(c)) continue;
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
            var fim = MaisPertoLivre(destino);
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
            if (destinoLivre) { if (pontos.Count > 0) pontos[pontos.Count - 1] = destino; else pontos.Add(destino); }
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
            if (!MostrarObstaculos || !UsaCaminhos) return;
            int n = Celulas;
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
            foreach (var d in destinos)
            {
                var teste = new Trabalhador { pos = Mesa.lugar };
                RotaLivre(teste, d);
                var a = Mesa.lugar;
                foreach (var b in teste.caminho)
                {
                    for (float u = 0; u <= 1; u += 0.05f) { var q = IP(Mathf.Lerp(a.x, b.x, u), Mathf.Lerp(a.y, b.y, u)); tela.Ret(q.x, q.y, 1, 1, new Color32(255, 214, 92, 255)); }
                    a = b;
                }
                var fim = IP(d.x, d.y);
                tela.Ret(fim.x - 1, fim.y - 1, 3, 3, new Color32(80, 255, 140, 255));
            }
        }
    }
}
