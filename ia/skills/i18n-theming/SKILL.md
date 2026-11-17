---
name: i18n-theming
description: Como o Own Expeditor suporta tema (Light/Dark/Sistema) e idioma (pt-BR/en) — recursos .resx, troca em runtime, formatação por cultura, ThemeVariant, persistência da preferência. Use ao adicionar qualquer texto visível, cor ou tela (ui-dev, ux-reviewer, core-dev para configurações).
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
- Nomes vindos do save (cidades, empresas, cargas) **não** são traduzidos
  pelo app; exiba o nome legível derivado do id.
- Checagem: chave nova nos dois `.resx`; texto em inglês ~30% de folga no layout.

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
