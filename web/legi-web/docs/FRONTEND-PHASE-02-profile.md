# Frontend — Fase 02: Profile (Composição Social + Library)

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00). Depende da Fase 01 (concluída).

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 1. Objetivo

Substituir o mock do perfil por dados reais, compondo **Social** (cabeçalho, contadores de follow) + **Library** (abas de leitura, contadores, livros lidos). É a primeira tela com leituras autenticadas reais — estabelece os padrões (`BookCard` ↔ `UserBookDto`, paginação por infinite query, estados de loading/empty/error, composição client-side) reusados nas Fases 03–05.

**Escopo:** apenas o **próprio perfil** (`/profile`), somente leitura.

**Fora de escopo (adiado):**
- **Editar perfil** (botão "Edit profile") — é um `UpdateProfileCommand` (write) com form/modal; vira um follow-up pequeno.
- **Ver perfil de outros usuários** — exige rota nova (`/users/:userId`) e usa o flag `IsFollowing`; entra junto do feed/follows.
- **Display name, genres, verified badge** — descartados no v1 (Doc 00 §3.7).

---

## 2. Endpoints e contratos

| Dado | Endpoint | Retorno |
|---|---|---|
| Cabeçalho + follows | `GET /social/users/{userId}` | `UserProfileDto` |
| Livros por status | `GET /library?status=X&page=&pageSize=` | `PaginatedList<UserBookDto>` |
| Contador por status | `GET /library?status=X&pageSize=1` | `PaginatedList<UserBookDto>` (lê `totalItems`) |
| Listas | `GET /library/lists` | lista de `UserListSummaryDto` |

`userId` vem de `useAuth().user.userId`. Em perfil próprio, `IsFollowing` volta `false` (irrelevante aqui).

> ⚠️ **Verificar 2 shapes** antes de codar os tipos:
> 1. As propriedades de `PaginatedList<T>` no Library (o construtor é `new PaginatedList<>(items, totalCount, pageNumber, pageSize)`). O design abaixo assume JSON `{ items, page, pageSize, totalItems, totalPages, hasNext, hasPrevious }`. Conferir os nomes reais das propriedades.
> 2. Se `GET /library/lists` devolve **array** ou `PaginatedList`. O design assume array de `UserListSummaryDto`; se for paginado, desempacotar `.items`.

Contratos confirmados (records positionais → JSON camelCase):

- **`UserBookDto`**: `userBookId, bookId, status, progressValue?, progressType?, wishlist, ratingStars?, book, createdAt, updatedAt`
- **`BookSnapshotDto`**: `bookId, title, authorDisplay, coverUrl?, pageCount?`
- **`UserListSummaryDto`**: `listId, name, description?, isPublic, booksCount, likesCount, createdAt`
- **`UserProfileDto`**: `userId, username, bio?, avatarUrl?, bannerUrl?, followersCount, followingCount, isFollowing, createdAt`
- **`status`** ∈ `"NotStarted" | "Reading" | "Finished" | "Abandoned" | "Paused"` (PascalCase, `.ToString()` do enum)
- **`progressType`** ∈ `"Page" | "Percentage"`
- **`ratingStars`**: `decimal?` em **passos de meia-estrela** (0.5–5.0)

---

## 3. Decisões da fase

### 3.1 `booksRead` = contagem de `Finished` (fonte única)

Não existe contador denormalizado. O stat "Read" vem de `GET /library?status=Finished` → `totalItems`. **O mesmo número** alimenta o badge da aba "Lidos" — uma única fonte. (No mock, o stat dizia 143 e o badge "Lidos" dizia 5; isso era inconsistência de mock. Aqui são o mesmo valor.)

### 3.2 Contadores das abas — 4 count-queries (frontend-only)

O design mostra os 5 contadores de cara (Lendo/Lidos/Pausados/Abandonados/Listas). Sem endpoint de stats, a estratégia v1 é:

- 4 queries de contagem leves (`pageSize=1`, lê `totalItems`) para os status, em paralelo via `useQueries`.
- 1 query de listas (serve conteúdo da aba **e** o badge).
- A aba ativa carrega os itens reais (infinite query).

Total: ~7 queries paralelas (perfil + 4 contagens + listas + itens da aba ativa), cacheadas. **N fixo e pequeno, em paralelo — não é N+1 em loop.** Aceitável no v1.

