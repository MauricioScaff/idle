using System.Collections.Generic;

// As salas montadas na escala da pessoa (ver Montar.cs). Cores medidas nas ilustrações de Arte/PixelLab/salas_base.
// Móveis: recorte na ilustração (x, y, largura, altura), parede ('E', 'D' ou 'C' = canto do fundo), posição na
// parede nova (casa do pé, embaixo da borda esquerda do recorte) e escala.
public static class Salas
{
    static int C(string hex) { return unchecked((int)(0xFF000000 | System.Convert.ToUInt32(hex, 16))); }
    static int[] Cs(params string[] h) { var r = new int[h.Length]; for (int i = 0; i < h.Length; i++) r[i] = C(h[i]); return r; }

    public static List<Montar.Sala> Todas()
    {
        var l = new List<Montar.Sala>();

        // Salinha (Sysadmin): 7 casas, piso de tábua, janela e planta no fundo, porta e mesa na parede da direita
        var s = new Montar.Sala
        {
            nome = "salinha", n = 7, altura = 118, laje = 10, espessura = 0.22,
            contorno = C("0E0807"), capaCor = C("737686"), capaLuz = C("D1CBCE"), paredeEsq = C("2A2D42"), paredeDir = C("5E6574"),
            pontaEsq = C("4A5064"), pontaDir = C("3A3E55"), lajeEsq = C("A9716F"), lajeDir = C("642D38"), bordaLuz = C("D1CBCE"),
            piso = "tabua", pisoCores = Cs("DDA87C", "D8A276", "8C5545", "98634D", "642D38", "B07A58"),
            ofx = 199, ofy = 143, oex = 47, oey = 219, odx = 351, ody = 219, on = 6,
            fundoOrig = Cs("2A2D42", "5E6574", "737686", "DDA87C", "98634D", "8C5545", "D1CBCE", "B8B7B6", "B4B2B1"),
        };
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.45, u1 = 3.75, altura = 98, moldura = C("582D1F"), painel = C("B8B7B6"), luz = C("D4D3D2"), macaneta = C("3A2A20") });
        s.moveis.Add(new Montar.Movel { nome = "janela", x = 210, y = 66, w = 40, h = 76, parede = 'D', u = 0.5, escala = 1.5 });
        s.moveis.Add(new Montar.Movel { nome = "planta", x = 172, y = 82, w = 52, h = 84, parede = 'C', escala = 1.5 });
        s.moveis.Add(new Montar.Movel { nome = "mesa", x = 264, y = 132, w = 94, h = 100, parede = 'D', u = 2.8, escala = 1.5 });
        l.Add(s);

        // Sala de racks (Analista e DevOps): 10 casas, piso de ladrilho, quadro e mesa na frente da parede da esquerda
        s = new Montar.Sala
        {
            nome = "racks", n = 10, altura = 132, laje = 12, espessura = 0.16,
            contorno = C("090708"), capaCor = C("A2A0A0"), capaLuz = C("D1D0D0"), paredeEsq = C("727171"), paredeDir = C("4E4B4B"),
            pontaEsq = C("4E4B4B"), pontaDir = C("727171"), lajeEsq = C("141313"), lajeDir = C("2C292B"), bordaLuz = C("D1D0D0"),
            piso = "ladrilho", pisoCores = Cs("A2A0A0", "9F9E9E", "9C9B9B", "3F3C3D", "BDBCBC", "B2B0B0"),
            ofx = 200, ofy = 122, oex = 42, oey = 200, odx = 357, ody = 200, on = 8,
            fundoOrig = Cs("727171", "4E4B4B", "A2A0A0", "9C9B9B", "B2B0B0", "BDBCBC", "2C292B", "3F3C3D", "D1D0D0"),
        };
        s.portas.Add(new Montar.Porta { parede = 'E', u0 = 3.0, u1 = 4.3, altura = 98, moldura = C("5C5B5B"), painel = C("3B3A3A"), luz = C("4C4A4A"), macaneta = C("D8D8D8") });
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.0, u1 = 3.3, altura = 98, moldura = C("3E3C3C"), painel = C("2C2B2B"), luz = C("383737"), macaneta = C("D8D8D8") });
        s.moveis.Add(new Montar.Movel { nome = "quadro", x = 48, y = 106, w = 50, h = 64, parede = 'E', u = 9.3, escala = 1.5 });
        s.moveis.Add(new Montar.Movel { nome = "extintor", x = 119, y = 104, w = 18, h = 36, parede = 'E', u = 6.5, escala = 1.5 });
        s.moveis.Add(new Montar.Movel { nome = "mesa", x = 50, y = 135, w = 82, h = 78, parede = 'E', u = 9.2, escala = 1.5 });
        l.Add(s);

        // Data center (SRE em diante): 12 casas, piso técnico com a faixa perfurada, NOC na frente da parede da esquerda
        s = new Montar.Sala
        {
            nome = "dc", n = 12, altura = 132, laje = 8, espessura = 0.12,
            contorno = C("040404"), capaCor = C("51515A"), capaLuz = C("6A6D76"), paredeEsq = C("37374B"), paredeDir = C("171427"),
            pontaEsq = C("1D172B"), pontaDir = C("37374B"), lajeEsq = C("14101E"), lajeDir = C("2A2838"), bordaLuz = C("6A707D"),
            piso = "dc", pisoCores = Cs("5A5E6B", "3F4045", "09080D", "43444E", "3E3F49", "787A83"),
            faixa = C("F79C42"), faixaSombra = C("A25B11"), faixaAltura = 0.55,
            ofx = 200, ofy = 105, oex = 30, oey = 190, odx = 371, ody = 190, on = 10,
            fundoOrig = Cs("43444E", "404050", "434656", "282830", "3F4045", "494B52", "565863", "3B3D4B", "646B79", "3F4045", "6A707D", "616571", "3A3B43", "6A6D76", "494C59", "71757F"),
        };
        s.portas.Add(new Montar.Porta { parede = 'E', u0 = 3.2, u1 = 4.7, altura = 100, dupla = true, moldura = C("787A83"), painel = C("1C1A2A"), luz = C("3A3F55"), macaneta = C("9AA0B0") });
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.5, u1 = 4.0, altura = 100, dupla = true, moldura = C("5A5C66"), painel = C("120F1E"), luz = C("2C2F45"), macaneta = C("9AA0B0") });
        s.moveis.Add(new Montar.Movel { nome = "noc", x = 34, y = 128, w = 58, h = 82, parede = 'E', u = 11.3, escala = 2 });
        l.Add(s);
        return l;
    }
}
