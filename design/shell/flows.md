# Fluxos do shell

Ciclo geral: **explorar → editar → revisar → aplicar**. O shell cuida do começo (escolher e abrir o save) e do
fim (revisar/aplicar, em `design/review-apply/`, UX-2); os módulos cuidam de explorar e editar.

Nomes entre crases são os contratos do plano 0005 (`IEditSessionHost`, `CoreError`, `LoadProgress`...).

## 1. Abertura do app

```mermaid
flowchart TD
    A([App abre]) --> B[Carregar preferências<br/>IAppSettingsService.LoadAsync]
    B --> C{Primeira execução?}
    C -- sim --> D[Tema = Sistema<br/>idioma = SO pt-* ? pt-BR : en]
    C -- não --> E[Aplicar tema e idioma salvos]
    D --> F
    E --> F[Estado SEM SESSÃO<br/>módulos desabilitados]
    F --> G[IProfileService.GetProfilesAsync]
    G --> H{Perfis encontrados?}
    H -- não --> I[Vazio: Nenhum perfil encontrado<br/>ação: Escolher pasta…]
    H -- sim --> J{LastSavePath válido?}
    J -- sim --> K[Cartão Continuar de onde parou<br/>+ listas de perfis e saves]
    J -- não --> L[Listas de perfis e saves<br/>Abrir save desabilitado]
    I --> M[Usuário escolhe pasta]
    M --> G
    K --> N([Fluxo 2: abrir save])
    L --> N
```

Nada abre sozinho. Configurações está sempre disponível (fluxo 4).

## 2. Abrir um save

```mermaid
flowchart TD
    A([Abrir save<br/>botão, Enter na lista ou Abrir de novo]) --> B{Há sessão com pendências?}
    B -- sim --> C[Diálogo: Trocar de save?<br/>Descartar e trocar / Continuar editando]
    C -- Continuar editando --> Z([Fica como está])
    C -- Descartar e trocar --> D
    B -- não --> D[Close da sessão atual<br/>estado CARREGANDO]
    D --> E[IEditSessionHost.OpenAsync<br/>progresso LoadProgress]
    E --> F{Resultado}
    F -- cancelado --> G[Volta para SEM SESSÃO]
    F -- ok --> H[Estado SESSÃO ABERTA<br/>módulo Fretes, foco no módulo]
    F -- SaveFormatUnsupported --> I[ERRO: não foi possível entender o save<br/>arquivo lido, conteúdo não entendido]
    F -- SaveCorrupt --> J[ERRO: não foi possível ler o save<br/>incompleto ou ilegível, sugestões + detalhes]
    F -- SaveNotFound --> J2[ERRO: save não encontrado<br/>mesmo layout do corrompido]
    F -- AccessDenied --> J3[ERRO: sem permissão<br/>mesmo layout do corrompido]
    F -- outro --> J4[ERRO genérico<br/>OpenError_Unknown_*]
    I --> K{Usuário}
    J --> K
    J2 --> K
    J3 --> K
    J4 --> K
    K -- Tentar de novo --> D
    K -- Escolher outro save --> L[SEM SESSÃO com a lista]
    K -- Abrir pasta do save --> M[Abre a pasta no sistema] --> K
    K -- Copiar detalhes --> N[Detalhes técnicos na área de transferência<br/>botão vira Copiado] --> K
    L --> A
```

Notas:
- Falhou ao abrir: **não há sessão** (aprovado). O arquivo nunca é alterado ao ler.
- "Tentar de novo" relê o **mesmo** arquivo: ajuda depois de o jogo terminar de salvar, de uma permissão resolvida ou de uma
  versão nova do app. Em "não foi possível entender", a ação principal é escolher outro save; só essa tela tem Copiar detalhes.
- Cancelar vale em qualquer ponto de Carregando (`CancellationToken`); volta para Sem sessão.
- O foco vai para o título do erro (leitor de tela anuncia), depois Tab segue a ordem da tela.

## 3. Sessão aberta: do módulo à gravação

