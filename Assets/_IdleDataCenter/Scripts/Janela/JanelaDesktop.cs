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
                return Mathf.Max(2, Mathf.RoundToInt(2f * Dpi / 96f)); // pelo DPI desde o começo (antes da janela ficar pronta também)
#else
                return Mathf.Max(1, Screen.height / Mathf.Max(AlturaVirtual, AlturaVirtualAtual));
#endif
            }
        }

        /// <summary>Pixels de tela por pixel de arte no painel (maior que a faixa, para o texto ficar legível).</summary>
        public int EscalaPainel
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                return Mathf.Max(3, Mathf.RoundToInt(3f * Dpi / 96f));
#else
                return Mathf.Max(1, Mathf.RoundToInt(Escala * 1.5f));
#endif
            }
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        uint Dpi { get { uint d = hwnd != IntPtr.Zero ? GetDpiForWindow(hwnd) : GetDpiForSystem(); return d == 0 ? 96 : d; } }
#endif

        /// <summary>Largura do monitor da faixa em pixels de arte.</summary>
        public float LarguraVirtualDaTela
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (Ativa && ultimaArea.Largura > 0) return ultimaArea.Largura / (float)Escala;
#endif
                return Screen.currentResolution.width / (float)Escala;
            }
        }

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
            Redimensionar();
#endif
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        int reaplicarEstilo;   // quadros até reaplicar o estilo depois de um SetResolution

        /// <summary>
        /// Muda o tamanho da janela depois que ela já está pronta. Não usa o SetResolution do Unity: ele devolve
        /// a borda e a barra de título e centraliza a janela. O SetWindowPos (em Posicionar) redimensiona e o Unity
        /// acompanha o novo tamanho; o estilo é conferido de novo alguns quadros depois, por garantia.
        /// </summary>
        void Redimensionar()
        {
            Posicionar();
            reaplicarEstilo = 3;
        }

        /// <summary>Janela sem borda, "layered" com cor-chave preta e o quadro do DWM estendido sobre toda a área.</summary>
        void AplicarEstilo()
        {
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(WS_POPUP | (visivel ? WS_VISIBLE : 0)));
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(WS_EX_LAYERED | WS_EX_TOOLWINDOW | (clicavel ? 0 : WS_EX_TRANSPARENT)));
            SetLayeredWindowAttributes(hwnd, 0, 0, LWA_COLORKEY);
            var margens = new MARGINS { cxLeftWidth = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margens);
        }

        /// <summary>
        /// Janela normal do Windows para o modo gerente: com barra de título, na barra de tarefas, redimensionável
        /// e sem "sempre por cima". Abre centralizada ocupando 90% da área útil do monitor.
        /// </summary>
        void EntrarJanelaNormal()
        {
            ultimaArea = AreaDeTrabalho();
            int areaW = ultimaArea.Largura, areaH = ultimaArea.Bottom - ultimaArea.Top;
            int w = Mathf.Min(1700, areaW * 9 / 10), h = Mathf.Min(1060, areaH * 9 / 10);
            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(WS_OVERLAPPEDWINDOW | WS_VISIBLE));
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(WS_EX_APPWINDOW));
            var semVidro = new MARGINS();
            DwmExtendFrameIntoClientArea(hwnd, ref semVidro);
            SetWindowPos(hwnd, HWND_NOTOPMOST, ultimaArea.Left + (areaW - w) / 2, ultimaArea.Top + (areaH - h) / 2, w, h,
                SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            ShowWindow(hwnd, SW_SHOW);
            SetForegroundWindow(hwnd);
            visivel = true;
            Application.targetFrameRate = 60;   // no modo gerente as pessoas andam na sala: 60 quadros, movimento liso
        }
#endif

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

        /// <summary>Cursor em pixels da área de trabalho (origem em cima à esquerda), para arrastar a faixa mesmo saindo da janela.</summary>
        public static Vector2Int CursorNaTela
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                if (GetCursorPos(out var p)) return new Vector2Int(p.x, p.y);
#endif
                return new Vector2Int(Mathf.RoundToInt(Input.mousePosition.x), Screen.height - Mathf.RoundToInt(Input.mousePosition.y));
            }
        }

        /// <summary>Botão esquerdo apertado agora, lido do Windows (a janela da faixa nem sempre recebe o mouse).</summary>
        public static bool BotaoEsquerdo
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                return (GetAsyncKeyState(0x01) & 0x8000) != 0;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }

        /// <summary>Quanto a faixa pode subir (pixels) sem sair da área de trabalho.</summary>
        public int SubidaMaxima
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                return Mathf.Max(0, (ultimaArea.Bottom - ultimaArea.Top) - AlturaFisica());
#else
                return 0;
