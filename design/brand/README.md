# brand/ — logo do app (cabeça do mascote)

Status: **aprovado** (vetor traçado). O app usa `src/OwnExpeditor.App/Assets/mascot-96.png` (botão Início)
e `mascot.ico` (janela e executável), exportados do vetor.

## Pipeline

| Passo | Comando | Saída |
| --- | --- | --- |
| 1. Recorte | `python3 crop-mascot-head.py mascot-original.jpg mascot-head.png` | cabeça sem fundo, resolução nativa |
| 2. Vetor | `python trace-mascot.py` | `mascot-head-traced.svg` (gerado, não editar à mão) |
| 3. Assets | `python make-icons.py` | `Assets/mascot-96.png` e `Assets/mascot.ico` |

Rode na pasta `design/brand`. Dependências (em um venv; o Python do sistema é protegido):
`pip install -r requirements.txt`.

O vetor reproduz o original assim: cores traçadas em camadas (vtracer), sombras com desfoque adaptativo,
traço escuro retraçado em 2x, e a viseira refeita à parte — faixas de contorno empilhadas para o degradê
verde e o reflexo (amarelo, risco claro, riscos brancos) como curvas medidas linha a linha no original.
Os parâmetros ficam no topo de cada bloco em `trace-mascot.py`.
