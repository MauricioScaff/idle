using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using IdleDataCenter.Simulacao;

namespace IdleDataCenter.Isometrico
{
    /// <summary>Tela gerencial isométrica. Nenhuma dependência da janela transparente da faixa.</summary>
    public partial class CentroDados : MonoBehaviour
    {
        const float W = 1440, H = 900;
        readonly Rect areaCentral = new Rect(184, 94, 978, 676);
        readonly Vector2[] racks = { new Vector2(.47f, .235f), new Vector2(.418f, .269f),
            new Vector2(.366f, .303f), new Vector2(.526f, .303f), new Vector2(.474f, .337f) };
        readonly string[] menus = { "Overview", "Racks", "Compute", "Storage", "Network", "Cooling", "Power", "Staff", "Research", "Upgrades", "Achievements" };
        CentroDadosSimulacao jogo;
        Economia E => jogo.Economia;
        IsoGui ui;
        Texture2D ambiente, rack, drone;
        readonly Texture2D[] passos = new Texture2D[4];
        readonly Rect[] passosUv = new Rect[4];
        readonly float[] passosAspecto = new float[4];
        Texture2D rackProcedural;
        Rect sala;
        string selecionado = "Overview", janela = "", aviso = "Welcome to DC-01. Build your next rack.";
        double ultimoTempo, proximoSave, ultimoClique;
        float avisoAte = 8, flashCompra;
        bool teste, som = true;
        int pagina;
        string captura;

