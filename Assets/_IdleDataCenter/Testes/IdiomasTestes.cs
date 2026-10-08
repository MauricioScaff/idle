using NUnit.Framework;

namespace IdleDataCenter.Testes
{
    /// <summary>A tabela de inglês (Resources/Idiomas/en.txt): textos inteiros, modelos com valores e o horário do terminal.</summary>
    public class IdiomasTestes
    {
        int idiomaAntes;

        [SetUp] public void Ingles() { idiomaAntes = Ajustes.Idioma; Ajustes.Idioma = 1; Idiomas.Recarregar(); }
        [TearDown] public void Voltar() { Ajustes.Idioma = idiomaAntes; Idiomas.Recarregar(); }

        [Test]
        public void TextoInteiroViraIngles()
        {
            Assert.AreEqual("New game", Idiomas.T("Novo jogo"));
            Assert.AreEqual("Client site", Idiomas.T("Site de cliente"));
            Assert.AreEqual("12 345", Idiomas.T("12 345"), "número não muda");
        }

        [Test]
        public void ModeloTraduzOsPedacosTambem()
        {
            Assert.AreEqual("Promoted to Sysadmin! New sectors unlocked.", Idiomas.T("Promovido a Sysadmin! Novos setores liberados."));
            Assert.AreEqual("Hired as IT Technician! New sectors unlocked.", Idiomas.T("Contratado como Técnico de TI! Novos setores liberados."));
            Assert.AreEqual("Goal for Sysadmin", Idiomas.T("Meta para Sysadmin"));
        }

        [Test]
        public void HorarioDoTerminalFicaETextoTraduz()
        {
            Assert.AreEqual("14:03:22  rm: nothing removed (thankfully).", Idiomas.T("14:03:22  rm: nada removido (ainda bem)."));
        }

        [Test]
        public void EmPortuguesNadaMuda()
        {
            Ajustes.Idioma = 0;
            Assert.AreEqual("Novo jogo", Idiomas.T("Novo jogo"));
        }

        [Test]
        public void TodoTermoDoDicionarioTemIngles()
        {
            Idiomas.Faltando.Clear();
            foreach (var t in Glossario.Termos) { Idiomas.T(t.Nome); Idiomas.T(t.OQueE); Idiomas.T(t.NoJogo); }
            Idiomas.T("No jogo:");
            Assert.IsEmpty(Idiomas.Faltando, "sem tradução: " + string.Join(" | ", Idiomas.Faltando));
        }

        [Test]
        public void TodaMetaTemIngles()
        {
            Idiomas.Faltando.Clear();
            foreach (var c in IdleDataCenter.Simulacao.Catalogo.Cargos)
                foreach (var m in c.MetasParaPromocao) Idiomas.T(m.Texto);
            Assert.IsEmpty(Idiomas.Faltando, "sem tradução: " + string.Join(" | ", Idiomas.Faltando));
        }

        [Test]
        public void TodoProdutoTemIngles()
        {
            Idiomas.Faltando.Clear();
            foreach (var d in IdleDataCenter.Simulacao.Catalogo.Melhorias)
            {
                Idiomas.T(d.Nome);
                if (d.Produtos != null) foreach (var p in d.Produtos) Idiomas.T(p.Nome);
            }
            Assert.IsEmpty(Idiomas.Faltando, "sem tradução: " + string.Join(" | ", Idiomas.Faltando));
        }

        [Test]
        public void DicionarioAchaOsTermosNoTexto()
        {
            Ajustes.Idioma = 0;
            var achados = Glossario.Encontrar("Rack 42U: vagas para servidores 1U, gasta kW");
            CollectionAssert.AreEqual(new[] { "Rack", "42U", "1U", "kW" }, achados.ConvertAll(a => "Rack 42U: vagas para servidores 1U, gasta kW".Substring(a.inicio, a.comprimento)));
            Assert.IsEmpty(Glossario.Encontrar("Rackzinho quebrado"), "só palavras inteiras");
            Assert.AreEqual("Kubernetes (K8s)", Glossario.Encontrar("Nó K8s")[0].termo.Nome);
            Ajustes.Idioma = 1;
            Assert.AreEqual("No-break", Glossario.Encontrar("UPS +1.5 kW")[0].termo.Nome, "em inglês, pelas formas em inglês");
        }
    }
}
