---
name: save-safety
description: Regras obrigatórias para qualquer código que lê, grava ou substitui o game.sii do usuário no Own Expeditor — backup, escrita atômica, verificação antes de substituir, jogo aberto, encoding, recuperação. Use ao implementar ou revisar escrita de save (sii-engine, core-dev, code-reviewer).
---

# Save safety

Um bug aqui destrói horas de jogo do usuário. Nenhuma destas regras é opcional.

## Ao gravar
1. **Backup primeiro**: copie o original para `game.sii.bak-<yyyyMMdd-HHmmss>`
   no mesmo diretório; se a cópia falhar, **não grave**.
   Nunca sobrescreva um `.bak` existente (`overwrite: false`).
2. **Escrita atômica**: grave em `game.sii.tmp` no mesmo diretório,
   `Flush(true)`, e só então `File.Replace`/`File.Move(tmp, final, overwrite: true)`.
   Nunca escreva direto no arquivo final.
3. **Verifique antes de substituir**: releia o `.tmp`, faça parse e confira
   que (a) o parse funciona, (b) a quantidade de blocos é a esperada, (c) a
   mudança pretendida está lá. Falhou → apague o `.tmp`, mantenha o original.
4. **Formato**: hoje gravamos texto `SiiNunit`, UTF-8 **sem BOM**, mesma
   quebra de linha do original (o writer preserva). Não recriptografe.
5. **Jogo aberto**: se o jogo estiver rodando, avise — ele sobrescreve o
   save ao salvar/sair. Cheque o processo (`eurotrucks2`/`amtrucks`) e peça
   para fechar ou salvar e voltar ao menu.
6. **`info.sii`** ao lado não é alterado, a menos que a feature exija.
7. Retorne ao chamador o caminho do backup; a UI mostra ao usuário.

## Ao ler
- Nunca altere o arquivo ao ler (sem "normalizar" encoding/quebras).
- Erro de formato → exceção com mensagem clara; nunca documento parcial.
- Saves podem ter dezenas de MB: leia uma vez, sem cópias repetidas.

## Recuperação (documentar para o usuário)
Fechar o jogo → apagar/renomear `game.sii` → renomear o `.bak-...` mais
recente para `game.sii`.

## Testes exigidos (pedir ao test-engineer)
- Backup existe e é idêntico ao original após gravar.
- Falha simulada no meio da escrita → original intacto, sem `.tmp` sobrando.
- Round-trip sem mudanças gera texto idêntico.
- Diretório sem permissão → erro claro, original intacto.
