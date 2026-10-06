using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Tradução na hora de desenhar: o código continua escrevendo em português, e todo texto que passa pela IsoGui (modo
    /// gerente e faixa) ou pelo PixelTexto (painel da faixa) é trocado aqui quando o idioma é inglês. A tabela fica em
    /// Resources/Idiomas/en.txt, uma linha por texto ("português TAB inglês"). Frases montadas com valores usam modelos:
    /// a linha começa com "~" e {1}, {2}... marcam os pedaços que variam (eles também são traduzidos, se der):
    ///     ~Promovido a {1}! Novos setores liberados.	Promoted to {1}! New sectors unlocked.
    /// Texto sem tradução continua em português (e entra na lista Faltando, para achar o que falta).
    /// </summary>
    public static class Idiomas
    {
        static Dictionary<string, string> exatos;
        static List<(Regex padrao, string modelo)> modelos;
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        static readonly Regex palavra = new Regex(@"\p{Ll}{2,}", RegexOptions.Compiled);
        static readonly Regex horario = new Regex(@"^(\d\d:\d\d:\d\d\s+)(.+)$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>Textos que apareceram em inglês sem tradução (para revisar; só os primeiros).</summary>
        public static readonly HashSet<string> Faltando = new HashSet<string>();

        public static bool Ingles => Forcado || Ajustes.Idioma == 1;

        /// <summary>Teste: -ingles abre em inglês sem mudar a preferência do jogador.</summary>
        public static bool Forcado;

        /// <summary>O texto no idioma escolhido.</summary>
        public static string T(string s)
        {
            if (string.IsNullOrEmpty(s) || !Ingles) return s;
            if (cache.TryGetValue(s, out var pronto)) return pronto;
            Carregar();
            // número, dinheiro, hora: nada a traduzir (e são os textos que mudam a cada quadro)
            string r = palavra.IsMatch(s) ? Traduzir(s, 0) : s;
            if (cache.Count > 4000) cache.Clear();
            cache[s] = r;
            return r;
        }

        static string Traduzir(string s, int nivel)
        {
            if (exatos.TryGetValue(s, out var e)) return e;
            // espaços nas pontas não mudam a tradução
            string limpo = s.Trim();
            if (limpo.Length != s.Length && exatos.TryGetValue(limpo, out e)) return s.Replace(limpo, e);
            // linha do terminal com o horário na frente ("14:03:22  texto"): traduz só o texto
            var hora = horario.Match(s);
            if (hora.Success) return hora.Groups[1].Value + Traduzir(hora.Groups[2].Value, nivel);
            foreach (var (padrao, modelo) in modelos)
            {
                var m = padrao.Match(s);
                if (!m.Success) continue;
                var sb = new StringBuilder(modelo);
                for (int g = m.Groups.Count - 1; g >= 1; g--)
                {
                    string pedaco = m.Groups[g].Value;
                    sb.Replace("{" + g + "}", nivel < 3 && palavra.IsMatch(pedaco) ? Traduzir(pedaco, nivel + 1) : pedaco);
                }
                return sb.ToString();
            }
            if (nivel == 0 && Faltando.Count < 2000) Faltando.Add(s);
            return s;
        }

        static void Carregar()
        {
            if (exatos != null) return;
            exatos = new Dictionary<string, string>();
            modelos = new List<(Regex, string)>();
            var arquivo = Resources.Load<TextAsset>("Idiomas/en");
            if (arquivo == null) return;
            foreach (var linhaCrua in arquivo.text.Split('\n'))
            {
                string linha = linhaCrua.TrimEnd('\r');
                int tab = linha.IndexOf('\t');
                if (tab <= 0 || linha.StartsWith("#")) continue;
                string pt = linha.Substring(0, tab), en = linha.Substring(tab + 1);
                if (pt.StartsWith("~")) modelos.Add((ModeloParaRegex(pt.Substring(1)), en));
                else exatos[pt] = en;
            }
            // modelos mais específicos (mais texto fixo) primeiro
            modelos.Sort((a, b) => b.padrao.ToString().Length.CompareTo(a.padrao.ToString().Length));
        }

        /// <summary>"Promovido a {1}!" vira ^Promovido\ a\ (.+?)!$</summary>
        static Regex ModeloParaRegex(string modelo)
        {
            var sb = new StringBuilder("^");
            int i = 0;
            while (i < modelo.Length)
            {
                int abre = modelo.IndexOf('{', i);
                if (abre < 0 || abre + 2 >= modelo.Length || !char.IsDigit(modelo[abre + 1]) || modelo[abre + 2] != '}')
                {
                    sb.Append(Regex.Escape(modelo.Substring(i, abre < 0 ? modelo.Length - i : abre + 1 - i)));
                    if (abre < 0) break;
                    i = abre + 1;
                    continue;
                }
                sb.Append(Regex.Escape(modelo.Substring(i, abre - i)));
                sb.Append("(.+?)");
                i = abre + 3;
            }
            sb.Append("$");
            return new Regex(sb.ToString(), RegexOptions.Compiled | RegexOptions.Singleline);
        }

        /// <summary>Grava a lista do que apareceu sem tradução (idioma_faltando.txt, na pasta do save). Só se jogou em inglês.</summary>
        public static void SalvarFaltando()
        {
            if (Faltando.Count == 0) return;
            try { System.IO.File.WriteAllLines(System.IO.Path.Combine(Application.persistentDataPath, "idioma_faltando.txt"), Faltando); }
            catch (System.Exception e) { Debug.LogWarning("idioma_faltando: " + e.Message); }
        }

        /// <summary>Recarrega a tabela (troca de idioma, ou o arquivo mudou).</summary>
        public static void Recarregar() { exatos = null; cache.Clear(); }
    }
}
