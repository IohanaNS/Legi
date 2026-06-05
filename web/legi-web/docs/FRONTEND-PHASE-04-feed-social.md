# Frontend — Fase 04: Feed + Interações + Grafo Social

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00). Depende das Fases 01–03 (concluídas). É a fase mais pesada — combina 04A (feed/interações) e 04B (grafo social) porque o feed nasce vazio sem um caminho para seguir pessoas.

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 0. Achados que moldam esta fase

1. **API de interação é resource-oriented, não polimórfica.** Existe rota só para `Post` (`/posts/{id}/...`) e `List` (`/lists/{id}/...`); cada controller fixa seu `InteractableType`. **`Review` não tem rota.** O frontend lê `targetType` de cada `FeedItemDto` em runtime e decide interatividade + recurso: `Post`→`posts`, `List`→`lists`, `Review`/`null`→**não-interagível** (sem botões de like/comment).
2. **Feed nasce vazio.** `GetFeed` só traz atividade de quem o usuário segue (e não inclui a própria). Sem um caminho de follow na UI, o feed fica vazio para sempre. Daí o grafo social entrar junto.
3. **Perfil de outro usuário ≠ perfil próprio.** `GET /library` é só do usuário autenticado (sem endpoint público de biblioteca alheia). O perfil público (`/users/:userId`) mostra a **atividade social** (`GET /social/users/{userId}/activity`), reusando o `FeedCard`. O perfil próprio (`/profile`) mantém as abas de biblioteca da Fase 02.
4. **Descoberta via lookup de username.** Sem busca de usuários nem suggestions no v1, a semente é `GET /identity/users/{username}` → `userId` → navegar para `/users/:userId`.
5. **Progresso `Page` não rende `%` no feed.** `FeedItemDto` não traz `pageCount`; só `Percentage` vira `%`. `Page` mostra "página N". Fix real (incluir `totalPages` no `Data`) adiado.

---

## 1. Objetivo e escopo

Tela de feed real + interações + o mínimo de grafo social para o feed ter conteúdo.

**Escopo:**
- Feed (`GET /social/feed`, infinite/offset), render por `activityType` com parser do `Data`.
- "Lendo agora" (biblioteca própria, reuso Fase 02) + **atualizar progresso** (write).
- Like/unlike **otimista** (via `targetType`→recurso).
- Comentários: ver (paginado) + adicionar.
- Follow/unfollow **otimista** + `IsFollowing`.
- Perfil público `/users/:userId` (header Social + follow + atividade).
- Listas de seguidores/seguindo.
- Descoberta por username (sidebar "Encontrar pessoas").

**Fora de escopo (adiado):**
- Editar perfil (write, ainda adiado).
- Suggestions e Trending na sidebar (sem endpoint / sem janela temporal — Doc 00 §6). Sidebar do feed = só "Encontrar pessoas".
- Abrir thread de comentário em página dedicada (fica inline sob o card).
- Like/comment em `Review` (sem rota).

---

## 2. Contratos (confirmados)

| Operação | Endpoint | Auth | Retorno |
|---|---|---|---|
| Feed | `GET /social/feed?page=&pageSize=` | 🔒 | `PaginatedList<FeedItemDto>` (offset) |
| Atividade de usuário | `GET /social/users/{userId}/activity?page=&pageSize=` | 🔓 | `PaginatedList<FeedItemDto>` |
| Like | `POST /social/{posts\|lists}/{id}/likes` | 🔒 | 201 |
| Unlike | `DELETE /social/{posts\|lists}/{id}/likes` | 🔒 | 204 |
| Comentários | `GET /social/{posts\|lists}/{id}/comments?page=&pageSize=` | 🔓 | `PaginatedList<CommentDto>` |
| Comentar | `POST /social/{posts\|lists}/{id}/comments` body `{ content }` | 🔒 | 201 `{ commentId }` |
| Follow | `POST /social/follows` body `{ followingId }` | 🔒 | 201 |
| Unfollow | `DELETE /social/follows/{userId}` | 🔒 | 204 |
| Seguidores/seguindo | `GET /social/users/{userId}/{followers\|following}?page=&pageSize=` | 🔓 | `PaginatedList<FollowUserDto>` |
| Perfil | `GET /social/users/{userId}` | 🔓 | `UserProfileDto` (Fase 02) |
| Lookup username | `GET /identity/users/{username}` | 🔓 | resolve `userId` (⚠️ verificar shape; usar só `userId`/`username`) |
| Lendo agora | `GET /library?status=Reading` | 🔒 | `PaginatedList<UserBookDto>` (reuso Fase 02) |
| Atualizar progresso | `POST /library/{userBookId}/posts` body `{ content?, progressValue?, progressType? }` | 🔒 | ⚠️ verificar shape do request DTO |

