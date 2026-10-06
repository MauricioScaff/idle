# Publicar no itch.io: passo a passo

Tudo o que você precisa está nesta pasta:

| Arquivo | Para quê |
|---|---|
| `IdleDataCenter-windows.zip` | o jogo (build atual, 35 MB) |
| `capa_630x500.png` | a capa (Cover image) |
| `imagens/` | as imagens da página (Screenshots), em ordem |
| `pagina.md` | os textos para colar em cada campo |
| `montar_capa.ps1` | refaz a capa se a arte mudar |

## 1. Criar a página (rascunho)

1. Entre em https://itch.io e faça login (crie a conta se ainda não tiver).
2. Clique na setinha ao lado do seu nome → **Dashboard** → **Create new project**.
3. Preencha os campos com o que está em `pagina.md` (título, URL, frase curta, classificação, tipo).
4. **Pricing:** escolha **Donate** (pague quanto quiser), com doação sugerida de US$ 2.

## 2. Subir o jogo

1. Em **Uploads**, clique em **Upload files** e escolha `IdleDataCenter-windows.zip`.
2. Depois de subir, marque o ícone do **Windows** ao lado do arquivo.

## 3. Página

1. Cole a **Description** de `pagina.md`.
2. **Genre:** Simulation. **Tags:** as dez de `pagina.md`.
3. **AI generation disclosure:** marque que usa IA generativa em **Graphics** (a arte foi feita com o PixelLab).
4. **Cover image:** `capa_630x500.png`.
5. **Screenshots:** as imagens de `imagens/`, na ordem dos números.

## 4. Salvar como rascunho e revisar

1. Em **Visibility & access**, deixe **Draft** e clique em **Save & view page**.
2. Abra a página, confira tudo e baixe o jogo por ela para testar.
3. Quando estiver tudo certo: volte em **Edit game**, troque para **Public** e salve.

## Atualizar depois

Para uma versão nova, é só subir o zip novo em **Uploads** (e apagar o antigo). Se preferir que eu envie as atualizações pelo
comando, dá para configurar o **butler** (a ferramenta oficial do itch): você faz o login nele uma vez e eu envio as versões.
