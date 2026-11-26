# Zombie Wave Arcade

Jogo de sobrevivência em terceira pessoa feito com **Unity 6.5 (6000.5.6f1)** e **Universal Render Pipeline (URP)**. Enfrente ondas de zumbis na cidade, recolha itens e sobreviva à Skull Wave com a ajuda do seu Doberman.

## Como jogar

1. Instale a versão **6000.5.6f1** pelo Unity Hub.
2. Adicione ao Hub a pasta do projeto que contém `Assets/`, `Packages/` e `ProjectSettings/`.
3. Abra o projeto e aguarde a importação dos assets e pacotes.
4. Abra a cena [MenuInicial](Assets/Scenes/MenuInicial.Unity) e pressione **Play**. Para entrar direto na partida, abra a cena [Prototype Map](<Assets/AngeloMaN87/Prototype Map/Scenes/Prototype Map.unity>).

## Controles

| Entrada | Ação |
| --- | --- |
| WASD ou setas | Mover o personagem |
| Shift esquerdo ou direito | Correr enquanto se move |
| Mouse | Girar a câmera e mirar |
| Clique esquerdo | Sacar a pistola e disparar |
| Segurar o botão esquerdo com o AK74 | Disparar em rajada contínua |
| Esc | Liberar o cursor |

## Ondas, radar e itens

- **Ondas.** Elimine os zumbis vivos para avançar. Os pontos de surgimento são liberados conforme a partida progride. O limite global é de **250 zumbis vivos** entre as ondas normais e a Skull Wave.
- **HUD e radar.** O HUD mostra vida, onda, abates, zumbis vivos e tempo de partida. O radar no canto inferior direito detecta zumbis em até **30 m**. Itens fora desse alcance aparecem na borda na direção em que estão. O HUD informa a distância do item mais próximo.
- **Drops.** Cada eliminação tem **25%** de chance de gerar um item. Entre os drops, **75%** são caveiras e **25%** são corações. O coração aparece em rosa no radar e restaura a vida ao máximo. A caveira aparece em amarelo e ativa ou prolonga a Skull Wave.
- **Skull Wave.** Cada caveira soma **120 segundos** ao evento e aumenta em **50** a meta de zumbis vivos. Os reforços entram conforme houver espaço no limite global. Durante o evento, o AK74 dispara **10 vezes por segundo** e causa **25 de dano por bala**, assim como a pistola. A pistola volta quando o tempo acumulado acaba.
- **Game Over.** Ao perder toda a vida, o jogador pode iniciar outra partida ou voltar ao menu.

## Estrutura do projeto

| Pasta | Conteúdo |
| --- | --- |
| `Assets/Script/` | Scripts do jogador, dos inimigos, das ondas, dos itens e da interface |
| `Assets/Prefab/` e `Assets/Animators/` | Prefabs e controladores de animação |
| `Assets/Audio/` | Efeitos sonoros e créditos |
| `Packages/` | Manifesto e lockfile das dependências |
| `ProjectSettings/` | Versão do Unity, configurações e cenas da build |

A raiz do repositório é a própria pasta do projeto Unity. Versione `Assets/` com os arquivos `.meta`, `Packages/` e `ProjectSettings/`. O `.gitignore` exclui caches locais, builds e arquivos gerados pela IDE.

## Créditos e licenças

O projeto usa assets de Apocalypse Free e Cartoon City Free (ithappy), Prototype Map (AngeloMaN87), FREE Shirtless Zombie (NewPunch), BTM Items Gems, Low Poly Weapons VOL.1 e animações do Mixamo (Adobe). Consulte os termos de cada fornecedor antes de publicar os arquivos-fonte dos assets. [Termos da Unity Asset Store](https://unity.com/legal/as-terms)

Os efeitos sonoros do Freesound usam a licença CC0. Os autores e as fontes estão nos [créditos dos áudios](Assets/Audio/Creditos.txt).
