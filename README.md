# LiveZoomIt

**Anotação de tela para Windows, no estilo do ZoomIt — mas sem congelar a tela.**

Feito para quem gosta do [ZoomIt](https://learn.microsoft.com/sysinternals/downloads/zoomit) e se incomoda com uma coisa: ao entrar no modo desenho, ele tira uma "foto" da tela e você passa a riscar uma imagem parada. O vídeo pausa, o chat para de atualizar, o terminal congela e você não consegue clicar em nada até sair do desenho.

O LiveZoomIt desenha numa camada transparente **por cima da tela viva**. Vídeos, chamadas, navegadores e terminais continuam rodando por baixo dos traços, e com um atalho você volta a usar os programas normalmente. Ao sair do desenho, as anotações são apagadas como no ZoomIt — ou, se preferir, ficam na tela enquanto você usa os programas.

- **Atalhos familiares:** Ctrl+2 para desenhar, R / G / B / Y para as cores, Ctrl para retângulo, Shift para linha, E para apagar, W / K para quadro branco e negro, Ctrl+Z para desfazer.
- **Um único `LiveZoomIt.exe` portátil:** sem instalação e sem precisar do .NET instalado.
- **Discreto:** roda em segundo plano; fica só um ícone de lupa na área de notificação (perto do relógio).
- **Cobre todos os monitores.**

## Download

Baixe o `LiveZoomIt.exe` mais recente na página de [**Releases**](https://github.com/mnisenbaum/live-zoomit/releases) e execute. Não há instalador: para "desinstalar", feche o programa (Ctrl+Shift+F11) e apague o arquivo — e, se quiser, a pasta `%APPDATA%\LiveZoomIt` com as configurações.

> Na primeira execução, o Windows pode mostrar o aviso azul **"O Windows protegeu o computador"** (SmartScreen), porque o executável não é assinado digitalmente. Clique em **Mais informações → Executar assim mesmo**. Se preferir, [compile você mesmo](#para-desenvolvedores) a partir do código-fonte.

## LiveZoomIt × ZoomIt

| | ZoomIt | LiveZoomIt |
|---|---|---|
| Tela durante o desenho | Congelada numa imagem estática | **Continua viva** (vídeos, chats e terminais seguem rodando) |
| Usar os programas com as anotações na tela | — | **Opcional:** desligue "Apagar desenho ao sair" e os traços ficam na tela, com os cliques passando para os programas |
| Desenho livre, retângulo, linha, elipse, seta | Sim | Sim (Alt + arrastar para a elipse) |
| Cores, marca-texto, texto, quadro branco/negro, desfazer | Sim | Sim |
| Lupa | Zoom da tela inteira | Lupa flutuante que segue o cursor (1,5× a 8×) |
| Extras | Timer de intervalo, gravação de tela, DemoType | Caneta laser, marcadores numerados, borracha por traço, holofote, destaque de cliques, exibição de teclas |

O LiveZoomIt não tenta substituir tudo o que o ZoomIt faz — o foco é anotar sem perder a tela viva.

---

## Início rápido

1. Execute `LiveZoomIt.exe`. Nenhuma janela abre — aparece só o **ícone de lupa azul** na área de notificação. (Se não o vir, clique na setinha **^** ao lado do relógio.)
2. Aperte **Ctrl+2** (ou dê dois cliques no ícone) para entrar no modo desenho e arraste com o mouse para riscar a tela.
3. Aperte **Esc** (ou **Ctrl+2** de novo) para voltar a usar o computador normalmente.
4. Aperte **Ctrl+Shift+F11** (ou use **Sair** no menu do ícone) para fechar o programa.

---

## Ícone na área de notificação

Clique com o **botão direito** no ícone de lupa para abrir o menu:

| Item | O que faz |
|---|---|
| Modo desenho, Lupa, Holofote, Destaque de cliques, Mostrar teclas | Liga / desliga o recurso. O item fica marcado (✓) quando está ligado, e o atalho atual aparece à direita. |
| Abrir pasta de capturas | Abre no Explorer a pasta onde o **Ctrl+S** salva as imagens. |
| Editar configurações… | Abre o `settings.json` (veja [Configurações](#configurações)). |
| Apagar desenho ao sair | Liga / desliga a limpeza do desenho ao sair do modo desenho (veja [Os dois modos](#os-dois-modos)). A escolha é gravada no `settings.json`. |
| Recarregar configurações | Aplica o que você mudou no `settings.json` sem reiniciar o programa. |
| Sair | Fecha o LiveZoomIt. |

**Dois cliques** no ícone ligam / desligam o modo desenho.

---

## Os dois modos

| Modo | Como entrar | O que acontece |
|---|---|---|
| **Transparente** (padrão) | Ao abrir, ou com **Esc** / **Ctrl+2** no modo desenho | Os cliques e o teclado passam direto para os programas. |
| **Desenho** | **Ctrl+2** | O mouse desenha. Uma barra no topo do monitor principal mostra a ferramenta, a cor, a espessura e os atalhos. |

Ao sair do modo desenho, o foco volta para o programa que você estava usando antes e o quadro branco/negro some.

**O que acontece com o desenho ao sair** depende da opção **Apagar desenho ao sair**, no menu do ícone (ou `ClearOnExit` nas [Configurações](#configurações)):

- **Ligada (padrão):** o desenho é apagado, como no ZoomIt. Apagou sem querer? Entre de novo no modo desenho e aperte **Ctrl+Z**: tudo volta.
- **Desligada:** os traços **continuam visíveis** por cima dos programas enquanto você os usa, até você apagá-los (**E**). A barra de dica mostra qual das duas está valendo.

---

## Atalhos globais

Funcionam em qualquer programa, a qualquer momento.

| Atalho | Ação |
|---|---|
| **Ctrl+2** | Liga / desliga o modo desenho |
| **Ctrl+1** | Liga / desliga a lupa |
| **Ctrl+3** | Liga / desliga o holofote |
| **Ctrl+4** | Liga / desliga o destaque de cliques |
| **Ctrl+5** | Liga / desliga a exibição de teclas |
| **Ctrl+Shift+F11** | Fecha o LiveZoomIt |

> Enquanto o LiveZoomIt estiver aberto, **Ctrl+1** a **Ctrl+5** ficam reservados para ele em todo o sistema (por exemplo, deixam de trocar de aba no navegador). Todos esses atalhos podem ser trocados ou desativados nas [Configurações](#configurações).

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
| **X** | Preto | Marca-texto preto (fica cinza) |
| **C** | Branco | Marca-texto branco |

As cores e suas teclas podem ser trocadas, removidas ou acrescentadas nas [Configurações](#configurações).

- O **marca-texto** é largo e semitransparente, para destacar sem esconder o que está por baixo. Ele tem 3× a espessura da caneta (mínimo de 12 px).
- Apertar uma cor **sem** Shift sai do marca-texto e volta para a caneta normal (ou continua no laser, se ele estiver ligado).
- A cor inicial é vermelho.

### Caneta laser (L)

- **L** liga / desliga a caneta laser.
- Tudo o que você desenha com o laser — traço livre ou forma — fica 1 segundo na tela e some em meio segundo. Ótimo para apontar algo numa reunião sem precisar apagar depois.
- O laser usa a cor e a espessura atuais, e trocar a cor (R, G, B…) mantém o laser ligado.
- Traços laser não entram no desfazer.

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

### Marcadores numerados (N)

1. Aperte **N** para ativar os marcadores (o cursor vira uma mão).
2. Cada clique coloca um círculo com o próximo número: 1, 2, 3…
3. Aperte **Esc** (ou **N**) para voltar à caneta.

- O círculo usa a cor atual; o número fica preto em cores claras (amarelo, laranja) e branco nas demais.
- O tamanho segue o tamanho da fonte: **Ctrl + roda** ou **Ctrl+↑/↓**.
- O próximo número é sempre um a mais que o maior que está na tela. Apagar tudo (**E**) recomeça do 1.

### Borracha (botão direito)

- **Segure o botão direito e arraste** sobre traços, formas, textos ou marcadores para apagar só o que você tocar.
- Funciona com qualquer ferramenta ativa.
- Cada passada da borracha conta como uma ação: **Ctrl+Z** traz de volta tudo o que ela apagou.

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
| **E** | Apaga todos os traços, textos e marcadores |
| Botão direito + arrastar | Apaga só o que for tocado |

Cada traço, forma, texto, marcador, passada de borracha ou "apagar tudo" conta como uma ação. Até o **E** pode ser desfeito com Ctrl+Z.

### Copiar e salvar a tela anotada

| Atalho | Ação |
|---|---|
| **Ctrl+C** | Copia uma imagem do monitor (com as anotações) para a área de transferência |
| **Ctrl+S** | Salva essa imagem em PNG |

- A imagem é do **monitor onde está o cursor**, já com os traços. A barra de dica não sai na imagem.
- Os arquivos vão para `Imagens\LiveZoomIt\` (ou a pasta definida em `CaptureFolder`), com nomes como `LiveZoomIt_20261022_213811_541.png`. Não há janela de "Salvar como"; um aviso no topo da tela mostra onde o arquivo foi salvo. A pasta pode ser aberta pelo menu do ícone.

### Sair do modo desenho

| Tecla | Ação |
|---|---|
| **Esc** | Sai do modo desenho (se o texto ou os marcadores estiverem ativos, primeiro volta para a caneta). Por padrão o desenho é apagado; veja [Os dois modos](#os-dois-modos). |
| **Ctrl+2** | Sai do modo desenho |
| **Alt+F4** | Também só sai do modo desenho — **não** fecha o programa |

---

## Lupa (Ctrl+1)

- Mostra uma ampliação da região sob o cursor (**2×** ao abrir), numa janela de cantos arredondados que acompanha o mouse em tempo real.
- **Ctrl+Alt + roda do mouse** muda a ampliação entre 1,5×, 2×, 3×, 4×, 6× e 8×; o valor aparece no canto da lupa. Enquanto a lupa estiver ligada, esse gesto não chega aos outros programas.
- O tamanho da lupa e a ampliação inicial podem ser definidos nas [Configurações](#configurações).
- Funciona nos dois modos e mostra também as anotações.
- Não atrapalha os cliques: você pode clicar e desenhar "através" dela.
- Normalmente fica centralizada no cursor. Em versões do Windows que não permitem esconder a lupa da captura de tela, ela aparece deslocada para baixo e para a direita do cursor, para não se ampliar a si mesma.

---

## Holofote (Ctrl+3)

- Escurece todos os monitores e deixa só um círculo claro em volta do cursor, que acompanha o mouse.
- Funciona nos dois modos. No modo transparente você continua clicando e usando os programas normalmente; vídeos continuam rodando.
- Pode ser combinado com a lupa e com o desenho.
- A borda do círculo é serrilhada (sem suavização) — é o preço de o holofote não pesar no computador mesmo com vários monitores.

---

## Destaque de cliques (Ctrl+4)

- Mostra um anel animado onde você clica: **amarelo** para o botão esquerdo e **azul** para o direito. Ajuda quem assiste a uma demonstração ou gravação a acompanhar seus cliques.
- Funciona só no **modo transparente** — no modo desenho os cliques já viram traços.
- Um aviso no topo da tela confirma quando o destaque é ligado ou desligado.

---

## Exibição de teclas (Ctrl+5)

- Mostra as teclas que você aperta em etiquetas no rodapé do monitor principal — útil para gravar tutoriais ou apresentar atalhos.
- Texto digitado em sequência aparece junto numa etiqueta só ("olá mundo"); combinações aparecem como **Ctrl + Shift + T**; a mesma combinação repetida ganha um contador (**Ctrl + Z ×3**).
- Teclas especiais têm nomes curtos: Enter, Esc, Backspace, Tab, ↑ ↓ ← →, F1–F24 etc. Ctrl, Shift e Alt sozinhos não aparecem.
- Com Shift, números e símbolos aparecem como combinação (**Shift + 1**), porque o símbolo resultante depende do layout do teclado.
- As etiquetas somem cerca de 2,5 segundos após a última tecla; no máximo 4 ficam visíveis.

> ⚠️ **Cuidado com senhas:** tudo o que você digita aparece na tela, inclusive em campos de senha. Desligue (Ctrl+5) antes de digitar algo sigiloso.

---

## Configurações

As configurações ficam em `%APPDATA%\LiveZoomIt\settings.json`, criado com os valores padrão na primeira execução. Para editar, use **Editar configurações…** no menu do ícone; depois de salvar, use **Recarregar configurações** (ou reinicie o programa).

```jsonc
{
  "Hotkeys": {
    "ToggleDraw": "Ctrl+2",
    "ToggleMagnifier": "Ctrl+1",
    "ToggleSpotlight": "Ctrl+3",
    "ToggleClickHighlights": "Ctrl+4",
    "ToggleKeystrokes": "Ctrl+5",
    "Quit": "Ctrl+Shift+F11"
  },
  "Colors": {
    "R": "#FF0000",
    "G": "#00D000",
    "B": "#0060FF",
    "Y": "#FFE000",
    "O": "#FF8C00",
    "P": "#FF3EA5",
    "X": "#000000",
    "C": "#FFFFFF"
  },
  "StartColor": "R",
  "ClearOnExit": true,
  "PenWidth": 4,
  "FontSize": 28,
  "Magnifier": { "Width": 420, "Height": 260, "Zoom": 2 },
  "Spotlight": { "Radius": 160, "Dim": 0.67 },
  "Laser": { "HoldSeconds": 1.0, "FadeSeconds": 0.5 },
  "CaptureFolder": ""
}
```

| Campo | Significado | Valores aceitos |
|---|---|---|
| `Hotkeys.*` | Atalhos globais | Modificadores `Ctrl`, `Alt`, `Shift`, `Win` + uma tecla (`A`–`Z`, `0`–`9`, `F1`–`F24`, nomes como `Home`, `PageUp`), ex.: `"Ctrl+Alt+D"`. Uma tecla de função pode ser usada sozinha (`"F9"`). Texto vazio (`""`) desativa o atalho — exceto `Quit`. |
| `Colors` | Tecla → cor do modo desenho | Tecla: uma letra, exceto **L, T, N, W, K, E** (já usadas). Cor: `#RRGGBB` ou um nome em inglês (`"Purple"`, `"Cyan"`). Você pode remover ou acrescentar cores. |
| `StartColor` | Cor inicial | Uma das teclas de `Colors`. |
| `ClearOnExit` | Apagar o desenho ao sair do modo desenho | `true` (padrão, como no ZoomIt) ou `false` (o desenho fica na tela). Também muda pelo menu do ícone. |
| `PenWidth` | Espessura inicial da caneta | 1 a 40 (arredondada para a espessura disponível mais próxima). |
| `FontSize` | Tamanho inicial do texto e dos números | 12 a 120. |
| `Magnifier` | Tamanho da lupa e ampliação inicial | `Width` 100–1600, `Height` 80–1200, `Zoom` 1,5–8. |
| `Spotlight` | Holofote | `Radius` (raio do círculo) 40–800; `Dim` (escurecimento) 0,1–0,95. |
| `Laser` | Tempo da caneta laser | `HoldSeconds` (tempo visível) 0–10; `FadeSeconds` (tempo sumindo) 0,1–5. |
| `CaptureFolder` | Pasta das capturas (Ctrl+S) | Caminho completo, ex.: `"D:\\Capturas"` (barras duplicadas no JSON); aceita `%USERPROFILE%`. Vazio = `Imagens\LiveZoomIt`. |

- Comentários (`//`) e vírgula sobrando no fim são aceitos, e maiúsculas/minúsculas nos nomes dos campos não importam.
- Um valor inválido **nunca impede o programa de abrir**: ele volta ao padrão (ou ao limite mais próximo) e um aviso aparece no topo da tela.
- Para voltar a todas as configurações padrão, apague o `settings.json`; ele é recriado na próxima execução.

---

## Referência rápida

```
GLOBAIS        Ctrl+2 desenho · Ctrl+1 lupa · Ctrl+3 holofote · Ctrl+4 cliques · Ctrl+5 teclas · Ctrl+Shift+F11 sair
LUPA           Ctrl+Alt+roda muda a ampliação

FORMAS         arrastar: livre · Ctrl: retângulo · Shift: linha · Alt: elipse · Ctrl+Shift: seta
CORES          R G B Y O P X C (caneta) · Shift+cor (marca-texto) · L laser
ESPESSURA      Ctrl+roda · Ctrl+↑/↓
TEXTO          T, clicar, digitar, Esc
NÚMEROS        N, clicar, clicar…, Esc
QUADRO         W branco · K preto
EDIÇÃO         Ctrl+Z desfazer · Ctrl+Y refazer · botão direito apaga · E apagar tudo
CAPTURA        Ctrl+C copiar · Ctrl+S salvar PNG
SAIR           Esc · Ctrl+2
```

---

## Problemas comuns

| Sintoma | Causa e solução |
|---|---|
| Aviso "Atalhos em uso por outro programa" | Outro programa já usa algum dos atalhos. Os atalhos livres continuam funcionando; troque o atalho em conflito nas [Configurações](#configurações) ou feche o outro programa. |
| Aviso "Configurações: …" no topo da tela | Algum valor do `settings.json` é inválido e foi trocado pelo padrão. O aviso diz qual campo; corrija e use **Recarregar configurações**. |
| Não acho o ícone | Ele pode estar escondido: clique na setinha **^** ao lado do relógio. Você pode arrastá-lo para ficar sempre visível. |
| Aviso "Nenhum atalho global pôde ser registrado" e o programa fecha | Provavelmente já existe um LiveZoomIt aberto. Use Ctrl+Shift+F11 ou o Gerenciador de Tarefas. |
| Apertei uma letra e nada aconteceu | Fora do modo desenho as teclas vão para os outros programas. Entre com Ctrl+2. Durante a digitação de um texto, as letras vão para o texto. |
| O desenho some ao sair do modo desenho | É o padrão, como no ZoomIt. Para mantê-lo na tela, desligue **Apagar desenho ao sair** no menu do ícone. Para recuperar o que sumiu, entre no desenho e aperte **Ctrl+Z**. |
| Os traços continuam na tela depois de sair do desenho | A opção **Apagar desenho ao sair** está desligada. Ligue-a no menu do ícone, ou entre no modo desenho e aperte **E**. |
| Traços borrados ou deslocados com monitores de escalas diferentes | Limitação conhecida quando os monitores usam escalas diferentes (ex.: 100% e 150%). |
| Aviso "O Windows protegeu o computador" ao abrir | O executável não é assinado digitalmente. Clique em **Mais informações → Executar assim mesmo**, ou compile a partir do código-fonte. |
| Antivírus reclama do programa | O destaque de cliques e a exibição de teclas usam hooks globais de mouse e teclado — o mesmo mecanismo de ferramentas de acessibilidade e gravação de tela, que alguns antivírus tratam com desconfiança. Eles só ficam ativos enquanto esses recursos estão ligados, e nada é gravado em disco nem enviado pela rede. |
| Encontrei um bug / tenho uma sugestão | Abra uma [issue](https://github.com/mnisenbaum/live-zoomit/issues). |

---

## Requisitos

- Windows 10 (versão 2004 ou superior) ou Windows 11, 64 bits.
- Nada mais: o executável já inclui o runtime do .NET.

---

## Para desenvolvedores

### Compilar e publicar

Requer o [.NET SDK 10](https://dotnet.microsoft.com/download) (no Windows: `winget install Microsoft.DotNet.SDK.10`).

```bash
git clone https://github.com/mnisenbaum/live-zoomit.git
```

```bash
cd live-zoomit
```

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
| `LiveZoomIt/MainWindow.Drawing.cs` | Ferramentas de desenho, laser, marcadores, borracha, teclado, texto, captura |
| `LiveZoomIt/MainWindow.Effects.cs` | Holofote, destaque de cliques e zoom da lupa (hook de mouse) |
| `LiveZoomIt/MainWindow.Keystrokes.cs` | Exibição de teclas (hook de teclado) |
| `LiveZoomIt/MainWindow.Settings.cs` | Leitura e aplicação das configurações |
| `LiveZoomIt/MainWindow.Tray.cs` | Ícone e menu na área de notificação |
| `LiveZoomIt/Settings/` | Modelo do `settings.json`, validação e leitura de atalhos |
| `LiveZoomIt/Keystrokes/` | Formatação e agrupamento das teclas exibidas |
| `LiveZoomIt/KeystrokeWindow.cs` | Janela das etiquetas de teclas |
| `LiveZoomIt/MagnifierZoom.cs` | Níveis de ampliação da lupa |
| `LiveZoomIt/Drawing/ShapeBuilder.cs` | Geometria das formas e encaixe de ângulos |
| `LiveZoomIt/Drawing/UndoStack.cs` | Desfazer / refazer por ação, incluindo borracha e "apagar tudo" |
| `LiveZoomIt/Drawing/LaserFade.cs` | Tempo de desaparecimento do laser |
| `LiveZoomIt/Drawing/StepMarker.cs` | Marcadores numerados |
| `LiveZoomIt/MagnifierWindow.cs` | Lupa em tempo real |
| `LiveZoomIt/SpotlightWindow.cs` | Holofote |
| `LiveZoomIt/ScreenCapture.cs` | Copiar / salvar a tela anotada |
| `LiveZoomIt/Native.cs` | Chamadas à API do Windows (Win32) |
| `LiveZoomIt.Tests/` | Testes automatizados (xUnit) |

### Como funciona

- O overlay é uma janela WPF transparente e sempre por cima, cobrindo toda a área de trabalho virtual.
- No modo transparente, ela recebe o estilo `WS_EX_TRANSPARENT`, e o Windows entrega os cliques às janelas de trás.
- No modo desenho, esse estilo é removido e um `InkCanvas` recebe o mouse.
- As formas são geradas como traços de tinta comuns, por isso cor, espessura, desfazer e "apagar tudo" funcionam igual para tudo.
- A lupa copia a região sob o cursor a cada quadro e se exclui da captura (`WDA_EXCLUDEFROMCAPTURE`) para não ampliar a si mesma.
- O holofote é uma janela preta semitransparente com um "furo" circular recortado por região de janela (`SetWindowRgn`), atualizado só quando o cursor se move.
- O destaque de cliques e o zoom da lupa compartilham um hook global de mouse (`WH_MOUSE_LL`), instalado apenas enquanto um dos dois estiver ligado.
- A exibição de teclas usa um hook global de teclado (`WH_KEYBOARD_LL`), ativo só enquanto ligada. O caractere vem de `MapVirtualKey`, que — ao contrário de `ToUnicode` — não interfere nos acentos que você está digitando. As etiquetas ficam numa janela pequena própria, para não redesenhar o overlay de tela cheia a cada tecla.
- O ícone da área de notificação usa o `NotifyIcon` do Windows Forms e é desenhado em tempo de execução.

---

## Licença

[MIT](LICENSE): você pode usar, copiar, modificar e redistribuir o LiveZoomIt livremente, inclusive em projetos comerciais, desde que mantenha o aviso de copyright e a licença. O software é fornecido "como está", sem garantias.

---

## Aviso

O LiveZoomIt é um projeto independente, sem nenhuma relação com a Microsoft ou a Sysinternals. ZoomIt é um produto da Microsoft; o nome é citado apenas como referência ao estilo de uso e aos atalhos que inspiraram este projeto.