```mermaid
flowchart TD
    A([SESSÃO ABERTA<br/>módulo ativo]) --> B[Explorar e editar no módulo<br/>cada edição faz IEditSession.Stage]
    B --> C{PendingCount}
    C -- 0 --> D[Selo: Sem mudanças<br/>desabilitado]
    C -- n maior que 0 --> E[Selo: Revisar n<br/>âmbar, Ctrl+R]
    E --> F[Página Revisar do shell<br/>UX-2: antes → depois, descartar]
    F --> G{Aplicar? Ctrl+Enter abre a confirmação}
    G -- não --> B
    G -- sim --> H{Jogo aberto?}
    H -- sim --> I[Erro GameRunning<br/>Feche o jogo e tente de novo] --> F
    H -- não --> J[ApplyAsync: backup, grava, verifica]
    J -- ok --> K[Página Resultado do shell, com caminho do backup<br/>sessão limpa, selo volta a Sem mudanças]
    J -- SaveChangedOnDisk --> L[Erro: o save mudou no disco<br/>reabrir; pendências perdidas]
    J -- outro erro --> M[Erro por CoreError<br/>original intacto] --> F
    K --> B
    L --> N([Fluxo 2: abrir save])
    A -- Trocar módulo<br/>nav ou Ctrl+1…5 --> B
    A -- Perfil ou Save<br/>Ctrl+O --> O[Flyout de troca de save] --> P{Pendências?}
    P -- sim --> Q[Descartar e trocar?] --> N
    P -- não --> N
    A -- Fechar a janela --> R{Pendências?}
    R -- sim --> S[Sair sem aplicar?<br/>Descartar e sair / Continuar editando]
    R -- não --> T([Fecha])
    S -- Descartar e sair --> T
    S -- Continuar editando --> A
```

Mudar tema ou idioma (fluxo 4) nunca descarta nem altera as pendências.

## 4. Configurações

```mermaid
flowchart TD
    A([Nav: CONFIG. ou Ctrl+,]) --> B[Página Configurações<br/>aberta por cima do módulo atual]
    B --> C[Escolher tema: Claro, Escuro ou Sistema]
    B --> D[Escolher idioma: pt-BR ou English]
    B --> P[Pasta de instalação por jogo<br/>Escolher… ou Usar detecção automática]
    P --> Q{Pasta válida?}
    Q -- sim --> G
    Q -- não --> R[Erro inline Settings_GameFolder_Invalid<br/>mantém a anterior]
    C --> E[IThemeService.Apply imediato]
    D --> F[Localizer.SetLanguage imediato<br/>CultureChangedMessage]
    E --> G[IAppSettingsService.UpdateAsync]
    F --> G
    G -- ok --> H[Status: Preferência salva]
    G -- falha --> I[Erro inline: Settings_SaveFailed<br/>a troca continua valendo na sessão]
    B --> J([Voltar: Esc, outro módulo ou Ctrl+1…5])
```

- Sem botão Salvar nem Cancelar: cada escolha vale na hora e é salva.
- A pasta de instalação (ETS2/ATS) é detectada automaticamente ao abrir o app e quando o usuário clica em "Usar detecção
  automática"; só é salva se a validação passar. Sem pasta, o app funciona igual; só o mapa fica indisponível (uso futuro).
- Abrir Configurações **não** fecha a sessão; o módulo anterior volta como estava (seleção, rolagem).
- Sistema: o app segue o tema do SO em tempo real enquanto estiver nessa opção.

## 5. Estados do shell

```mermaid
stateDiagram-v2
    [*] --> SemSessao
    SemSessao --> Carregando: abrir save
    Carregando --> SemSessao: cancelar
    Carregando --> ErroLeitura: CoreError ao abrir
    Carregando --> SessaoLimpa: ok
    ErroLeitura --> Carregando: tentar de novo
    ErroLeitura --> SemSessao: escolher outro save
    SessaoLimpa --> SessaoComPendencias: Stage
    SessaoComPendencias --> SessaoLimpa: descartar tudo ou aplicar
    SessaoLimpa --> Carregando: trocar de save
    SessaoComPendencias --> Carregando: trocar de save, após confirmar
    SessaoLimpa --> SemSessao: fechar sessão
```

| Estado | Navegação | Barra de sessão | Selo | Área principal |
| --- | --- | --- | --- | --- |
| Sem sessão | módulos desabilitados | "Nenhum save aberto" | oculto | Abra um save (ou vazio) |
| Carregando | módulos desabilitados | perfil, save, jogo | oculto | progresso, etapas, Cancelar |
| Erro de leitura | módulos desabilitados | perfil, save (borda de erro), jogo | oculto | tela de erro |
| Sessão aberta, limpa | habilitada | perfil, save, jogo, estado do jogo | "Sem mudanças" (desabilitado) | módulo |
| Sessão aberta, pendências | habilitada | idem | "Revisar (n)" âmbar | módulo |
