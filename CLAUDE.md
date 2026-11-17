# Own Expeditor

Editor de jobs (fretes) para Euro Truck Simulator 2 / American Truck Simulator,
inspirado no Virtual Speditor (forum.scssoft.com t=199114). Substitui job offers
existentes no save (`game.sii`) por origem, destino, carga e reboque escolhidos.

## Stack
- C# / .NET 8, UI em Avalonia (MVVM), testes com xUnit.
- Alvo principal: Windows (exe self-contained). Desenvolvimento em Linux.

## Camadas (dependências só apontam para baixo)
- `src/OwnExpeditor.App`     → UI Avalonia. Depende de Core.
- `src/OwnExpeditor.Core`    → casos de uso (ler save, listar opções, injetar job, backup).
- `src/OwnExpeditor.Data`    → perfis/saves no disco, cache de distâncias.
- `src/OwnExpeditor.Sii`     → parser/writer SII (texto e decrypt do formato binário/criptografado).
- `src/OwnExpeditor.Domain`  → entidades puras (Company, City, Cargo, Trailer, JobOffer). Sem dependências.

## Regras
- Nunca gravar um save sem antes criar backup ao lado (`game.sii.bak-<timestamp>`).
- Não ter listas fixas de cidades/cargas: tudo é lido do save (compatível com mods).
- O writer SII deve fazer round-trip: ler e gravar sem mudanças gera arquivo equivalente.
- Saves de exemplo ficam em `samples/` (não versionar saves pessoais grandes) e
  fixtures pequenos em `tests/**/Fixtures/`.
- Formato SII: documentar descobertas em `docs/sii-format.md` antes de codar o parser.
- Tema e idioma: a UI suporta tema Light/Dark (e Sistema) e idiomas pt-BR/en;
  nenhum texto visível fixo no código e nenhuma cor fixa (ver skill `i18n-theming`).

## Agentes
- Agentes e skills ficam em `ia/`; quem faz o quê e o fluxo estão em `ia/README.md`.

## Comandos
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/OwnExpeditor.App`
