# Frontend — Fase 06A: Ciclo de Vida do Livro (mutações)

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00). Depende das Fases 01–05 (concluídas). Primeira das três fases de mutação (06A ciclo de vida, 06B listas, 06C editar perfil).

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 1. Objetivo

Dar interatividade aos cards de livro já existentes (abas do perfil, wishlist, "lendo agora"), fechando o ciclo de vida iniciado na Fase 05: um livro adicionado como `NotStarted` agora pode mudar de status, ser avaliado e ser removido.

**Escopo:** mudar status, avaliar (set/remover), remover da biblioteca — tudo sobre `UserBook`s que já existem.

**Fora de escopo:**
- Gerenciar listas (criar/editar/excluir, adicionar/remover livro de lista, página de detalhe) → **06B**.
- Editar perfil (bio/avatar/banner) → **06C**.
- Atualizar progresso já existe (Doc 04, "lendo agora").

---

## 2. Contratos (confirmados, `UserBooksController`)

| Operação | Endpoint | Body | Retorno |
|---|---|---|---|
| Mudar status/wishlist/progresso | `PATCH /library/{userBookId}` | `{ status?, wishlist?, progressValue?, progressType? }` | 200 (não usado — ver §3) |
| Avaliar | `PUT /library/{userBookId}/rating` | `{ stars }` (0.5–5.0, meia-estrela) | 200 |
| Remover avaliação | `DELETE /library/{userBookId}/rating` | — | 204 |
| Remover da biblioteca | `DELETE /library/{userBookId}` | — | 204 (soft delete) |

- `status` ∈ `NotStarted | Reading | Finished | Abandoned | Paused` (PascalCase — reusar o mapeamento da Fase 02).
- **Regra de domínio:** mover o status para qualquer coisa ≠ `NotStarted` zera `wishlist` no backend. O frontend não precisa fazer nada além de invalidar — a wishlist some sozinha.
- ⚠️ **Verificar** o shape de `UpdateUserBookResponse` — mas não dependemos dele (§3).

---

## 3. Decisões da fase

- **Invalidação, não otimismo.** Mudar status move o card entre abas e altera dois contadores (origem e destino); avaliar/remover também mexem em contagem. Patch otimista cross-aba é frágil. Aqui mutate → `invalidateQueries(libraryKeys.all)` é correto e simples (refetch de abas, contadores, wishlist e "lendo agora", todos sob `libraryKeys.all`). Trade-off aceito: um refetch em vez de update local instantâneo. Mostrar estado `isPending` no controle.
- **Resposta das mutações ignorada** — como invalidamos e refazemos a query, o body de retorno não é consumido (evita depender do shape exato).
- **Confirmação só para remover** (ação destrutiva).
- **Onde ficam as ações:** num menu (kebab) no `BookCard`, reusado em todas as telas que o renderizam (perfil/wishlist/lendo-agora). "Lendo agora" pode também expor um atalho "marcar como lido".

---

## 4. Implementação

### 4.1 API — `src/features/library/api.ts` (estender)

```ts
import type { BackendReadingStatus, ProgressType } from "./types";

export interface UpdateUserBookBody {
  status?: BackendReadingStatus;
  wishlist?: boolean;
  progressValue?: number;
  progressType?: ProgressType;
}

export const updateUserBook = (userBookId: string, body: UpdateUserBookBody) =>
  http.patch(`/library/${userBookId}`, body);

export const rateUserBook = (userBookId: string, stars: number) =>
  http.put(`/library/${userBookId}/rating`, { stars });

export const removeUserBookRating = (userBookId: string) =>
  http.delete(`/library/${userBookId}/rating`);

export const removeBookFromLibrary = (userBookId: string) =>
  http.delete(`/library/${userBookId}`);
```

### 4.2 Hooks — `src/features/library/hooks/useBookLifecycle.ts` (novo)

Todas invalidam `libraryKeys.all` no sucesso. Uma fábrica enxuta:

