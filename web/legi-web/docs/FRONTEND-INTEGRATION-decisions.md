# Integração Frontend ↔ Backend — Decisões de Arquitetura

Documento vivo com as decisões transversais da integração do frontend React (`web/legi-web`) com as APIs dos quatro serviços (Identity, Catalog, Library, Social). É a fonte de verdade para tudo que atravessa fases; cada fase tem seu próprio doc de implementação (ver §5).

**Convenção de linguagem:** código, identificadores e snippets em inglês; documentação em português.

**Status:** 📋 Planejamento. Nenhuma fase implementada.

---

## 1. Ponto de partida

O frontend está **UI-completo, 100% mockado e sem integração**:

- `src/services/` e `src/hooks/` estão **vazios**.
- Não há `AuthContext`, página de login/registro, nem guards de rota.
- `routes.tsx` expõe apenas `Layout` + 5 páginas (`/feed`, `/explore`, `/lists`, `/wishlist`, `/profile`), todas lendo de `mockFeedData.ts`, `mockCatalogData.ts`, `mockProfileData.ts`.

O backend está completo (4 serviços + mensageria outbox/inbox). O `docker-compose.yml` sobe tudo + RabbitMQ + o container web, e o nginx faz proxy de `/api/v1/social/` → `social-api:8080` corretamente.

Consequência: "conectar as peças" são três trabalhos empilhados — **auth → camada de dados → troca de mocks por chamadas reais, tela a tela**. Nada do feed/biblioteca renderiza sem auth, pois `/library` e `/feed` são `[Authorize]`.

---

## 2. Convenções de código

- **Estrutura feature-based** mantida: `features/{social,catalog,library}/`. Cada feature ganha `hooks/` (hooks de dados TanStack), `api/` (funções de chamada), `types/` (tipos que espelham os DTOs).
- **Camada HTTP compartilhada** em `services/http.ts`. Hooks de auth/contexto em `features/auth/` (nova feature).
- **Tipos espelham os DTOs do backend, não os mocks.** Os tipos atuais (`UserProfile` com `name`, `genres`, `isVerified`) serão reescritos para refletir os DTOs reais (ver §3.7). Os arquivos `mock*Data.ts` são removidos ao final de cada fase correspondente.

---

## 3. Decisões transversais

### 3.1 TanStack Query (adotado)

**Por quê:** o feed exige paginação + cache + curtidas otimistas + invalidação entre páginas; o perfil compõe dois serviços. Resolver isso à mão em `useEffect` apodrece rápido. TanStack paga o custo da dependência aqui.

**Padrões a seguir:**

- **`queryKeys` centralizados** numa factory por feature, evitando strings soltas:

```ts
export const feedKeys = {
  all: ["feed"] as const,
  list: () => [...feedKeys.all, "list"] as const,
};
export const libraryKeys = {
  all: ["library"] as const,
  byStatus: (status: ReadingStatus) => [...libraryKeys.all, status] as const,
};
```

- **Reads** com `staleTime` conservador (ex.: 30s).
- **Curtidas/follows** via `useMutation` com **optimistic update** (`onMutate` aplica, `onError` faz rollback, `onSettled` invalida).
- **Feed** com `useInfiniteQuery` sobre paginação offset (ver §3.6): `getNextPageParam` deriva a próxima página de `hasNext`/`page`.

### 3.2 Composição client-side (sem BFF)

Perfil e feed combinam dados de **Social + Library** no próprio browser (duas+ chamadas, merge no cliente). Aceito para um projeto de aprendizado.

**Tradeoff explícito:** múltiplos round-trips e ausência de camada de agregação. Se doer no futuro, um gateway/BFF entra no v2 — não antes (YAGNI).

### 3.3 Camada HTTP

`services/http.ts`: **uma única instância axios**, `baseURL: "/api/v1"`. Os prefixos de serviço (`identity/`, `catalog/`, `library/`, `social/`) entram na URL de cada chamada — o roteamento por prefixo é responsabilidade do nginx (prod) / proxy do Vite (dev), ver §3.5.

- **Request interceptor:** injeta `Authorization: Bearer <accessToken>` quando há sessão.
- **Response interceptor:** em `401`, tenta `POST /identity/auth/refresh` **uma vez**, repete a requisição original; se o refresh falhar, faz logout e redireciona para `/login`.
- **Erros:** o backend devolve `ProblemDetails` (RFC 7807). Mapear para um erro tipado e propagar (ver §3.6).

