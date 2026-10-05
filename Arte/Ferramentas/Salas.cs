using System.Collections.Generic;

// As salas montadas na escala da pessoa (ver Montar.cs). Cores medidas nas ilustrações de Arte/PixelLab/salas_base.
// Móveis: recorte numa ilustração (x, y, largura, altura), parede ('E', 'D' ou 'C' = canto do fundo), posição na
// parede nova (casa do pé, embaixo da borda esquerda do recorte) e escala.
public static class Salas
{
    static int C(string hex) { return unchecked((int)(0xFF000000 | System.Convert.ToUInt32(hex, 16))); }
    static int[] Cs(params string[] h) { var r = new int[h.Length]; for (int i = 0; i < h.Length; i++) r[i] = C(h[i]); return r; }

    /// As ilustrações de onde saem os móveis: losango do piso e as cores do fundo.
    public static List<Montar.Base> Bases()
    {
        return new List<Montar.Base>
        {
            new Montar.Base { nome = "salinha", ofx = 199, ofy = 143, oex = 47, oey = 219, odx = 351, ody = 219,
                fundo = Cs("2A2D42", "5E6574", "737686", "DDA87C", "98634D", "8C5545", "D1CBCE", "B8B7B6", "B4B2B1") },
            new Montar.Base { nome = "racks", ofx = 200, ofy = 122, oex = 42, oey = 200, odx = 357, ody = 200,
                fundo = Cs("727171", "4E4B4B", "A2A0A0", "9C9B9B", "B2B0B0", "BDBCBC", "2C292B", "3F3C3D", "D1D0D0") },
            new Montar.Base { nome = "dc", ofx = 200, ofy = 105, oex = 30, oey = 190, odx = 371, ody = 190,
                fundo = Cs("43444E", "404050", "434656", "282830", "3F4045", "494B52", "565863", "3B3D4B", "646B79", "3F4045", "6A707D", "616571", "3A3B43", "6A6D76", "494C59", "71757F") },
        };
    }

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
        };
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.45, u1 = 3.75, altura = 98, moldura = C("582D1F"), painel = C("B8B7B6"), luz = C("D4D3D2"), macaneta = C("3A2A20") });
        s.moveis.Add(new Montar.Movel { nome = "janela", x = 210, y = 66, w = 40, h = 76, parede = 'D', u = 0.5, escala = 1.5, arquivo = "moveis_pro/janela.png" });
        s.moveis.Add(new Montar.Movel { nome = "planta", x = 172, y = 82, w = 52, h = 84, parede = 'C', escala = 1.5, arquivo = "moveis_pro/planta.png" });
        s.moveis.Add(new Montar.Movel { nome = "mesa", x = 264, y = 132, w = 94, h = 100, parede = 'D', u = 2.8, escala = 1.5, arquivo = "moveis_pro/mesa_salinha.png", pedacoMinimo = 0.15 });
        l.Add(s);

        // Sala de racks (Analista e DevOps): 10 casas, piso de ladrilho, quadro e mesa na frente da parede da esquerda.
        // Porta da esquerda: Rede e segurança; da direita: Dados e backup.
        s = new Montar.Sala
        {
            nome = "racks", n = 10, altura = 132, laje = 12, espessura = 0.16,
            contorno = C("090708"), capaCor = C("A2A0A0"), capaLuz = C("D1D0D0"), paredeEsq = C("727171"), paredeDir = C("4E4B4B"),
            pontaEsq = C("4E4B4B"), pontaDir = C("727171"), lajeEsq = C("141313"), lajeDir = C("2C292B"), bordaLuz = C("D1D0D0"),
            piso = "ladrilho", pisoCores = Cs("A2A0A0", "9F9E9E", "9C9B9B", "3F3C3D", "BDBCBC", "B2B0B0"),
        };
        s.portas.Add(new Montar.Porta { parede = 'E', u0 = 3.0, u1 = 4.3, altura = 98, moldura = C("5C5B5B"), painel = C("3B3A3A"), luz = C("4C4A4A"), macaneta = C("D8D8D8") });
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.0, u1 = 3.3, altura = 98, moldura = C("3E3C3C"), painel = C("2C2B2B"), luz = C("383737"), macaneta = C("D8D8D8") });
        s.moveis.Add(new Montar.Movel { nome = "quadro", x = 48, y = 106, w = 50, h = 64, parede = 'E', u = 9.3, escala = 1.5, arquivo = "moveis_pro/quadro.png", soMaior = true });
        s.moveis.Add(new Montar.Movel { nome = "extintor", x = 119, y = 104, w = 18, h = 36, parede = 'E', u = 6.5, escala = 1.5, arquivo = "moveis_pro/extintor.png" });
        s.moveis.Add(new Montar.Movel { nome = "mesa", x = 50, y = 135, w = 82, h = 78, parede = 'E', u = 9.2, escala = 1.5, arquivo = "moveis_pro/mesa_racks.png", pedacoMinimo = 0.15 });
        l.Add(s);

        // DevOps (sala virtualizada): a mesma planta da sala de racks (o jogo encaixa igual), com cara de time de
        // produto: paredes azul-ardósia, carpete, faixa roxa, quadro kanban, mesa com dois monitores e o canto do café
        s = new Montar.Sala
        {
            nome = "devops", n = 10, altura = 132, laje = 12, espessura = 0.16,
            contorno = C("0B0C16"), capaCor = C("6E7393"), capaLuz = C("A9AED0"), paredeEsq = C("4A4F6B"), paredeDir = C("383C55"),
            pontaEsq = C("383C55"), pontaDir = C("4A4F6B"), lajeEsq = C("15172A"), lajeDir = C("24273E"), bordaLuz = C("8F95B8"),
            piso = "ladrilho", pisoCores = Cs("5D6178", "595D73", "555970", "2E3142", "6E7290", "676B88"),
            faixa = C("B37CFF"), faixaSombra = C("7A4FC0"), faixaAltura = 0.55,
        };
        s.portas.Add(new Montar.Porta { parede = 'E', u0 = 3.0, u1 = 4.3, altura = 98, moldura = C("6E7393"), painel = C("2E3142"), luz = C("3B3F57"), macaneta = C("D8D8D8") });
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.0, u1 = 3.3, altura = 98, moldura = C("4A4F6B"), painel = C("24273E"), luz = C("2E3142"), macaneta = C("D8D8D8") });
        s.moveis.Add(new Montar.Movel { nome = "kanban", origem = "racks", x = 48, y = 106, w = 50, h = 64, parede = 'E', u = 9.3, escala = 1.5, arquivo = "moveis_pro/kanban.png", soMaior = true });
        s.moveis.Add(new Montar.Movel { nome = "mesa", origem = "racks", x = 50, y = 135, w = 82, h = 78, parede = 'E', u = 9.2, escala = 1.5, arquivo = "moveis_pro/mesa_devops.png", pedacoMinimo = 0.15 });
        s.moveis.Add(new Montar.Movel { nome = "cafe", arquivo = "moveis_pro/cafe.png", gx = 0.55, gy = 5.9 });
        l.Add(s);

        // Data center (SRE em diante): 12 casas, piso técnico com a faixa perfurada, NOC na frente da parede da esquerda
        s = new Montar.Sala
        {
            nome = "dc", n = 12, altura = 132, laje = 8, espessura = 0.12,
            contorno = C("040404"), capaCor = C("51515A"), capaLuz = C("6A6D76"), paredeEsq = C("37374B"), paredeDir = C("171427"),
            pontaEsq = C("1D172B"), pontaDir = C("37374B"), lajeEsq = C("14101E"), lajeDir = C("2A2838"), bordaLuz = C("6A707D"),
            piso = "dc", pisoCores = Cs("5A5E6B", "3F4045", "09080D", "43444E", "3E3F49", "787A83"),
            faixa = C("F79C42"), faixaSombra = C("A25B11"), faixaAltura = 0.55,
        };
        s.portas.Add(new Montar.Porta { parede = 'E', u0 = 3.2, u1 = 4.7, altura = 100, dupla = true, moldura = C("787A83"), painel = C("1C1A2A"), luz = C("3A3F55"), macaneta = C("9AA0B0") });
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 2.5, u1 = 4.0, altura = 100, dupla = true, moldura = C("5A5C66"), painel = C("120F1E"), luz = C("2C2F45"), macaneta = C("9AA0B0") });
        s.moveis.Add(new Montar.Movel { nome = "noc", x = 34, y = 128, w = 58, h = 82, parede = 'E', u = 11.3, escala = 2, arquivo = "moveis_pro/noc.png" });
        l.Add(s);

        // Dados e backup (atrás da porta da direita): sala fria e clara, storage na parede da esquerda, fitas na da direita.
        // A porta de volta fica na frente da parede da esquerda.
        s = new Montar.Sala
        {
            nome = "dados", n = 7, altura = 124, laje = 10, espessura = 0.16,
            contorno = C("0B0D14"), capaCor = C("8A93A6"), capaLuz = C("C8D0DE"), paredeEsq = C("5A6478"), paredeDir = C("3E4658"),
            pontaEsq = C("3E4658"), pontaDir = C("5A6478"), lajeEsq = C("1E2230"), lajeDir = C("2E3344"), bordaLuz = C("C8D0DE"),
            piso = "ladrilho", pisoCores = Cs("A9B3C2", "A4AEBD", "9DA7B6", "3C4350", "C6CFDC", "B8C1CF"),
            faixa = C("8FD3FF"), faixaSombra = C("4C7DA6"), faixaAltura = 0.86,
        };
        s.portas.Add(new Montar.Porta { parede = 'E', u0 = 5.2, u1 = 6.5, altura = 98, moldura = C("8A93A6"), painel = C("2E3344"), luz = C("3E4658"), macaneta = C("D8D8D8") });
        s.moveis.Add(new Montar.Movel { nome = "extintor", origem = "racks", x = 119, y = 104, w = 18, h = 36, parede = 'E', u = 4.6, escala = 1.5, arquivo = "moveis_pro/extintor.png" });
        l.Add(s);

        // Rede e segurança (atrás da porta da esquerda): sala escura com faixa verde, os racks de rede e de segurança e a
        // mesa do SOC. A porta de volta fica na frente da parede da direita.
        s = new Montar.Sala
        {
            nome = "rede", n = 7, altura = 124, laje = 8, espessura = 0.12,
            contorno = C("05060A"), capaCor = C("4A5266"), capaLuz = C("7A849A"), paredeEsq = C("2B3140"), paredeDir = C("1C2130"),
            pontaEsq = C("1C2130"), pontaDir = C("2B3140"), lajeEsq = C("10141C"), lajeDir = C("222838"), bordaLuz = C("5A6A70"),
            piso = "dc", pisoCores = Cs("56666A", "3A4446", "08100C", "3B4446", "343C3E", "5FA88A"),
            faixa = C("4FD18B"), faixaSombra = C("2E8A5A"), faixaAltura = 0.55,
        };
        s.portas.Add(new Montar.Porta { parede = 'D', u0 = 5.2, u1 = 6.5, altura = 98, moldura = C("4A5266"), painel = C("161A26"), luz = C("2B3140"), macaneta = C("9AA0B0") });
        s.moveis.Add(new Montar.Movel { nome = "soc", origem = "dc", x = 34, y = 128, w = 58, h = 82, parede = 'E', u = 6.6, escala = 2, arquivo = "moveis_pro/noc.png",
            trocas = Cs("37374B", "2B3140", "353545", "293040", "171427", "1C2130", "F79C42", "2B3140", "A25B11", "1C2130") });
        l.Add(s);
        return l;
    }
}
