# Revisar e aplicar mudanças

- **Status:** aprovado (2026-11-17) — UX-2 de `docs/architecture/plan-0005-shell.md`
- **Canvas:** https://claude.ai/artifact/Sc4DPk3qimRg3ivvk58d5z. Fonte em [`canvas/`](canvas/) (5 artboards 1280×800).

## Objetivo do usuário
Ver tudo o que mudou, de todos os módulos, em "antes → depois", descartar o que
não quer e gravar no save com segurança: backup à vista, aviso de jogo aberto e
uma mensagem clara quando algo falha.

## Telas
| # | Tela | Arquivo | Descrição |
| --- | --- | --- | --- |
| 1 | Revisar mudanças | [`Revisar.dc.html`](canvas/Revisar.dc.html) | por módulo e `ChangeSet`, antes → depois, descartar item/módulo/tudo, resumo, Aplicar (perigo) |
| 2 | Confirmar gravação | [`Confirmar.dc.html`](canvas/Confirmar.dc.html) | diálogo destrutivo: o que será gravado, onde fica o backup, aviso de fechar o jogo, estado "Gravando…" |
| 3 | Resultado | [`Resultado.dc.html`](canvas/Resultado.dc.html) | sucesso, caminho do backup, Abrir pasta, Copiar caminho, como restaurar, Continuar editando |
| 4 | Erro ao aplicar | [`ErroJogoAberto.dc.html`](canvas/ErroJogoAberto.dc.html) | modal de erro; faixa de prévia alterna entre os 18 `CoreError` |
| 5 | Nada para revisar | [`Vazio.dc.html`](canvas/Vazio.dc.html) | estado vazio |

Documentos: [`flows.md`](flows.md) · [`screens.md`](screens.md) · [`strings.md`](strings.md).
Todo artboard tem Claro/Escuro e PT/EN; Revisar e Confirmar são interativos
(descartar com Desfazer; "Gravando…").

## Decisões
1. **Revisão e resultado são páginas do shell; confirmação e erro são modal.** Um único lugar de revisão para todos os módulos.
2. **Total = nº de `ChangeSet`s**, não de patches (termo interno). `PatchCount` não aparece na UI.
3. **Descartar item e módulo: imediato + Desfazer (Toast 6 s). Descartar tudo: confirmação.**
4. **Aplicar é `Button.danger`** (vermelho, não âmbar): âmbar fica para seleção/rota/ação principal de leitura; gravação é destrutiva. Exige token novo `OnDangerColor`.
5. **Confirmação com foco inicial em Cancelar**, sem atalho que confirme; `Ctrl+Enter` na revisão só abre o diálogo.
6. **Erros ao aplicar aparecem no mesmo modal** (troca de conteúdo), sempre com "o que aconteceu + o que fazer", a garantia "nada foi gravado" e o destino das mudanças pendentes.
7. **Resultado como tela, não Toast** (diverge do padrão atual da skill `design-system`; precisa atualizar a skill).
8. Nomes de cidade/empresa/carga em texto do save, sem tradução; números e dinheiro pela cultura.
9. Mudanças do módulo Perfil (dinheiro) aparecem só como exemplo de agrupamento; o módulo não existe ainda.

## Perguntas em aberto
1. ~~`g_save_format "2"`~~ resolvido: o app decodifica saves binários; `Error_SaveFormatUnsupported` = "formato não reconhecido" (ver `design/shell/`).
2. **Pré-checagem do jogo aberto**: basta checar ao abrir o diálogo, ou a barra de sessão (UX-1) deve mostrar "jogo aberto" em tempo real (exige polling do `IGameProcessMonitor`)?
3. **Desfazer após descartar**: a App re-enfileira os `ChangeSet`s guardados via `Stage`; tudo bem para o módulo (a `Key` é a mesma)? Alternativa: sem desfazer.
4. **`SaveChangedOnDisk` perde as pendentes** (ADR 0005). Aceitável, ou vale oferecer "reaplicar sobre o novo arquivo" quando nenhum `OldValue` conflitar? (Mais complexo; fica fora desta versão.)
5. **Caminho do backup no diálogo**: o nome final só existe na gravação; mostramos o padrão (`game.sii.bak-AAAAMMDD-HHMMSS`). Aceito?
6. **Nota "grava em texto simples"** (risco da ADR 0002) fica no Resultado até a primeira aplicação ser validada no jogo.
7. **Abrir pasta** com o arquivo selecionado é por plataforma; em Linux (dev) só abre a pasta. Definir `IShellLauncher` na App ou Core?
8. A **lateral e a barra de sessão** dos mockups são simplificadas; alinhar com `design/shell/` (UX-1) quando existir.
9. **Nomes legíveis de carga/reboque** (`GameIds.Humanize`): os mockups usam nomes em inglês do jogo ("Concrete beams"); confirmar o formato real de saída.
