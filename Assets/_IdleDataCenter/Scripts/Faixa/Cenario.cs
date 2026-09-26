using System.Collections.Generic;
using IdleDataCenter.Simulacao;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// O cômodo onde o técnico trabalha, montado conforme o cargo:
    /// armário (Técnico de TI) ou salinha com rack, no-break e ar-condicionado (Sysadmin).
    /// Na promoção, o cenário inteiro é destruído e montado de novo.
    /// Coordenadas locais: x a partir da borda esquerda, y a partir de baixo (piso em y = 5).
    /// </summary>
    public class Cenario : MonoBehaviour
    {
        public const int Altura = 44, AlturaPiso = 5;
        const float DistanciaServidor = 11f;

        Faixa faixa;
        Economia economia;
        readonly List<ServidorVelho> torres = new List<ServidorVelho>();
        float[] posicoesTorres;
        RackVisual rack;
        NoBreakVisual noBreak;
        ArCondicionadoVisual arCondicionado;

        public int Largura { get; private set; }
        public TelaTerminal Tela { get; private set; }
        public Tecnico Tecnico { get; private set; }
        public ServidorVelho PrimeiraTorre => torres[0];

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
            Largura = salinha ? 220 : 160;
            posicoesTorres = salinha ? new float[] { 124, 108, 92 } : new float[] { 140, 124, 108 };

            var fundo = gameObject.AddComponent<SpriteRenderer>();
            fundo.sprite = salinha ? PixelArt.Salinha(Largura, Altura, AlturaPiso) : PixelArt.Armario(Largura, Altura, AlturaPiso);
            gameObject.AddComponent<BoxCollider2D>(); // o cômodo inteiro "segura" o clique

            Decoracao("Planta", Arte.Planta, new Vector2(8, AlturaPiso), 2);
            MontarCantoDaMesa();
            var ventilador = Decoracao("Ventilador", Arte.VentiladorA, new Vector2(Largura - 10, AlturaPiso), 2);
            Animacao.Aplicar(ventilador, 10f, Arte.VentiladorA, Arte.VentiladorB);

            AtualizarEquipamentos();

            Decoracao("Caneca", Arte.Caneca, torres[0].Topo + new Vector2(-3, 0), 4).gameObject.AddComponent<Vapor>();

            Tecnico = new GameObject("Tecnico").AddComponent<Tecnico>();
            Tecnico.Iniciar(faixa, this, economia.Cargo);
        }

        /// <summary>Era 1 (TI improvisada): mesa com CRT, ferramentas, relógio e cabo solto. Vai junto na promoção.</summary>
        void MontarCantoDaMesa()
        {
            Decoracao("Mesa", Arte.Mesa, new Vector2(14, AlturaPiso), 2);
            Decoracao("Ferramentas", Arte.CaixaFerramentas, new Vector2(18, AlturaPiso), 3);
            Decoracao("Cabo", Arte.CaboSolto, new Vector2(38, AlturaPiso), 1);
            var monitor = Decoracao("Monitor", Arte.MonitorCrt, new Vector2(20, AlturaPiso + 10), 3);
            Tela = TelaTerminal.Criar(monitor.transform);
            Decoracao("PostIt", Arte.PostIt, new Vector2(31, AlturaPiso + 19), 5);
            RelogioParede.Criar(Decoracao("Relogio", Arte.Relogio, new Vector2(68, 29), 1));
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
                rack.Iniciar(faixa, transform, new Vector2(140, AlturaPiso));
            }
            rack?.DefinirQuantidade(economia.ServidoresRack);

            int nivelNoBreak = economia.Nivel(Catalogo.NoBreak);
            if (nivelNoBreak > 0 && noBreak == null)
            {
                noBreak = new GameObject("NoBreak").AddComponent<NoBreakVisual>();
                noBreak.Iniciar(transform, new Vector2(168, AlturaPiso));
            }
            noBreak?.Atualizar(nivelNoBreak, economia.Sobrecarga);

            if (economia.Nivel(Catalogo.ArCondicionado) > 0 && arCondicionado == null)
            {
                arCondicionado = new GameObject("ArCondicionado").AddComponent<ArCondicionadoVisual>();
                arCondicionado.Iniciar(transform, new Vector2(186, 30));
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

        /// <summary>Reflete os servidores travados (LEDs vermelhos e alertas).</summary>
        public void AtualizarIncidentes()
        {
            for (int i = 0; i < torres.Count; i++) torres[i].DefinirTravado(economia.Travado(i));
            rack?.DefinirTravados(vaga => economia.Travado(economia.Torres + vaga));
            noBreak?.Atualizar(economia.Nivel(Catalogo.NoBreak), economia.Sobrecarga);
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
