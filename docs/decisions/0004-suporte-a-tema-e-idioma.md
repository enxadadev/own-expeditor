# 0004. Suporte a tema e idioma

- Status: aceita
- Data: 2026-11-17

## Contexto

O app é usado por jogadores de ETS2 e ATS que podem ter o sistema em tema
claro ou escuro, e em português ou inglês. Cores fixas no XAML e textos escritos
direto no código tornam a troca de tema ou de idioma cara de fazer depois. Além
disso, os textos ficam difíceis de revisar nos dois idiomas.

## Decisão

- **Tema**: Light, Dark ou Sistema (padrão). Cores próprias ficam em
  `ThemeDictionaries` (Light e Dark) e são consumidas com `DynamicResource`.
- **Idioma**: pt-BR e en, por arquivos `.resx` em
  `src/OwnExpeditor.App/Resources/` (`Strings.resx` em en e `Strings.pt-BR.resx`).
  Na primeira execução, o idioma é a cultura do SO se for pt-*, senão en.
- **Preferências**: tema e idioma ficam em `settings.json` dentro de
  `Environment.SpecialFolder.ApplicationData/OwnExpeditor/`. A camada `Data`
  lê e grava o arquivo, por uma interface definida no `Core`.
- **Core**: devolve códigos de erro, e a App traduz. O Core não depende de
  recursos de UI.
- **Troca em runtime**: tema e idioma mudam sem reiniciar o app.
- Nomes vindos do save (cidades, empresas, cargas) não são traduzidos.

## Consequências

- Todo texto visível vai para os dois `.resx`, e cada tela é conferida em Light,
  Dark, pt-BR e en. Isso aumenta o trabalho de cada tela nova.
- O `Core` fica independente de UI e pode ser testado sem Avalonia.
- O app mostra os nomes do save como estão. Não há tradução de cidades ou cargas.
- Nada disso está implementado ainda. Hoje só o tema do sistema está ligado
  (ver [overview](../architecture/overview.md#requisitos-transversais-tema-e-idioma)).
- Regras, exemplos e checklist estão na skill
  [i18n-theming](../../ia/skills/i18n-theming/SKILL.md).