> **FORK (decisão sua):** se o número de queries incomodar, a alternativa limpa é um endpoint **`GET /library/stats`** (counts por status numa chamada) — ~30 min de backend (query + handler + controller + read repo) que colapsa as 4 contagens em 1. Recomendo **shippar o frontend-only agora** e construir `/library/stats` quando medir que importa (já está no backlog do Doc 00 §6). Avise se preferir o endpoint primeiro.

### 3.3 Rating no card — pessoal, nunca média

`ratingStars` é meia-estrela (0.5–5.0). Um valor como **4.8 é impossível como rating pessoal** — só pode ser média global do Catalog. Portanto: o card mostra `ratingStars` **quando presente** (tipicamente livros finalizados); **omite** caso contrário. **Sem N+1 ao Catalog** para buscar médias. (Backlog se um dia quisermos média global no card.)

### 3.4 Progresso computado (Page vs Percentage)

`progressValue` + `progressType` → percentual de exibição:
- `Percentage`: o valor já é o %.
- `Page`: `% = round(value / pageCount * 100)` quando há `pageCount`; senão sem barra.

### 3.5 Mapeamento de status (PascalCase ↔ frontend)

Backend usa PascalCase (`"Finished"`); a UI/i18n usa as chaves de aba (`finished`). Centralizar a conversão nos dois sentidos (passo 4.3). Query param enviado em PascalCase (`?status=Finished`).

### 3.6 Outras

- **Listas sem colagem de capas:** `UserListSummaryDto` não traz capas. O card de lista mostra nome/descrição/`booksCount`/visibilidade — sem colagem (evita N+1 de `GET /library/lists/{id}/books` por lista). Adiado.
- **Conteúdo da aba** via `useInfiniteQuery` (pode haver muitos livros; ex.: 143 finalizados), `pageSize=20`.
- **Toggle grid/list** = estado local de UI.

---

## 4. Implementação

### 4.1 Tipos — reescrever `src/features/library/types/index.ts`

Remover os tipos mockados (`UserProfile` com `name`/`genres`/`isVerified`, `UserBook`/`UserList` com shapes inventados) e espelhar os DTOs reais. `ProfileTab`/`ViewMode` permanecem.

```ts
// ---- Library DTOs (camelCase JSON) ----
export interface PaginatedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export type BackendReadingStatus =
  | "NotStarted" | "Reading" | "Finished" | "Abandoned" | "Paused";
export type ProgressType = "Page" | "Percentage";

export interface BookSnapshotDto {
  bookId: string;
  title: string;
  authorDisplay: string;
  coverUrl?: string | null;
  pageCount?: number | null;
}

export interface UserBookDto {
  userBookId: string;
  bookId: string;
  status: BackendReadingStatus;
  progressValue?: number | null;
  progressType?: ProgressType | null;
  wishlist: boolean;
  ratingStars?: number | null; // 0.5–5.0, meia-estrela
  book: BookSnapshotDto;
  createdAt: string;
  updatedAt: string;
}

export interface UserListSummaryDto {
  listId: string;
  name: string;
  description?: string | null;
  isPublic: boolean;
  booksCount: number;
  likesCount: number;
  createdAt: string;
}

// chaves de aba na UI (i18n)
export type ProfileTab = "reading" | "finished" | "paused" | "abandoned" | "lists";
export type ViewMode = "grid" | "list";
```

### 4.2 Tipo de perfil — `src/features/social/types.ts` (novo)

```ts
export interface UserProfileDto {
  userId: string;
  username: string;
  bio?: string | null;
  avatarUrl?: string | null;
  bannerUrl?: string | null;
  followersCount: number;
  followingCount: number;
  isFollowing: boolean;
  createdAt: string;
}
```

### 4.3 Mappers — `src/features/library/lib/mappers.ts` (novo)

```ts
import type { BackendReadingStatus, ProfileTab, ProgressType } from "../types";

const TAB_TO_STATUS: Record<Exclude<ProfileTab, "lists">, BackendReadingStatus> = {
  reading: "Reading",
  finished: "Finished",
  paused: "Paused",
  abandoned: "Abandoned",
};

export const tabToStatus = (tab: Exclude<ProfileTab, "lists">) => TAB_TO_STATUS[tab];

export function progressPercent(
  value?: number | null,
  type?: ProgressType | null,
  pageCount?: number | null,
): number | null {
  if (value == null || type == null) return null;
  if (type === "Percentage") return value;
  if (type === "Page" && pageCount) return Math.round((value / pageCount) * 100);
  return null;
}
```

