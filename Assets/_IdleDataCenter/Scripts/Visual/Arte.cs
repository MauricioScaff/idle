using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Detalhes pequenos em pixel art desenhados como mapas de caracteres (cores em PixelArt.Paleta):
    /// ícones, efeitos e decoração miúda. Personagens e equipamentos vêm do PixelLab (ArteGerada).
    /// A primeira linha de cada mapa é o topo do sprite.
    /// </summary>
    public static class Arte
    {
        static readonly Vector2 Base = new Vector2(0.5f, 0f);   // centro da base
        static readonly Vector2 Canto = Vector2.zero;           // canto de baixo à esquerda

        // --- Decoração ---
        public static readonly Sprite Caneca = PixelArt.Criar(new[]
        {
            "mmmm..",
            "mmmmmm",
            "mmmm.m",
            "mmmmmm",
            "MMMM..",
        }, Base);

        /// <summary>Mostrador do relógio de parede (7×7); os ponteiros são pixels animados por cima.</summary>
        public static readonly Sprite Relogio = PixelArt.Criar(new[]
        {
            ".kkkkk.", "kyyyyyk", "kyyyyyk", "kyyyyyk", "kyyyyyk", "kyyyyyk", ".kkkkk.",
        }, Canto);

        /// <summary>Quadrinho de paisagem carimbado na parede do cenário (14×10).</summary>
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
        /// <summary>Alerta de incidente (5×7) que flutua sobre o equipamento travado.</summary>
        public static readonly Sprite Alerta = PixelArt.Criar(new[]
        {
            "..r..",
            ".ryr.",
            ".ryr.",
            "rryrr",
            "rrrrr",
            "rryrr",
            "rrrrr",
        }, Canto);

        public static readonly Sprite AbrirPainel = PixelArt.Criar(new[] { "..S..", ".SSS.", "SSSSS", ".....", "SSSSS" }, Canto);
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
