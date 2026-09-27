using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Canto DevOps da sala virtualizada: hosts de containers com "pods" coloridos piscando, o servidor de CI
    /// e uma esteira de CI/CD levando caixinhas (as versões novas) do CI até os hosts.
    /// Com deploy quebrado, pods e caixinhas ficam vermelhos, a esteira para e clicar faz o rollback.
    /// </summary>
    public class DevOpsVisual : MonoBehaviour
    {
        public const float AlturaEsteira = 34;   // y da esteira (acima dos hosts)
        static readonly Color[] CoresPods =
        {
            PixelArt.Hex("5cc8ff"), PixelArt.Hex("5cff8a"), PixelArt.Hex("ff8cc6"), PixelArt.Hex("ffd65c"), PixelArt.Hex("b48cff"),
        };
        static readonly Color Vermelho = PixelArt.Hex("ff3b4e"), Apagado = PixelArt.Hex("2a2d44");

        // Host de containers (18×26): gabinete escuro com 3 prateleiras
        static readonly Sprite SpriteHost = PixelArt.Criar(new[]
        {
            "kkkkkkkkkkkkkkkkkk",
            "kWWWWWWWWWWWWWWWWk",
            "kWkkkkkkkkkkkkkkWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkwwwwwwwwwwwwkWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkwwwwwwwwwwwwkWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkooooooooooookWk",
            "kWkwwwwwwwwwwwwkWk",
            "kWkkkkkkkkkkkkkkWk",
            "kWWWWWWWWWWWWWWWWk",
            "kWWgWWWWWWWWWWWWWk",
            "kWWWWWWWWWWWWWWWWk",
            "kWWWWWWWWWWWWWWWWk",
            "kWWWWWWWWWWWWWWWWk",
            "kWWWWWWWWWWWWWWWWk",
            "kkkkkkkkkkkkkkkkkk",
        }, new Vector2(0.5f, 0f));

        static readonly Sprite SpritePod = PixelArt.Criar(new[] { "###", "###" }, Vector2.zero);
        static readonly Sprite SpriteCaixa = PixelArt.Criar(new[] { "####", "####", "####" }, Vector2.zero);

        class Host
        {
            public SpriteRenderer sr;
            public readonly List<SpriteRenderer> pods = new List<SpriteRenderer>();
            public float proximo;
        }

        /// <summary>Recebe o clique no gabinete do host: com deploy quebrado faz rollback, senão rende um clique.</summary>
        class AlvoHost : MonoBehaviour, IClicavel
        {
            public DevOpsVisual dono;
            public Destaque destaque;
            public int Ordem => 3;
            public void Clicar() => dono.faixa.ClicarContainers(dono.Topo);
            public void DefinirDestaque(bool ligado) => destaque.Ligado = ligado;
        }

        Faixa faixa;
        Economia economia;
        float[] posicoesHosts;
        float xCi, esteiraInicio, esteiraFim;
        readonly List<Host> hosts = new List<Host>();
        readonly List<SpriteRenderer> caixas = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> roletes = new List<SpriteRenderer>();
        EquipamentoVisual ci;
        GameObject esteira;
        float proximaCaixa, faseEsteira;

        public Vector2 Topo => new Vector2((esteiraInicio + esteiraFim) / 2f, AlturaEsteira + 6);

        public void Iniciar(Faixa faixa, Economia economia, Transform pai, float[] posicoesHosts, float xCi)
        {
            this.faixa = faixa;
            this.economia = economia;
            this.posicoesHosts = posicoesHosts;
            this.xCi = xCi;
            transform.SetParent(pai, false);
            esteiraInicio = posicoesHosts[0] - 10;
            esteiraFim = xCi - 16;
            Atualizar();
        }

        /// <summary>Cria o que foi comprado e ainda não aparece (hosts, CI e a esteira).</summary>
        public void Atualizar()
        {
            while (hosts.Count < economia.HostsContainers && hosts.Count < posicoesHosts.Length)
                hosts.Add(CriarHost(posicoesHosts[hosts.Count]));
            if (economia.TemCi && ci == null)
            {
                ci = new GameObject("ServidorCI").AddComponent<EquipamentoVisual>();
                ci.Iniciar(transform.parent, "servidor_ci", new Vector2(xCi, Cenario.AlturaPiso), false);
                ci.TornarClicavel(() => faixa.ClicarContainers(ci.Topo));
            }
            if (economia.HostsContainers > 0 && esteira == null) CriarEsteira();
        }

        Host CriarHost(float x)
        {
            var h = new Host();
            h.sr = Filho("HostContainers", SpriteHost, new Vector2(x, Cenario.AlturaPiso), 3);
            h.sr.gameObject.AddComponent<BoxCollider2D>();
            var alvo = h.sr.gameObject.AddComponent<AlvoHost>();
            alvo.dono = this;
            alvo.destaque = Destaque.Para(h.sr);
            // 2 pods por prateleira, 3 prateleiras (y a partir da base do host)
            for (int prateleira = 0; prateleira < 3; prateleira++)
            for (int i = 0; i < 3; i++)
            {
                var p = Filho("Pod", SpritePod, new Vector2(x - 6 + i * 4, Cenario.AlturaPiso + 10 + prateleira * 5), 4);
                h.pods.Add(p);
            }
            faixa.Faiscas(new Vector2(x, Cenario.AlturaPiso + 26), 4);
            return h;
        }

        void CriarEsteira()
        {
            esteira = new GameObject("Esteira");
            esteira.transform.SetParent(transform, false);
            int largura = Mathf.RoundToInt(esteiraFim - esteiraInicio);
            var linhas = new string[3];
            linhas[0] = new string('W', largura);
            linhas[1] = new string('w', largura);
            linhas[2] = new string('k', largura);
            var sr = Filho("Correia", PixelArt.Criar(linhas, Vector2.zero), new Vector2(esteiraInicio, AlturaEsteira), 2);
            sr.transform.SetParent(esteira.transform, true);
            // roletes: pixels escuros que andam na faixa do meio (dá a sensação de movimento)
            for (int x = 0; x < largura; x += 4)
            {
                var r = Filho("Rolete", PixelArt.Pixel, new Vector2(esteiraInicio + x, AlturaEsteira + 1), 3);
                r.color = PixelArt.Hex("3a3d57");
                roletes.Add(r);
            }
            // pés da esteira
            Filho("Pe", PixelArt.Criar(new[] { "k", "k", "k", "k" }, Vector2.zero), new Vector2(esteiraInicio + 1, AlturaEsteira - 4), 2);
            Filho("Pe", PixelArt.Criar(new[] { "k", "k", "k", "k" }, Vector2.zero), new Vector2(esteiraFim - 2, AlturaEsteira - 4), 2);
        }

        SpriteRenderer Filho(string nome, Sprite sprite, Vector2 pos, int ordem)
        {
            var s = new GameObject(nome).AddComponent<SpriteRenderer>();
            s.transform.SetParent(transform, false);
            s.transform.localPosition = pos;
            s.sprite = sprite;
            s.sortingOrder = ordem;
            return s;
        }

        void Update()
        {
            bool quebrado = economia.DeployQuebrado;
            bool piscar = Mathf.FloorToInt(Time.time / 0.25f) % 2 == 0;
            ci?.DefinirAlerta(quebrado);

            foreach (var h in hosts)
            {
                if (quebrado) { foreach (var p in h.pods) p.color = piscar ? Vermelho : Apagado; continue; }
                if (Time.time < h.proximo) continue;
                h.proximo = Time.time + Random.Range(0.3f, 0.9f);
                foreach (var p in h.pods)
                    p.color = Random.value < 0.8f ? CoresPods[Random.Range(0, CoresPods.Length)] : Apagado;
            }

            if (esteira == null) return;

            // esteira anda para a esquerda (do CI para os hosts); parada com deploy quebrado
            if (!quebrado)
            {
                faseEsteira = (faseEsteira + Time.deltaTime * 10f) % 4f;
                for (int i = 0; i < roletes.Count; i++)
                    roletes[i].transform.localPosition = new Vector3(Mathf.Min(esteiraFim - 1, esteiraInicio + i * 4 + 3 - Mathf.Floor(faseEsteira)), AlturaEsteira + 1, 0);

                if (economia.TemCi && Time.time >= proximaCaixa)
                {
                    proximaCaixa = Time.time + 1.3f;
                    var c = Filho("Versao", SpriteCaixa, new Vector2(esteiraFim - 5, AlturaEsteira + 3), 4);
                    c.color = CoresPods[Random.Range(0, CoresPods.Length)];
                    caixas.Add(c);
                }
            }
            for (int i = caixas.Count - 1; i >= 0; i--)
            {
                var c = caixas[i];
                if (quebrado) { c.color = piscar ? Vermelho : PixelArt.Hex("8a1d2a"); continue; }
                var p = c.transform.localPosition;
                p.x -= Time.deltaTime * 10f;
                c.transform.localPosition = new Vector3(p.x, AlturaEsteira + 3, 0);
                if (p.x <= esteiraInicio + 1) { Destroy(c.gameObject); caixas.RemoveAt(i); }
            }
        }

        /// <summary>Depois do rollback as caixinhas quebradas somem da esteira.</summary>
        public void LimparEsteira()
        {
            foreach (var c in caixas) Destroy(c.gameObject);
            caixas.Clear();
        }
    }
}
