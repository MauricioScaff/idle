using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Arte provisória em pixel art, desenhada como mapas de caracteres (cores em PixelArt.Paleta).
    /// A primeira linha de cada mapa é o topo do sprite.
    /// </summary>
    public static class Arte
    {
        static readonly Vector2 Base = new Vector2(0.5f, 0f);   // centro da base
        static readonly Vector2 Canto = Vector2.zero;           // canto de baixo à esquerda

        // --- Técnico (12×16): cabeção, bochechas rosadas ---
        static readonly string[] Cabeca =
        {
            "...hhhhhh...",
            "..hhhhhhhh..",
            ".hhhhhhhhhh.",
            ".hhhssssshh.",
            ".hhsssssssh.",
            ".hsssessess.", // olhos
            ".hssxssssxs.", // bochechas
            "..ssssssss..",
        };
        static readonly string[] Corpo =
        {
            "...cccccc...",
            "..cccccccc..",
            ".sccccccccs.",
            ".sCCCCCCCCs.",
        };
        static readonly string[] PernasJuntas =
        {
            "..pppppppp..",
            "..ppp..ppp..",
            "..ppp..ppp..",
            "..ooo..ooo..",
        };
        static readonly string[] PernasAbertas =
        {
            "..pppppppp..",
            ".ppp....ppp.",
            ".pp......pp.",
            "ooo......ooo",
        };

        static string[] Juntar(params string[][] partes)
        {
            var lista = new System.Collections.Generic.List<string>();
            foreach (var p in partes) lista.AddRange(p);
            return lista.ToArray();
        }

        static string[] SemOlhos(string[] cabeca)
        {
            var c = (string[])cabeca.Clone();
            c[5] = c[5].Replace('e', 's');
            return c;
        }

        public static readonly Sprite TecnicoParado = PixelArt.Criar(Juntar(Cabeca, Corpo, PernasJuntas), Base);
        public static readonly Sprite TecnicoPiscando = PixelArt.Criar(Juntar(SemOlhos(Cabeca), Corpo, PernasJuntas), Base);
        public static readonly Sprite TecnicoPasso = PixelArt.Criar(Juntar(Cabeca, Corpo, PernasAbertas), Base);

        // --- Servidor velho (12×18), com baia de CD e grades de ventilação ---
        public static readonly Sprite Servidor = PixelArt.Criar(new[]
        {
            ".kkkkkkkkkk.",
            "kbbbbbbbbbbk",
            "kbBBBBBBBBbk",
            "kbkkkkkkkkbk",
            "kbBBBBBBBBbk",
            "kbbbbbbbbbbk",
            "kbvvvvvvvvbk",
            "kbbbbbbbbbbk",
            "kbvvvvvvvvbk",
            "kbbbbbbbbbbk",
            "kbvvvvvvvvbk",
            "kbbbbbbbbbbk",
            "kbbbbbbbbbbk",
            "kbbbbbbbbbbk",
            "kbbdbdbbbbbk", // buracos dos LEDs (x = 3 e 5, y = 3)
            "kBBBBBBBBBBk",
            "kkkkkkkkkkkk",
            ".kk......kk.",
        }, Base);

        // --- Decoração ---
        public static readonly Sprite Planta = PixelArt.Criar(new[]
        {
            "...l.l..",
            "..lLlLl.",
            ".lLlllLl",
            "..lLlLl.",
            "...lLl..",
            "..tttt..",
            ".tTTTTt.",
            ".tTTTTt.",
            "..tttt..",
        }, Base);

        public static readonly Sprite Caneca = PixelArt.Criar(new[]
        {
            "mmmm..",
            "mmmmmm",
            "mmmm.m",
            "mmmmmm",
            "MMMM..",
        }, Base);

        /// <summary>Quadrinho de paisagem carimbado na parede do armário (14×10).</summary>
        public static readonly string[] Quadrinho =
        {
            "kkkkkkkkkkkkkk",
            "kSSSSSSSSSYYSk",
            "kSSSSSSSSSYYSk",
            "kSSyySSSSSSSSk",
            "kSyyyySSSSSSSk",
            "kSSSSSSSSSSSSk",
            "kSSSSSGGSSSSSk",
            "kSSSSGGGGSSGGk",
            "kGGGGGGGGGGGGk",
            "kkkkkkkkkkkkkk",
        };

        // --- Melhorias do servidor ---
        /// <summary>Ventoinha 4×4 em dois quadros: as pás alternam na diagonal e parecem girar.</summary>
        public static readonly Sprite VentoinhaA = PixelArt.Criar(new[] { "kkkk", "kyvk", "kvyk", "kkkk" }, Canto);
        public static readonly Sprite VentoinhaB = PixelArt.Criar(new[] { "kkkk", "kvyk", "kyvk", "kkkk" }, Canto);

        // --- Efeitos e interface ---
        public static readonly Sprite Seta = PixelArt.Criar(new[] { "YYYYY", ".YYY.", "..Y.." }, Canto);

        public static readonly Sprite Coracao = PixelArt.Criar(new[]
        {
            "rr.rr",
            "rrrrr",
            "rrrrr",
            ".rrr.",
            "..r..",
        }, Canto);

        public static readonly Sprite Faisca = PixelArt.Criar(new[] { ".Y.", "YYY", ".Y." }, Canto);

        public static readonly Sprite Fechar = PixelArt.Criar(new[]
        {
            "X...X",
            ".X.X.",
            "..X..",
            ".X.X.",
            "X...X",
        }, Canto);
    }
}
