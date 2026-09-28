using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// A roupa do técnico muda a cada cargo: camiseta azul (Técnico), verde-água (Sysadmin), polo azul-marinho (Analista),
    /// moletom roxo (DevOps), moletom laranja de plantão (SRE), blazer vinho (Arquiteto) e gola alta preta (CTO).
    /// </summary>
    public static class Uniformes
    {
        const float MatizSysadmin = 0.46f;
        const float MatizAnalista = 0.63f, BrilhoAnalista = 0.55f;
        const float MatizDevOps = 0.77f, BrilhoDevOps = 0.85f;
        const float MatizSre = 0.03f, BrilhoSre = 0.9f;
        const float MatizArquiteto = 0.97f, BrilhoArquiteto = 0.5f;
        const float MatizCto = 0.62f, BrilhoCto = 0.25f;

        /// <param name="prefixo">"tecnico" troca a roupa conforme o cargo; outros personagens ficam como estão.</param>
        public static Texture2D TexturaDoCargo(string arquivo, string prefixo, int cargo)
        {
            var t = ArteGerada.Textura(arquivo);
            if (prefixo != "tecnico" || cargo == 0) return t;
            // sprites da arte nova (Iso/): só o tronco muda, porque a calça jeans também é azul
            bool novo = arquivo.StartsWith("Iso/");
            float cabeca = novo ? 0.30f : 0.36f, pernas = novo ? 0.45f : 0f;
            float matiz = cargo == 1 ? MatizSysadmin : cargo == 2 ? MatizAnalista : cargo == 3 ? MatizDevOps : cargo == 4 ? MatizSre : cargo == 5 ? MatizArquiteto : MatizCto;
            float brilho = cargo == 1 ? 1f : cargo == 2 ? BrilhoAnalista : cargo == 3 ? BrilhoDevOps : cargo == 4 ? BrilhoSre : cargo == 5 ? BrilhoArquiteto : BrilhoCto;
            return ArteGerada.TrocarCorDaRoupa(t, matiz, brilho: brilho, cabeca: cabeca, pernas: pernas);
        }
    }
}