### 3.4 Autenticação

O backend devolve **access token + refresh token no corpo** da resposta (não em cookie). Portanto o frontend **precisa** persistir ambos no cliente.

- **Decisão v1:** `localStorage` para access + refresh token.
  **Tradeoff:** exposição a XSS. O endurecimento correto (refresh token em cookie `httpOnly`) exige suporte no backend e está no backlog v2 (§6).
- **`AuthContext`** expõe `{ user, isAuthenticated, login, logout }`. `user` é populado a partir do payload do login (`userId`, `email`, `username`) — sem display name (§3.7).
- **Bootstrap:** ao montar, se há token persistido, hidratar a sessão. Token expirado dispara o fluxo de refresh do interceptor.
- **Guard:** wrapper `RequireAuth` envolvendo as rotas protegidas; sem sessão → redireciona para `/login`.

Endpoints de auth: `POST /identity/auth/register`, `POST /identity/auth/login` (`{ emailOrUsername, password }`), `POST /identity/auth/refresh` (`{ refreshToken }`), `POST /identity/auth/logout` (`{ refreshToken }`, autenticado).

### 3.5 Proxy de desenvolvimento

Em produção o nginx roteia por prefixo. Em `npm run dev` **não há nginx** — sem proxy, toda chamada dá 404/CORS. O `vite.config.ts` ganha um proxy espelhando o nginx, para que dev e prod usem **URLs relativas idênticas** (`/api/v1/...`):

| Prefixo | Serviço (porta host) |
|---|---|
| `/api/v1/identity` | `localhost:5000` |
| `/api/v1/catalog` | `localhost:5112` |
| `/api/v1/library` | `localhost:5200` |
| `/api/v1/social` | `localhost:5300` |

### 3.6 Paginação, erros e estados de UI

- **Paginação: offset (implementação real).** Todas as queries paginadas retornam `PaginatedList<T> = { items, page, pageSize, totalItems, totalPages, hasNext, hasPrevious }`.
  ⚠️ **Drift de doc:** `SOCIAL-ARCHITECTURE-decisions.md §12.4` descreve paginação por **cursor** para o feed, mas `GetFeedQueryHandler`/`FeedItemReadRepository` implementam **offset**. O frontend segue a **implementação**. Correção do doc do backend registrada no backlog (§6).
- **Erros:** `ProblemDetails` → toast (ações) ou inline (formulários). `401` é tratado no interceptor; `400/403/404/409` exibidos ao usuário.
- **Estados:** toda query renderiza **loading / empty / error** explicitamente. Padronizar `Skeleton`, `EmptyState`, `ErrorState` reutilizáveis em `components/ui/`.

### 3.7 Decisões de produto desta rodada

| Item | Decisão v1 | Plano futuro |
|---|---|---|
| **Display name** | **Removido.** Backend só tem `Username`. UI usa `@username` como identificador único; greeting vira "Hi, {{username}}"; iniciais do avatar derivam do username. | v2/v3: `DisplayName` em `Identity.User` → eventos `UserRegistered`/`UserProfileUpdated` → snapshot em `Social.UserProfile`. |
| **Genres** | **Removido.** Remover a linha de tags no perfil e a seção "Your genres" no feed. | v2: coleção `genres` em `UserProfile` + campo em `UpdateProfileCommand`. |
| **Verified badge** | **Removido** (sempre foi mock). | — |
| **i18n pt-BR** | **Corrigir** `pt-BR.json`: seções `feed`/`explore`/`genres` ainda contêm strings em inglês. | — |

### 3.8 `FeedItemDto.Data` — união discriminada

`FeedItemDto.Data` é uma **string JSON** interpretada por `ActivityType`. Definir tipos discriminados no frontend e **um parser central** (não `JSON.parse` espalhado pelos componentes):

- `progress` → `{ percentage, page?, note? }`
- `finished` / `review` → `{ rating, text? }`
- `started` → `{ note? }`

Os shapes exatos devem ser confirmados inspecionando o payload real publicado pelo Library (na fase do feed).

---

## 4. Mapa UI → endpoints

