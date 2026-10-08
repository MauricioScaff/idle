using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O ícone do jogo na bandeja do Windows (perto do relógio) enquanto a faixa está escondida: um clique traz a faixa
    /// de volta; o botão direito abre um menu com "Mostrar a faixa" e "Sair". O ícone é o do próprio .exe.
    /// Os cliques chegam por uma janela invisível do jogo; aqui só se anotam os pedidos, a Faixa atende no Update.
    /// Só faz algo no build de Windows.
    /// </summary>
    public static class Bandeja
    {
        /// <summary>O jogador clicou no ícone (ou em "Mostrar a faixa").</summary>
        public static bool PedidoMostrar;
        /// <summary>O jogador escolheu "Sair" no menu do ícone.</summary>
        public static bool PedidoSair;

        public static void Mostrar()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (visivel || (janela == IntPtr.Zero && !CriarJanela())) return;
            var d = Dados();
            visivel = Shell_NotifyIcon(NIM_ADD, ref d);
#endif
        }

        public static void Remover()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            if (!visivel) return;
            visivel = false;
            var d = Dados();
            Shell_NotifyIcon(NIM_DELETE, ref d);
#endif
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        static IntPtr janela, icone;
        static bool visivel;
        static uint taskbarCriada;
        static readonly WndProc procedimento = Procedimento;   // fica guardado: o Windows chama este delegate

        const string Classe = "IdleDataCenterBandeja";
        const uint WM_BANDEJA = 0x8000 + 1;   // WM_APP + 1
        const int WM_LBUTTONUP = 0x0202, WM_RBUTTONUP = 0x0205;
        const int NIM_ADD = 0, NIM_DELETE = 2, NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4;
        const uint MF_STRING = 0, MF_SEPARATOR = 0x800, TPM_RIGHTBUTTON = 0x2, TPM_NONOTIFY = 0x80, TPM_RETURNCMD = 0x100;
        const uint WS_POPUP = 0x80000000, WS_EX_TOOLWINDOW = 0x80;

        static NOTIFYICONDATA Dados() => new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
            hWnd = janela,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_BANDEJA,
            hIcon = icone,
            szTip = Idiomas.T("Idle Data Center: clique para mostrar a faixa"),
        };

        static bool CriarJanela()
        {
            IntPtr instancia = GetModuleHandle(null);
            var classe = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(procedimento),
                hInstance = instancia,
                lpszClassName = Classe,
            };
            if (RegisterClassEx(ref classe) == 0 && Marshal.GetLastWin32Error() != 1410) return false;   // 1410: já registrada
            // janela comum (nunca mostrada), não "message-only": o menu do botão direito precisa de uma janela em primeiro plano
            janela = CreateWindowEx(WS_EX_TOOLWINDOW, Classe, "Idle Data Center", WS_POPUP, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, instancia, IntPtr.Zero);
            if (janela == IntPtr.Zero) return false;
            taskbarCriada = RegisterWindowMessage("TaskbarCreated");

            var exe = new StringBuilder(1024);
            GetModuleFileName(IntPtr.Zero, exe, exe.Capacity);
            var pequeno = new IntPtr[1];
            if (ExtractIconEx(exe.ToString(), 0, null, pequeno, 1) > 0) icone = pequeno[0];
            if (icone == IntPtr.Zero) icone = LoadIcon(IntPtr.Zero, new IntPtr(32512));   // ícone genérico do Windows
            return true;
        }

        [AOT.MonoPInvokeCallback(typeof(WndProc))]
        static IntPtr Procedimento(IntPtr h, uint msg, IntPtr w, IntPtr l)
        {
            if (msg == WM_BANDEJA)
            {
                int evento = (int)(l.ToInt64() & 0xFFFF);
                if (evento == WM_LBUTTONUP) PedidoMostrar = true;
                else if (evento == WM_RBUTTONUP) Menu(h);
                return IntPtr.Zero;
            }
            if (taskbarCriada != 0 && msg == taskbarCriada && visivel)
            {
                // o Explorer reiniciou e levou o ícone junto: põe de novo
                var d = Dados();
                Shell_NotifyIcon(NIM_ADD, ref d);
            }
            return DefWindowProc(h, msg, w, l);
        }

        static void Menu(IntPtr h)
        {
            IntPtr menu = CreatePopupMenu();
            AppendMenu(menu, MF_STRING, new IntPtr(1), Idiomas.T("Mostrar a faixa"));
            AppendMenu(menu, MF_SEPARATOR, IntPtr.Zero, null);
            AppendMenu(menu, MF_STRING, new IntPtr(2), Idiomas.T("Sair"));
            SetMenuDefaultItem(menu, 1, 0);
            GetCursorPos(out POINT p);
            SetForegroundWindow(h);   // sem isso o menu não fecha ao clicar fora dele
            int escolha = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_NONOTIFY, p.x, p.y, 0, h, IntPtr.Zero);
            PostMessage(h, 0, IntPtr.Zero, IntPtr.Zero);
            DestroyMenu(menu);
            if (escolha == 1) PedidoMostrar = true;
            else if (escolha == 2) PedidoSair = true;
        }

        delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID, uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
            public int dwState, dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
            public int uVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public uint cbSize, style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra, cbWndExtra;
            public IntPtr hInstance, hIcon, hCursor, hbrBackground;
            public string lpszMenuName, lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern bool Shell_NotifyIcon(int mensagem, ref NOTIFYICONDATA dados);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern uint ExtractIconEx(string arquivo, int indice, IntPtr[] grandes, IntPtr[] pequenos, uint quantos);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string modulo);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern uint GetModuleFileName(IntPtr modulo, StringBuilder caminho, int tamanho);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern ushort RegisterClassEx(ref WNDCLASSEX classe);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateWindowEx(uint exEstilo, string classe, string nome, uint estilo, int x, int y, int w, int h, IntPtr pai, IntPtr menu, IntPtr instancia, IntPtr param);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string nome);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadIcon(IntPtr instancia, IntPtr nome);
        [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool AppendMenu(IntPtr menu, uint flags, IntPtr id, string texto);
        [DllImport("user32.dll")] static extern bool SetMenuDefaultItem(IntPtr menu, uint item, uint porPosicao);
        [DllImport("user32.dll")] static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reservado, IntPtr hWnd, IntPtr retangulo);
        [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr menu);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
#endif
    }
}