#endif
            }
        }

        /// <summary>Reposiciona já (a faixa sendo arrastada).</summary>
        public void Reposicionar()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (Ativa && !ModoGerente) Posicionar();
#endif
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
                if (!visivel || !GetCursorPos(out var p)) return false;
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

            if (ModoGerente)
            {
                EntrarJanelaNormal();
                Ativa = true;
                Ajustes.Mudou += () => proximaChecagem = 0;
                yield break;
            }

            ultimaArea = AreaDeTrabalho();
            AlturaVirtualAtual = Mathf.Clamp(AlturaVirtualAtual, AlturaVirtual, (ultimaArea.Bottom - ultimaArea.Top) / Escala);
            Screen.SetResolution(ultimaArea.Largura, AlturaFisica(), FullScreenMode.Windowed);
            yield return null;
            yield return null;

            // Validado no Unity 6.6 + D3D11 (sem flip model): janela "layered" com cor-chave preta,
            // mais o quadro do DWM estendido sobre toda a área. Preto puro vira transparente.
            AplicarEstilo();
            Posicionar();
            Ativa = true;
            Ajustes.Mudou += () => proximaChecagem = 0; // monitor ou tela cheia mudou: reavalia já
#else
            yield break;
#endif
        }

        /// <summary>
        /// Liga ou desliga o "clique atravessando". Chamar todo quadro: true quando o cursor
        /// está sobre algo do jogo, false quando está sobre área vazia.
        /// </summary>
        /// <summary>Modo gerente: janela normal do Windows com a vista isométrica (em vez da faixa transparente).</summary>
        public bool ModoGerente { get; private set; }

        public void DefinirModoGerente(bool sim)
        {
            if (sim == ModoGerente) return;
            ModoGerente = sim;
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (!Ativa) return;   // o Start ainda vai montar a janela no modo certo
            if (sim) { EntrarJanelaNormal(); return; }
            // de volta à faixa: estilo transparente, sempre por cima, colada na barra de tarefas
            if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);   // minimizada, a janela ignoraria o novo tamanho
            clicavel = true;
            AlturaVirtualAtual = AlturaVirtual;
            ultimaArea = AreaDeTrabalho();
            AplicarEstilo();
            Redimensionar();
            Application.targetFrameRate = 30;   // a faixa fica discreta: 30 bastam
#endif
        }

        /// <summary>A janela foi minimizada (botão "_" ou Win+D)? No modo gerente, isso leva o jogo de volta para a faixa.</summary>
        public bool Minimizada
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
                return Ativa && IsIconic(hwnd);
#else
                return false;
#endif
            }
        }

        public void DefinirClicavel(bool sim)
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (!Ativa || ModoGerente || sim == clicavel) return;
            clicavel = sim;
            long ex = WS_EX_LAYERED | WS_EX_TOOLWINDOW | (sim ? 0 : WS_EX_TRANSPARENT);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
