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
            if (prefixo != "tecnico") return t;
            if (cargo == 1) return ArteGerada.TrocarCorDaRoupa(t, MatizSysadmin);
            if (cargo == 2) return ArteGerada.TrocarCorDaRoupa(t, MatizAnalista, brilho: BrilhoAnalista);
            if (cargo == 3) return ArteGerada.TrocarCorDaRoupa(t, MatizDevOps, brilho: BrilhoDevOps);
            if (cargo == 4) return ArteGerada.TrocarCorDaRoupa(t, MatizSre, brilho: BrilhoSre);
            if (cargo == 5) return ArteGerada.TrocarCorDaRoupa(t, MatizArquiteto, brilho: BrilhoArquiteto);
            if (cargo >= 6) return ArteGerada.TrocarCorDaRoupa(t, MatizCto, brilho: BrilhoCto);
            return t;
        }
    }
}
