using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>Engenheiros da equipe (sprite do protótipo do Codex), desenhados na grade de pixels do jogo.</summary>
    public static class IsoSprites
    {
        static Color32 C(string s) => PixelArt.Hex(s);
        static Texture2D Pronto(PixelCanvas p) { p.Aplicar(); Object.Destroy(p.Sprite); return p.Textura; }

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