**`FeedItemDto`** (JSON camelCase): `id, actorId, actorUsername, actorAvatarUrl?, activityType, targetType?, referenceId, bookTitle?, bookAuthor?, bookCoverUrl?, data?, likesCount, commentsCount, isLikedByMe, createdAt`.

**`activityType`** ∈ `ProgressPosted | BookFinished | BookStarted | BookRated | ReviewCreated | ListCreated`.
**`targetType`** ∈ `"Post" | "Review" | "List" | null`.

**`Data` por `activityType`** (JSON; chaves nulas omitidas — ⚠️ confirmar/estender contra os 5 handlers da Fase 4C):
- `ProgressPosted` → `{ progress?, progressType?, content? }`
- `BookFinished` → `{ content?, rating? }`
- `BookRated` → `{ rating? }`
- `BookStarted` → `{ content? }`
- `ReviewCreated` → `{ content?, rating? }` (não-interagível no v1)
- `ListCreated` → `{ name?, description? }`

**`CommentDto`**: `{ id, userId, username, avatarUrl?, content, createdAt }`.
**`FollowUserDto`**: `{ userId, username, avatarUrl?, bio?, isFollowedByViewer }`.

> `PaginatedList<T>` aqui é o mesmo shape corrigido da Fase 03 (`items, pageNumber, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage`). ⚠️ Confirmar na 1ª chamada de feed (o construtor do Social é idêntico ao do Library).

---

## 3. Decisões da fase

- **Interatividade dirigida por `targetType` em runtime** (§0.1) — nunca hardcodar por `activityType`.
- **`activityType` escolhe o layout do card** (verbo + quais campos do `Data` ler); `targetType` escolhe interatividade + recurso.
- **Likes e follows otimistas** (rollback no erro); **comentários não-otimistas** (mutate → invalidate). Likes são a interação que precisa de snappiness; comentário tolera o refetch.
- **Perfil público = atividade Social** (§0.3), reusa `FeedCard`.
- **Sidebar mínima:** só "Encontrar pessoas". Suggestions/Trending/Genres fora.
- **Progresso `Page` sem `%`** (§0.5) — degrada para "página N".

---

## 4. Implementação

### 4.1 Tipos — `src/features/social/types.ts` (estender)

```ts
export type ActivityType =
  | "ProgressPosted" | "BookFinished" | "BookStarted"
  | "BookRated" | "ReviewCreated" | "ListCreated";

export type TargetType = "Post" | "Review" | "List";

export interface FeedItemDto {
  id: string;
  actorId: string;
  actorUsername: string;
  actorAvatarUrl?: string | null;
  activityType: ActivityType;
  targetType?: TargetType | null;
  referenceId: string;
  bookTitle?: string | null;
  bookAuthor?: string | null;
  bookCoverUrl?: string | null;
  data?: string | null; // JSON string
  likesCount: number;
  commentsCount: number;
  isLikedByMe: boolean;
  createdAt: string;
}

export interface CommentDto {
  id: string;
  userId: string;
  username: string;
  avatarUrl?: string | null;
  content: string;
  createdAt: string;
}

export interface FollowUserDto {
  userId: string;
  username: string;
  avatarUrl?: string | null;
  bio?: string | null;
  isFollowedByViewer: boolean;
}

// união discriminada do Data, após parse
export type ActivityData =
  | { kind: "ProgressPosted"; progress?: number; progressType?: "Page" | "Percentage"; content?: string }
  | { kind: "BookFinished"; rating?: number; content?: string }
  | { kind: "BookRated"; rating?: number }
  | { kind: "BookStarted"; content?: string }
  | { kind: "ReviewCreated"; rating?: number; content?: string }
  | { kind: "ListCreated"; name?: string; description?: string };
```

### 4.2 Parser do `Data` + mapa `targetType`→recurso — `src/features/social/lib/feed.ts` (novo)

