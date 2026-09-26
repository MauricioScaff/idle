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

        // Digitando: a mão da frente sobe e desce (virado para a direita; flipX vira para a esquerda)
        static readonly string[] CorpoDigitandoA =
        {
            "...cccccc...",
            "..cccccccc..",
            ".sccccccccs.",
            ".sCCCCCCCCss",
        };
        static readonly string[] CorpoDigitandoB =
        {
            "...cccccc...",
            "..cccccccc..",
            ".sccccccccss",
            ".sCCCCCCCC..",
        };

        /// <summary>Quadros de animação do técnico com o uniforme de um cargo.</summary>
        public class Visual
        {
            public Sprite Parado, Passo, Piscando, DigitandoA, DigitandoB;
        }

        static readonly System.Collections.Generic.Dictionary<int, Visual> visuais = new System.Collections.Generic.Dictionary<int, Visual>();

        /// <summary>
        /// Cargo 0: camiseta azul. Cargo 1 (Sysadmin): camisa verde-água com crachá.
        /// </summary>
        public static Visual Tecnico(int cargo)
        {
            if (visuais.TryGetValue(cargo, out var v)) return v;
            string[] U(string[] mapa) => cargo == 0 ? mapa : Uniforme(mapa);
            return visuais[cargo] = new Visual
            {
                Parado = PixelArt.Criar(U(Juntar(Cabeca, Corpo, PernasJuntas)), Base),
                Passo = PixelArt.Criar(U(Juntar(Cabeca, Corpo, PernasAbertas)), Base),
                Piscando = PixelArt.Criar(U(Juntar(SemOlhos(Cabeca), Corpo, PernasJuntas)), Base),
                DigitandoA = PixelArt.Criar(U(Juntar(Cabeca, CorpoDigitandoA, PernasJuntas)), Base),
                DigitandoB = PixelArt.Criar(U(Juntar(Cabeca, CorpoDigitandoB, PernasJuntas)), Base),
            };
        }

        static string[] Uniforme(string[] mapa)
        {
            var m = new string[mapa.Length];
            for (int i = 0; i < mapa.Length; i++) m[i] = mapa[i].Replace('c', 'q').Replace('C', 'Q');
            // crachá branco no peito (linha 10, coluna 4)
            var linha = m[10].ToCharArray();
            linha[4] = 'y';
            m[10] = new string(linha);
            return m;
        }

        public static Sprite TecnicoDigitandoA => Tecnico(0).DigitandoA;
        public static Sprite TecnicoDigitandoB => Tecnico(0).DigitandoB;
        public static Sprite TecnicoParado => Tecnico(0).Parado;
        public static Sprite TecnicoPiscando => Tecnico(0).Piscando;
        public static Sprite TecnicoPasso => Tecnico(0).Passo;

        // --- Servidor velho (12×18), com baia de CD e grades de ventilação ---
        public static readonly string[] MapaServidor =
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
        };
        public static readonly Sprite Servidor = PixelArt.Criar(MapaServidor, Base);

        // --- Decoração ---
        public static readonly string[] MapaPlanta =
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
        };
        public static readonly Sprite Planta = PixelArt.Criar(MapaPlanta, Base);

        public static readonly Sprite Caneca = PixelArt.Criar(new[]
        {
            "mmmm..",
            "mmmmmm",
            "mmmm.m",
            "mmmmmm",
            "MMMM..",
        }, Base);

        // --- Era 1: TI improvisada ---
        /// <summary>Mesa de madeira (26×10), vista de lado.</summary>
        public static readonly Sprite Mesa = PixelArt.Criar(new[]
        {
            "nnnnnnnnnnnnnnnnnnnnnnnnnn",
            "NNNNNNNNNNNNNNNNNNNNNNNNNN",
            ".NN...............NNNNNNN.",
            ".NN...............NnnnnNN.",
            ".NN...............NNNNNNN.",
            ".NN...............NnnnnNN.",
            ".NN...............NNNNNNN.",
            ".NN....................NN.",
            ".NN....................NN.",
            ".NN....................NN.",
        }, Canto);

        /// <summary>Monitor CRT bege (14×12). A tela (8×5) fica em x = 3..10, y = 4..8 a partir do canto de baixo.</summary>
        public static readonly Sprite MonitorCrt = PixelArt.Criar(new[]
        {
            ".kkkkkkkkkkkk.",
            "kbbbbbbbbbbbbk",
            "kbkkkkkkkkkkbk",
            "kbkooooooookbk",
            "kbkooooooookbk",
            "kbkooooooookbk",
            "kbkooooooookbk",
            "kbkooooooookbk",
            "kbkkkkkkkkkkbk",
            "kbbbbbbbbbgbbk",
            ".kkkkkkkkkkkk.",
            "....kbbbbk....",
        }, Canto);

        public static readonly Sprite PostIt = PixelArt.Criar(new[] { "YYY", "YYY", "YY." }, Canto);

        /// <summary>Ventilador de chão (7×13) em dois quadros.</summary>
        public static readonly Sprite VentiladorA = PixelArt.Criar(new[]
        {
            ".kkkkk.", "kvy.yvk", "ky.v.yk", "k.vkv.k", "ky.v.yk", "kvy.yvk", ".kkkkk.",
            "...k...", "...k...", "...k...", "...k...", "...k...", ".kkkkk.",
        }, Canto);
        public static readonly Sprite VentiladorB = PixelArt.Criar(new[]
        {
            ".kkkkk.", "k.yvy.k", "kvy.yvk", "ky.k.yk", "kvy.yvk", "k.yvy.k", ".kkkkk.",
            "...k...", "...k...", "...k...", "...k...", "...k...", ".kkkkk.",
        }, Canto);

        public static readonly Sprite CaixaFerramentas = PixelArt.Criar(new[]
        {
            "...kkkk...",
            "...k..k...",
            "kkkkkkkkkk",
            "krrrrrrrrk",
            "krrrryrrrk",
            "kkkkkkkkkk",
        }, Canto);

        /// <summary>Mostrador do relógio de parede (7×7); os ponteiros são pixels animados por cima.</summary>
        public static readonly Sprite Relogio = PixelArt.Criar(new[]
        {
            ".kkkkk.", "kyyyyyk", "kyyyyyk", "kyyyyyk", "kyyyyyk", "kyyyyyk", ".kkkkk.",
        }, Canto);

        /// <summary>Cabo solto descendo da mesa e serpenteando no chão (16×3).</summary>
        public static readonly Sprite CaboSolto = PixelArt.Criar(new[]
        {
            "o...............",
            "o......oo.......",
            ".oooooo..oooooo.",
        }, Canto);

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

        // --- Era 1, segunda metade: salinha do Sysadmin ---
        public static readonly Sprite Rack = PixelArt.Rack();

        /// <summary>Servidor 1U (20×3) que encaixa numa vaga do rack.</summary>
        public static readonly Sprite Servidor1U = PixelArt.Criar(new[]
        {
            "wwwwwwwwwwwwwwwwwwww",
            "wkkkwkkkwwwwwwwwwwww",
            "WWWWWWWWWWWWWWWWWWWW",
        }, Canto);

        /// <summary>No-break (16×12). O visor (x = 3..12, 2 linhas a partir de y = 7) mostra as baterias.</summary>
        public static readonly Sprite NoBreak = PixelArt.Criar(new[]
        {
            "kkkkkkkkkkkkkkkk",
            "kWWWWWWWWWWWWWWk",
            "kWkkkkkkkkkkkkWk",
            "kWkooooooooookWk",
            "kWkooooooooookWk",
            "kWkkkkkkkkkkkkWk",
            "kWWWWWWWWWWWWWWk",
            "kWwWwWwWwWwWwWWk",
            "kWWWWWWWWWWWWWWk",
            "kWWWWWWWWWWgWWWk",
            "kWWWWWWWWWWWWWWk",
            "kkkkkkkkkkkkkkkk",
        }, Canto);

        /// <summary>Ar-condicionado split de parede (24×8).</summary>
        public static readonly Sprite ArCondicionado = PixelArt.Criar(new[]
        {
            "kkkkkkkkkkkkkkkkkkkkkkkk",
            "kyyyyyyyyyyyyyyyyyyyyyyk",
            "kyyyyyyyyyyyyyyyyyggyyyk",
            "kyyyyyyyyyyyyyyyyyyyyyyk",
            "kbbbbbbbbbbbbbbbbbbbbbbk",
            "kbvvvvvvvvvvvvvvvvvvvvbk",
            "kbbbbbbbbbbbbbbbbbbbbbbk",
            ".kkkkkkkkkkkkkkkkkkkkkk.",
        }, Canto);

        /// <summary>Alerta de incidente (5×7) que flutua sobre o servidor travado.</summary>
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

        // --- Efeitos e interface ---
        public static readonly Sprite AbrirPainel = PixelArt.Criar(new[] { "..S..", ".SSS.", "SSSSS", ".....", "SSSSS" }, Canto);
        public static readonly Sprite Seta =PixelArt.Criar(new[] { "YYYYY", ".YYY.", "..Y.." }, Canto);

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
