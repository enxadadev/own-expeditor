# Guia do usuário

O Own Expeditor troca ofertas de frete que já existem no seu save por uma
origem, um destino, uma carga e um reboque que você escolhe. Nada é gravado
sem sua confirmação, e todo save gravado ganha um backup ao lado.

## Antes de começar

- Feche o Euro Truck Simulator 2 ou o American Truck Simulator antes de gravar.
  O app recusa gravar com o jogo aberto, porque o jogo sobrescreve o save ao
  salvar ou sair.
- Salve o jogo pelo menos uma vez num perfil. Sem save, a lista fica vazia.
- Para ver as cidades e as cargas, o app lê a pasta de instalação do jogo.
  Ele detecta a pasta sozinho (Steam, Windows ou Linux/Proton). Se não achar,
  escolha a pasta em **Configurações** (ver [abaixo](#configurações)).

## Como abrir

No Windows, use o `OwnExpeditor.App.exe` gerado por `ia/toolkit/release/release.sh` (ver
[development.md](development.md#publicar-para-windows)). Para rodar a partir do
código-fonte:

```bash
dotnet run --project src/OwnExpeditor.App
```

## Escolher o save

A tela inicial lista os **perfis** encontrados e, em cada um, os **saves**.

1. Escolha o perfil na coluna da esquerda.
2. Escolha o save na lista. Os saves são os slots do jogo, com o nome que você
   deu no jogo.
3. O app lê o save e abre o módulo **Fretes**. Saves grandes podem levar alguns
   segundos; a tela mostra o progresso e deixa você cancelar.

Se o app já abriu um save antes, o botão **Continuar de onde parou** volta a ele.
Se não encontrar perfil nenhum, escolha a pasta do jogo com **Escolher pasta…**
(a pasta de documentos que contém `Euro Truck Simulator 2` ou
`American Truck Simulator`).

Se o save não abrir, a tela de erro diz o que aconteceu e o que fazer (ver
[Problemas comuns](#problemas-comuns)). Nesses casos o seu save não foi alterado.

## Módulos e barra de sessão

A janela tem uma barra lateral com os módulos e uma barra de sessão no topo com
o perfil, o save e o estado do jogo (aberto ou fechado). Os módulos são:

| Módulo | Situação |
|--------|----------|
| Fretes | Disponível: cria mudanças de frete (descrito abaixo) |
| Frota, Perfil, Empresa, Mapa | Só espaço reservado; ainda não fazem nada |

Os módulos ficam desabilitados até você abrir um save.

## Criar um frete

Na página **Fretes** há dois blocos iguais, **Saída** e **Chegada**, com os
mesmos campos. Escolha, de cima para baixo:

1. **Saída**: **País**, **Cidade** e **Empresa**. Só aparecem países e cidades
   que têm uma empresa com vaga livre ou oferta para substituir.
2. **Vaga**: aparece só quando a empresa tem mais de uma opção. **Vaga livre (n)**
   usa uma vaga vazia (é o padrão quando há); cada **Substituir** troca uma oferta
   atual. Com uma única opção, o app escolhe sozinho e mostra **Única opção**.
3. **Chegada**: **País**, **Cidade** e **Empresa**. Só aparecem as empresas que
   recebem alguma carga que a empresa de saída envia. A cidade de saída nunca é
   opção de chegada.
4. **Carga**: só o que a empresa de saída envia e a empresa de chegada recebe.
5. **Prazo**: 24 h, 48 h, 72 h ou 7 dias de tempo de jogo, a partir do horário
   do save.

Abaixo fica a rota: as duas cidades, com a distância no mapa. Ele é o último
bloco da página; a barra de ação fica sempre visível.

Quando tudo estiver escolhido, use **Adicionar à revisão**. O frete vai para a
revisão e **ainda não é gravado**. Depois de adicionar, o app mantém a saída e
os países e cidades de chegada, para você montar outro frete entre as mesmas
cidades. Limpa a vaga, a empresa de chegada e a carga.

Você pode editar uma mudança pendente pelo botão **Editar** e descartá-la na
revisão. Na edição, a saída (país, cidade, empresa e vaga) fica travada: para
mudar a saída, descarte a pendência e crie outra. Destino, carga, empresa de
chegada e prazo continuam editáveis.

Notas sobre a distância:

- Quando o app conhece a distância pelas suas ofertas, ela aparece normal.
- Quando não conhece, mostra **≈ km** e marca como **estimada**. A estimativa
  é aproximada (erro típico perto de 25%). O jogo recalcula a rota no GPS.

### Reboque

O campo **Reboque** mostra o tipo do reboque em texto, com um ícone ao lado:

- Nome: o tipo de carroceria e o acabamento, como `Baú refrigerado` ou
  `Prancha baixa`. Quando a cadeia não é simples, ela entra no nome:
  `Sider · Rodotrem`.
- Detalhes, numa linha abaixo: eixos e comprimento, como `3+3 eixos · longo`.
  O que o app não reconhece não aparece aqui.
- Ícone: mostra o tipo de relance. Cadeias duplas levam `×2` e HCT leva `×3`.
  Reboque de mod que o app não conhece usa um ícone genérico e um nome derivado
  do id.
- Passe o mouse sobre o nome para ver o id do reboque (por exemplo,
  `scs.box.single_3.reefer`). É útil para identificar um reboque de mod.

Os mesmos nomes aparecem no seletor de carga, nas ofertas da lista e na revisão.
Na revisão, a linha de reboque mostra os detalhes entre parênteses, para que uma
troca só de comprimento não pareça sem efeito. Os ícones são só decoração: eles
não filtram nem reordenam as opções.

### Trocar origem e destino

Use **Trocar saída e destino** (⇄) para inverter os países e as cidades dos dois
lados. Empresas, vaga e carga são limpas, porque mudam com a saída. Se um campo
ficar com uma única opção, o app a escolhe sozinho.

Quando você troca um campo, o app limpa só o que deixou de valer e mostra um
aviso (por exemplo, a carga que a nova empresa de saída não envia). O que
continua valendo fica como está.

## Revisar e gravar

1. Abra a revisão com **Ctrl+R** ou pela barra de sessão. Ela mostra cada
   mudança com o valor **antes → depois**.
2. Você pode descartar uma mudança, descartar um módulo ou descartar tudo. O
   save não muda enquanto isso.
3. Use **Aplicar mudanças** (ou **Ctrl+Enter**). Abre uma confirmação. Nada é
   gravado antes dela.
4. Na confirmação, escolha o destino:
   - **Sobrescrever este save** (padrão): grava no save aberto. Um backup é
     criado antes.
   - **Criar um novo save**: copia a pasta do save para o próximo slot livre do
     perfil e grava a mudança ali. O save original não é tocado. Você pode dar
     um nome ao novo save.
5. Confirme. Todas as mudanças pendentes são gravadas de uma vez, com um único
   backup.

O app grava primeiro num arquivo temporário (`game.sii.tmp`), relê e confere o
conteúdo e só então troca o arquivo. Se qualquer etapa falhar, o save original
fica como estava.

Depois de gravar, a tela de resultado mostra onde está o backup (ou o novo
save), com os passos para desfazer.

### Atalhos

| Atalho | Ação |
|--------|------|
| Ctrl+O | Escolher outro save |
| Ctrl+R | Abrir a revisão |
| Ctrl+Enter | Abrir a confirmação de gravação (na revisão) |
| Ctrl+1 a Ctrl+5 | Ir para o módulo da barra lateral |
| Ctrl+, | Configurações |
| Esc | Voltar ou fechar |

Trocar de save ou sair com mudanças pendentes pede confirmação, porque descarta
as mudanças.

## Configurações

Em **Configurações** (Ctrl+,):

- **Tema**: Claro, Escuro ou Sistema. Muda na hora.
- **Idioma**: Português (Brasil) ou English. Muda na hora, sem reiniciar. Os
  nomes de cidades, empresas e cargas seguem a tradução do jogo.
- **Pasta de instalação do jogo**: detectada sozinha ou escolhida por você, para
  cada jogo (ETS2 e ATS). Uma pasta que não parece ser a instalação do jogo é
  recusada. Use **Usar detecção automática** para voltar ao padrão.

As preferências são salvas em `settings.json` na pasta de dados do app (no
Windows, `%AppData%\OwnExpeditor`). Se não der para salvar, o app avisa que a
preferência vale só até fechar.

## Backups

Toda gravação sobre um save cria um backup **na mesma pasta do save**, com este
nome:

```
game.sii.bak-20261117-153012
```

O nome é `game.sii.bak-` seguido da data e da hora da gravação
(`AAAAMMDD-HHMMSS`). O backup é uma cópia byte a byte do `game.sii` que existia
antes. O app nunca apaga backups e nunca sobrescreve um backup existente.

### Como restaurar

1. Feche o jogo.
2. Na pasta do save, renomeie `game.sii` para `game.sii.old`.
3. Renomeie o backup (`game.sii.bak-<data-hora>`) para `game.sii`.
4. Abra o jogo e carregue o save.

O app não tem botão de restauração. Os backups não são apagados por ele: remova
os que não precisar mais.

> TODO: confirmar no jogo que ele carrega o `game.sii` restaurado sem nenhum
> passo extra.

### Novo save

Um novo save não tem um `game.sii` original para copiar como backup: a pasta do
save de origem é copiada inteira para o próximo slot, e a mudança vai só para a
cópia. Para descartar o novo save, feche o jogo e apague a pasta dele (o caminho
aparece na tela de resultado).

## Onde ficam os saves

Os perfis ficam em uma destas pastas:

- Windows: `Documentos\Euro Truck Simulator 2\profiles\<perfil>\save\` (ou
  `American Truck Simulator`). Perfis do Steam Cloud ficam em `steam_profiles`.
- Linux nativo: `~/.local/share/Euro Truck Simulator 2\profiles\…` (ou a pasta
  do American Truck Simulator).
- Linux com Proton: dentro do prefixo da Steam, em
  `steamapps/compatdata/<appid>/pfx/drive_c/users/steamuser/Documents/`.

Cada save é uma pasta de slot (`save/manual1`, `save/quick`, `save/autosave…`)
com `game.sii` e `info.sii`. O app lista essas pastas; ele não cria nem apaga
perfis.

### Steam Cloud

Se o perfil usa Steam Cloud, a tela avisa. Há dois riscos:

- Ao sobrescrever, a nuvem pode trazer de volta o save antigo quando o jogo
  abrir. Se isso acontecer, feche o jogo e grave de novo.
- Um novo save criado pelo app pode não aparecer no jogo. Se isso acontecer,
  grave de novo com **Sobrescrever este save**.

> TODO: confirmar no jogo o comportamento com Steam Cloud (plano 0006, risco 1 e 2).

## Problemas comuns

As mensagens abaixo são as que o app mostra hoje.

| Mensagem (resumo) | O que significa | O que fazer |
|-------------------|-----------------|-------------|
| Não foi possível ler este save | O arquivo parece incompleto ou danificado, como se o jogo tivesse fechado durante o salvamento | Abra o salvamento automático mais recente do mesmo perfil, ou salve o jogo de novo |
| Não foi possível entender o save | O conteúdo não foi reconhecido (versão mais nova do jogo, mod ou arquivo danificado) | Tente outro save do mesmo perfil. O detalhe técnico fica em "Detalhes técnicos" |
| Save não encontrado | O arquivo foi movido, renomeado ou apagado | Volte à lista e escolha outro save |
| Sem permissão para ler ou gravar o save | Outro programa usa a pasta, ou ela é somente leitura | Feche o programa (antivírus, sincronização na nuvem) e tente de novo |
| O jogo está aberto | Gravar agora sobrescreveria as mudanças | Feche o jogo e clique em **Já fechei, tentar de novo** |
| O arquivo do save mudou depois que você o abriu | O jogo salvou depois da leitura; nada foi gravado para não perder progresso | Abra o save de novo e refaça as mudanças |
| Já existe um backup com este mesmo horário | Duas gravações no mesmo segundo | Espere um segundo e tente de novo. Nada foi gravado |
| Não foi possível criar o backup | Sem espaço ou sem permissão na pasta | Verifique o espaço e as permissões. Nada foi gravado |
| O arquivo gravado não passou na verificação | A conferência pós-gravação falhou | O save original foi mantido. Tente de novo |
| Não foi possível criar o novo save | O perfil não tem slot livre ou a pasta não pôde ser criada | Grave com **Sobrescrever este save**, ou apague um slot antigo |
| Não encontramos o item que você quis alterar | A cidade, empresa ou oferta sumiu do save | Descarte a mudança e escolha de novo |
| Este save não tem o campo que o app tentou alterar | Save de mod ou de outra versão do jogo | Descarte a mudança e tente outro item |
| Pasta do jogo não encontrada | Sem a pasta, o app usa só o que o próprio save mostra: não há campo País, as cidades vêm numa lista só com os nomes do save, e a lista pode ter menos opções | Escolha a pasta em **Configurações** |

Antes de gravar um save que você não copiou manualmente, faça uma cópia da
pasta do perfil por fora do app.
