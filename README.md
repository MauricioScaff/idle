# Idle Data Center

Jogo idle para Windows que vive numa faixa acima da barra de tarefas. Você começa como técnico de TI com um servidor velho num armário e sobe na carreira até comandar datacenters pelo mundo.

- **Engine:** Unity 6.6 (6000.6.3f1), só Windows
- **Arte:** pixel art fofa, gerada por código enquanto não há arte final (`Scripts/Visual/Arte.cs`)

## Como rodar

- **No editor:** abra a cena `Assets/_IdleDataCenter/Scenes/Faixa` e aperte Play. No editor a janela não fica transparente.
- **Na faixa de verdade:** menu **Idle Data Center → Gerar build de Windows** e abra `Builds/IdleDataCenter.exe`.

## Estrutura

```
Assets/_IdleDataCenter/
  Scripts/Janela/   janela transparente acima da barra de tarefas (Win32)
  Scripts/Visual/   pixel art, paleta e fonte de pixel
  Scripts/Faixa/    cenário, técnico, servidor e cliques
  Editor/           menu "Idle Data Center" (configurar projeto, gerar build)
```
