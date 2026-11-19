---
name: i18n-theming
description: Tema Light/Dark/Sistema e idioma pt-BR/en: .resx, troca em runtime, cultura, persistência; verifica com check-i18n.sh. Use ao adicionar texto visível, cor ou tela.
---

# Tema e idioma

Requisito do projeto: **todo** texto visível existe em pt-BR e en; **toda**
cor funciona em Light e Dark. Preferências do usuário são persistidas.

## Idioma
- Textos em `src/OwnExpeditor.App/Resources/Strings.resx` (en, neutro) e
  `Strings.pt-BR.resx`. Mesmas chaves nos dois; chave `Area_Elemento`
  (`JobEditor_InjectButton`, `Error_SaveLocked`).
- Na View: binding para o localizador, que notifica ao trocar de língua:
  ```xml
  <Button Content="{Binding [JobEditor_InjectButton], Source={x:Static loc:Localizer.Instance}}"/>
  ```
  (`Localizer` expõe indexer + `PropertyChanged("Item[]")` ao trocar `CultureInfo`).
- No ViewModel/Core: `ILocalizer.Get("Chave", args)`; o Core devolve
  **códigos de erro**, a App traduz — Core não depende de recursos de UI.
- Números, distâncias, dinheiro e datas: `ToString(format, CultureInfo.CurrentCulture)`.
  Nunca concatenar unidades à mão fora da string de recurso (`"{0:N0} km"`).
- Plural: chaves `Chave_Zero` (opcional), `Chave_One` e `Chave_Other`, usadas
  via `ILocalizer.GetPlural(baseKey, count, args)` (planejado, ADR 0006; hoje
  ainda não existe no código).
- Dinheiro: moeda fixa por jogo, EUR no ETS2 e USD no ATS, com a chave
  `Format_Money_<Jogo>` (`Format_Money_Ets2`, `Format_Money_Ats`). O número
  segue a cultura do app.
- Checagem: chave nova nos dois `.resx`; texto em inglês ~30% de folga no layout.

## Nomes do jogo (cidades, empresas, cargas, reboques, caminhões)
- Os nomes **vêm da localização do próprio jogo**, não dos `.resx` do app. O
  Core expõe `IGameNames.Get(GameNameKind, id)` (planejado, ADR 0006; hoje a
  App lê `GameCatalog` da Data direto, o que o ADR proíbe). `Get` nunca devolve
  `null`.
- Cadeia de fallback: **tradução** (`@@chave@@` do `locale.scs`) → **nome local**
  (`city_name`, ou o `name` da empresa) → **id legível** (`Domain.Games.GameIds.Humanize`).
- Idioma dos nomes: segue o idioma do app (pt-BR → `pt_br`; en → `en_us` ou
  `en_gb`), não o `config.cfg` do jogo. Hoje o código usa `g_lang_init` do
  jogo; isso muda com a ADR 0006.
- Os nomes do jogo entram no `DiffValue` como `DiffValue.GameName(kind, id)`
  e são resolvidos na hora de exibir, nunca gravados como texto traduzido.
- Não crie listas fixas de nomes: tudo vem do save e dos pacotes do jogo
  (ADR 0003).

## Tema
- `Application.RequestedThemeVariant` = `ThemeVariant.Light | Dark | Default` (sistema).
- Cores próprias só em `ThemeDictionaries` (`Light`/`Dark`) e consumidas com
  `{DynamicResource}`; `StaticResource` só para valores que não mudam com o tema
  (raios, espaços).
- Prefira recursos do Fluent (`SystemControl*`, `SystemAccentColor`) antes
  de criar cor nova.

## Preferências
- `AppSettings { Theme, Language }` em JSON no diretório de dados do usuário
  (`Environment.SpecialFolder.ApplicationData/OwnExpeditor/settings.json`),
  lido/gravado pela camada Data via interface do Core.
- Padrão na primeira execução: tema do sistema; idioma = cultura do SO se
  for pt-*, senão en.
- Troca em runtime, sem reiniciar o app.

## Checklist de PR
- [ ] Nenhum texto literal visível em `.axaml`/ViewModel.
- [ ] Chaves novas em ambos os `.resx`.
- [ ] Tela conferida em Light/Dark × pt-BR/en.
