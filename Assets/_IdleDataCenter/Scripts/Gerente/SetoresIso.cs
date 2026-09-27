using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter.Gerente
{
    /// <summary>
    /// Setores da ilustração isométrica do data center e em que cargo cada um "acende".
    /// Cada pixel da ilustração pertence ao setor de centro mais próximo (distância com o eixo Y
    /// esticado, como no isométrico); os setores ainda bloqueados ficam escuros e dessaturados.
    /// </summary>
    public static class SetoresIso
    {
        public class Setor
        {
            public string Id, Nome;
            public Vector2 Centro;      // fração da imagem (0..1, origem em cima à esquerda)
            public Vector2 Placa;       // onde fica a placa com o nome
            public int Cargo;           // cargo em que libera (-1 = área comum, nunca escurece)
            public Color Cor;
        }

        public static readonly Setor[] Todos =
        {
            new Setor { Id = "Compute", Nome = "COMPUTE", Centro = new Vector2(.60f, .27f), Placa = new Vector2(.64f, .12f), Cargo = 0, Cor = IsoGui.Cyan },
            new Setor { Id = "Compute", Nome = null, Centro = new Vector2(.44f, .28f), Cargo = 0 },          // piso onde nascem os racks
            new Setor { Id = "Energia", Nome = "ENERGIA", Centro = new Vector2(.19f, .59f), Placa = new Vector2(.22f, .44f), Cargo = 1, Cor = IsoGui.Laranja },
            new Setor { Id = "Refrigeracao", Nome = "REFRIGERACAO", Centro = new Vector2(.76f, .60f), Placa = new Vector2(.80f, .45f), Cargo = 1, Cor = IsoGui.Cyan },
            new Setor { Id = "Storage", Nome = "STORAGE", Centro = new Vector2(.50f, .07f), Placa = new Vector2(.47f, .02f), Cargo = 2, Cor = IsoGui.Cyan },
            new Setor { Id = "Rede", Nome = "REDE", Centro = new Vector2(.46f, .53f), Placa = new Vector2(.46f, .40f), Cargo = 2, Cor = IsoGui.Cyan },
            new Setor { Id = "Automacao", Nome = "LABORATORIO", Centro = new Vector2(.84f, .38f), Placa = new Vector2(.84f, .24f), Cargo = 2, Cor = IsoGui.Roxo },
            new Setor { Id = "NOC", Nome = "NOC", Centro = new Vector2(.16f, .30f), Placa = new Vector2(.16f, .14f), Cargo = 3, Cor = IsoGui.Cyan },
            // áreas comuns: piso central, doca de carga e o lado de fora
            new Setor { Id = null, Centro = new Vector2(.40f, .78f), Cargo = -1 },
            new Setor { Id = null, Centro = new Vector2(.62f, .76f), Cargo = -1 },
            new Setor { Id = null, Centro = new Vector2(.82f, .88f), Cargo = -1 },
            new Setor { Id = null, Centro = new Vector2(.08f, .92f), Cargo = -1 },
            new Setor { Id = null, Centro = new Vector2(.10f, .02f), Cargo = -1 },
            new Setor { Id = null, Centro = new Vector2(.93f, .08f), Cargo = -1 },
            new Setor { Id = null, Centro = new Vector2(.98f, .60f), Cargo = -1 },
        };

        public static bool Liberado(Setor s, int cargo) => s.Cargo <= cargo;

        public static Setor Buscar(string id)
        {
            foreach (var s in Todos) if (s.Id == id && s.Nome != null) return s;
            return null;
        }

        static readonly Dictionary<int, Texture2D> cache = new Dictionary<int, Texture2D>();

        /// <summary>Cópia da ilustração com os setores ainda bloqueados escurecidos (uma por cargo, em cache).</summary>
        public static Texture2D Escurecida(Texture2D original, int cargo)
        {
            if (original == null) return null;
            if (!original.isReadable) return original;
            if (cache.TryGetValue(cargo, out var pronta) && pronta != null) return pronta;

            int w = original.width, h = original.height;
            var px = original.GetPixels32();
            const float esticaY = 1.7f;
            for (int y = 0; y < h; y++)
            {
                float fy = 1f - (y + 0.5f) / h;   // GetPixels32 começa embaixo
                for (int x = 0; x < w; x++)
                {
                    float fx = (x + 0.5f) / w;
                    Setor perto = null;
                    float melhor = float.MaxValue;
                    foreach (var s in Todos)
                    {
                        float dx = fx - s.Centro.x, dy = (fy - s.Centro.y) * esticaY;
                        float d = dx * dx + dy * dy;
                        if (d < melhor) { melhor = d; perto = s; }
                    }
                    if (perto.Cargo < 0 || Liberado(perto, cargo)) continue;
                    int i = y * w + x;
                    var c = px[i];
                    byte cinza = (byte)((c.r * 30 + c.g * 59 + c.b * 11) / 100);
                    px[i] = new Color32((byte)((cinza * 0.6f + c.r * 0.4f) * 0.32f), (byte)((cinza * 0.6f + c.g * 0.4f) * 0.34f),
                                        (byte)((cinza * 0.6f + c.b * 0.4f) * 0.42f), c.a);
                }
            }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = original.filterMode, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply(false, true);   // não precisa ficar legível
            cache[cargo] = t;
            return t;
        }

        /// <summary>Nome do cargo que libera, no formato das placas ("ANALISTA", "SRE"...).</summary>
        public static string CargoCurto(int cargo)
        {
            var nome = Catalogo.Cargos[Mathf.Clamp(cargo, 0, Catalogo.Cargos.Count - 1)].Nome;
            return nome.Contains("Analista") ? "ANALISTA" : nome.Contains("DevOps") ? "DEVOPS" : nome.ToUpperInvariant();
        }
    }
}
