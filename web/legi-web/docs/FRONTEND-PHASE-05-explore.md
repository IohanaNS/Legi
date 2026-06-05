# Frontend — Fase 05: Explore (Catalog) + Entrada na Biblioteca

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00). Depende das Fases 01–04 (concluídas). Última **tela**; ver §6 sobre a Fase 06.

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 0. Achados que moldam esta fase

1. **Terceiro shape de paginação.** `GET /catalog/books` devolve `SearchBooksResponse { books, pagination }` — **não** é `PaginatedList`. A `pagination` é `{ currentPage, pageSize, totalCount, totalPages, hasPrevious, hasNext }` (note `currentPage`/`hasPrevious`/`hasNext`, diferente do `pageNumber`/`hasNextPage` do Library/Social). Precisa de tipo próprio.
2. **"Gênero" = tags do Catalog.** O filtro de gênero é `tagSlug`; os chips vêm de `GET /catalog/tags/popular`. Não há enum de gênero — são tags `{ name, slug }`.
3. **Busca só casa título + ISBN** (não autor). O placeholder "título, autor ou gênero" promete demais. Ajustar para "título ou ISBN" no v1; busca por autor (via `authorSlug` + `authors/search`) fica adiada.
4. **Entrada na biblioteca:** `POST /library` adiciona o livro como `NotStarted` (ou wishlist). É o único ponto de entrada de livros — Explore completa esse loop.
5. **Gap do ciclo de vida (Fase 06):** depois de adicionar, o livro fica `NotStarted` e **não há UI para movê-lo** (status/rating/remover). As telas de perfil/wishlist são read-only. Isso é a Fase 06, não esta.

---

## 1. Objetivo e escopo

**Escopo:**
- Busca de livros (`GET /catalog/books`): campo de busca (debounced), ordenação, filtro por tag, grade de resultados, "carregar mais", contagem, estados.
- **Adicionar à biblioteca / wishlist** por card (`POST /library`), com tratamento de `409` (já na biblioteca).

**Fora de escopo (adiado):**
- "Recommended for you" (sem endpoint).
- Página de detalhe do livro + reviews (reviews são "planejado" no Catalog).
- Busca por autor no campo de texto (só `tagSlug`/`authorSlug` filtram; autor exige autocomplete).
- Mutações de ciclo de vida do livro (status/rating/remover) → **Fase 06**.

---

## 2. Contratos (confirmados)

| Operação | Endpoint | Auth | Retorno |
|---|---|---|---|
| Buscar livros | `GET /catalog/books?searchTerm=&tagSlug=&minRating=&pageNumber=&pageSize=&sortBy=&sortDescending=` | 🔓 | `SearchBooksResponse` |
| Tags populares | `GET /catalog/tags/popular` | 🔓 | lista de tags (⚠️ verificar shape) |
| Adicionar à biblioteca | `POST /library` body `{ bookId, wishlist }` | 🔒 | 201 `AddBookToLibraryResponse`; `409` se já existe |

**`SearchBooksResponse`**: `{ books: BookSummaryDto[], pagination: PaginationMetadata }`
**`BookSummaryDto`**: `{ bookId, isbn, title, authors: { name, slug }[], coverUrl?, averageRating, ratingsCount, tags: { name, slug }[] }`
**`PaginationMetadata`**: `{ currentPage, pageSize, totalCount, totalPages, hasPrevious, hasNext }`
**`AddBookToLibraryResponse`**: `{ userBookId, bookId, status, wishlist, createdAt }`

**`sortBy`** ∈ `Relevance | Title | AverageRating | RatingsCount | CreatedAt` + `sortDescending` (bool).

---

## 3. Decisões da fase

- **Tipo próprio para a resposta de busca** (§0.1); não tentar reusar `PaginatedList`.
- **Filtro de gênero = tags** (§0.2): chips de `tags/popular`, seleção define `tagSlug` (um por vez no v1).
- **Placeholder corrigido** para "título ou ISBN" (§0.3).
- **Mapa de ordenação UI → backend:** `bestRated`→`AverageRating`/desc, `mostRecent`→`CreatedAt`/desc, `mostPopular`→`RatingsCount`/desc, padrão `Relevance`/desc.
- **Add sem checar estado prévio:** a busca não informa o que já está na biblioteca; o botão tenta adicionar e trata `409` ("já está na sua biblioteca"). Evita N+1 de detalhe por card.

---

## 4. Implementação

### 4.1 Tipos — `src/features/catalog/types.ts` (novo/estender)