### 4.4 Funções de API

**`src/features/library/api.ts`** (novo):

```ts
import { http } from "../../services/http";
import type {
  BackendReadingStatus, PaginatedList, UserBookDto, UserListSummaryDto,
} from "./types";

export interface LibraryQuery {
  status?: BackendReadingStatus;
  wishlist?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const libraryApi = {
  getUserBooks: (q: LibraryQuery) =>
    http.get<PaginatedList<UserBookDto>>("/library", { params: q }).then((r) => r.data),
  // ⚠️ se /library/lists for paginado, trocar para PaginatedList e retornar r.data.items
  getLists: () =>
    http.get<UserListSummaryDto[]>("/library/lists").then((r) => r.data),
};
```

**`src/features/social/api.ts`** (novo):

```ts
import { http } from "../../services/http";
import type { UserProfileDto } from "./types";

export const socialApi = {
  getUserProfile: (userId: string) =>
    http.get<UserProfileDto>(`/social/users/${userId}`).then((r) => r.data),
};
```

### 4.5 Query keys

**`src/features/library/queryKeys.ts`** (novo):

```ts
import type { LibraryQuery } from "./api";
import type { BackendReadingStatus } from "./types";

export const libraryKeys = {
  all: ["library"] as const,
  books: (q: LibraryQuery) => [...libraryKeys.all, "books", q] as const,
  count: (status: BackendReadingStatus) => [...libraryKeys.all, "count", status] as const,
  lists: () => [...libraryKeys.all, "lists"] as const,
};
```

**`src/features/social/queryKeys.ts`** (novo):

```ts
export const socialKeys = {
  all: ["social"] as const,
  profile: (userId: string) => [...socialKeys.all, "profile", userId] as const,
};
```

### 4.6 Hooks

**`src/features/social/hooks/useUserProfile.ts`** (novo):

```ts
import { useQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { socialKeys } from "../queryKeys";

export function useUserProfile(userId: string | undefined) {
  return useQuery({
    queryKey: socialKeys.profile(userId ?? ""),
    queryFn: () => socialApi.getUserProfile(userId!),
    enabled: !!userId,
  });
}
```

**`src/features/library/hooks/useLibraryCounts.ts`** (novo) — 4 contagens paralelas; `finished` também é o stat "Read":

```ts
import { useQueries } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";
import type { BackendReadingStatus, PaginatedList, UserBookDto } from "../types";

const STATUSES: BackendReadingStatus[] = ["Reading", "Finished", "Paused", "Abandoned"];

export function useLibraryCounts() {
  const results = useQueries({
    queries: STATUSES.map((status) => ({
      queryKey: libraryKeys.count(status),
      queryFn: () => libraryApi.getUserBooks({ status, page: 1, pageSize: 1 }),
      select: (d: PaginatedList<UserBookDto>) => d.totalItems,
    })),
  });

  const counts: Partial<Record<BackendReadingStatus, number>> = {};
  STATUSES.forEach((s, i) => (counts[s] = results[i].data));
  const isLoading = results.some((r) => r.isLoading);

  return { counts, isLoading };
}
```

**`src/features/library/hooks/useLibraryBooks.ts`** (novo) — conteúdo da aba ativa, infinite:

```ts
import { useInfiniteQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";
import type { BackendReadingStatus } from "../types";

const PAGE_SIZE = 20;

export function useLibraryBooks(status: BackendReadingStatus) {
  return useInfiniteQuery({
    queryKey: libraryKeys.books({ status, pageSize: PAGE_SIZE }),
    queryFn: ({ pageParam }) =>
      libraryApi.getUserBooks({ status, page: pageParam, pageSize: PAGE_SIZE }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNext ? last.page + 1 : undefined),
  });
}
```

**`src/features/library/hooks/useLists.ts`** (novo):

```ts
import { useQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";

export function useLists() {
  return useQuery({ queryKey: libraryKeys.lists(), queryFn: libraryApi.getLists });
}
```

### 4.7 Componentes — reconstruir `ProfilePage`

`ProfilePage` (em `src/features/library/components/`) compõe os dois serviços. Reusar os componentes de UI existentes (`Avatar`, `Badge`, `BookCard`, `ProgressBar`, `StarRating`, `Card`). Estrutura:

