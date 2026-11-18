---
name: large-save-performance
description: Técnicas para ler, editar e gravar saves grandes (8+ MB) sem travar a UI — medição, alocação, streaming, índices. Use quando houver lentidão, ao mexer no parser/writer ou em listas grandes.
---

# Performance com saves grandes

Referência: save de texto real tem ~8 MB, ~6.500 `job_offer_data`, ~1.900
`company`, ~9.000 `economy_event`.

## Meça antes de otimizar
- Tempo: `Stopwatch` em teste/console sobre um save de `ia/toolkit/inspect/samples.sh --text`.
- Para comparar alternativas, um projeto BenchmarkDotNet em `ia/toolkit/` (fora
  da solução). Registre o número antes/depois no PR.
- Metas iniciais: abrir + parse < 1 s; gravar < 1 s; filtro de lista < 50 ms.

## Leitura/parse
- Leia o arquivo uma vez (`File.ReadAllBytes`), decodifique uma vez.
- Varra com `ReadOnlySpan<char>`; crie `string` só para o que fica no modelo.
- Índices por id e por tipo (`Dictionary`) criados durante o parse, não depois.
- Evite LINQ em laço quente sobre todos os blocos.

## Escrita
- `StringBuilder` com capacidade ≈ tamanho original; um único `Write`.
- Não re-serialize o que não mudou se o writer suportar (preserva round-trip).

## UI
- Parse/gravação em `Task.Run` com progresso (`IProgress<T>`) e cancelamento.
- Listas virtualizadas; filtro com debounce; projeções leves (`record` de
  exibição) em vez de expor blocos do SII à View.

## Memória
- Não mantenha texto original + documento + cópias ao mesmo tempo sem
  necessidade; libere o texto após o parse se o writer não precisar dele.
