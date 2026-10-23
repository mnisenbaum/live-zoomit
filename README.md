# LiveZoomIt

Ferramenta de anotação de tela para Windows, inspirada no ZoomIt (Sysinternals / PowerToys), com uma diferença principal: **a tela nunca congela**. Você desenha por cima da tela viva — vídeos, chamadas, navegadores e terminais continuam rodando por baixo dos traços.

- Um único `LiveZoomIt.exe` portátil, sem instalação e sem precisar do .NET instalado.
- Roda em segundo plano, sem janela, sem ícone na barra de tarefas e sem ícone na bandeja.
- Cobre todos os monitores.

---

## Início rápido

1. Execute `LiveZoomIt.exe`. **Nada aparece** — é esperado. Ele fica aguardando os atalhos.
2. Aperte **Ctrl+2** para entrar no modo desenho e arraste com o mouse para riscar a tela.
3. Aperte **Esc** (ou **Ctrl+2** de novo) para voltar a usar o computador normalmente.
4. Aperte **Ctrl+Shift+Q** para fechar o programa.

> Para saber se ele está rodando, procure `LiveZoomIt` no Gerenciador de Tarefas.

---

## Os dois modos

| Modo | Como entrar | O que acontece |
|---|---|---|
| **Transparente** (padrão) | Ao abrir, ou com **Esc** / **Ctrl+2** no modo desenho | Os cliques e o teclado passam direto para os programas. Os traços já feitos **continuam visíveis** por cima. |
| **Desenho** | **Ctrl+2** | O mouse desenha. Uma barra no topo do monitor principal mostra a ferramenta, a cor, a espessura e os atalhos. |

Ao sair do modo desenho, o foco volta para o programa que você estava usando antes. O quadro branco/negro some, mas os traços permanecem até você apagá-los (**E**).

---

## Atalhos globais

Funcionam em qualquer programa, a qualquer momento.

| Atalho | Ação |
|---|---|
| **Ctrl+2** | Liga / desliga o modo desenho |
| **Ctrl+1** | Liga / desliga a lupa |
| **Ctrl+Shift+Q** | Fecha o LiveZoomIt |

> Enquanto o LiveZoomIt estiver aberto, **Ctrl+1** e **Ctrl+2** ficam reservados para ele em todo o sistema (por exemplo, deixam de trocar de aba no navegador).

---

## Modo desenho

### Desenhar e formas

A forma é escolhida pela tecla que está **segurada no momento em que você aperta o botão do mouse**.

| Gesto | Resultado |
|---|---|
| Arrastar | Traço livre |
| **Ctrl** + arrastar | Retângulo |
| **Shift** + arrastar | Linha reta |
| **Alt** + arrastar | Elipse |
| **Ctrl+Shift** + arrastar | Seta (a ponta fica onde você solta) |

- A **linha** e a **seta** se encaixam automaticamente na horizontal, na vertical ou a 45° quando você chega a menos de 4° desses ângulos.
- As formas usam a cor, a espessura e o modo (caneta ou marca-texto) atuais.
- Caneta de tablet/tela touch também funciona, inclusive com as teclas de forma.

### Cores, caneta e marca-texto

| Tecla | Caneta | Com **Shift** (marca-texto) |
|---|---|---|
| **R** | Vermelho | Marca-texto vermelho |
| **G** | Verde | Marca-texto verde |
| **B** | Azul | Marca-texto azul |
| **Y** | Amarelo | Marca-texto amarelo |
| **O** | Laranja | Marca-texto laranja |
| **P** | Rosa | Marca-texto rosa |

- O **marca-texto** é largo e semitransparente, para destacar sem esconder o que está por baixo. Ele tem 3× a espessura da caneta (mínimo de 12 px).
- Apertar uma cor **sem** Shift volta para a caneta normal.
- A cor inicial é vermelho.

### Espessura

| Atalho | Ação |
|---|---|
| **Ctrl + roda do mouse** | Aumenta / diminui |
| **Ctrl+↑** / **Ctrl+↓** | Aumenta / diminui |

Espessuras disponíveis: 1, 2, 3, 4, 6, 8, 10, 13, 16, 20, 25, 32 e 40 px (padrão: 4 px). A barra no topo mostra o valor atual. Com a ferramenta de texto ativa, os mesmos atalhos mudam o tamanho da fonte.

### Texto

1. Aperte **T** para ativar a ferramenta de texto (o cursor vira um "I").
2. Clique onde o texto deve começar e digite. **Enter** quebra a linha.
3. Aperte **Esc** para confirmar o texto. Clicar em outro lugar também confirma e já começa um novo texto ali.
4. Aperte **Esc** de novo (ou **T**) para voltar à caneta.

- O texto usa a cor atual. Escolha a cor **antes** de clicar: enquanto você digita, as letras vão para o texto, e não para os atalhos.
- Tamanho da fonte: **Ctrl + roda** ou **Ctrl+↑/↓**, de 12 a 120 pt (padrão: 28 pt). Funciona também durante a digitação.
- Texto vazio é descartado.

### Quadro branco e quadro negro

| Tecla | Ação |
|---|---|
| **W** | Liga / desliga o fundo branco |
| **K** | Liga / desliga o fundo preto |

O quadro cobre a tela inteira por trás dos traços e some automaticamente ao sair do modo desenho.

### Apagar, desfazer e refazer

| Atalho | Ação |
|---|---|
| **Ctrl+Z** | Desfaz a última ação |
| **Ctrl+Y** ou **Ctrl+Shift+Z** | Refaz |
| **E** | Apaga todos os traços e textos |

