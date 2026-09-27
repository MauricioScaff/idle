# Data center isométrico

## Como jogar

Abra `Builds/Isometrico/IdleDevOps.exe`. No Unity, use **Idle Data Center → Isometrico → Abrir cena**.

A campanha começa com R$ 350 mil, um rack 42U, um rack de compute, storage e infraestrutura de apoio. A primeira compra de compute custa R$ 180 mil. A pesquisa Watchdog custa R$ 150 mil e leva três minutos; ela ativa o drone e reduz o tempo de reparo automático para cinco segundos.

- Clique em um setor ou no menu para consultar preços, efeitos, níveis e pré-requisitos.
- O marcador BUILD RACK compra diretamente o próximo rack; o botão BUILD abre a loja.
- Racks novos aparecem no piso de compute. Hosts de containers aparecem no espaço de expansão.
- NOC e o botão de alertas resolvem incidentes. DEPLOY faz rollback quando necessário ou entrega um trabalho com bônus quando há containers.
- B abre racks, R abre pesquisas, Esc fecha o painel e F12 captura a tela.

## Escopo desta versão

A interface é funcional e lê os valores da economia existente. Usa receitas, limites de energia, refrigeração e rede, incidentes, oito automações e ganhos offline. Há duas metas de expansão com recompensa única, até cinco racks de compute no total. Os equipamentos e a equipe nos setores de apoio são parte da ilustração; racks compráveis, hosts, LEDs, técnicos que caminham e drone são desenhados e animados pelo jogo.

O cenário e o rack vieram do gerador integrado de imagens; os prompts estão em `Concepts/isometric-game-assets-prompts.md`. Os técnicos, o drone, as placas e a interface usam pixel art produzida pelo código.

O save isométrico e seu backup ficam separados do modo faixa. A geração do build também restaura as configurações originais do projeto.

## Verificações realizadas

- Build Windows com Unity 6000.6.3f1: concluído sem erros.
- Suite EditMode: **49 testes aprovados**, incluindo seis testes novos para a campanha isométrica.
- Interface real: abrir a loja, comprar um rack, conferir alterações de saldo/produção/energia/metas, abrir pesquisas, iniciar Watchdog e verificar o progresso.
- Persistência: fechar e reabrir preservou o rack comprado e a pesquisa em andamento.
- Progresso offline: a pesquisa terminou durante a pausa, o drone foi ativado e a receita offline foi creditada ao reabrir.
- Captura da tela jogável: `Concepts/isometric-gameplay-v1.png`.

Relatórios locais: `Logs/isometric-build.log`, `Logs/isometric-tests.xml`. O modo descartável `-iso-test` não lê nem grava a campanha do jogador.
