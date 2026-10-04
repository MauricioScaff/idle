using System;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Preferências do jogador (som, monitor, lado da tela...). Ficam no PlayerPrefs, separadas do save
    /// do jogo, para zerar o progresso não apagar as preferências.
    /// </summary>
    public static class Ajustes
    {
        public static readonly string[] NomesVolume = { "Baixo", "Médio", "Alto" };
        static readonly float[] Volumes = { 0.25f, 0.5f, 0.9f };

        /// <summary>Disparado quando qualquer ajuste muda (para quem precisa reagir na hora).</summary>
        public static event Action Mudou;

        public static bool Som
        {
            get => PlayerPrefs.GetInt("som", 1) == 1;
            set => Gravar("som", value ? 1 : 0);
        }

        /// <summary>0 = baixo (padrão), 1 = médio, 2 = alto.</summary>
        public static int Volume
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("volume", 0), 0, Volumes.Length - 1);
            set => Gravar("volume", Mathf.Clamp(value, 0, Volumes.Length - 1));
        }

        public static float VolumeFinal => Som ? Volumes[Volume] : 0f;

        /// <summary>Som ambiente da sala: ventoinhas, ar-condicionado, discos e o NOC (desligado por padrão: o jogo convive com o seu trabalho).</summary>
        public static bool Zumbido
        {
            get => PlayerPrefs.GetInt("zumbido", 0) == 1;
            set => Gravar("zumbido", value ? 1 : 0);
        }

        /// <summary>Índice do monitor (0 = principal).</summary>
        public static int Monitor
        {
            get => PlayerPrefs.GetInt("monitor", 0);
            set => Gravar("monitor", value);
        }

        /// <summary>Encosta o cenário e a loja no lado direito da tela, em vez do esquerdo.</summary>
        public static bool Direita
        {
            get => PlayerPrefs.GetInt("direita", 0) == 1;
            set => Gravar("direita", value ? 1 : 0);
        }

        /// <summary>Esconde a faixa quando outro programa está em tela cheia (padrão: sim).</summary>
        public static bool EsconderEmTelaCheia
        {
            get => PlayerPrefs.GetInt("telaCheia", 1) == 1;
            set => Gravar("telaCheia", value ? 1 : 0);
        }

        /// <summary>Em que modo o jogo abre: o último usado (padrão: modo gerente, a vista isométrica). Não dispara Mudou.</summary>
        public static bool AbrirNoGerente
        {
            get => PlayerPrefs.GetInt("abrirNoGerente", 1) == 1;
            set { PlayerPrefs.SetInt("abrirNoGerente", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static void Gravar(string chave, int valor)
        {
            PlayerPrefs.SetInt(chave, valor);
            PlayerPrefs.Save();
            Mudou?.Invoke();
        }
    }
}