Cada traço, forma, texto ou "apagar tudo" conta como uma ação. Até o **E** pode ser desfeito com Ctrl+Z.

### Copiar e salvar a tela anotada

| Atalho | Ação |
|---|---|
| **Ctrl+C** | Copia uma imagem do monitor (com as anotações) para a área de transferência |
| **Ctrl+S** | Salva essa imagem em PNG |

- A imagem é do **monitor onde está o cursor**, já com os traços. A barra de dica não sai na imagem.
- Os arquivos vão para `Imagens\LiveZoomIt\`, com nomes como `LiveZoomIt_20261022_213811_541.png`. Não há janela de "Salvar como"; um aviso no topo da tela mostra onde o arquivo foi salvo.

### Sair do modo desenho

| Tecla | Ação |
|---|---|
| **Esc** | Sai do modo desenho (se a ferramenta de texto estiver ativa, primeiro volta para a caneta) |
| **Ctrl+2** | Sai do modo desenho |
| **Alt+F4** | Também só sai do modo desenho — **não** fecha o programa |

---

## Lupa (Ctrl+1)

- Mostra uma ampliação de **2×** da região sob o cursor, numa janela de cantos arredondados que acompanha o mouse em tempo real.
- Funciona nos dois modos e mostra também as anotações.
- Não atrapalha os cliques: você pode clicar e desenhar "através" dela.
- Normalmente fica centralizada no cursor. Em versões do Windows que não permitem esconder a lupa da captura de tela, ela aparece deslocada para baixo e para a direita do cursor, para não se ampliar a si mesma.

---

## Referência rápida

```
GLOBAIS        Ctrl+2 desenho · Ctrl+1 lupa · Ctrl+Shift+Q sair

FORMAS         arrastar: livre · Ctrl: retângulo · Shift: linha · Alt: elipse · Ctrl+Shift: seta
CORES          R G B Y O P (caneta) · Shift+cor (marca-texto)
ESPESSURA      Ctrl+roda · Ctrl+↑/↓
TEXTO          T, clicar, digitar, Esc
QUADRO         W branco · K preto
EDIÇÃO         Ctrl+Z desfazer · Ctrl+Y refazer · E apagar tudo
CAPTURA        Ctrl+C copiar · Ctrl+S salvar PNG
SAIR           Esc · Ctrl+2
```

---

## Problemas comuns

| Sintoma | Causa e solução |
|---|---|
| Aviso "Atalhos em uso por outro programa" | Outro programa já usa Ctrl+1, Ctrl+2 ou Ctrl+Shift+Q. Os atalhos livres continuam funcionando; feche o outro programa e reabra o LiveZoomIt. |
| Aviso "Nenhum atalho global pôde ser registrado" e o programa fecha | Provavelmente já existe um LiveZoomIt aberto. Use Ctrl+Shift+Q ou o Gerenciador de Tarefas. |
| Apertei uma letra e nada aconteceu | Fora do modo desenho as teclas vão para os outros programas. Entre com Ctrl+2. Durante a digitação de um texto, as letras vão para o texto. |
| Os traços continuam na tela depois de sair do desenho | É intencional. Entre no modo desenho e aperte **E**. |
| Traços borrados ou deslocados com monitores de escalas diferentes | Limitação conhecida quando os monitores usam escalas diferentes (ex.: 100% e 150%). |

---

## Requisitos

- Windows 10 (versão 2004 ou superior) ou Windows 11, 64 bits.
- Nada mais: o executável já inclui o runtime do .NET.

---

## Para desenvolvedores

### Compilar e publicar

Requer o .NET SDK 10.

```bash
dotnet build LiveZoomIt.slnx
```

```bash
dotnet test LiveZoomIt.slnx
```

```bash
dotnet publish LiveZoomIt -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o LiveZoomIt/publish
```

O executável fica em `LiveZoomIt/publish/LiveZoomIt.exe`.

### Estrutura

| Arquivo | Conteúdo |
|---|---|
| `LiveZoomIt/MainWindow.xaml(.cs)` | Janela transparente, modos, atalhos globais |
| `LiveZoomIt/MainWindow.Drawing.cs` | Ferramentas de desenho, teclado, texto, captura |
| `LiveZoomIt/Drawing/ShapeBuilder.cs` | Geometria das formas e encaixe de ângulos |
| `LiveZoomIt/Drawing/UndoStack.cs` | Desfazer / refazer por ação |
| `LiveZoomIt/MagnifierWindow.cs` | Lupa em tempo real |
| `LiveZoomIt/ScreenCapture.cs` | Copiar / salvar a tela anotada |
| `LiveZoomIt/Native.cs` | Chamadas à API do Windows (Win32) |
| `LiveZoomIt.Tests/` | Testes automatizados (xUnit) |

### Como funciona

- O overlay é uma janela WPF transparente e sempre por cima, cobrindo toda a área de trabalho virtual.
- No modo transparente, ela recebe o estilo `WS_EX_TRANSPARENT`, e o Windows entrega os cliques às janelas de trás.
- No modo desenho, esse estilo é removido e um `InkCanvas` recebe o mouse.
- As formas são geradas como traços de tinta comuns, por isso cor, espessura, desfazer e "apagar tudo" funcionam igual para tudo.
- A lupa copia a região sob o cursor a cada quadro e se exclui da captura (`WDA_EXCLUDEFROMCAPTURE`) para não ampliar a si mesma.