```ts
import type { ActivityData, ActivityType, FeedItemDto, TargetType } from "../types";

/** Recurso REST para like/comment, ou null se não-interagível (Review/null). */
export function interactionResource(targetType?: TargetType | null): "posts" | "lists" | null {
  if (targetType === "Post") return "posts";
  if (targetType === "List") return "lists";
  return null; // Review e null → sem rota no v1
}

export const isInteractable = (item: FeedItemDto) => interactionResource(item.targetType) !== null;

/** Faz parse seguro do Data e o discrimina por activityType. */
export function parseActivityData(item: FeedItemDto): ActivityData {
  let raw: Record<string, unknown> = {};
  if (item.data) {
    try { raw = JSON.parse(item.data) as Record<string, unknown>; } catch { raw = {}; }
  }
  const t = item.activityType satisfies ActivityType;
  return { kind: t, ...raw } as ActivityData;
}

/** % de exibição do progresso, ou null se Page (sem pageCount no feed). */
export function feedProgressPercent(d: ActivityData): number | null {
  if (d.kind !== "ProgressPosted") return null;
  return d.progressType === "Percentage" && d.progress != null ? d.progress : null;
}
```

### 4.3 API

**`src/features/social/api.ts`** (estender):

```ts
import { http } from "../../services/http";
import type { CommentDto, FeedItemDto, FollowUserDto, UserProfileDto } from "./types";
import type { PaginatedList } from "../library/types";

type Resource = "posts" | "lists";

export const socialApi = {
  getUserProfile: (userId: string) =>
    http.get<UserProfileDto>(`/social/users/${userId}`).then((r) => r.data),

  getFeed: (page: number, pageSize: number) =>
    http.get<PaginatedList<FeedItemDto>>("/social/feed", { params: { page, pageSize } }).then((r) => r.data),

  getUserActivity: (userId: string, page: number, pageSize: number) =>
    http.get<PaginatedList<FeedItemDto>>(`/social/users/${userId}/activity`, { params: { page, pageSize } }).then((r) => r.data),

  like: (resource: Resource, id: string) => http.post(`/social/${resource}/${id}/likes`),
  unlike: (resource: Resource, id: string) => http.delete(`/social/${resource}/${id}/likes`),

  getComments: (resource: Resource, id: string, page: number, pageSize: number) =>
    http.get<PaginatedList<CommentDto>>(`/social/${resource}/${id}/comments`, { params: { page, pageSize } }).then((r) => r.data),
  addComment: (resource: Resource, id: string, content: string) =>
    http.post<{ commentId: string }>(`/social/${resource}/${id}/comments`, { content }).then((r) => r.data),

  follow: (followingId: string) => http.post("/social/follows", { followingId }),
  unfollow: (userId: string) => http.delete(`/social/follows/${userId}`),
  getFollowers: (userId: string, page: number, pageSize: number) =>
    http.get<PaginatedList<FollowUserDto>>(`/social/users/${userId}/followers`, { params: { page, pageSize } }).then((r) => r.data),
  getFollowing: (userId: string, page: number, pageSize: number) =>
    http.get<PaginatedList<FollowUserDto>>(`/social/users/${userId}/following`, { params: { page, pageSize } }).then((r) => r.data),
};
```

**`src/features/auth/api.ts`** (estender) — lookup de descoberta:

```ts
// ⚠️ verificar o response real; usar apenas userId/username
export const identityApi = {
  findUserByUsername: (username: string) =>
    http.get<{ userId: string; username: string }>(`/identity/users/${username}`).then((r) => r.data),
};
```

**`src/features/library/api.ts`** (estender) — atualizar progresso:

```ts
// ⚠️ verificar os campos reais do request DTO de POST /library/{id}/posts
export interface CreateReadingPostBody {
  content?: string;
  progressValue?: number;
  progressType?: "Page" | "Percentage";
}
export const createReadingPost = (userBookId: string, body: CreateReadingPostBody) =>
  http.post(`/library/${userBookId}/posts`, body);
```

### 4.4 Query keys — `src/features/social/queryKeys.ts` (estender)

```ts
export const feedKeys = {
  all: ["feed"] as const,
  list: () => [...feedKeys.all, "list"] as const,
  activity: (userId: string) => [...feedKeys.all, "activity", userId] as const,
};
export const interactionKeys = {
  comments: (resource: string, id: string) => ["comments", resource, id] as const,
  followers: (userId: string) => ["followers", userId] as const,
  following: (userId: string) => ["following", userId] as const,
};
```

### 4.5 Hooks

**Feed** — `src/features/social/hooks/useFeed.ts`:

```ts
import { useInfiniteQuery } from "@tanstack/react-query";
import { socialApi } from "../api";
import { feedKeys } from "../queryKeys";

const PAGE_SIZE = 20;

export function useFeed() {
  return useInfiniteQuery({
    queryKey: feedKeys.list(),
    queryFn: ({ pageParam }) => socialApi.getFeed(pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
  });
}

export function useUserActivity(userId: string) {
  return useInfiniteQuery({
    queryKey: feedKeys.activity(userId),
    queryFn: ({ pageParam }) => socialApi.getUserActivity(userId, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.pageNumber + 1 : undefined),
  });
}
```

**Like otimista** — `src/features/social/hooks/useToggleLike.ts` (a peça central). Parametrizada com a `queryKey` da lista exibida (feed ou atividade), para atualizar o cache certo:

```ts
import { useMutation, useQueryClient, type InfiniteData, type QueryKey } from "@tanstack/react-query";
import { socialApi } from "../api";
import { interactionResource } from "../lib/feed";
import type { FeedItemDto } from "../types";
import type { PaginatedList } from "../library/types";

type FeedCache = InfiniteData<PaginatedList<FeedItemDto>>;

function patchItem(data: FeedCache | undefined, id: string, patch: (i: FeedItemDto) => FeedItemDto) {
  if (!data) return data;
  return {
    ...data,
    pages: data.pages.map((p) => ({ ...p, items: p.items.map((it) => (it.id === id ? patch(it) : it)) })),
  };
}

export function useToggleLike(listKey: QueryKey) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (item: FeedItemDto) => {
      const resource = interactionResource(item.targetType);
      if (!resource) throw new Error("Not interactable");
      return item.isLikedByMe
        ? socialApi.unlike(resource, item.referenceId)
        : socialApi.like(resource, item.referenceId);
    },
    onMutate: async (item) => {
      await qc.cancelQueries({ queryKey: listKey });
      const prev = qc.getQueryData<FeedCache>(listKey);
      qc.setQueryData<FeedCache>(listKey, (d) =>
        patchItem(d, item.id, (it) => ({
          ...it,
          isLikedByMe: !it.isLikedByMe,
          likesCount: it.likesCount + (it.isLikedByMe ? -1 : 1),
        })),
      );
      return { prev };
    },
    onError: (_e, _item, ctx) => {
      if (ctx?.prev) qc.setQueryData(listKey, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: listKey }),
  });
}
```

**Comentários** — `src/features/social/hooks/useComments.ts`: `useInfiniteQuery` em `getComments(resource, id, ...)` (key `interactionKeys.comments`). `useAddComment`: `useMutation` → `onSuccess` invalida `interactionKeys.comments(resource,id)` **e** a lista de feed exibida (para `commentsCount` re-subir). Não-otimista.

**Follow otimista** — `src/features/social/hooks/useToggleFollow.ts`: `useMutation` que, no `onMutate`, faz patch do `UserProfileDto` em cache (`socialKeys.profile(userId)`): flip `isFollowing` e `followersCount ± 1`; rollback no erro; invalida no `onSettled`. `mutationFn`: `isFollowing ? unfollow(userId) : follow(userId)`.

**Lendo agora** — reusar `libraryApi.getUserBooks({ status: "Reading", page: 1, pageSize: 1 })` (basta o item mais recente). `useUpdateProgress`: `useMutation` em `createReadingPost` → `onSuccess` invalida a query de "lendo agora".

### 4.6 Componentes

- **`FeedPage`** (`features/social/components/`, reconstruir): coluna principal = `ReadingNowCard` + lista do `useFeed` (achatar `pages.flatMap(p => p.items)` em `FeedCard`, "carregar mais" via `fetchNextPage`). Sidebar = `FindPeople`. Empty state quando o feed vem vazio: "Você ainda não segue ninguém" + apontar para o `FindPeople`. Passar `feedKeys.list()` como `listKey` aos `FeedCard`.
- **`ReadingNowCard`** + **`UpdateProgressModal`**: card do livro atual (capa/título/autor + barra de progresso via `progressPercent` da Fase 02). Botão "Atualizar progresso" → modal (valor + tipo Page/Percentage + nota opcional) → `useUpdateProgress`.
- **`FeedCard`** (recebe `item: FeedItemDto` e `listKey: QueryKey`):
  - cabeçalho: `Avatar` + `@{actorUsername}` (Link para `/users/{actorId}`) + tempo relativo (`createdAt`) + verbo do `activityType` + `bookTitle`.
  - corpo por `parseActivityData(item)`:
    - `ProgressPosted` → barra/`%` se `feedProgressPercent` != null, senão "página N"; + `content`.
    - `BookFinished`/`ReviewCreated` → `StarRating(rating)` se presente + `content`.
    - `BookRated` → `StarRating(rating)`.
    - `BookStarted`/`ListCreated` → texto/nome.
  - **`InteractionBar`** (só se `isInteractable(item)`): botão like (estado `isLikedByMe`, contador) → `useToggleLike(listKey)`; botão comentário (contador) que expande `CommentThread`. Itens não-interagíveis (Review/null) não renderizam a barra.