```ts
export type BookSortBy = "Relevance" | "Title" | "AverageRating" | "RatingsCount" | "CreatedAt";

export interface AuthorDto { name: string; slug: string; }
export interface TagDto { name: string; slug: string; }

export interface BookSummaryDto {
  bookId: string;
  isbn: string;
  title: string;
  authors: AuthorDto[];
  coverUrl?: string | null;
  averageRating: number;
  ratingsCount: number;
  tags: TagDto[];
}

export interface PaginationMetadata {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface SearchBooksResponse {
  books: BookSummaryDto[];
  pagination: PaginationMetadata;
}

export interface SearchBooksParams {
  searchTerm?: string;
  tagSlug?: string;
  minRating?: number;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: BookSortBy;
  sortDescending?: boolean;
}

// opções da UI de ordenação
export type SortOption = "bestRated" | "mostRecent" | "mostPopular";
```

### 4.2 Mapa de ordenação — `src/features/catalog/lib/sort.ts` (novo)

```ts
import type { BookSortBy, SortOption } from "../types";

export const sortOptionToBackend: Record<SortOption, { sortBy: BookSortBy; sortDescending: boolean }> = {
  bestRated:   { sortBy: "AverageRating", sortDescending: true },
  mostRecent:  { sortBy: "CreatedAt",     sortDescending: true },
  mostPopular: { sortBy: "RatingsCount",  sortDescending: true },
};
```

### 4.3 API — `src/features/catalog/api.ts` (novo)

```ts
import { http } from "../../services/http";
import type { SearchBooksParams, SearchBooksResponse, TagDto } from "./types";

export const catalogApi = {
  searchBooks: (params: SearchBooksParams) =>
    http.get<SearchBooksResponse>("/catalog/books", { params }).then((r) => r.data),
  // ⚠️ verificar o shape real de tags/popular
  getPopularTags: () =>
    http.get<TagDto[]>("/catalog/tags/popular").then((r) => r.data),
};
```

`libraryApi` (estender) — entrada na biblioteca:

```ts
import type { AddBookToLibraryResponse } from "./types"; // adicionar o tipo

export const addBookToLibrary = (bookId: string, wishlist: boolean) =>
  http.post<AddBookToLibraryResponse>("/library", { bookId, wishlist }).then((r) => r.data);
```

```ts
// features/library/types/index.ts (adicionar)
export interface AddBookToLibraryResponse {
  userBookId: string;
  bookId: string;
  status: string;
  wishlist: boolean;
  createdAt: string;
}
```

### 4.4 Query keys — `src/features/catalog/queryKeys.ts` (novo)

```ts
import type { SearchBooksParams } from "./types";

export const catalogKeys = {
  all: ["catalog"] as const,
  search: (p: SearchBooksParams) => [...catalogKeys.all, "search", p] as const,
  popularTags: () => [...catalogKeys.all, "tags", "popular"] as const,
};
```

### 4.5 Hooks

**Busca (infinite)** — `src/features/catalog/hooks/useSearchBooks.ts`:

```ts
import { useInfiniteQuery } from "@tanstack/react-query";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";
import { sortOptionToBackend } from "../lib/sort";
import type { SortOption } from "../types";

const PAGE_SIZE = 20;

export function useSearchBooks(args: { searchTerm?: string; tagSlug?: string; sort: SortOption }) {
  const { sortBy, sortDescending } = sortOptionToBackend[args.sort];
  const params = { searchTerm: args.searchTerm || undefined, tagSlug: args.tagSlug, sortBy, sortDescending, pageSize: PAGE_SIZE };

  return useInfiniteQuery({
    queryKey: catalogKeys.search(params),
    queryFn: ({ pageParam }) => catalogApi.searchBooks({ ...params, pageNumber: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.pagination.hasNext ? last.pagination.currentPage + 1 : undefined),
  });
}
```

**Tags populares** — `usePopularTags`: `useQuery(catalogKeys.popularTags(), catalogApi.getPopularTags)`.

**Adicionar à biblioteca** — `src/features/catalog/hooks/useAddToLibrary.ts`:

```ts
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { addBookToLibrary } from "../../library/api";
import { libraryKeys } from "../../library/queryKeys";

export function useAddToLibrary() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ bookId, wishlist }: { bookId: string; wishlist: boolean }) =>
      addBookToLibrary(bookId, wishlist),
    onSuccess: () => qc.invalidateQueries({ queryKey: libraryKeys.all }),
  });
}

export const isAlreadyInLibrary = (e: unknown) => isAxiosError(e) && e.response?.status === 409;
```