| Tela / elemento | Serviço | Endpoint |
|---|---|---|
| Login / Registro | Identity | `POST /identity/auth/{login,register,refresh,logout}` |
| Greeting (feed) | Identity | `username` do payload de login |
| "Reading now" (feed) | Library | `GET /library?status=reading` |
| "Update progress" | Library | `POST /library/{userBookId}/posts` |
| Lista de atividades (feed) | Social | `GET /social/feed` → `PaginatedList<FeedItemDto>` |
| Curtir item do feed | Social | `POST`/`DELETE /social/posts/{id}/likes` |
| Comentários | Social | `GET`/`POST /social/posts/{id}/comments` |
| Follow | Social | `POST /social/follows` · `DELETE /social/follows/{userId}` |
| Perfil (cabeçalho) | Social | `GET /social/users/{userId}` → `UserProfileDto` |
| Perfil (booksRead) | Library | `GET /library?status=finished` → `totalItems` |
| Perfil (abas/contadores) | Library | `GET /library?status=X` · `GET /library/lists` |
| Editar perfil | Social | `UpdateProfileCommand` (bio, avatar, banner) |
| Wishlist | Library | `GET /library?wishlist=true` |
| Listas | Library | `GET /library/lists` |
| Explore (busca) | Catalog | `GET /books?search=&sortBy=&page=` |

Sort do Explore → `BookSortBy`: bestRated → `AverageRating`, mostRecent → `CreatedAt`, mostPopular → `RatingsCount`.

---

## 5. Sequência de fases (índice do doc set)

| Doc | Fase | Escopo | Serviços | Depende de |
|---|---|---|---|---|
| `00` (este) | Overview | Decisões transversais + backlog | — | — |
| `01` | **Foundation** | Proxy Vite, `http.ts`, setup TanStack, `AuthContext`, login/registro, `RequireAuth`, fix i18n | Identity | — |
| `02` | **Profile** | Composição Social+Library, `BookCard`/`UserBook` mapping, abas e contadores, `booksRead` | Social, Library | 01 |
| `03` | **Wishlist & Lists** | `GET /library?wishlist=true`, `GET /library/lists` | Library | 02 |
| `04` | **Feed** | `GetFeed` + `Data` discriminada + curtidas otimistas + comentários + "Reading now" + follow nos cards | Social, Library | 02 |
| `05` | **Explore** | Busca/sort/filtro no Catalog | Catalog | 01 |

**Racional da ordem:** `01` destrava tudo. `02` (Profile) estabelece os padrões de `BookCard`, paginação e composição reusados depois — e deixou de ser bloqueada por display name/genres (ambos descartados). `03` reusa esses padrões. `04` (Feed) é o mais complexo (união discriminada + otimismo + multi-serviço). `05` é uma feature isolada que só depende da fundação.

---

## 6. Backlog adiado (v2+)

| Item | Natureza | Nota |
|---|---|---|
| `DisplayName` | Domínio (Identity → eventos → Social) | UI usa `@username` no v1 |
| `genres` em `UserProfile` | Domínio (Social) | Some do perfil e do feed no v1 |
| Endpoint de follow suggestions | Backend (Social) | Seção "Suggestions" escondida no v1 |
| Trending com janela temporal | Backend (Catalog) | `BookSortBy` não tem dimensão de tempo; rotular "Popular" e ordenar por `RatingsCount` |
| Like/comment de **Review** | Backend (Social) | `InteractableType.Review` existe, mas só há endpoints para `posts`/`lists`. **Verificar** se itens "finished" no feed são `TargetType=Review` e como seriam curtidos |
| `GET /library/stats` | Backend (Library) | Contadores por status numa chamada; hoje os badges do perfil exigem N queries |
| Verified badge | Domínio | — |
| Refresh token em cookie `httpOnly` | Backend + Frontend | Endurecimento; substitui `localStorage` do §3.4 |
| Drift cursor vs offset no doc do Social | Doc | Corrigir `SOCIAL-ARCHITECTURE-decisions.md §12.4` |
| Identity `GetPublicProfile` | Backend (esclarecer) | `Stats { TotalBooks, TotalReviews }` não são populáveis dentro do Identity sem HTTP cross-service (viola decisão 2.1). Provável resíduo do design pré-refactor — aposentar ou redefinir |
