using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Transforma a janela do jogo numa faixa transparente, sem borda e sempre por cima,
    /// colada logo acima da barra de tarefas. As áreas vazias deixam o clique passar
    /// para as janelas de baixo. Só funciona no build de Windows; no editor não faz nada.
    /// </summary>
    public class JanelaDesktop : MonoBehaviour
    {
        /// <summary>Altura da faixa em "pixels de arte" (cada um vira Escala × Escala pixels na tela).</summary>
        public const int AlturaVirtual = 64;

        /// <summary>True quando a janela já foi convertida em faixa (build de Windows).</summary>
        public bool Ativa { get; private set; }

        /// <summary>Altura atual da janela em pixels de arte: só a faixa, ou faixa + painel aberto.</summary>
        public int AlturaVirtualAtual { get; private set; } = AlturaVirtual;

        /// <summary>Quantos pixels de tela vale cada pixel de arte da faixa (sempre inteiro, para a pixel art ficar nítida).</summary>
        public int Escala
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (Ativa) return Mathf.Max(2, Mathf.RoundToInt(2f * Dpi / 96f));
#endif
                return Mathf.Max(1, Screen.height / Mathf.Max(AlturaVirtual, AlturaVirtualAtual));
            }
        }

        /// <summary>Pixels de tela por pixel de arte no painel (maior que a faixa, para o texto ficar legível).</summary>
        public int EscalaPainel
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (Ativa) return Mathf.Max(3, Mathf.RoundToInt(3f * Dpi / 96f));
#endif
                return Mathf.Max(1, Mathf.RoundToInt(Escala * 1.5f));
            }
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        uint Dpi { get { uint d = GetDpiForWindow(hwnd); return d == 0 ? 96 : d; } }
#endif

        /// <summary>A maior altura (em pixels de arte) que cabe acima da barra de tarefas.</summary>
        public int AlturaVirtualMaxima
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (Ativa) return (ultimaArea.Bottom - ultimaArea.Top) / Escala;
#endif
                return 10000;
            }
        }

        /// <summary>Muda a altura da janela (em pixels de arte). A base continua colada na barra de tarefas.</summary>
        public void DefinirAlturaVirtual(int altura)
        {
            AlturaVirtualAtual = Mathf.Clamp(altura, AlturaVirtual, AlturaVirtualMaxima);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (!Ativa) return;
            Screen.SetResolution(ultimaArea.Largura, AlturaFisica(), FullScreenMode.Windowed);
            Posicionar();
#endif
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        IntPtr hwnd;
        bool clicavel = true;
        bool escondida;
        float proximaChecagem;
        RECT ultimaArea;
#endif

        /// <summary>Posição do cursor em coordenadas de tela do Unity (origem embaixo à esquerda).</summary>
        public Vector2 PosicaoCursor
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (Ativa && GetCursorPos(out var p) && ScreenToClient(hwnd, ref p))
                    return new Vector2(p.x, Screen.height - p.y);
#endif
                return Input.mousePosition;
            }
        }

        /// <summary>
        /// True só se um clique agora iria de fato para a faixa: janela visível e ela mesma sob o cursor.
        /// A Unity lê o mouse mesmo em segundo plano, então sem essa checagem cliques feitos em
        /// outros programas (ou com a faixa escondida) seriam contados como cliques no jogo.
        /// </summary>
        public bool CursorSobreAJanela
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (!Ativa) return Application.isFocused;
                if (escondida || !GetCursorPos(out var p)) return false;
                return GetAncestor(WindowFromPoint(p), GA_ROOT) == hwnd;
#else
                return true;
#endif
            }
        }

        IEnumerator Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            yield return null;
            hwnd = EncontrarJanela();
            if (hwnd == IntPtr.Zero)
            {
                Debug.LogWarning("JanelaDesktop: não encontrei a janela do jogo.");
                yield break;
            }

            ultimaArea = AreaDeTrabalho();
            Screen.SetResolution(ultimaArea.Largura, AlturaFisica(), FullScreenMode.Windowed);
            yield return null;
            yield return null;

            // Validado no Unity 6.6 + D3D11 (sem flip model): janela "layered" com cor-chave preta,
            // mais o quadro do DWM estendido sobre toda a área. Preto puro vira transparente.
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(WS_POPUP | WS_VISIBLE));
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(WS_EX_LAYERED | WS_EX_TOOLWINDOW));
            SetLayeredWindowAttributes(hwnd, 0, 0, LWA_COLORKEY);
            var margens = new MARGINS { cxLeftWidth = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margens);
            Posicionar();
            Ativa = true;