### 4.6 Componentes — reconstruir `ExplorePage`

`ExplorePage` (`features/catalog/components/`). Estado local: `searchTerm` (debounced ~300ms antes de virar query), `tagSlug` selecionada, `sort: SortOption` (padrão `mostPopular`).

- **`SearchBar`**: input com placeholder corrigido (`explore.searchPlaceholder` = "título ou ISBN"); debounce antes de atualizar o estado que alimenta `useSearchBooks`.
- **`SortDropdown`**: 3 opções (`bestRated`/`mostRecent`/`mostPopular`).
- **`TagFilter`**: chips de `usePopularTags`; clicar seta/limpa `tagSlug` (toggle, um por vez).
- **Contagem:** `data.pages[0]?.pagination.totalCount` → `explore.booksFound`.
- **Grade:** `data.pages.flatMap((p) => p.books)` em **`BookSummaryCard`**; "carregar mais" via `fetchNextPage`.
- **`BookSummaryCard`** (`book: BookSummaryDto`): capa (placeholder se null), título, `authors.map(a => a.name).join(", ")`, `StarRating(averageRating)` + `ratingsCount`, primeiras tags. Menu/botões **"Adicionar à biblioteca"** (`wishlist:false`) e **"Adicionar à wishlist"** (`wishlist:true`) → `useAddToLibrary`; em sucesso, toast/estado "adicionado"; em `409` (`isAlreadyInLibrary`), toast "já está na sua biblioteca".
- **Estados:** skeleton; empty ("Nenhum livro encontrado"); error com retry. "Recommended for you" **não** é renderizado (sem endpoint).

### 4.7 i18n — `en.json` e `pt-BR.json`

- Corrigir `explore.searchPlaceholder` → "Search by title or ISBN" / "Buscar por título ou ISBN".
- Adicionar: `explore.empty` ("Nenhum livro encontrado"), `explore.addToLibrary` ("Adicionar à biblioteca"), `explore.addToWishlist` ("Adicionar à wishlist"), `explore.added` ("Adicionado!"), `explore.alreadyInLibrary` ("Já está na sua biblioteca"), `explore.filterByTag` ("Filtrar por tag").
- A seção `genres` (i18n) fica sem uso aqui (gêneros viram tags do Catalog).

### 4.8 Limpeza

Remover `mockCatalogData.ts` e imports órfãos.

---

## 5. Critérios de aceitação (gate)

Com backend no ar e logado:

1. `npm run build` passa.
2. Buscar por um título retorna resultados; contagem = `pagination.totalCount`; "carregar mais" pagina.
3. Trocar a ordenação reordena (bestRated/mostRecent/mostPopular).
4. Selecionar um chip de tag filtra por `tagSlug`; limpar volta ao geral.
5. Cards mostram capa/título/autores/`averageRating`/tags.
6. **Adicionar à biblioteca** num livro novo → 201 e some/atualiza o estado do botão; reabrir o perfil mostra o livro em "NotStarted". **Adicionar à wishlist** → aparece na `/wishlist`.
7. Adicionar um livro já presente → `409` tratado com "já está na sua biblioteca" (sem erro cru).
8. Estados loading/empty/error presentes; `mockCatalogData.ts` removido.

---

## 6. Fim das telas — e a Fase 06

Esta é a última **tela** com mock. Mas o app ainda não fecha o ciclo de vida do livro:

- Adicionado via Explore, o livro fica **`NotStarted`** e não há UI para `Reading`/`Finished`/`Paused`/`Abandoned`, nem para avaliar ou remover.
- Endpoints prontos, **sem UI**: `PATCH /library/{id}` (status/wishlist/progress), `PUT /library/{id}/rating`, `DELETE /library/{id}/rating`, `DELETE /library/{id}` (remover), e os de lista (`POST /library/{id}/lists`, etc.).

**Fase 06 — Library management** (a verdadeira linha de chegada): ações de mutação nos cards já existentes (perfil/wishlist/reading-now) — mudar status, avaliar, remover, adicionar/remover de lista, editar perfil. É onde as telas read-only das Fases 02–03 ganham interatividade.

**Backlog (Doc 00 §6) — itens novos:**
- Fase 06: mutações de ciclo de vida (status/rating/remover/listas) + editar perfil.
- Busca por autor no Explore (autocomplete via `authors/search`).
- Detalhe do livro + reviews (quando o Catalog expor reviews).
- `tags/popular`: confirmar shape da resposta.
