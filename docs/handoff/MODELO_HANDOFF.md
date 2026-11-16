# Modelo de handoff

Copie para `docs/handoff/ESTADO_ATUAL.md` ao terminar cada passo (substitui o anterior; o histórico
fica no git e no registro da sprint). Escreva para um agente que nunca viu esta conversa.

```markdown
# Estado atual — <data AAAA-MM-DD>

## Onde estamos
- Sprint / passo: <Sprint N — passo X: título>
- Branch: <nome> (base: main @ <sha curto>) · Último commit: <sha curto> <assunto>
- PR: <link ou "não aberto">
- Status: <concluído | parcial | bloqueado>

## O que foi feito neste passo
- <mudança> — arquivos: <caminhos>

## Evidências
- Testes: job <id> — total X, passou Y, falhou Z, ignorados W (categorias rodadas)
- Console: <0 erros / lista>
- Cena(s) tocadas: <nenhuma | quais>
- Save do jogador restaurado: <sim/não/não se aplica>

## Pendente / não verificado
- <item> — por quê

## Riscos abertos e decisões que dependem do humano
- <item>

## Próximo passo sugerido
- <passo> — arquivos prováveis: <caminhos> — critério de aceite: <...>

## Como retomar
1. `git fetch && git checkout <branch>`
2. Abrir o projeto no Unity 6000.3.10f1; aguardar import.
3. Rodar a suíte (ver AGENTS.md §6) e comparar com as evidências acima.
```
