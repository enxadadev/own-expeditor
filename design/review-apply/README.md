# Revisar e aplicar mudanças

- **Status:** aprovado (2026-11-17) — UX-2 de `docs/architecture/plan-0005-shell.md`. **Alteração em rascunho (2026-11-17):** destino da gravação escolhido pelo usuário na confirmação e no resultado, e novos erros (`SaveParseFailed`, `InvalidEdit`, `SlotUnavailable`), por [ADR 0006](../../docs/decisions/0006-integracao-do-codigo-de-injecao-ao-shell.md) e [plano 0006](../../docs/architecture/plan-0006-integracao.md) tarefa 0.1. Precisa de OK do usuário.
- **Canvas:** https://claude.ai/artifact/Sc4DPk3qimRg3ivvk58d5z. Fonte em [`canvas/`](canvas/) (5 artboards 1280×800; a confirmação, o resultado e o erro mudaram com a alteração acima).

## Objetivo do usuário
Ver tudo o que mudou, de todos os módulos, em "antes → depois", descartar o que
não quer e gravar no save com segurança: backup à vista, aviso de jogo aberto e
uma mensagem clara quando algo falha.

## Telas
| # | Tela | Arquivo | Descrição |
| --- | --- | --- | --- |
| 1 | Revisar mudanças | [`Revisar.dc.html`](canvas/Revisar.dc.html) | por módulo e `ChangeSet`, antes → depois, descartar item/módulo/tudo, resumo, Aplicar (perigo) |
| 2 | Confirmar gravação | [`Confirmar.dc.html`](canvas/Confirmar.dc.html) | diálogo destrutivo: aviso de fechar o jogo e de Steam Cloud, o que será gravado, **como gravar (Sobrescrever este save, padrão, ou Criar um novo save com nome editável)**, onde fica o backup / o novo save, estado "Gravando…". Barra de prévia: Steam Cloud, nome inválido, jogo aberto |
| 3 | Resultado | [`Resultado.dc.html`](canvas/Resultado.dc.html) | sucesso, **slot gravado (nome, slot, pasta)** e, se sobrescrita, o caminho do backup; Abrir pasta, Copiar caminho, como restaurar (ou como descartar o novo save), Continuar editando. Barra de prévia: Novo save, Steam Cloud |
| 4 | Erro ao aplicar | [`ErroJogoAberto.dc.html`](canvas/ErroJogoAberto.dc.html) | modal de erro; faixa de prévia alterna entre os 20 `CoreError` |
| 5 | Nada para revisar | [`Vazio.dc.html`](canvas/Vazio.dc.html) | estado vazio |

Documentos: [`flows.md`](flows.md) · [`screens.md`](screens.md) · [`strings.md`](strings.md).
Todo artboard tem Claro/Escuro e PT/EN; Revisar e Confirmar são interativos
(descartar com Desfazer; escolher o destino; "Gravando…").

## Decisões
1. **Revisão e resultado são páginas do shell; confirmação e erro são modal.** Um único lugar de revisão para todos os módulos.
2. **Total = nº de `ChangeSet`s**, não de patches (termo interno). `PatchCount` não aparece na UI.
3. **Descartar item e módulo: imediato + Desfazer (Toast 6 s). Descartar tudo: confirmação.**
4. **Aplicar é `Button.danger`** (vermelho, não âmbar): âmbar fica para seleção/rota/ação principal de leitura; gravação é destrutiva. Exige token novo `OnDangerColor`.
5. **Confirmação com foco inicial em Cancelar**, sem atalho que confirme; `Ctrl+Enter` na revisão só abre o diálogo.
6. **Erros ao aplicar aparecem no mesmo modal** (troca de conteúdo), sempre com "o que aconteceu + o que fazer", a garantia "nada foi gravado" e o destino das mudanças pendentes.
7. **Resultado como tela, não Toast** (diverge do padrão atual da skill `design-system`; precisa atualizar a skill).
8. Nomes de cidade, empresa, carga e reboque vêm da **localização do jogo** (ADR 0006; sem a pasta do jogo, do id legível); números e dinheiro pela cultura. (Antes: "texto do save, sem tradução".)
9. Mudanças do módulo Perfil (dinheiro) aparecem só como exemplo de agrupamento; o módulo não existe ainda.
10. **O destino da gravação é escolha do usuário, na confirmação** (ADR 0006): **Sobrescrever este save** (padrão; backup `game.sii.bak-<timestamp>`) ou **Criar um novo save** (nome editável, 1 a 40 caracteres; o original não é tocado; vai para o próximo slot livre do perfil). Duas opções em cartões de rádio, sem pré-escolher a destrutiva por engano: o padrão é a que já era o comportamento aprovado.
11. **Steam Cloud**: perfil com nuvem mostra um aviso `Warning` na confirmação, com texto por destino (sobrescrever: a nuvem pode restaurar o save antigo; novo save: o jogo pode não mostrar o slot novo). Não bloqueia nenhuma das opções; o mesmo aviso se repete no Resultado em uma linha.
12. **Os avisos (jogo aberto e Steam Cloud) vão para o topo do diálogo**, acima do que será gravado, e a lista "O que será gravado" fica compacta (um bloco por módulo com os títulos numa linha): o diálogo ganhou conteúdo e o aviso crítico não pode ficar escondido pela rolagem. O corpo rola; os botões ficam fixos.
13. **Resultado mostra o slot gravado sempre** (nome, nº do slot, pasta) e o backup só quando sobrescreveu. Depois de criar um novo save a sessão passa a apontar para ele (nova base): a barra do shell mostra o nome novo e "Continuar editando" edita o novo save.

