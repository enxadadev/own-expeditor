# Mapa do jogo — fluxos

```mermaid
flowchart TD
  A[Abrir save no módulo Fretes] --> B{Pasta do jogo configurada?}
  B -- não --> S[Mapa esquemático + dica 'Configure a pasta do jogo para ver o mapa']
  B -- sim --> C{Índice do mapa em cache e válido?\nchave = .scs do jogo + mods ativos}
  C -- sim --> M[Mapa geográfico]
  C -- não --> D[Montar índice em segundo plano]
  D --> P[Esquemático + 'Preparando o mapa do jogo… N%']
  D -- pronto --> M
  D -- versão de mapa desconhecida / erro --> E[Esquemático + aviso 'Não foi possível ler o mapa desta versão do jogo']
  M --> R{Saída e destino escolhidos?}
  R -- não --> V[Mapa com cidades de fundo, sem rota]
  R -- sim --> F{Fase 3 disponível e rota encontrada?}
  F -- sim --> G[Rota GPS sólida + distância calculada]
  F -- não --> L[Linha reta tracejada + distância do app]
```

Trocar mods ativos no perfil muda a chave do cache → novo índice na próxima abertura.
Cancelamento: fechar o save cancela a montagem; o que já foi lido não é gravado no cache.
