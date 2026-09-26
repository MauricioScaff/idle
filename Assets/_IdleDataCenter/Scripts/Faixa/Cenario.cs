using System.Collections.Generic;
using System.Linq;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O cômodo onde o técnico trabalha, montado conforme o cargo:
    /// armário (Técnico de TI) ou salinha com rack, no-break e refrigeração (Sysadmin).
    /// Na promoção, o cenário inteiro é destruído e montado de novo.
    /// Coordenadas locais: x a partir da borda esquerda, y a partir de baixo (piso em y = AlturaPiso).
    /// </summary>
    public class Cenario : MonoBehaviour
    {
        public const int Altura = 60, AlturaPiso = 6;
        const float DistanciaServidor = 17f;   // do centro do técnico ao centro da torre

        Faixa faixa;
        Economia economia;
        readonly List<ServidorVelho> torres = new List<ServidorVelho>();
        float[] posicoesTorres;
        RackVisual rack;
        EquipamentoVisual noBreak, refrigeracao;
        Tecnico estagiario;

        public int Largura { get; private set; }
        public TelaTerminal Tela { get; private set; }
        public Tecnico Tecnico { get; private set; }
        public ServidorVelho PrimeiraTorre => torres[0];

        /// <summary>Onde o técnico para para digitar (em frente à mesa).</summary>
        public float PosicaoMesa => 74f;
        /// <summary>O técnico anda até aqui (fica ao lado da torre mais à esquerda).</summary>
        public float LimiteTecnico => torres[torres.Count - 1].X - DistanciaServidor;
        public Vector2 TopoMaisProximo => torres[torres.Count - 1].Topo;
        public Vector2 TopoDoIncidente { get; private set; }

        public void Montar(Faixa faixa, Economia economia, Vector2 posicao)
        {
            this.faixa = faixa;
            this.economia = economia;
            transform.position = posicao;
            bool salinha = economia.Cargo >= 1;
            Largura = salinha ? 290 : 200;
            posicoesTorres = salinha ? new float[] { 170, 146, 122 } : new float[] { 176, 152, 128 };

            var fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = salinha ? PixelArt.Salinha(Largura, Altura, AlturaPiso) : PixelArt.Armario(Largura, Altura, AlturaPiso);
            gameObject.AddComponent<BoxCollider2D>(); // o cômodo inteiro "segura" o clique

            Decoracao("Planta", ArteGerada.Objeto("planta"), new Vector2(13, AlturaPiso), 2);
            MontarMesa();
            RelogioParede.Criar(Decoracao("Relogio", Arte.Relogio, new Vector2(104, 42), 1));

            AtualizarEquipamentos();

            Decoracao("Caneca", Arte.Caneca, torres[0].Topo + new Vector2(-2, -1), 4).gameObject.AddComponent<Vapor>();

            Tecnico = new GameObject("Tecnico").AddComponent<Tecnico>();
            Tecnico.Iniciar(faixa, this, economia.Cargo);
            AtualizarEquipamentos(); // o estagiário, se já foi contratado, entra depois do técnico
        }

        /// <summary>Mesa com o CRT (arte do PixelLab); a tela ganha um terminal animado por cima do texto desenhado.</summary>
        void MontarMesa()
        {
            var mesa = Decoracao("Mesa", ArteGerada.Objeto("mesa_crt"), new Vector2(44, AlturaPiso), 2);
            // o texto verde da tela é achado pela cor; o terminal animado cobre exatamente essa área
            var texto = ArteGerada.Leds(mesa.sprite);
            int x0 = texto.Min(p => p.x), x1 = texto.Max(p => p.x), y0 = texto.Min(p => p.y), y1 = texto.Max(p => p.y);
            Tela = TelaTerminal.Criar(mesa.transform, new Vector2(x0, y0), x1 - x0 + 1, y1 - y0 + 1, CorDeFundoDaTela(mesa.sprite, x0, y0, x1, y1), 3);
        }

        /// <summary>A cor escura mais comum dentro da tela (o fundo do CRT), ignorando o texto verde.</summary>
        static Color32 CorDeFundoDaTela(Sprite s, int x0, int y0, int x1, int y1)
        {
            var contagem = new Dictionary<Color32, int>();
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Color32 c = s.texture.GetPixel((int)(s.rect.x + s.pivot.x) + x, (int)(s.rect.y + s.pivot.y) + y);
                if (c.a == 0 || c.g > 140) continue;
                contagem[c] = contagem.TryGetValue(c, out int n) ? n + 1 : 1;
            }
            return contagem.Count > 0 ? contagem.OrderByDescending(k => k.Value).First().Key : new Color32(20, 40, 28, 255);
        }

        public SpriteRenderer Decoracao(string nome, Sprite sprite, Vector2 posicaoLocal, int ordem)
        {
            var sr = new GameObject(nome).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.transform.localPosition = posicaoLocal;
            sr.sprite = sprite;
            sr.sortingOrder = ordem;
            return sr;
        }

        /// <summary>Cria o que foi comprado e ainda não está no cenário. Retorna a torre nova, se surgiu uma.</summary>
        public ServidorVelho AtualizarEquipamentos()
        {
            ServidorVelho nova = null;
            while (torres.Count < economia.Torres && torres.Count < posicoesTorres.Length)
            {
                nova = new GameObject("Torre").AddComponent<ServidorVelho>();
                nova.Iniciar(faixa, transform, new Vector2(posicoesTorres[torres.Count], AlturaPiso), torres.Count);
                torres.Add(nova);
            }
            foreach (var t in torres) t.AtualizarVisual(economia.TemSsd, economia.Ventoinhas);

            if (economia.TemRack && rack == null)
            {
                rack = new GameObject("Rack").AddComponent<RackVisual>();
                rack.Iniciar(faixa, transform, new Vector2(200, AlturaPiso));
            }
            rack?.DefinirQuantidade(economia.ServidoresRack);

            if (economia.Nivel(Catalogo.NoBreak) > 0 && noBreak == null)
            {
                noBreak = new GameObject("NoBreak").AddComponent<EquipamentoVisual>();
                noBreak.Iniciar(transform, "nobreak", new Vector2(226, AlturaPiso), false);
            }
            if (economia.Nivel(Catalogo.ArCondicionado) > 0 && refrigeracao == null)
            {
                refrigeracao = new GameObject("Refrigeracao").AddComponent<EquipamentoVisual>();
                refrigeracao.Iniciar(transform, "refrigeracao", new Vector2(262, AlturaPiso), true);
            }
            if (economia.TemEstagiario && estagiario == null && Tecnico != null)
            {
                estagiario = new GameObject("Estagiario").AddComponent<Tecnico>();
                estagiario.Iniciar(faixa, this, economia.Cargo, "estagiario", 14f);
            }
            return nova;
        }

        /// <summary>Onde soltar efeitos de um servidor (topo da torre, ou topo do rack para os 1U).</summary>
        public Vector2 TopoDoServidor(int servidor) =>
            economia.EhTorre(servidor) && servidor < torres.Count ? torres[servidor].Topo
            : rack != null ? rack.Topo : TopoMaisProximo;

        public void PularTudo()
        {
            foreach (var t in torres) t.Pular();
            rack?.Pular();
        }

        /// <summary>Reflete os servidores travados (LEDs vermelhos e alertas), a sobrecarga e o calor.</summary>
        public void AtualizarIncidentes()
        {
            for (int i = 0; i < torres.Count; i++) torres[i].DefinirTravado(economia.Travado(i));
            rack?.DefinirTravados(vaga => economia.Travado(economia.Torres + vaga));
            noBreak?.DefinirAlerta(economia.Sobrecarga);
            refrigeracao?.DefinirAlerta(economia.Quente);
        }

        /// <summary>Para onde o técnico deve correr: o servidor travado há mais tempo.</summary>
        public bool AlvoDeConserto(out float x)
        {
            x = 0;
            var lista = economia.Travamentos;
            if (lista.Count == 0) return false;
            int s = lista[0].servidor;
            if (economia.EhTorre(s) && s < torres.Count)
            {
                x = torres[s].X - DistanciaServidor;
                TopoDoIncidente = torres[s].Topo;
            }
            else
            {
                x = LimiteTecnico; // o rack fica atrás das torres: ele conserta do lado
                TopoDoIncidente = rack != null ? rack.Topo : TopoMaisProximo;
            }
            return true;
        }
    }
}
