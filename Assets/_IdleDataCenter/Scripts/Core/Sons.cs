using UnityEngine;

namespace IdleDataCenter
{
    /// <summary>
    /// Sons sintetizados por código (sem arquivos), curtos e discretos. O único que chama atenção
    /// é o alerta de servidor travado. Volume e liga/desliga vêm dos Ajustes.
    /// </summary>
    public class Sons : MonoBehaviour
    {
        const int Taxa = 44100;
        enum Onda { Seno, Triangulo, Quadrada }

        static Sons instancia;

        AudioSource[] fontes;
        int proxima;
        AudioSource zumbido;
        AudioClip moeda, compra, alerta, conserto, promocao, tique;
        float ultimaMoeda;

        static Sons I
        {
            get
            {
                if (instancia == null) instancia = new GameObject("Sons").AddComponent<Sons>();
                return instancia;
            }
        }

        public static void Iniciar() => _ = I;
        public static void Moeda() { if (Time.unscaledTime - I.ultimaMoeda > 0.05f) { I.ultimaMoeda = Time.unscaledTime; I.Tocar(I.moeda, 0.5f, Random.Range(0.97f, 1.03f)); } }
        public static void Compra() => I.Tocar(I.compra, 0.55f);
        public static void Alerta() => I.Tocar(I.alerta, 0.8f);
        public static void Conserto() => I.Tocar(I.conserto, 0.5f);
        public static void Promocao() => I.Tocar(I.promocao, 0.7f);
        public static void Tique() => I.Tocar(I.tique, 0.35f);

        void Awake()
        {
            // a cena não vem com AudioListener; sem ele nenhum som toca
            if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<AudioListener>();

            fontes = new AudioSource[6];
            for (int i = 0; i < fontes.Length; i++)
            {
                fontes[i] = gameObject.AddComponent<AudioSource>();
                fontes[i].playOnAwake = false;
            }

            moeda = Clip("moeda", Notas(Onda.Quadrada, 0.06f, 0.18f, 988, 1319));
            compra = Clip("compra", Notas(Onda.Triangulo, 0.07f, 0.35f, 523, 659, 784, 1047));
            alerta = Clip("alerta", Notas(Onda.Quadrada, 0.15f, 0.22f, 440, 0, 440));   // bipe de no-break: dois toques graves
            conserto = Clip("conserto", Deslizar(0.16f, 523, 784, 0.4f));
            promocao = Clip("promocao", Notas(Onda.Triangulo, 0.1f, 0.4f, 523, 659, 784, 1047, 1047, 1319));
            tique = Clip("tique", Notas(Onda.Quadrada, 0.02f, 0.12f, 1800));

            zumbido = gameObject.AddComponent<AudioSource>();
            zumbido.clip = Clip("zumbido", Ventoinha());
            zumbido.loop = true;
            zumbido.volume = 0.18f;

            Ajustes.Mudou += Aplicar;
            Aplicar();
        }

        void OnDestroy() => Ajustes.Mudou -= Aplicar;

        void Aplicar()
        {
            AudioListener.volume = Ajustes.VolumeFinal;
            if (Ajustes.Zumbido && Ajustes.Som && !zumbido.isPlaying) zumbido.Play();
            if ((!Ajustes.Zumbido || !Ajustes.Som) && zumbido.isPlaying) zumbido.Stop();
        }

        void Tocar(AudioClip clip, float volume, float pitch = 1f)
        {
            if (!Ajustes.Som) return;
            var f = fontes[proxima];
            proxima = (proxima + 1) % fontes.Length;
            f.pitch = pitch;
            f.PlayOneShot(clip, volume);
        }

        static AudioClip Clip(string nome, float[] dados)
        {
            var c = AudioClip.Create(nome, dados.Length, 1, Taxa, false);
            c.SetData(dados, 0);
            return c;
        }

        static float Amostra(Onda onda, float fase)
        {
            float p = fase - Mathf.Floor(fase);
            switch (onda)
            {
                case Onda.Seno: return Mathf.Sin(p * Mathf.PI * 2f);
                case Onda.Triangulo: return 1f - 4f * Mathf.Abs(p - 0.5f);
                default: return p < 0.5f ? 0.6f : -0.6f; // quadrada mais baixa, para não ser estridente
            }
        }

        /// <summary>Sequência de notas curtas (0 = pausa), cada uma com ataque rápido e queda suave.</summary>
        static float[] Notas(Onda onda, float duracaoNota, float volume, params float[] freqs)
        {
            int porNota = (int)(duracaoNota * Taxa);
            var d = new float[porNota * freqs.Length + Taxa / 20];
            for (int n = 0; n < freqs.Length; n++)
            {
                if (freqs[n] <= 0) continue;
                float fase = 0;
                bool ultima = n == freqs.Length - 1;
                int dur = ultima ? porNota * 2 : porNota;
                for (int i = 0; i < dur && n * porNota + i < d.Length; i++)
                {
                    float t = i / (float)dur;
                    fase += freqs[n] / Taxa;
                    float env = Mathf.Min(1f, i / (0.004f * Taxa)) * (1f - t) * (1f - t);
                    d[n * porNota + i] += Amostra(onda, fase) * env * volume;
                }
            }
            return d;
        }

        static float[] Deslizar(float duracao, float f0, float f1, float volume)
        {
            int n = (int)(duracao * Taxa);
            var d = new float[n];
            float fase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                fase += Mathf.Lerp(f0, f1, t) / Taxa;
                d[i] = Amostra(Onda.Seno, fase) * Mathf.Min(1f, i / (0.005f * Taxa)) * (1f - t) * volume;
            }
            return d;
        }

        /// <summary>Zumbido de ventoinha: ruído grave e suave + um leve tom de 120 Hz, em loop de 4 s sem estalo.</summary>
        static float[] Ventoinha()
        {
            int n = 4 * Taxa;
            var d = new float[n];
            var r = new System.Random(7);
            float marrom = 0;
            for (int i = 0; i < n; i++)
            {
                marrom = Mathf.Clamp(marrom + (float)(r.NextDouble() * 2 - 1) * 0.02f, -1f, 1f) * 0.998f;
                d[i] = marrom * 0.5f + 0.08f * Mathf.Sin(2f * Mathf.PI * 120f * i / Taxa);
            }
            // funde o fim com o começo e descarta o começo: o fim termina exatamente onde o loop recomeça
            int cruzamento = Taxa / 4;
            for (int i = 0; i < cruzamento; i++)
            {
                float k = i / (float)cruzamento;
                d[n - cruzamento + i] = d[n - cruzamento + i] * (1 - k) + d[i] * k;
            }
            var loop = new float[n - cruzamento];
            System.Array.Copy(d, cruzamento, loop, 0, loop.Length);
            return loop;
        }
    }
}
