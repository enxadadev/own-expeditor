# design/ — fluxos e telas antes do código

Nenhuma tela é implementada sem um desenho aprovado aqui. Dono: agente
`ux-designer` (fonte de verdade do *como deve ser*); o `ui-dev` implementa a
partir destes arquivos e o `ux-reviewer` compara o resultado com eles.

## Estrutura

```
design/
  README.md
  direcao-visual/          direção visual escolhida e alternativas
  <tema-ou-modulo>/        ex.: shell/, review-apply/, fretes/, controles/
    README.md              objetivo, status (rascunho | aprovado | implementado), link do canvas
    flows.md               fluxos em Mermaid (passos, decisões, erros)
    screens.md             wireframes em texto, estados, componentes, teclado, acessibilidade
    strings.md             tabela Chave | pt-BR | en (vira os .resx)
    canvas/                fonte dos mockups (canvas.json + *.dc.html)
```

## Canvas

Os mockups são canvases do Claude (formato `.dc.html`): a fonte fica em
`canvas/` (versionada) e a visualização interativa no link do README de cada
pasta. Para atualizar: edite os `.dc.html`, publique no mesmo canvas e
commite a fonte. Os arquivos precisam do runtime do canvas — abra pelo link,
não direto no navegador.

## Status

| Pasta | Status | Canvas |
| --- | --- | --- |
| [direcao-visual](direcao-visual/README.md) | aprovado (A+C) | [link](https://claude.ai/artifact/EBn8WvYp5z8dgDmK8rNmQV) |
| [shell](shell/README.md) | aprovado | [link](https://claude.ai/artifact/YSdcaqn7AyAqvc6wVuBSYN) |
| [review-apply](review-apply/README.md) | aprovado | [link](https://claude.ai/artifact/Sc4DPk3qimRg3ivvk58d5z) |
| [controles](controles/README.md) | aprovado | [link](https://claude.ai/artifact/AHbpXwyEujo1i82j8AZEED) |
| [fretes](fretes/README.md) | aprovado | https://claude.ai/artifact/DGcBW2CtBSZWnZXgzE7PhM |

## Regras
- Mudou a tela? Atualize o desenho **antes**, no mesmo PR.
- Status `aprovado` só com OK explícito do usuário.
- Dados de exemplo anonimizados; nada de arte, logos ou marcas da SCS.
- Textos sempre em pt-BR e en (skill `ux-writing`).
