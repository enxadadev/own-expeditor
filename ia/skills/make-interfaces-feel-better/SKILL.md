---
name: make-interfaces-feel-better
description: Aplica detalhes concretos de design-engineering para deixar a UI Avalonia do Own Expeditor polida. Use ao revisar ou melhorar espaçamento, tipografia, bordas, sombras, transições, áreas de clique, ícones, quebra de texto e estados de interação (pointerover, pressed, focus, disabled, loading, vazio) em src/OwnExpeditor.App.
metadata:
  origin: adaptado de community skill "make-interfaces-feel-better" (PR #1659, linus707)
---

# Make Interfaces Feel Better (Avalonia / Own Expeditor)

Pequenos detalhes que, somados, fazem o editor de jobs parecer acabado.
Escopo: apenas `src/OwnExpeditor.App` (XAML `.axaml`, estilos, ControlThemes,
recursos). Nunca mover lógica para a View para "polir" a UI — o MVVM continua
valendo: estados visuais vêm de propriedades do ViewModel + classes/pseudo-classes.

## Quando usar

- A UI parece "crua", apertada, pula ao atualizar, ou genérica.
- Criando/alterando telas típicas do app: seleção de perfil/save, listas de
  cidades/empresas/cargas/reboques, formulário do job, barra de ações
  (Injetar job, Backup, Recarregar), diálogos de confirmação.
- Um controle precisa de estados: hover, pressed, foco, disabled, carregando
  (lendo/decriptando save), vazio (save sem job offers, filtro sem resultado),
  erro (save inválido) e sucesso (job injetado + backup criado).
- Revisão de UI pedindo recomendações antes/depois.

## Princípios

### Raio concêntrico

Para superfícies arredondadas aninhadas e próximas:

```text
CornerRadius externo = CornerRadius interno + Padding
```

```xml
<Border CornerRadius="12" Padding="4">
  <Border CornerRadius="8"> ... </Border>
</Border>
```

Com padding grande, trate como superfícies separadas. Centralize raios em
recursos (`<CornerRadius x:Key="RadiusSm">6</CornerRadius>`, `RadiusMd`,
`RadiusLg`) em vez de números soltos no XAML.

### Alinhamento óptico

Centro geométrico ≠ centro visual. Ícones `PathIcon`/`Path` assimétricos
(setas, play, caminhão, check) costumam precisar de 1px de deslocamento:
prefira corrigir a geometria; senão use `Margin="1,0,0,0"`. Use `Stretch` e
`Width/Height` fixos para ícones — não deixe o tamanho variar com o texto.

### Bordas e sombras

- `BorderBrush`/`BorderThickness` para separação e anéis de foco.
- `BoxShadow` (em `Border`) para profundidade de cards, flyouts e popups:
  sutil, com alpha neutro, e em camadas:
  ```xml
  <Border BoxShadow="0 1 2 0 #14000000, 0 4 12 0 #0F000000" />
  ```
- Defina pincéis em `ThemeDictionaries` (`Light`/`Dark`) e use
  `{DynamicResource ...}` — nunca cores fixas que quebram no tema escuro.

### Texto

Avalonia não tem `text-wrap: balance/pretty`; equivalentes práticos:

- Títulos e rótulos curtos: `TextWrapping="NoWrap"` + `TextTrimming="CharacterEllipsis"`
  e `ToolTip.Tip` com o texto completo (nomes de empresas/cidades de mods podem
  ser longos).
- Descrições curtas: `TextWrapping="Wrap"` com `MaxWidth` razoável (~60–70
  caracteres por linha) para evitar linhas órfãs.
- Nunca quebrar identificadores do SII (tokens, nomes internos, paths): use
  fonte monoespaçada e `NoWrap` + trimming.
- Números que mudam ou se alinham em coluna (distância, preço, peso, contagem
  de jobs, horário/expiração): números tabulares para não "pular":
  ```xml
  <TextBlock FontFeatures="+tnum" Text="{Binding DistanceKm, StringFormat='{}{0:N0} km'}" />
  ```
  Alinhe números à direita em colunas de `DataGrid`/listas.

### Renderização de texto

Defina na raiz (janela principal) uma vez, não por controle:

```xml
<Window RenderOptions.TextRenderingMode="Antialias" ... />
```

Use a fonte do app via recurso (`FontFamily` em `App.axaml`) para consistência
entre Windows e Linux.

### Imagens

Imagens (ícones de empresa/carga, se existirem) precisam de um contorno neutro
interno para não se dissolverem no fundo:

```xml
<Border BorderThickness="1" CornerRadius="{StaticResource RadiusSm}"
        BorderBrush="{DynamicResource ImageOutlineBrush}" ClipToBounds="True">
  <Image Source="{Binding Logo}" />
</Border>
```

`ImageOutlineBrush` = `#1A000000` no tema claro e `#1AFFFFFF` no escuro.
Nunca tingir com a cor de destaque.

### Movimento

Use `Transitions` para mudanças de estado interativas (são interrompíveis e
retomam do valor atual). Reserve `Animation`/`KeyFrame` para entradas encenadas
ou indicadores de carregamento.

- Entrada: `Opacity` 0→1 + `TranslateTransform` Y pequeno (4–8px), ~200ms.
- Saída: mais curta e discreta, ~150ms.
- Pressionar: `scale(0.96)` em botões táteis via `:pressed`, desligável por
  classe (ex. `Classes="no-press"`) quando distrair.
- Troca de ícone (ex. "Injetar" → check de sucesso): cross-fade com `Opacity` +
  escala, não troca instantânea de `IsVisible`.

```xml
<Style Selector="Button">
  <Setter Property="RenderTransformOrigin" Value="50%,50%" />
  <Setter Property="Transitions">
    <Transitions>
      <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15" Easing="CubicEaseOut" />
      <BrushTransition Property="Background" Duration="0:0:0.15" />
    </Transitions>
  </Setter>
</Style>
<Style Selector="Button:pressed">
  <Setter Property="RenderTransform" Value="scale(0.96)" />
</Style>
<Style Selector="Button.no-press:pressed">
  <Setter Property="RenderTransform" Value="none" />
</Style>
```

### Escopo das transições

- Liste só as propriedades que realmente mudam (o equivalente a nunca usar
  `transition: all`).
- Anime `Opacity`, `RenderTransform` e pincéis. Não anime `Width`, `Height`,
  `Margin` ou `Padding` — disparam layout a cada frame e ficam travados em listas
  grandes (saves com milhares de job offers).
- Em listas virtualizadas, não coloque animações de entrada por item; anime o
  container.

### Áreas de clique

Controles interativos com no mínimo 40×40 px, idealmente 44×44 quando o layout
permitir. Para ícones pequenos, aumente com `MinWidth/MinHeight` + `Padding`
(fundo `Transparent`, não `null`, para receber hit-test), sem sobrepor áreas
vizinhas.

### Estados e foco

- Sempre estilize `:pointerover`, `:pressed`, `:disabled` e `:focus-visible`
  (anel de foco com borda, não só mudança de cor) — o app deve ser usável por
  teclado.
- Ações destrutivas/irreversíveis (gravar `game.sii`) ficam visualmente
  distintas e desabilitadas enquanto não há backup/seleção válida.
- Carregamento: indicador + controles desabilitados; não congelar a UI
  (trabalho pesado fica em Core, assíncrono).
- Vazio: mensagem explicando o porquê ("Nenhuma oferta de job neste save —
  abra o mercado de fretes no jogo e salve de novo") em vez de lista em branco.

## Saída da revisão

Ao revisar um passe de polimento, reporte mudanças concretas em linhas
antes/depois:

| Princípio | Antes | Depois |
| --- | --- | --- |
| Raio concêntrico | Mesmo `CornerRadius` no pai e no filho | Pai = filho + `Padding` |
| Números tabulares | Distância "pula" ao trocar destino | `FontFeatures="+tnum"` |
| Escopo de transição | Animando `Height` do painel | `Opacity` + `TranslateTransform` |

Inclua arquivo `.axaml` e propriedade quando não estiverem óbvios. Omita
princípios verificados que não exigiram mudança.

## Checklist

- [ ] Elementos arredondados aninhados são opticamente coerentes; raios vêm de recursos.
- [ ] Ícones visualmente centralizados e com tamanho fixo.
- [ ] Bordas/sombras usadas pelo motivo certo e com pincéis de tema (claro/escuro).
- [ ] Rótulos curtos não quebram feio; nomes longos têm trimming + tooltip.
- [ ] Números dinâmicos/colunas usam `+tnum` e alinhamento à direita.
- [ ] Imagens têm contorno neutro.
- [ ] Transições cobrem só propriedades necessárias; nada de animar layout.
- [ ] Botões têm estado pressed tátil e discreto.
- [ ] Áreas de clique ≥ 40×40.
- [ ] Estados hover/pressed/disabled/focus-visible/loading/vazio/erro existem.