        void Awake()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-iso-test") teste = true;
                if (arg.StartsWith("-capture=")) captura = arg.Substring(9);
            }
            jogo = new CentroDadosSimulacao(teste ? CentroDadosSimulacao.NovoEstado() : SaveCentroDados.Carregar(), teste ? new System.Random(8) : null);
            double ganho = E.AplicarOffline(Salvamento.AgoraUnix);
            if (ganho > 0) Notificar("Welcome back! Offline income: " + Dinheiro(ganho), 12);
            E.AutomacaoPronta += id => { Notificar("Research complete: " + NomeAutomacao(id), 8); Salvar(false); };
            E.Travou += _ => { Notificar("Server incident. Click NOC to restore service."); if (som) Sons.Alerta(); };
            E.DiscoQueimou += () => Notificar("Storage degraded. Click STORAGE to replace disk.");
            E.DeployQuebrou += () => Notificar("Deploy failed. Click DEPLOY to roll back.");
            Application.runInBackground = true;
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            ui = new IsoGui();
            ambiente = Resources.Load<Texture2D>("Isometrico/DataCenter");
            rack = Resources.Load<Texture2D>("Isometrico/Rack");
            if (rack == null) rack = rackProcedural = IsoSprites.Rack(true);
            drone = IsoSprites.Drone();
            for (int i = 0; i < 4; i++)
            {
                passos[i] = IsoSprites.Engenheiro(i);
                var area = ArteGerada.AreaOpaca(passos[i]);
                passosUv[i] = new Rect((float)area.x / passos[i].width, (float)area.y / passos[i].height,
                    (float)area.width / passos[i].width, (float)area.height / passos[i].height);
                passosAspecto[i] = (float)area.width / area.height;
            }
            ultimoTempo = Time.realtimeSinceStartupAsDouble;
            proximoSave = ultimoTempo + 20;
            if (!teste) Salvar(false);
        }

        IEnumerator Start()
        {
            if (string.IsNullOrEmpty(captura)) yield break;
            yield return new WaitForSecondsRealtime(3);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(captura);
            Debug.Log("ISOMETRIC_CAPTURE " + captura);
            yield return new WaitForSecondsRealtime(2);
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg == "-quit-after-capture") Application.Quit();
        }

        void Update()
        {
            double agora = Time.realtimeSinceStartupAsDouble, dt = agora - ultimoTempo;
            ultimoTempo = agora;
            if (dt > 60)
            {
                E.Estado.ultimoSalvamentoUnix = Salvamento.AgoraUnix - (long)dt;
                double ganho = E.AplicarOffline(Salvamento.AgoraUnix);
                Notificar("While away: " + Dinheiro(ganho));
                Salvar(false);
            }
            else jogo.Avancar(dt);
            if (agora >= proximoSave) { proximoSave = agora + 20; Salvar(false); }
            if (Input.GetKeyDown(KeyCode.Escape)) { janela = ""; selecionado = "Overview"; }
            if (Input.GetKeyDown(KeyCode.B)) Abrir("Racks");
            if (Input.GetKeyDown(KeyCode.R)) Abrir("Research");
            if (Input.GetKeyDown(KeyCode.F12))
            {
                string pasta = Path.Combine(Application.persistentDataPath, "Screenshots");
                Directory.CreateDirectory(pasta);
                string arquivo = Path.Combine(pasta, "datacenter-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
                ScreenCapture.CaptureScreenshot(arquivo);
                Debug.Log("ISOMETRIC_CAPTURE " + arquivo);
            }
            flashCompra = Mathf.Max(0, flashCompra - Time.unscaledDeltaTime);
        }

        void OnApplicationQuit() => Salvar(false);
        void OnApplicationPause(bool paused) { if (paused) Salvar(false); }
        void OnDestroy()
        {
            ui?.Dispose();
            if (rackProcedural != null) Destroy(rackProcedural);
            if (drone != null) Destroy(drone);
            foreach (var passo in passos) if (passo != null) Destroy(passo);
        }

        void Salvar(bool informar)
        {
            if (jogo == null) return;
            if (teste) { if (informar) Notificar("Test session: campaign save is isolated."); return; }
            bool ok = SaveCentroDados.Salvar(jogo.Estado);
            if (informar || !ok) Notificar(ok ? "Progress saved." : "Could not save. Check available disk space.");
        }

        void Notificar(string texto, float segundos = 5) { aviso = texto; avisoAte = Time.unscaledTime + segundos; }
        void Abrir(string secao) { selecionado = secao; janela = secao == "Overview" ? "" : secao; pagina = 0; }
        static string Numero(double n) => n.ToString("0.#", CultureInfo.InvariantCulture);
        static string Dinheiro(double n) => "$" + Faixa.Formatar(n);

        bool Comprar(string id)
        {
            if (!jogo.Comprar(id))
            {
                Notificar(E.NoMaximo(id) ? "Upgrade already complete." : "Not enough funds or missing prerequisite.");
                return false;
            }
            flashCompra = 1;
            Notificar(NomeMelhoria(id) + " installed. Production updated.");
            if (som) Sons.Compra();
            Salvar(false);
            return true;
        }

        void Reparar()
        {
            int quantidade = E.Travamentos.Count + (E.DiscoQueimado ? 1 : 0) + (E.DeployQuebrado ? 1 : 0);
            for (int i = E.Travamentos.Count - 1; i >= 0; i--) E.Clicar(E.Travamentos[i].servidor);
            if (E.DiscoQueimado) E.TrocarDisco(false);
            if (E.DeployQuebrado) E.FazerRollback(false);
            Notificar(quantidade > 0 ? quantidade + " incidents resolved. Services restored." : "All services operational.");
            if (quantidade > 0 && som) Sons.Conserto();
            Salvar(false);
        }

        void Deploy()
        {
            if (E.DeployQuebrado) { E.FazerRollback(false); Notificar("Rollback complete. Apps are online."); Salvar(false); return; }
            if (E.HostsContainers == 0) { Abrir("Compute"); Notificar("Install a container host to deploy applications."); return; }
            if (Time.realtimeSinceStartupAsDouble - ultimoClique < 1) return;
            ultimoClique = Time.realtimeSinceStartupAsDouble;
            double ganho = E.ClicarEquipamento();
            Notificar("Deployment delivered: +" + Dinheiro(ganho));
            if (som) Sons.Moeda();
        }

        void OnGUI()
        {
            if (ui == null) return;
            var anterior = GUI.matrix;
            float escala = Mathf.Min(Screen.width / W, Screen.height / H);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - W * escala) / 2, (Screen.height - H * escala) / 2, 0), Quaternion.identity, new Vector3(escala, escala, 1));
            ui.Ret(new Rect(0, 0, W, H), IsoGui.Fundo);
            DesenharSala();
            Topo(); Navegacao(); Objetivos(); Rodape();
            if (!string.IsNullOrEmpty(janela)) Loja();
            if (Time.unscaledTime < avisoAte)
            {
                var rect = new Rect(226, 739, 890, 28);
                ui.Caixa(rect, IsoGui.Cor("162c40"), IsoGui.Cyan);
                ui.Texto(aviso, rect.center.x, rect.y + 9, IsoGui.Branco, 2, true);
            }
            GUI.matrix = anterior;
        }
    }
}
