# Frontend — Fase 03: Wishlist & Lists (Read-only)

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00). Depende das Fases 01–02 (concluídas).

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 0. Correção herdada — shape de `PaginatedList<T>`

O Doc 02 trazia nomes errados. O shape **real** (já correto no `types/index.ts` do repo) é:

```ts
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

Implicações para o código (já aplicadas na Fase 02): paginação usa `last.hasNextPage ? last.pageNumber + 1 : undefined`; contagem usa `totalCount`. **Doc 03 segue esses nomes.**

`GET /library/lists` é **array puro** de `UserListSummaryDto` (não paginado) — `getLists` já está tipado como `UserListSummaryDto[]`.

---

## 1. Objetivo e escopo

Substituir os placeholders de `/wishlist` e `/lists` por páginas reais, **somente leitura**, reaproveitando ao máximo o que a Fase 02 já entregou.

**Escopo:**
- **Wishlist** (`/wishlist`): cabeçalho + contagem, grade de `BookCard` a partir de `GET /library?wishlist=true`, infinite query / "carregar mais", estados loading/empty/error. Sem progresso, sem rating, **sem mutations**.
- **Lists index** (`/lists`): cabeçalho + contagem, `GET /library/lists`, grade de `ListCard`, estados. **Sem mutations.**

**Fora de escopo (adiado):**
- Adicionar/remover da wishlist, "começar a ler", criar/editar/excluir lista — todas writes; entram quando houver mockup.
- **Abrir uma lista** (`/lists/:listId`) — documentado no **Apêndice A (opt-in)**, **fora** do gate principal. Sem mockup, fica desligado.

> Trade-off honesto: o índice de listas read-only é um beco — clicar num `ListCard` não faz nada no escopo principal. Aceitável no v1; o clique vira o Apêndice A quando quisermos.

---

## 2. O que já existe (reuso da Fase 02)

| Artefato | Onde | Uso aqui |
|---|---|---|
| `libraryApi.getUserBooks` | `features/library/api.ts` | Wishlist (`{ wishlist: true }`) |
| `libraryApi.getLists` | `features/library/api.ts` | Lists index |
| `useLists` | `features/library/hooks/useLists.ts` | Lists index (sem mudança) |
| `ListCard` | `features/library/components/` | Lists index |
| `BookCard` | `features/library/components/` | Wishlist |
| `libraryKeys` | `features/library/queryKeys.ts` | chaves de query |
| estados (`Skeleton`/`EmptyState`/`ErrorState`) | `components/ui/` | ambas |

Novo nesta fase: **um hook (`useWishlist`)** e **a reconstrução das duas páginas**.

---

## 3. Implementação

### 3.1 Hook — `src/features/library/hooks/useWishlist.ts` (novo)

Infinite query sobre `getUserBooks({ wishlist: true })`. Mesmo padrão de `useLibraryBooks`, com o parâmetro `wishlist` no lugar de `status`.

```ts
import { useInfiniteQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useWishlist() {
  return useInfiniteQuery({
    queryKey: libraryKeys.books({ wishlist: true, pageSize: PAGE_SIZE }),
    queryFn: ({ pageParam }) =>
      libraryApi.getUserBooks({ wishlist: true, page: pageParam, pageSize: PAGE_SIZE }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
  });
}
```

A `libraryKeys.books` já aceita uma `LibraryQuery` arbitrária, então não precisa de chave nova.

### 3.2 Página — `WishlistPage` (`src/features/library/components/`, reconstruir)

- Itens: `data.pages.flatMap((p) => p.items)`.
- Contagem no cabeçalho: `data.pages[0]?.totalCount ?? 0`.
- Grade de `BookCard` reusando o mapeamento da Fase 02, **mas**: wishlist é sempre `NotStarted` → **omitir** barra de progresso e estrelas. Omitir também o badge de status (redundante numa página de wishlist).
- "Carregar mais": chama `fetchNextPage` quando `hasNextPage`.
- Estados: skeleton no carregamento; empty state ("Sua lista de desejos está vazia"); error com retry.

### 3.3 Página — `ListsPage` (`src/features/library/components/`, reconstruir)

- `const { data: lists, isLoading, isError } = useLists();`
- Contagem no cabeçalho: `lists?.length ?? 0`.
- Grade de `ListCard` (idêntico ao usado na aba "Listas" do perfil): nome, descrição, `booksCount`, badge de visibilidade. **Sem colagem de capas** (DTO não traz capas) e **não interativo** no escopo principal.
- Estados: skeleton; empty state ("Você ainda não criou nenhuma lista"); error com retry.

### 3.4 Limpeza

- Substituir os placeholders de `WishlistPage`/`ListsPage` e remover qualquer mock que estivessem usando.

### 3.5 i18n — `en.json` e `pt-BR.json`

Mesclar nas seções existentes (`nav` já tem os rótulos do menu):

```json
"wishlist": {
  "title": "Wishlist",                         // pt-BR: "Lista de Desejos"
  "count": "{{count}} books",                  // pt-BR: "{{count}} livros"
  "empty": "Your wishlist is empty"            // pt-BR: "Sua lista de desejos está vazia"
},
"lists": {
  "title": "Lists",                            // pt-BR: "Listas"
  "count": "{{count}} lists",                  // pt-BR: "{{count}} listas"
  "empty": "You haven't created any lists yet" // pt-BR: "Você ainda não criou nenhuma lista"
}
```

---

## 4. Critérios de aceitação (gate)

Com backend no ar e logado:

1. `npm run build` (`tsc -b`) passa.
2. `/wishlist` lista os livros com `wishlist = true`; contagem no cabeçalho = `totalCount`; cards sem progresso/rating/badge; "carregar mais" pagina; wishlist vazia mostra empty state.
3. `/lists` lista as listas do usuário; contagem = tamanho do array; `ListCard` mostra nome/descrição/contagem/visibilidade; sem listas → empty state.
4. Ambas mostram skeleton no carregamento e error state com retry.
5. Placeholders/mocks removidos; sem imports órfãos.

---

## 5. Decisões desta fase (resumo)

- Somente leitura; nenhuma mutation (add/remove wishlist, criar/editar lista) — adiado até haver mockup.
- Wishlist reusa `getUserBooks({ wishlist: true })` + `BookCard` (sem progresso/rating/badge, pois é tudo `NotStarted`).
- Lists index é quase todo reuso da Fase 02 (`useLists` + `ListCard`); `ListCard` não interativo no v1 (beco aceito).
- Nomes reais de `PaginatedList<T>` (`pageNumber`/`totalCount`/`hasNextPage`).

---

## Apêndice A — Detalhe de lista (opt-in, fora do gate)

Ative **apenas** se decidir que clicar numa lista deve abri-la. Read-only.

**Contratos:**
- `GET /library/lists/{id}` → metadados da lista. ⚠️ **Verificar** o shape (pode ser `UserListSummaryDto` ou um `UserListDetailDto` dedicado).
- `GET /library/lists/{id}/books` → `PaginatedList<UserListBookDto>` (confirmado).

**`UserListBookDto`** (confirmado): `userBookId, order, book (BookSnapshotDto), status, ratingStars?, addedAt`.

**Trabalho:**
1. Tipo `UserListBookDto` em `types/index.ts`.
2. `libraryApi.getList(id)` e `libraryApi.getListBooks(id, { page, pageSize })`.
3. `libraryKeys.list(id)` e `libraryKeys.listBooks(id)`.
4. Hooks `useList(id)` (query) e `useListBooks(id)` (infinite query — mesmo padrão, ordenar por `order`).
5. Rota `/lists/:listId` em `routes.tsx` (dentro de `RequireAuth`); `ListCard` vira `Link` para ela.
6. `ListDetailPage`: cabeçalho da lista (nome/descrição/visibilidade/contagem) + grade de `BookCard` a partir dos `UserListBookDto` (reusa o mapeamento; `book` + `status` + `ratingStars` quando presente).
7. Estados loading/empty/error; empty = "Esta lista está vazia".
