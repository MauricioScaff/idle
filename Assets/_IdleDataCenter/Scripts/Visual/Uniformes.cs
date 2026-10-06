using IdleDataCenter.Simulacao;
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
            if (prefixo != "tecnico" || cargo == Catalogo.CargoTecnico) return t;
            // sprites da arte nova (Iso/): só o tronco muda, porque a calça jeans também é azul
            bool novo = arquivo.StartsWith("Iso/");
            float cabeca = novo ? 0.30f : 0.36f, pernas = novo ? 0.45f : 0f;
            float matiz = cargo == Catalogo.CargoSysadmin ? MatizSysadmin : cargo == Catalogo.CargoAnalista ? MatizAnalista : cargo == Catalogo.CargoDevOps ? MatizDevOps : cargo == Catalogo.CargoSre ? MatizSre : cargo == Catalogo.CargoArquiteto ? MatizArquiteto : MatizCto;
            float brilho = cargo == Catalogo.CargoSysadmin ? 1f : cargo == Catalogo.CargoAnalista ? BrilhoAnalista : cargo == Catalogo.CargoDevOps ? BrilhoDevOps : cargo == Catalogo.CargoSre ? BrilhoSre : cargo == Catalogo.CargoArquiteto ? BrilhoArquiteto : BrilhoCto;
            return ArteGerada.TrocarCorDaRoupa(t, matiz, brilho: brilho, cabeca: cabeca, pernas: pernas);
        }
    }
}