- **`CommentThread`** (recebe `resource`, `id`, `listKey`): lista paginada (`useComments`) + input de novo comentário (`useAddComment`). Inline sob o card.
- **`UserProfilePage`** (`/users/:userId`, novo): header com `useUserProfile(userId)` (banner/avatar/`@username`/bio/contadores) + **`FollowButton`** (oculto se for o próprio usuário) + lista de atividade via `useUserActivity(userId)` reusando `FeedCard` (passar `feedKeys.activity(userId)` como `listKey`). Links para seguidores/seguindo (modal ou seção com `getFollowers`/`getFollowing`).
- **`FollowButton`** (recebe `userId`, `isFollowing`): `useToggleFollow`.
- **`FindPeople`** (sidebar): input de username → `identityApi.findUserByUsername` → navega para `/users/{userId}`; erro "usuário não encontrado" no 404.

### 4.7 Rotas — `src/app/routes.tsx` (editar)

Adicionar `/users/:userId` dentro do `RequireAuth`/`Layout`, apontando para `UserProfilePage`.

### 4.8 i18n — `en.json` e `pt-BR.json`

Adicionar chaves para: verbos de atividade (`activity.progressPosted`, `activity.bookFinished`, `activity.bookStarted`, `activity.bookRated`, `activity.reviewCreated`, `activity.listCreated`), `feed.empty` ("Você ainda não segue ninguém"), `feed.findPeople` ("Encontrar pessoas"), `feed.findPeoplePlaceholder`, `feed.userNotFound`, `feed.follow`/`feed.unfollow`, `feed.like`/`feed.comment`, `feed.addComment`, `feed.updateProgressModal.*`. Traduzir tudo em pt-BR.

### 4.9 Limpeza

Remover `mockFeedData.ts` e imports órfãos.

---

## 5. Critérios de aceitação (gate)

Com backend no ar, **dois usuários** (A segue B):

1. `npm run build` passa.
2. Logado como A: o feed mostra a atividade de B, paginada; "carregar mais" funciona.
3. Cards renderizam por tipo: progresso com `%` (se `Percentage`) ou "página N" (se `Page`); finished/review com estrelas + texto; started/list com texto. Itens `Review`/`null` **sem** barra de interação.
4. **Like otimista:** clicar curtir reflete na hora (ícone + contador); recarregar mantém; erro de rede faz rollback.
5. **Comentários:** abrir thread lista comentários; adicionar um faz o contador subir.
6. **Lendo agora:** mostra o livro atual de A; "atualizar progresso" cria o post e a barra atualiza.
7. **Grafo social:** abrir `/users/{B}` mostra perfil + atividade de B; botão seguir/deixar de seguir é otimista e mexe no contador; o próprio perfil não mostra o botão.
8. **Descoberta:** "Encontrar pessoas" com um username válido navega ao perfil; username inexistente mostra erro.
9. **Feed vazio:** um usuário que não segue ninguém vê o empty state apontando para "Encontrar pessoas".
10. `mockFeedData.ts` removido; estados loading/error presentes.

---

## 6. Decisões desta fase (resumo) + backlog

- Interatividade/recurso dirigidos por `targetType` em runtime; `Review`/`null` não-interagíveis.
- Likes/follows otimistas; comentários por invalidação.
- Perfil público usa atividade Social (não biblioteca alheia); perfil próprio mantém abas da Fase 02.
- Descoberta por lookup de username (Identity); sidebar só "Encontrar pessoas".

**Backlog atualizado (Doc 00 §6):**
- `Review` like/comment: precisa de `ReviewInteractionsController` no Social (rota inexistente) **e** que reviews do Catalog sejam integradas (hoje "planejado").
- `totalPages` no `Data` do feed (para `%` em progresso `Page`).
- Endpoint público de biblioteca por usuário (se um dia o perfil alheio mostrar abas de status).
- Busca de usuários / suggestions (substituir o lookup por username).
- Editar perfil (`UpdateProfileCommand`).
- Trending na sidebar (Catalog; sem janela temporal — rotular "Popular").
