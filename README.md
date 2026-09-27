# Idle Data Center

Jogo idle para Windows: um data center em vista isométrica numa janela normal, que encolhe para uma faixa discreta acima da barra de tarefas enquanto você trabalha. Você começa como técnico de TI com um servidor velho num armário e sobe na carreira até comandar datacenters pelo mundo.

- **Engine:** Unity 6.6 (6000.6.3f1), só Windows
- **Arte:** pixel art fofa. Personagens, equipamentos e a cena do painel vêm do PixelLab (`Assets/_IdleDataCenter/Resources/Arte/`, originais em `Arte/PixelLab/`); ícones e efeitos pequenos são desenhados em código (`Scripts/Visual/Arte.cs`)

## O que já dá pra jogar

- **Faixa:** o cômodo do técnico (armário, depois salinha) e a loja de melhorias, sempre acima da barra de tarefas.
- **Painel:** clique no ícone ▲ da faixa (ou em "Meta x/3"). Abas Visão geral, Melhorias, Carreira e Ajustes. Fecha com Esc, com o "x" ou clicando fora.
- **Incidentes:** servidores travam de vez em quando. O técnico corre e conserta em 30 s, ou você clica no servidor e reinicia na hora. Contratando o **estagiário** (loja do Técnico), o conserto cai para 15 s e ele corre junto.
- **Carreira:** Técnico de TI → Sysadmin → Analista de Infra → Engenheiro DevOps → SRE. Cada promoção pede metas do cargo (servidores, faturamento, incidentes, backups, automações).
- **Sysadmin:** rack 42U com servidores 1U, energia (no-break) e temperatura (ar-condicionado). Para subir: encher o rack, faturar R$ 600K e resolver 60 incidentes.
- **Analista de Infra:** sala de racks com energia e ar de precisão próprios. Racks cheios (8 servidores cada), storage RAID (receita +25% por nível), backup em fita e link de fibra. A **banda** vira o novo limite: com o link saturado a receita cai. Discos do storage queimam: com backup, o técnico restaura os dados; sem backup, os clientes são reembolsados. Clique no storage para trocar o disco na hora. Para subir: restaurar um backup, ter 3 automações ativas e faturar R$ 5M.
- **Engenheiro DevOps:** a sala escurece e vira sala virtualizada. Hypervisor (VMs: servidores +40% por nível), hosts de containers (apps, +250/s cada), servidor de CI (apps +50%, com a esteira de CI/CD levando versões novas) e link 10G. Deploys às vezes quebram e derrubam os apps: o técnico faz rollback no terminal, ou você clica nos containers ou no CI. Para subir: 7 automações ativas, 4 hosts de containers e faturar R$ 40M.
- **SRE:** data center pequeno com cluster Kubernetes (nós de +400/s), balanceador de carga (K8s +25% e mais 10 s para escalar), observabilidade (receita +15% por nível) e o telão do NOC com o tráfego ao vivo. **Picos de tráfego** (Black Friday, final da copa...) chegam a cada 15 a 25 min e duram 1 min: escale o cluster a tempo (botão "Escalar!" no HUD, clique nos nós ou no painel) e o pico rende o dobro; sem escalar, o SLA é violado e vem multa. Com o jogo fechado não há picos.
- **Automação** (aba do painel, a partir do Analista): o técnico escreve um script por vez, que leva alguns minutos de tempo real (e continua com o jogo fechado). No Analista: watchdog (reinicia servidor em 5 s), troca de disco automática, monitoramento (metade dos incidentes), cron de faturamento (offline a 75%) e plantão 24h (offline a 100%, até 24 h). No DevOps: pipeline com testes (4x menos deploys quebrados), rollback automático e infra como código (melhorias 15% mais baratas). No SRE: autoscaling (escala sozinho nos picos), chaos engineering (metade dos incidentes) e runbooks (todo conserto automático em 3 s). A aba mostra 8 por vez: primeiro as que dá para escrever, as ativas por último.
- **Modo gerente** (a tela principal): o jogo abre no data center em vista isométrica, numa janela normal do Windows (barra de título, barra de tarefas, redimensionável). O botão **Ir para a faixa** encolhe tudo para a faixa discreta; o ícone ◇ da faixa traz de volta. O jogo reabre no modo em que foi fechado. Mesmo dinheiro e mesmo save nos dois modos. A sala é desenhada em pixel art isométrica e **cresce a cada promoção** (armário, salinha, sala de racks, sala virtualizada, data center pequeno); tudo o que você compra aparece nela, e um contorno tracejado mostra a próxima expansão. Clique nos equipamentos (rende dinheiro ou conserta o que quebrou) e nas placas dos setores (Compute, Energia, Refrigeração, Storage, Rede, Laboratório, NOC). Dá para comprar por setor (inclusive o que ficou para trás nos cargos anteriores), escrever automações, resolver incidentes, escalar picos e ser promovido. A interface veio do protótipo isométrico feito no Codex (branch `isometrico`).
- **Café e chamados** (nos dois modos): clique na caneca (na mesa, ou em cima da torre na faixa) e a receita dobra por 30 s, com 3 min de recarga. A cada 3 a 6 min aparece um chamado urgente ("é sempre o DNS", "impressora não imprime"...): clique no papel em até 20 s e ganhe 60 s de receita. Nenhum dos dois vale com o jogo fechado.
- **Tutorial** no modo gerente: 6 dicas com uma seta apontando onde clicar (servidor, SSD, café, construir, metas, faixa). Quem já passou do começo não vê.
- **Esconder:** a setinha "v" na faixa, ou **Ctrl+Alt+D** de qualquer lugar (o mesmo atalho traz de volta). Escondido, o jogo continua rendendo.
- Save automático e ganho offline (50% da receita, até 12 h). Ao abrir o jogo, o aviso "Enquanto você estava fora" mostra o tempo, quanto rendeu e o que o técnico consertou.
- **Sons:** moeda, compra, alerta de servidor travado, conserto e promoção, todos sintetizados em código. Volume baixo por padrão.
- **Ajustes** (aba do painel): som, volume, zumbido de ventoinha ao fundo, em qual monitor a faixa fica, lado da tela (esquerda/direita) e esconder ou não durante tela cheia. Ficam no PlayerPrefs, separados do save.