#else
            yield break;
#endif
        }

        /// <summary>
        /// Liga ou desliga o "clique atravessando". Chamar todo quadro: true quando o cursor
        /// está sobre algo do jogo, false quando está sobre área vazia.
        /// </summary>
        public void DefinirClicavel(bool sim)
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (!Ativa || sim == clicavel) return;
            clicavel = sim;
            long ex = WS_EX_LAYERED | WS_EX_TOOLWINDOW | (sim ? 0 : WS_EX_TRANSPARENT);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
#endif
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        void Update()
        {
            if (!Ativa || Time.unscaledTime < proximaChecagem) return;
            proximaChecagem = Time.unscaledTime + 2f;

            // Some quando outro programa está em tela cheia (jogo, vídeo, apresentação)
            bool telaCheia = SHQueryUserNotificationState(out int estado) == 0 && (estado == 2 || estado == 3 || estado == 4);
            if (telaCheia != escondida)
            {
                escondida = telaCheia;
                ShowWindow(hwnd, escondida ? SW_HIDE : SW_SHOWNA);
                Application.targetFrameRate = escondida ? 5 : 30;
            }
            if (escondida) return;

            // Barra de tarefas mudou (resolução, DPI, barra oculta)? Reposiciona.
            var area = AreaDeTrabalho();
            if (!area.Equals(ultimaArea) || Screen.height != AlturaFisica())
            {
                ultimaArea = area;
                Screen.SetResolution(area.Largura, AlturaFisica(), FullScreenMode.Windowed);
            }
            Posicionar(); // também reafirma o "sempre por cima", que a barra de tarefas às vezes rouba
        }

        int AlturaFisica() => AlturaVirtualAtual * Mathf.Max(2, Mathf.RoundToInt(2f * Dpi / 96f));

        void Posicionar()
        {
            int altura = AlturaFisica();
            SetWindowPos(hwnd, HWND_TOPMOST, ultimaArea.Left, ultimaArea.Bottom - altura, ultimaArea.Largura, altura,
                SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        }

        static RECT AreaDeTrabalho()
        {
            var r = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref r, 0);
            return r;
        }

        static IntPtr janelaEncontrada;

        static IntPtr EncontrarJanela()
        {
            janelaEncontrada = IntPtr.Zero;
            EnumWindows(VerificarJanela, IntPtr.Zero);
            return janelaEncontrada != IntPtr.Zero ? janelaEncontrada : GetActiveWindow();
        }

        [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
        static bool VerificarJanela(IntPtr h, IntPtr _)
        {
            GetWindowThreadProcessId(h, out uint pid);
            if (pid != (uint)System.Diagnostics.Process.GetCurrentProcess().Id) return true;
            var classe = new StringBuilder(64);
            GetClassName(h, classe, classe.Capacity);
            if (classe.ToString() != "UnityWndClass") return true;
            janelaEncontrada = h;
            return false;
        }

        // --- Win32 ---

        const int GWL_STYLE = -16, GWL_EXSTYLE = -20;
        const long WS_POPUP = 0x80000000L, WS_VISIBLE = 0x10000000L;
        const long WS_EX_LAYERED = 0x80000L, WS_EX_TRANSPARENT = 0x20L, WS_EX_TOOLWINDOW = 0x80L;
        const uint LWA_COLORKEY = 0x1;
        const uint SWP_NOACTIVATE = 0x10, SWP_FRAMECHANGED = 0x20, SWP_SHOWWINDOW = 0x40;
        const uint SPI_GETWORKAREA = 0x30;
        const int SW_HIDE = 0, SW_SHOWNA = 8;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Largura => Right - Left;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, StringBuilder nome, int max);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int indice, IntPtr valor);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr depoisDe, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint corChave, byte alfa, uint flags);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int comando);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
        const uint GA_ROOT = 2;
        [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint acao, uint param, ref RECT r, uint winIni);
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS m);
        [DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int estado);
#endif
    }
}
