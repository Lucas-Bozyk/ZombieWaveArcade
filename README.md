# ZombieWave

Protótipo de sobrevivência em terceira pessoa no Unity. Enfrente ondas de zumbis e elimine os inimigos vivos para liberar a próxima onda. Quando a vida do jogador chega a zero, o jogo abre a tela de Game Over.

## Como abrir

1. Instale o Unity **6000.5.6f1** pelo Unity Hub.
2. No Unity Hub, adicione a pasta que contém `Assets`, `Packages` e `ProjectSettings`.
3. Aguarde a importação dos assets e pacotes.
4. Abra `Assets/Scenes/MenuInicial.Unity` e clique em **Play**. Para testar a partida diretamente, abra a cena `Prototype Map`.

## Controles

| Entrada | Ação |
| --- | --- |
| WASD ou setas | Mover o personagem |
| Mouse | Girar a câmera |
| Botão esquerdo | Sacar a pistola e disparar |
| Esc | Liberar o cursor |

## Estrutura

- `Assets/Script` contém os scripts do jogador, dos inimigos, das ondas e dos menus.
- `Assets/Prefab` e `Assets/Animators` contêm os prefabs e os controladores de animação.
- `Packages/manifest.json` e `Packages/packages-lock.json` registram as dependências do Unity.

## Assets de terceiros

O projeto inclui assets de Apocalypse Free e Cartoon City Free (ithappy), Prototype Map (AngeloMaN87), FREE Shirtless Zombie (NewPunch) e animações do Mixamo (Adobe). Cada asset mantém os termos do respectivo fornecedor. Antes de disponibilizar publicamente os arquivos-fonte, confira quais podem ser redistribuídos nesse formato. [Termos da Unity Asset Store](https://unity.com/legal/as-terms)