```ts
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  updateUserBook, rateUserBook, removeUserBookRating, removeBookFromLibrary,
} from "../api";
import { libraryKeys } from "../queryKeys";
import type { BackendReadingStatus } from "../types";

function useLibraryMutation<TArgs>(fn: (a: TArgs) => Promise<unknown>) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: fn,
    onSuccess: () => qc.invalidateQueries({ queryKey: libraryKeys.all }),
  });
}

export const useUpdateBookStatus = () =>
  useLibraryMutation(({ userBookId, status }: { userBookId: string; status: BackendReadingStatus }) =>
    updateUserBook(userBookId, { status }));

export const useRateBook = () =>
  useLibraryMutation(({ userBookId, stars }: { userBookId: string; stars: number }) =>
    rateUserBook(userBookId, stars));

export const useRemoveRating = () =>
  useLibraryMutation(({ userBookId }: { userBookId: string }) => removeUserBookRating(userBookId));

export const useRemoveBook = () =>
  useLibraryMutation(({ userBookId }: { userBookId: string }) => removeBookFromLibrary(userBookId));
```

### 4.3 Componentes

**`BookCardActions`** (novo, recebe `userBook: UserBookDto`) — menu kebab no `BookCard`:

- **Mudar status:** lista os 5 status (rótulos i18n `profile.status.*`, valor PascalCase); o atual fica marcado/desabilitado. Selecionar → `useUpdateBookStatus`. Durante `isPending`, desabilitar o menu.
- **Avaliar:** controle de estrelas editável (0.5–5.0, meia-estrela) → `useRateBook`. Se já há `ratingStars`, mostrar valor atual + opção "Remover avaliação" → `useRemoveRating`.
- **Remover da biblioteca:** abre confirmação → `useRemoveBook`.

Adicionar `<BookCardActions userBook={item} />` ao `BookCard` existente (Fase 02), visível **apenas no contexto do próprio usuário** (perfil próprio, wishlist, lendo-agora) — nunca em cards de atividade de outro usuário (Doc 04) nem em resultados do Explore (lá a ação é "adicionar", Fase 05).

> Como o `BookCard` é reusado em vários lugares, controlar a exibição das ações via prop (ex.: `editable?: boolean`) em vez de detectar contexto internamente.

**"Lendo agora"** (Doc 04): além de "atualizar progresso", expor atalho "marcar como lido" → `useUpdateBookStatus({ status: "Finished" })`.

### 4.4 i18n — `en.json` e `pt-BR.json`

Adicionar (mesclar em `profile`/`common`): `actions.changeStatus`, `actions.rate`, `actions.removeRating`, `actions.removeFromLibrary`, `actions.confirmRemove` ("Remover '{{title}}' da sua biblioteca?"), `actions.markAsFinished`. Garantir que `profile.status.{notStarted,reading,finished,paused,abandoned}` existam (vindos da Fase 02).

---

## 5. Critérios de aceitação (gate)

Com backend no ar e logado, com livros na biblioteca:

1. `npm run build` passa.
2. Mudar o status de um livro de `Reading` → `Finished`: ele sai da aba "Lendo" e aparece em "Lidos"; os contadores das duas abas se ajustam.
3. Mover um livro da **wishlist** para qualquer status: some da `/wishlist` (regra de domínio) — confirmar após invalidação.
4. Avaliar um livro: estrelas aparecem; "remover avaliação" volta ao estado sem nota.
5. Remover um livro: confirmação → some da lista; contadores ajustam.
6. "Lendo agora": "marcar como lido" transita para `Finished`.
7. As ações **não** aparecem em cards de atividade de outros usuários nem no Explore.
8. Estados `isPending`/erro presentes nos controles.

---

## 6. Decisões desta fase (resumo) + o que falta

- Invalidação (não otimismo) por causa do movimento cross-aba; resposta das mutações ignorada.
- Ações no `BookCard` via prop `editable`, restritas ao contexto do próprio usuário.

**Restante das mutações:**
- **06B — Listas:** criar/editar/excluir lista (`POST`/`PUT`/`DELETE /library/lists/...`), adicionar/remover livro de lista (`POST /library/{listId}/books`, `DELETE /library/{listId}/books/{userBookId}`), e a página de detalhe de lista (Apêndice A do Doc 03). Torna a aba "Listas" e a `/lists` interativas.
- **06C — Editar perfil:** `UpdateProfileCommand` (bio/avatar/banner) no perfil próprio. ⚠️ Verificar o shape do request DTO.