## Perguntas em aberto
1. ~~`g_save_format "2"`~~ resolvido: o app decodifica saves binários. `Error_SaveFormatUnsupported` foi **renomeada `Error_SaveParseFailed`** ("conteúdo não reconhecido"; ADR 0006 item 3; ver `design/shell/`).
2. **Pré-checagem do jogo aberto**: basta checar ao abrir o diálogo, ou a barra de sessão (UX-1) deve mostrar "jogo aberto" em tempo real (exige polling do `IGameProcessMonitor`)?
3. **Desfazer após descartar**: a App re-enfileira os `ChangeSet`s guardados via `Stage`; tudo bem para o módulo (a `Key` é a mesma)? Alternativa: sem desfazer.
4. **`SaveChangedOnDisk` perde as pendentes** (ADR 0005). Aceitável, ou vale oferecer "reaplicar sobre o novo arquivo" quando nenhum `OldValue` conflitar? (Mais complexo; fica fora desta versão.)
5. **Caminho do backup no diálogo**: o nome final só existe na gravação; mostramos o padrão (`game.sii.bak-AAAAMMDD-HHMMSS`). Aceito?
6. **Nota "grava em texto simples"** (risco da ADR 0002) fica no Resultado até a primeira aplicação ser validada no jogo.
7. **Abrir pasta** com o arquivo selecionado é por plataforma; em Linux (dev) só abre a pasta. Definir `IShellLauncher` na App ou Core?
8. A **lateral e a barra de sessão** dos mockups são simplificadas; alinhar com `design/shell/` (UX-1) quando existir.
9. **Nomes legíveis de carga/reboque** (`GameIds.Humanize`): os mockups usam nomes em inglês do jogo ("Concrete beams"); confirmar o formato real de saída.
10. **Nome do novo save**: o limite de 1 a 40 caracteres foi **aprovado pelo usuário**. Resta confirmar com o `core-dev` quais caracteres o jogo aceita e se o nome vai só em `info.sii`. Nome repetido de outro save do perfil: bloquear ou deixar duplicar?
11. **Perfil sem slot livre** (`SlotUnavailable`): o desenho só avisa depois de tentar. Dá para o Core dizer antes (`ProfileInfo` com slots livres) e desabilitar "Criar um novo save" com a explicação?
12. **Backup no destino "novo save"**: não há backup, porque o original não muda. Aceito, ou criar um backup mesmo assim?
13. **Desfazer após criar o novo save** é descartar o save novo (apagar a pasta, com o jogo fechado). O app não oferece apagar; só explica. Vale um botão "Abrir pasta" que já mostra a pasta (existe) e nada além disso?
14. ~~**Dados do mockup de Revisar**~~ resolvido pelo módulo Fretes: com **Substituir uma oferta** o diff mostra antes → depois (como no mockup); com **Usar vaga livre** o antes é "—" (`Diff_Empty`). A distância estimada aparece como "≈ 1.330 km" (`Format_KmApprox`). Ver `design/fretes/README.md`.
