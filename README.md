# Idle Data Center

## Versão isométrica em janela completa

A cena **DataCenter** usa a arte conceitual de `Design/Concepts/` como referência. Tem setores NOC, Compute, Storage, Network, Power, Cooling e Research, menus interativos, recursos atualizados pela simulação, objetivos com recompensas e pesquisa de automações.

- **Jogar:** abra `Builds/Isometrico/IdleDevOps.exe`.
- **No Unity:** menu **Idle Data Center → Isometrico → Abrir cena**, depois Play.
- **Gerar novamente:** menu **Idle Data Center → Isometrico → Gerar build de Windows**.
- **Controles:** clique nos setores ou no menu lateral; clique em um rack para atender um trabalho; **BUILD** abre a construção; **DEPLOY** entrega um deploy quando há um host de containers, ou faz rollback de um deploy quebrado. **B** abre os racks, **R** abre a pesquisa, **Esc** fecha o painel e **F12** salva uma captura na pasta `Screenshots` dos dados do jogo.
- **Incidentes:** o NOC e o botão de alertas restauram os serviços. A pesquisa **Watchdog + drone** ativa reparos automáticos em cinco segundos e coloca o drone em movimento.
- **Campanha própria:** começa como Engenheiro DevOps, com dois racks, storage e R$ 350 mil. Usa a economia e as oito automações existentes. A expansão de compute chega a cinco racks nesta versão; as metas de quatro e cinco racks pagam R$ 50 mil e R$ 100 mil uma única vez.
- **Progresso:** save automático a cada 20 segundos e após compras e pesquisas; ganhos offline seguem as regras existentes. O arquivo é `save-isometrico.json`, com backup `.bak`, na pasta de dados da aplicação. O executável usa `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Idle DevOps - Data Center\`.
- **Arte:** cenário isométrico ilustrado, racks construídos em tempo de execução, personagens animados, LEDs e drone. Os equipamentos de apoio do cenário são ilustrados; os racks compráveis e os hosts de containers são objetos desenhados conforme o estado da campanha.

O modo faixa e sua cena continuam disponíveis pelos menus originais. Os builds isométricos restauram as configurações de janela do projeto após compilar.

### Verificação da versão isométrica

Os testes `IsometricoTestes` verificam compras, limite de racks, recompensas únicas, pesquisa, reparos e persistência. O build aceita `-iso-test` para uma sessão descartável, `-capture=C:\caminho\tela.png` para capturar a tela após iniciar e `-quit-after-capture` para encerrar após a captura. A sessão descartável não grava o progresso da campanha.

## Versão original: faixa de desktop

Jogo idle para Windows que vive numa faixa acima da barra de tarefas. Você começa como técnico de TI com um servidor velho num armário e sobe na carreira até comandar datacenters pelo mundo.

- **Engine:** Unity 6.6 (6000.6.3f1), só Windows
- **Arte:** pixel art fofa. Personagens, equipamentos e a cena do painel vêm do PixelLab (`Assets/_IdleDataCenter/Resources/Arte/`, originais em `Arte/PixelLab/`); ícones e efeitos pequenos são desenhados em código (`Scripts/Visual/Arte.cs`)

## O que já dá pra jogar

- **Faixa:** o cômodo do técnico (armário, depois salinha) e a loja de melhorias, sempre acima da barra de tarefas.
- **Painel:** clique no ícone ▲ da faixa (ou em "Meta x/3"). Abas Visão geral, Melhorias, Carreira e Ajustes. Fecha com Esc, com o "x" ou clicando fora.
- **Incidentes:** servidores travam de vez em quando. O técnico corre e conserta em 30 s, ou você clica no servidor e reinicia na hora. Contratando o **estagiário** (loja do Técnico), o conserto cai para 15 s e ele corre junto.
- **Carreira:** Técnico de TI → Sysadmin → Analista de Infra → Engenheiro DevOps. Cada promoção pede metas do cargo (servidores, faturamento, incidentes, backups, automações).
- **Sysadmin:** rack 42U com servidores 1U, energia (no-break) e temperatura (ar-condicionado). Para subir: encher o rack, faturar R$ 600K e resolver 60 incidentes.
- **Analista de Infra:** sala de racks com energia e ar de precisão próprios. Racks cheios (8 servidores cada), storage RAID (receita +25% por nível), backup em fita e link de fibra. A **banda** vira o novo limite: com o link saturado a receita cai. Discos do storage queimam: com backup, o técnico restaura os dados; sem backup, os clientes são reembolsados. Clique no storage para trocar o disco na hora. Para subir: restaurar um backup, ter 3 automações ativas e faturar R$ 5M.
- **Engenheiro DevOps:** a sala escurece e vira sala virtualizada. Hypervisor (VMs: servidores +40% por nível), hosts de containers (apps, +250/s cada), servidor de CI (apps +50%, com a esteira de CI/CD levando versões novas) e link 10G. Deploys às vezes quebram e derrubam os apps: o técnico faz rollback no terminal, ou você clica nos containers ou no CI.
- **Automação** (aba do painel, a partir do Analista): o técnico escreve um script por vez, que leva alguns minutos de tempo real (e continua com o jogo fechado). No Analista: watchdog (reinicia servidor em 5 s), troca de disco automática, monitoramento (metade dos incidentes), cron de faturamento (offline a 75%) e plantão 24h (offline a 100%, até 24 h). No DevOps: pipeline com testes (4x menos deploys quebrados), rollback automático e infra como código (melhorias 15% mais baratas).
- **Esconder:** a setinha "v" na faixa, ou **Ctrl+Alt+D** de qualquer lugar (o mesmo atalho traz de volta). Escondido, o jogo continua rendendo.
- Save automático e ganho offline (50% da receita, até 12 h). Ao abrir o jogo, o aviso "Enquanto você estava fora" mostra o tempo, quanto rendeu e o que o técnico consertou.
- **Sons:** moeda, compra, alerta de servidor travado, conserto e promoção, todos sintetizados em código. Volume baixo por padrão.
- **Ajustes** (aba do painel): som, volume, zumbido de ventoinha ao fundo, em qual monitor a faixa fica, lado da tela (esquerda/direita) e esconder ou não durante tela cheia. Ficam no PlayerPrefs, separados do save.

## Como rodar

- **No editor:** abra a cena `Assets/_IdleDataCenter/Scenes/Faixa` e aperte Play. No editor a janela não fica transparente.
- **Na faixa de verdade:** menu **Idle Data Center → Gerar build de Windows** e abra `Builds/IdleDataCenter.exe`.
- **Opções de teste** (linha de comando do build): `-painel=visao|melhorias|carreira|ajustes|automacao` abre o painel numa aba; `-incidente` trava o primeiro servidor; `-promover` promove (se as metas estiverem cumpridas); `-alternar-painel` abre e fecha o painel a cada 4 s (teste da janela).
- **Zerar o progresso:** apague `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Idle Data Center\save.json`.

## Estrutura

```
Assets/_IdleDataCenter/
  Scripts/Core/       ajustes do jogador e sons
  Scripts/Simulacao/  regras do jogo em C# puro: economia, incidentes, energia, carreira, save
  Scripts/Janela/     janela transparente acima da barra de tarefas (Win32)
  Scripts/Visual/     pixel art, paleta, fonte de pixel e a "tela de desenho" do painel
  Scripts/Faixa/      cenário, técnico, servidores, loja, painel e cliques
  Testes/             testes da economia e do ritmo (Window > General > Test Runner)
  Editor/             menu "Idle Data Center" (configurar projeto, gerar build)
```

Balanceamento: todos os números ficam em `Scripts/Simulacao/Catalogo.cs`. O teste `RitmoTestes` simula um jogador ocioso e confere o ritmo: Sysadmin entre 10 e 90 min, Analista entre 2 e 6 h e DevOps de 2 a 8 h depois do Analista (hoje: 39 min, cerca de 4 h e cerca de 7 h 45 min; o DevOps inteiro leva mais 3 h 30 min).
