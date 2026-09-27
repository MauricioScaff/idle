using UnityEngine;

namespace IdleDataCenter.Isometrico
{
    /// <summary>Objetos dinâmicos da cena, desenhados na grade de pixels do próprio jogo.</summary>
    public static class IsoSprites
    {
        static Color32 C(string s) => PixelArt.Hex(s);

        public static void Linha(PixelCanvas p, int x, int y, int x2, int y2, Color32 c)
        {
            int dx = Mathf.Abs(x2 - x), sx = x < x2 ? 1 : -1, dy = -Mathf.Abs(y2 - y), sy = y < y2 ? 1 : -1;
            int erro = dx + dy;
            while (true)
            {
                p.Pixel(x, y, c);
                if (x == x2 && y == y2) break;
                int e = erro * 2;
                if (e >= dy) { erro += dy; x += sx; }
                if (e <= dx) { erro += dx; y += sy; }
            }
        }

        static void Poligono(PixelCanvas p, Color32 c, params Vector2Int[] pontos)
        {
            int min = p.Altura, max = 0;
            foreach (var v in pontos) { min = Mathf.Min(min, v.y); max = Mathf.Max(max, v.y); }
            for (int y = min; y <= max; y++)
            {
                int esquerda = p.Largura, direita = -1;
                for (int i = 0; i < pontos.Length; i++)
                {
                    var a = pontos[i]; var b = pontos[(i + 1) % pontos.Length];
                    if (a.y == b.y || y < Mathf.Min(a.y, b.y) || y > Mathf.Max(a.y, b.y)) continue;
                    int x = Mathf.RoundToInt(a.x + (float)(y - a.y) * (b.x - a.x) / (b.y - a.y));
                    esquerda = Mathf.Min(esquerda, x); direita = Mathf.Max(direita, x);
                }
                if (direita >= esquerda) p.Ret(esquerda, y, direita - esquerda + 1, 1, c);
            }
        }

        static Vector2Int P(int x, int y) => new Vector2Int(x, y);
        static Texture2D Pronto(PixelCanvas p) { p.Aplicar(); Object.Destroy(p.Sprite); return p.Textura; }

        public static Texture2D Rack(bool ativo)
        {
            var p = new PixelCanvas(80, 112);
            Poligono(p, C("344655"), P(6, 91), P(41, 109), P(77, 90), P(42, 73));
            Poligono(p, C("152335"), P(10, 19), P(40, 34), P(40, 104), P(10, 89));
            Poligono(p, C("253e56"), P(40, 34), P(69, 19), P(69, 89), P(40, 104));
            Poligono(p, C("607a91"), P(10, 19), P(39, 4), P(69, 19), P(40, 34));
            Poligono(p, C("40566d"), P(15, 19), P(39, 7), P(64, 19), P(40, 30));
            Linha(p, 40, 35, 40, 102, C("8ba2b6"));
            Linha(p, 68, 20, 68, 88, C("7993ad"));
            Linha(p, 11, 20, 11, 87, C("71849b"));
            for (int i = 0; i < 7; i++)
            {
                int yy = 39 + i * 9;
                Poligono(p, C("0d192b"), P(44, yy), P(65, yy - 11), P(65, yy - 5), P(44, yy + 6));
                Linha(p, 44, yy + 6, 65, yy - 5, C("547088"));
                for (int j = 0; j < 5; j++) p.Ret(47 + j * 3, yy + 2 - j * 2, 2, 1, C("395568"));
                p.Ret(62, yy - 8, 2, 2, C(ativo ? (i % 3 == 0 ? "ffb34b" : "64f4a6") : "385165"));
                p.Ret(45, yy + 1, 2, 2, C(ativo ? "44cffa" : "385165"));
                Linha(p, 15, yy - 9, 34, yy, C("2b3d50"));
            }
            return Pronto(p);
        }

        public static Texture2D Drone()
        {
            var p = new PixelCanvas(56, 48);
            p.Ret(13, 24, 29, 2, C("28435a"));
            p.Ret(17, 17, 22, 12, C("e4f8ff"));
            p.Ret(20, 19, 17, 9, C("233954"));
            p.Ret(23, 21, 3, 3, C("4ee9ff")); p.Ret(31, 21, 3, 3, C("4ee9ff"));
            p.Ret(8, 14, 15, 3, C("8cb2ce")); p.Ret(35, 14, 14, 3, C("8cb2ce"));
            p.Ret(4, 11, 18, 2, C("d5f5ff")); p.Ret(35, 11, 18, 2, C("d5f5ff"));
            p.Ret(14, 28, 3, 8, C("728ba6")); p.Ret(38, 28, 3, 8, C("728ba6"));
            Poligono(p, C("dcab65"), P(16, 34), P(28, 28), P(41, 34), P(28, 41));
            Poligono(p, C("967045"), P(16, 34), P(28, 41), P(28, 47), P(16, 40));
            Poligono(p, C("b88850"), P(28, 41), P(41, 34), P(41, 41), P(28, 47));
            return Pronto(p);
        }

        public static Texture2D Engenheiro(int quadro)
        {
            var p = new PixelCanvas(18, 24);
            var cabeca = new[]
            {
                "...hhhhhhh...", "..hhhhhhhhh..", ".hhhhhhhhhhh.", ".hhhhhhhhhhh.",
                ".hhssssssshh.", ".hsssssssssh.", ".hssessesssh.", "..ssxssssss..",
                "...sssssss...", "....sssss....",
                "...ccccccc...", "..scccccccs..", "..scccccccs..", "...cCCCCcc...",
                "...CCCCCCC...", "....ppppp...."
            };
            p.Mapa(cabeca, 2, 1);
            bool passo = quadro % 2 == 1;
            p.Mapa(passo ? new[] { "..ppp.ppp....", "..pp...ppp...", ".ooo....ooo.." }
                : new[] { "...pp.ppp....", "...pp.ppp....", "..ooo.ooo...." }, 2, 17);
            // Tablet e crachá ajudam a identificar a equipe de infraestrutura.
            p.Ret(12, 13, 4, 5, C("233954"));
            p.Ret(13, 14, 2, 3, C("74e4f9"));
            p.Pixel(7, 13, C("e5f5ff"));
            return Pronto(p);
        }
    }
}
