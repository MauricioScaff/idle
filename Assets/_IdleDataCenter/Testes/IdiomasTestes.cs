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
    }
}