#endif
        }

        /// <summary>O jogador escondeu a faixa (botão ou Ctrl+Alt+D). O jogo continua rodando e rendendo.</summary>
        public bool OcultaPeloJogador { get; private set; }

        /// <summary>Quantos monitores o Windows informou na última checagem.</summary>
        public static int QuantidadeMonitores { get; private set; } = 1;

        /// <summary>Atalho global para esconder e mostrar a faixa.</summary>
        public const string Atalho = "Ctrl+Alt+D";

        public void AlternarOculta()
        {
            OcultaPeloJogador = !OcultaPeloJogador;
            // escondida, a faixa vira um ícone perto do relógio (clicar nele traz a faixa de volta)
            if (OcultaPeloJogador) Bandeja.Mostrar(); else Bandeja.Remover();
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            AplicarVisibilidade();
#endif
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        bool atalhoPressionado, visivel = true;

        void AplicarVisibilidade()
        {
            bool mostrar = !escondida && !OcultaPeloJogador;
            if (mostrar == visivel) return;
            visivel = mostrar;
            ShowWindow(hwnd, mostrar ? SW_SHOWNA : SW_HIDE);
            Application.targetFrameRate = mostrar ? 30 : 5;
        }

        void Update()
        {
            if (!Ativa || ModoGerente) return;   // a janela normal é do Windows: nada de reposicionar nem esconder

            // Ctrl+Alt+D, lido direto do Windows para funcionar mesmo sem foco (só essas três teclas são consultadas)
            bool atalho = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 && (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 && (GetAsyncKeyState(VK_D) & 0x8000) != 0;
            if (atalho && !atalhoPressionado) AlternarOculta();
            atalhoPressionado = atalho;

            // depois de um SetResolution: estilo transparente de volta e janela colada na barra de tarefas
            if (reaplicarEstilo > 0 && --reaplicarEstilo == 0) { AplicarEstilo(); Posicionar(); }

            if (Time.unscaledTime < proximaChecagem) return;
            proximaChecagem = Time.unscaledTime + 2f;

            // Some quando outro programa está em tela cheia (jogo, vídeo, apresentação)
            escondida = Ajustes.EsconderEmTelaCheia && SHQueryUserNotificationState(out int estado) == 0 && (estado == 2 || estado == 3 || estado == 4);
            AplicarVisibilidade();
            if (!visivel) return;

            // Barra de tarefas mudou (resolução, DPI, barra oculta)? Reposiciona.
            var area = AreaDeTrabalho();
            if (!area.Equals(ultimaArea) || Screen.height != AlturaFisica())
            {
                ultimaArea = area;
                Redimensionar();
            }
            // rede de segurança: se algo devolveu a barra de título, tira de novo
            if ((GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64() & WS_CAPTION) != 0) AplicarEstilo();
            Posicionar(); // também reafirma o "sempre por cima", que a barra de tarefas às vezes rouba
        }

        int AlturaFisica() => AlturaVirtualAtual * Mathf.Max(2, Mathf.RoundToInt(2f * Dpi / 96f));

        void Posicionar()
        {
            int altura = AlturaFisica();
            SetWindowPos(hwnd, HWND_TOPMOST, ultimaArea.Left, ultimaArea.Bottom - altura - Mathf.Clamp(Ajustes.FaixaY, 0, Mathf.Max(0, (ultimaArea.Bottom - ultimaArea.Top) - altura)), ultimaArea.Largura, altura,
                SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);
        }

        /// <summary>Área útil (sem a barra de tarefas) do monitor escolhido nos Ajustes; o principal é o 0.</summary>
        static RECT AreaDeTrabalho()
        {
            monitores.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, AnotarMonitor, IntPtr.Zero);
            if (monitores.Count == 0)
            {
                var r = new RECT();
                SystemParametersInfo(SPI_GETWORKAREA, 0, ref r, 0);
                return r;
            }
            // principal primeiro, depois da esquerda para a direita
            monitores.Sort((a, b) => a.primario != b.primario ? (a.primario ? -1 : 1) : a.area.Left.CompareTo(b.area.Left));
            QuantidadeMonitores = monitores.Count;
            return monitores[((Ajustes.Monitor % monitores.Count) + monitores.Count) % monitores.Count].area;
        }

        static readonly System.Collections.Generic.List<(RECT area, bool primario)> monitores = new System.Collections.Generic.List<(RECT, bool)>();

        [AOT.MonoPInvokeCallback(typeof(MonitorEnumProc))]
        static bool AnotarMonitor(IntPtr monitor, IntPtr hdc, IntPtr retangulo, IntPtr dados)
        {
            var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (GetMonitorInfo(monitor, ref info)) monitores.Add((info.rcWork, (info.dwFlags & 1) != 0));
            return true;
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
        const long WS_POPUP = 0x80000000L, WS_VISIBLE = 0x10000000L, WS_CAPTION = 0x00C00000L, WS_OVERLAPPEDWINDOW = 0x00CF0000L;
        const long WS_EX_APPWINDOW = 0x40000L;
        const long WS_EX_LAYERED = 0x80000L, WS_EX_TRANSPARENT = 0x20L, WS_EX_TOOLWINDOW = 0x80L;
        const uint LWA_COLORKEY = 0x1;
        const uint SWP_NOACTIVATE = 0x10, SWP_FRAMECHANGED = 0x20, SWP_SHOWWINDOW = 0x40;
        const uint SPI_GETWORKAREA = 0x30;
        const int SW_HIDE = 0, SW_SHOW = 5, SW_SHOWNA = 8, SW_RESTORE = 9;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

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
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int indice);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr depoisDe, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint corChave, byte alfa, uint flags);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int comando);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
        delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr retangulo, IntPtr dados);
        [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr recorte, MonitorEnumProc cb, IntPtr dados);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO { public int cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int tecla);
        const int VK_CONTROL = 0x11, VK_MENU = 0x12, VK_D = 0x44;
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT p);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
        const uint GA_ROOT = 2;
        [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT p);
        [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern uint GetDpiForSystem();
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint acao, uint param, ref RECT r, uint winIni);
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS m);
        [DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int estado);
#endif
    }
}