## Como rodar

- **No editor:** abra a cena `Assets/_IdleDataCenter/Scenes/Faixa` e aperte Play. No editor a janela não fica transparente.
- **Na faixa de verdade:** menu **Idle Data Center → Gerar build de Windows** e abra `Builds/IdleDataCenter.exe`.
- **Opções de teste** (linha de comando do build): `-painel=visao|melhorias|carreira|ajustes|automacao` abre o painel numa aba; `-incidente` trava o primeiro servidor; `-promover` promove (se as metas estiverem cumpridas); `-gerente` e `-faixa` forçam o modo inicial (sem mudar a preferência do jogador); `-alternar-gerente` entra e sai do modo gerente a cada 5 s; `-alternar-painel` abre e fecha o painel a cada 4 s (teste da janela).
- **Zerar o progresso:** apague `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Idle Data Center\save.json`.

## Estrutura

```
Assets/_IdleDataCenter/
  Scripts/Core/       ajustes do jogador e sons
  Scripts/Simulacao/  regras do jogo em C# puro: economia, incidentes, energia, carreira, save
  Scripts/Janela/     janela transparente acima da barra de tarefas (Win32)
  Scripts/Visual/     pixel art, paleta, fonte de pixel e a "tela de desenho" do painel
  Scripts/Faixa/      cenário, técnico, servidores, loja, painel e cliques
  Scripts/Gerente/    modo gerente: sala isométrica desenhada em código (SalaIso, IsoDesenho) e a interface
  Testes/             testes da economia e do ritmo (Window > General > Test Runner)
  Editor/             menu "Idle Data Center" (configurar projeto, gerar build)
```

Balanceamento: todos os números ficam em `Scripts/Simulacao/Catalogo.cs`. O teste `RitmoTestes` simula um jogador ocioso e confere o ritmo: Sysadmin entre 10 e 90 min, Analista entre 2 e 6 h e DevOps de 2 a 8 h depois do Analista e SRE de 3 a 12 h depois do DevOps (hoje: 39 min, cerca de 4 h, 7 h 45 min e 12 h 15 min; o SRE inteiro leva mais 5 h 20 min).
