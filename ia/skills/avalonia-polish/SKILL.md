---
name: avalonia-polish
description: Checklist de polimento visual e de interação da UI Fluent — raios, sombras, tipografia, estados, foco, acessibilidade, Light/Dark. Use ao criar, revisar ou polir telas em src/OwnExpeditor.App; exemplos em reference.md.
metadata:
  origin: adaptado de "make-interfaces-feel-better" (community PR #1659, linus707) para Avalonia 11.3 + FluentTheme
---

# Avalonia polish

Pequenos detalhes que somados fazem o app parecer acabado. Escopo:
`src/OwnExpeditor.App`. MVVM continua valendo: estado visual vem de
propriedades do ViewModel + classes/pseudo-classes. Exemplos prontos:
`reference.md` (leia só a seção que precisar).

## Fatos verificados neste projeto (Avalonia 11.3.0, Fluent)
- O Fluent **já** aplica `scale(0.98)` no `Button:pressed`. Não some outro
  scale; ajuste o existente se precisar (ref. §Pressed).
- O fundo do botão é pintado no template: transições e cores de botão vão em
  `Button /template/ ContentPresenter#PART_ContentPresenter` (ref. §Transições).
- `FontFeatures="+tnum"` funciona em `TextBlock` (números tabulares).
- Não force `RenderOptions.TextRenderingMode`: o padrão usa subpixel no
  Windows e fica mais nítido.

## Checklist
1. **Tokens, não números soltos**: raios, espaços, cores, fontes vêm de
   recursos em `Styles/`; cores com `{DynamicResource}` e variantes
   `Light`/`Dark` em `ThemeDictionaries`. Nada de cor fixa (quebra o tema).
2. **Raio concêntrico**: externo = interno + padding entre eles.
3. **Bordas vs sombras**: borda separa e marca foco; `BoxShadow` sutil em
   camadas só para elevar (cards, flyouts). Alpha neutro.
4. **Texto**: rótulos curtos `NoWrap` + `TextTrimming` + `ToolTip.Tip` com
   o texto inteiro (nomes de mods são longos); descrições com `Wrap` e
   `MaxWidth`; identificadores do SII em mono, nunca quebrados.
5. **Números**: `+tnum`, alinhados à direita em colunas, formatados pela
   cultura atual (pt-BR `1.234 km`, en `1,234 km`) — nunca string montada à mão.
6. **Ícones**: tamanho fixo, opticamente centralizados (corrija a geometria
   ou 1px de `Margin`).
7. **Transições**: só nas propriedades que mudam (`Opacity`,
   `RenderTransform`, pincéis); 150–200 ms; nunca `Width/Height/Margin`.
   Listas virtualizadas: anime o container, não os itens.
8. **Áreas de clique** ≥ 40×40 (ideal 44); fundo `Transparent` para hit-test.
9. **Estados**: `:pointerover`, `:pressed`, `:disabled`, `:focus-visible`;
   tela com carregando / vazio (explica o porquê) / erro (o que fazer) / sucesso.
10. **Ação perigosa** (gravar `game.sii`): visual distinto, confirmação,
    desabilitada sem seleção válida, mostra onde ficou o backup.
11. **Acessibilidade**: `AutomationProperties.Name` em controles só com
    ícone; ordem de Tab lógica; contraste ok nos **dois** temas.
12. **Tema e língua**: confira a tela em Light e Dark e em pt-BR e en
    (textos em inglês costumam ser mais curtos, alemão não existe ainda —
    mas deixe folga de ~30% para tradução).

## Saída de revisão
| Princípio | Antes | Depois |
| --- | --- | --- |
| ex.: Números | distância "pula" | `FontFeatures="+tnum"` |

Com `arquivo:linha`; omita o que estava ok. O que só dá para ver rodando o
app, marque `verificar visualmente`.