- **`ProfileHeader`** — `bannerUrl` (placeholder se null), `Avatar` (iniciais do `username` se sem `avatarUrl`), `@{username}`, `bio`. **Sem** name/genres/verified.
- **`ProfileStats`** — Read = `counts.Finished`, Followers = `profile.followersCount`, Following = `profile.followingCount`.
- **`ProfileTabs`** — 5 abas com badges (`counts.Reading`, `counts.Finished`, `counts.Paused`, `counts.Abandoned`, `lists.length`); aba ativa = estado local (`ProfileTab`). Toggle grid/list = estado local (`ViewMode`).
- **Conteúdo:**
  - aba `lists` → grade de `ListCard` (a partir de `useLists`).
  - demais → `useLibraryBooks(tabToStatus(tab))`, achatando `data.pages.flatMap(p => p.items)` em `BookCard`s. Botão "carregar mais" chama `fetchNextPage` quando `hasNextPage`.

**`BookCard` ↔ `UserBookDto`:**
- capa: `book.coverUrl` (placeholder se null)
- título: `book.title`; autor: `book.authorDisplay`
- badge de status: traduzido via i18n (`profile.status.*`) a partir de `status`
- progresso: se `status === "Reading"`, usar `progressPercent(progressValue, progressType, book.pageCount)` → barra + `%` (omitir se null)
- estrelas: `StarRating` com `ratingStars` **somente se != null** (§3.3)

**`ListCard` ↔ `UserListSummaryDto`:** nome, descrição, `booksCount`, badge de visibilidade (`isPublic ? público : privado`). Sem colagem de capas (§3.6).

**Estados (Doc 00 §3.6):** skeleton no cabeçalho enquanto `useUserProfile` carrega; skeleton no conteúdo enquanto a aba carrega; empty state por aba ("Nenhum livro aqui ainda"); error state com retry.

### 4.8 Limpeza

- Remover `src/features/library/data/mockProfileData.ts` e qualquer import remanescente.

### 4.9 i18n — adicionar em `en.json` e `pt-BR.json`

```json
"profile": {
  "emptyTab": "No books here yet",      // pt-BR: "Nenhum livro aqui ainda"
  "errorLoading": "Couldn't load",      // pt-BR: "Não foi possível carregar"
  "loadMore": "Load more",              // pt-BR: "Carregar mais"
  "listVisibility": { "public": "Public", "private": "Private" } // pt-BR: "Pública"/"Privada"
}
```

(Mesclar com a seção `profile` existente — não substituir as chaves de `stats`/`tabs`/`status`.)

---

## 5. Critérios de aceitação (gate)

Com backend no ar e logado:

1. `npm run build` (`tsc -b`) passa.
2. `/profile` carrega dados reais: `@username`, bio, banner/avatar (ou iniciais), Followers/Following do Social.
3. **Stat "Read" === badge "Lidos"** (mesmo número — confirma a fonte única §3.1).
4. Badges das abas batem com a quantidade real por status; aba "Listas" mostra a contagem real.
5. Trocar de aba carrega os livros daquele status; aba vazia mostra empty state; "carregar mais" pagina.
6. Cards: capa/título/autor corretos; livro em leitura mostra `%`; livro finalizado mostra estrelas **só quando há rating**; nenhum valor estilo 4.8 (§3.3).
7. Aba "Listas" mostra nome/descrição/contagem/visibilidade.
8. **Refresh test (pendente da Fase 01):** logado, apagar `legi.accessToken` no DevTools (manter `legi.refreshToken`), recarregar `/profile` → as queries dão `401`, o interceptor faz refresh **uma vez** e tudo carrega, **sem deslogar**.
9. Skeletons e error states renderizam; `mockProfileData.ts` removido.

---

## 6. Decisões desta fase (resumo)

- Apenas perfil próprio, somente leitura; editar perfil e ver outros usuários adiados.
- `@username` no cabeçalho (sem name/genres/verified).
- `booksRead` = `totalItems` de `Finished`; fonte única para o stat "Read" e o badge "Lidos".
- Contadores via 4 count-queries paralelas (`pageSize=1`); `/library/stats` adiado (**fork** em §3.2).
- Rating no card é pessoal (`ratingStars`, meia-estrela) e só quando presente; sem N+1 ao Catalog para médias.
- Progresso computado (`Page` → `/pageCount`; `Percentage` → direto).
- Mapeamento de status PascalCase ↔ frontend centralizado.
- Cards de lista sem colagem de capas (DTO não traz capas); adiado.
- Conteúdo de aba via `useInfiniteQuery` (`pageSize=20`).
