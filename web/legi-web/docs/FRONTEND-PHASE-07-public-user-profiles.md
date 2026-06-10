# Frontend — Fase 07: Perfis Públicos Ricos

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00). Depende das Fases 01–06A (concluídas ou em andamento), porque reusa autenticação, feed, perfil próprio, cards de livro e ações de ciclo de vida.

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 0. Estado atual verificado

Hoje existem duas experiências separadas:

- `/profile` usa a página rica do perfil próprio (`ProfilePage`), com cabeçalho, stats, abas, livros por status, listas e atividade.
- `/users/:userId` usa `UserProfilePage`, uma página pública mais simples, com cabeçalho, follow e atividade.
- `/users/:userId/read` já existe e usa `ReadBooksPage` para mostrar livros `Finished` de qualquer usuário.
- `GET /library/users/{userId}/books?status=&page=&pageSize=` já existe e retorna `PaginatedList<UserBookDto>`.

Surpresas importantes:

- A rota pública v1 continua sendo por `userId`, não por `username`.
- A aplicação React inteira ainda está dentro de `RequireAuth`, então o escopo desta fase é **logado apenas**.
- `GET /library/lists` retorna um array do usuário autenticado e inclui listas privadas; não pode ser usado para outro usuário.
- ⚠️ **Vazamento ativo de dados privados (corrigir em PR próprio, antes desta fase — ver §3.4 e §9):** `GET /library/lists/{listId}` e `GET /library/lists/{listId}/books` estão `[AllowAnonymous]`, não recebem viewer context e não validam visibilidade. Qualquer chamador — **inclusive deslogado** — lê detalhe e livros de uma lista **privada** sabendo apenas o `listId` (IDOR). É um bug que já existe hoje, independente desta fase.
- Library `PaginatedList<T>` usa `pageNumber`, `totalCount`, `hasNextPage`; Social `PaginatedList<T>` usa `page`, `totalItems`, `hasNext`.

---

## 1. Objetivo e escopo

Transformar o perfil de outro usuário em uma experiência rica, quase igual ao perfil próprio:

- cabeçalho com banner/avatar/bio/username;
- stats de lidos, seguidores e seguindo;
- botão follow/unfollow para outros usuários;
- abas `Activity`, `Reading`, `Read`, `Paused`, `Abandoned`, `Not Started`, `Lists`;
- livros por status com progresso/nota/status;
- listas públicas;
- atividade com likes/comments preservados.

Diferença central: quando `targetUserId !== viewerUserId`, tudo que pertence à biblioteca, listas e perfil do outro usuário é **read-only**. O frontend esconde ações de edição, mas a proteção real continua no backend pelos comandos que recebem `ActorId`/`UserId` e validam dono.

**Fora de escopo:**

- Rotas por username (`/users/:username`) e redirects após mudança de username.
- Perfil público para visitante deslogado.
- Privacidade avançada de perfil/biblioteca.
- Bloqueios entre usuários.
- Editar perfil/listas/livros de outro usuário, em qualquer forma.

---

## 2. Decisões da fase

### 2.1 Manter `/users/:userId` no v1

Todas as fontes atuais já navegam por `userId`: busca global, cards do feed, comentários, seguidores/seguindo e perfil público. Username é melhor UX no futuro, mas exige contrato de lookup e política de rename. Esta fase não abre esse eixo.

Rotas finais:

| Rota | Uso |
|---|---|
| `/profile` | Perfil próprio, modo editável |
| `/users/:userId` | Perfil rico de outro usuário, modo read-only |
| `/users/:userId/read` | **Mantida** como deep link dedicado (ver §5.4); a aba `Read` é a experiência principal |
| `/users/:userId/followers` | Lista de seguidores |
| `/users/:userId/following` | Lista de seguindo |

Se `/users/:userId` apontar para o usuário logado, renderizar em modo próprio ou redirecionar para `/profile`. Preferência: redirecionar para manter uma URL canônica para o próprio perfil.

### 2.2 Composição client-side, sem BFF

Continuar a decisão do Doc 00: o browser compõe Social + Library.

- Social: cabeçalho de perfil, follow state, followers/following, atividade.
- Library: livros, contadores por status, progresso, rating, finished date, listas.
- Catalog: só é acessado quando o usuário clica no livro e vai para `/books/{bookId}`.

Não criar endpoint agregador cross-service nesta fase.

### 2.3 Um shell compartilhado com permissões

Evitar manter duas páginas de perfil divergentes. Criar uma camada de composição compartilhada que recebe `targetUserId`, `viewerUserId` e permissões calculadas.

Modelo recomendado:

```ts
interface ProfilePermissions {
  isOwnProfile: boolean;
  canEditProfile: boolean;
  canEditLibrary: boolean;
  canEditLists: boolean;
  canFollow: boolean;
  canReactToActivity: boolean;
}
```

Valores:

| Permissão | Próprio perfil | Outro usuário |
|---|---:|---:|
| `isOwnProfile` | `true` | `false` |
| `canEditProfile` | `true` | `false` |
| `canEditLibrary` | `true` | `false` |
| `canEditLists` | `true` | `false` |
| `canFollow` | `false` | `true` |
| `canReactToActivity` | `true` | `true` |

`canReactToActivity` fica `true` para outro usuário porque read-only aqui significa "não editar os dados do dono"; likes/comments continuam sendo interação social normal.

### 2.4 `UserBookDto` pode ser reutilizado

Não criar DTO separado para leitura pública de livro. `UserBookDto` já contém o necessário para renderizar o card e o `userBookId` não é segredo: todos os comandos de escrita precisam validar dono no backend. Reusar o DTO reduz mapeamento duplicado.

### 2.5 Listas públicas para outros usuários

Perfil próprio vê todas as listas. Perfil de outro usuário vê somente listas `IsPublic = true`.

List detail/books devem aplicar a mesma regra:

- dono pode ver lista privada;
- outro usuário só pode ver lista pública;
- lista inexistente ou privada sem permissão deve responder como not found/forbidden de forma consistente com o restante da Library.

---

## 3. Contratos de API

### 3.1 Social existente

| Dado | Endpoint | Auth | Retorno |
|---|---|---|---|
| Perfil | `GET /social/users/{userId}` | opcional hoje, usado logado no v1 | `UserProfileDto` |
| Atividade | `GET /social/users/{userId}/activity?page=&pageSize=` | opcional hoje, usado logado no v1 | `SocialPaginatedList<FeedItemDto>` |
| Follow | `POST /social/follows` | auth | `FollowResponse` |
| Unfollow | `DELETE /social/follows/{userId}` | auth | `204` |
| Seguidores | `GET /social/users/{userId}/followers?page=&pageSize=` | auth no frontend | `SocialPaginatedList<FollowUserDto>` |
| Seguindo | `GET /social/users/{userId}/following?page=&pageSize=` | auth no frontend | `SocialPaginatedList<FollowUserDto>` |

`UserProfileDto`: `userId, username, bio?, avatarUrl?, bannerUrl?, followersCount, followingCount, isFollowing, createdAt`.

### 3.2 Library existente

| Dado | Endpoint | Auth | Retorno |
|---|---|---|---|
| Livros do usuário autenticado | `GET /library?status=&wishlist=&search=&page=&pageSize=` | auth | `PaginatedList<UserBookDto>` |
| Livros de qualquer usuário | `GET /library/users/{userId}/books?status=&page=&pageSize=` | auth | `PaginatedList<UserBookDto>` |
| Listas do usuário autenticado | `GET /library/lists` | auth | `IReadOnlyList<UserListSummaryDto>` |

`GET /library/users/{userId}/books` já captura `ViewerUserId` no query object para futuras regras de visibilidade/bloqueio, mas hoje qualquer usuário autenticado pode ler.

### 3.3 Library a adicionar

| Dado | Endpoint | Auth | Retorno |
|---|---|---|---|
| Stats da biblioteca | `GET /library/users/{userId}/stats` | auth | `UserLibraryStatsDto` |
| Listas visíveis de um usuário | `GET /library/users/{userId}/lists?page=&pageSize=` | auth | `PaginatedList<UserListSummaryDto>` |

`UserLibraryStatsDto` sugerido:

```csharp
public record UserLibraryStatsDto(
    int Reading,
    int Finished,
    int Paused,
    int Abandoned,
    int NotStarted,
    int Lists
);
```

Regras:

- Para `TargetUserId == ViewerUserId`, `Lists` conta todas as listas.
- Para outro usuário, `Lists` conta somente listas públicas.
- Os status contam livros ativos do target, sem soft-deleted.
- O endpoint não precisa contar wishlist, porque wishlist não aparece no perfil público.

`GET /library/users/{userId}/lists`:

- Para o próprio usuário, pode retornar todas as listas.
- Para outro usuário, retorna somente `IsPublic`.
- Usar paginação nova para evitar uma lista pública grande travar o perfil.
- Ordenação recomendada: `UpdatedAt desc`, igual ao padrão atual de `GetByUserIdAsync`.

### 3.4 Endurecer list detail/books (fix de segurança, PR independente)

Esta correção **não depende do perfil rico** e deve sair primeiro (ver §9). Atualizar:

- `GET /library/lists/{listId}`
- `GET /library/lists/{listId}/books?page=&pageSize=`

Mudanças:

- **Remover `[AllowAnonymous]`** dos dois endpoints (deixar `[Authorize]`). O app é logado-apenas; `[AllowAnonymous]` + checagem de dono é contraditório (um chamador anônimo não tem viewer para autorizar).
- Passar viewer context:

```csharp
new GetListDetailsQuery(listId, viewerUserId)
new GetListBooksQuery(listId, viewerUserId, page, pageSize)
```

Regra:

- se a lista não existe: `NotFoundException`;
- se `IsPublic`: liberar;
- se `OwnerId == ViewerUserId`: liberar;
- caso contrário (lista privada de outro): `NotFoundException`. **Preferência v1: `NotFoundException`, não `ForbiddenException`** — `Forbidden` confirma que a lista existe e permite enumeração; `NotFound` esconde a existência. Se quiser facilitar diagnóstico em dev, logue a decisão, mas responda `NotFound` ao cliente.

---

## 4. Backend — implementação

### 4.1 User library stats

Adicionar em Library Application:

- `UserBooks/Queries/GetUserLibraryStats/GetUserLibraryStatsQuery.cs`
- `GetUserLibraryStatsQueryHandler.cs`
- `UserLibraryStatsDto.cs` em `Common/DTOs` ou na pasta da query.

Adicionar no read repository:

- `IUserBookReadRepository.GetStatusCountsByUserIdAsync(...)`, ou um método dedicado que já retorne `UserLibraryStatsDto`. **Computar todos os status em uma única query (`GROUP BY status`), não cinco counts.** Caso contrário, apenas movemos o padrão N-queries do cliente (hoje `useLibraryCounts` dispara 5 requests) para o servidor.
- `IUserListReadRepository.CountVisibleByUserIdAsync(targetUserId, viewerUserId, ...)`.

Controller:

```csharp
[HttpGet("users/{userId:guid}/stats")]
public async Task<IActionResult> GetUserLibraryStats(Guid userId, CancellationToken cancellationToken)
{
    var query = new GetUserLibraryStatsQuery(userId, GetUserId());
    var result = await _mediator.Send(query, cancellationToken);
    return Ok(result);
}
```

### 4.2 Visible user lists

Adicionar:

- `UserLists/Queries/GetUserLists/GetUserListsQuery.cs`
- `GetUserListsQueryHandler.cs`
- método em `IUserListReadRepository`:

```csharp
Task<PaginatedList<UserListSummaryDto>> GetVisibleByUserIdAsync(
    Guid targetUserId,
    Guid viewerUserId,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default);
```

Controller: **colocar no `UserBooksController`** (rota `api/v1/library`), que já hospeda `users/{userId}/books` e `users/{userId}/stats`. Assim todos os reads `users/{userId}/*` ficam num só controller e evitamos override de rota.

```csharp
// UserBooksController (Route = "api/v1/library")
[HttpGet("users/{userId:guid}/lists")]
public async Task<IActionResult> GetUserLists(
    Guid userId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
```

Não colocar no `UserListsController` (rota `api/v1/library/lists`): cairia em `/library/lists/users/{userId}/lists` e exigiria override absoluto.

### 4.3 Private list visibility

**Não é preciso estender `UserListDetailDto`.** O handler já carrega a entidade `UserList` (que tem `UserId` + `IsPublic`); a checagem de visibilidade é feita sobre a entidade, antes do map para DTO.

- `GetListDetailsQueryHandler` carrega detalhe, valida `IsPublic || UserId == viewerUserId`, retorna.
- `GetListBooksQueryHandler` valida visibilidade primeiro e só depois chama `GetListBooksAsync`.

**Extrair `IUserListVisibilityPolicy` (não opcional).** A regra `IsPublic || OwnerId == viewer` aparece agora em **quatro** pontos: list-detail, list-books, listagem de listas (§4.2) e a contagem de `Lists` do stats (§4.1). Uma policy testada uma vez evita drift entre eles. Reusar o filtro de público que `SearchPublicListsQuery` já aplica no read repository, em vez de reimplementar.

---

## 5. Frontend — implementação

### 5.1 Criar camada compartilhada de perfil

Criar uma feature de composição:

- `src/features/profile/components/ProfileExperience.tsx`
- `src/features/profile/hooks/useProfilePermissions.ts`
- `src/features/profile/hooks/useUserLibraryStats.ts`
- `src/features/profile/hooks/useUserProfileBooks.ts`
- `src/features/profile/hooks/useUserProfileLists.ts`

Esta feature não cria APIs próprias. Ela chama `socialApi` e `libraryApi`, preservando os bounded contexts no frontend.

Wrappers:

- `features/library/components/ProfilePage.tsx` vira wrapper do próprio perfil:
  - pega `viewerUserId` de `useAuth()`;
  - passa `targetUserId = viewerUserId`;
  - `mode = "self"`.
- `features/social/components/UserProfilePage.tsx` vira wrapper público:
  - pega `targetUserId` de `useParams()`;
  - se `targetUserId === viewerUserId`, redireciona para `/profile`;
  - passa `mode = "public"`.

### 5.2 APIs e query keys

Estender `libraryApi`:

```ts
getUserLibrary: (
  userId: string,
  status: BackendReadingStatus,
  page: number,
  pageSize: number,
) => Promise<PaginatedList<UserBookDto>>;

getUserLibraryStats: (userId: string) => Promise<UserLibraryStatsDto>;

getUserLists: (
  userId: string,
  page: number,
  pageSize: number,
) => Promise<PaginatedList<UserListSummaryDto>>;
```

Estender `libraryKeys`:

```ts
userBooks: (userId: string, status: BackendReadingStatus) =>
  [...libraryKeys.all, "userBooks", userId, status] as const;

userStats: (userId: string) =>
  [...libraryKeys.all, "userStats", userId] as const;

userLists: (userId: string) =>
  [...libraryKeys.all, "userLists", userId] as const;
```

Manter `useLibraryBooks`/`useLists` se ainda forem úteis para outras telas (`/wishlist`, `/lists`). Para o perfil rico, preferir hooks por `targetUserId` para que self e public usem o mesmo caminho de leitura.

**Aposentar `useLibraryCounts` e `libraryKeys.count`.** Hoje o perfil próprio conta status com **5 requests** (um `getUserBooks({ status, pageSize: 1 })` por status, lendo `totalCount`). O novo `GET /library/users/{userId}/stats` traz tudo em **1 request**. Self deve passar a usar `useUserLibraryStats(viewerUserId)` igual ao público — unifica o caminho e remove 4 requests do `/profile`.

### 5.3 Tabs e conteúdo

`ProfileExperience` mantém:

- `activeTab: ProfileTab`;
- `viewMode: ViewMode`;
- `tabs` derivados de `UserLibraryStatsDto`, activity first page e `stats.Lists`.

Mapeamento:

| Tab | Dados |
|---|---|
| `activity` | `useUserActivity(targetUserId)` |
| `reading` | `useUserProfileBooks(targetUserId, "Reading")` |
| `finished` | `useUserProfileBooks(targetUserId, "Finished")` |
| `paused` | `useUserProfileBooks(targetUserId, "Paused")` |
| `abandoned` | `useUserProfileBooks(targetUserId, "Abandoned")` |
| `not_started` | `useUserProfileBooks(targetUserId, "NotStarted")` |
| `lists` | `useUserProfileLists(targetUserId)` |

Renderização:

- `BookGridItem editable={permissions.canEditLibrary}`.
- `ListCard` sem ações para outro usuário.
- `FeedCard` continua com `InteractionBar` quando o item é interactable; delete só aparece para o dono do feed item, como já está.
- Cover/title dos livros apontam para `/books/{bookId}`.

### 5.4 Header/stats/follow

Reusar o visual de `ProfileHeader` e `ProfileStats`, mas ajustar props:

- `ProfileHeader` recebe `profile` e um slot/prop para ação à direita.
- Em modo público, renderizar `FollowButton` no header.
- Em modo próprio, renderizar ação de editar perfil quando a fase de editar perfil estiver pronta; se ainda não houver UI de edição, não inventar botão morto.
- `ProfileStats` deve receber `userId` e continuar linkando para:
  - `/users/{userId}/read`;
  - `/users/{userId}/followers`;
  - `/users/{userId}/following`.

Quando a aba `Read` existir dentro de `/users/:userId`, decidir se `/users/:userId/read`:

- redireciona para `/users/:userId?tab=finished`; ou
- permanece como página dedicada de deep link.

Preferência v1: manter a rota dedicada para não quebrar links existentes, mas garantir que a aba `Read` seja a experiência principal.

### 5.5 Estados e i18n

Reusar os estados do perfil próprio:

- skeleton de header;
- skeleton de conteúdo;
- empty state por tab;
- error state com retry.

Adicionar i18n só se faltar copy específica:

- `profile.publicEmptyLists`;
- `profile.privateOrUnavailable`;
- `profile.userNotFound`;

Não adicionar texto em tela explicando "read-only"; o próprio sumiço das ações de edição é suficiente.

---

## 6. Critérios de aceitação

Com backend no ar e usuário logado:

1. `yarn build` passa.
2. `/profile` mantém o comportamento atual: abas ricas, ações de livro/lista/perfil próprias quando existirem, contadores corretos.
3. `/users/:otherUserId` mostra header, stats, follow, activity e todas as abas do perfil rico.
4. Em `/users/:otherUserId`, os cards de livro mostram status/progresso/nota e linkam para `/books/{bookId}`, mas não exibem menu de lifecycle.
5. Em `/users/:otherUserId`, a aba `Lists` mostra somente listas públicas.
6. Atividade de outro usuário ainda permite likes/comments em posts/reviews interactable.
7. Nenhuma ação de edição/remove/update aparece para livros, listas ou perfil de outro usuário.
8. Tentar chamar write endpoints com `userBookId`/`listId` de outro usuário continua retornando `403`/erro equivalente.
9. Lista privada de outro usuário não é acessível por detail/books.
10. Loading, empty, error e load-more funcionam nas abas paginadas.

---

## 7. Plano de testes

### Backend

Adicionar testes de aplicação para:

- `GetUserLibraryStatsQueryHandler` retorna counts corretos por status.
- Stats contam todas as listas para o dono e somente públicas para outro viewer.
- `GetUserListsQueryHandler` retorna listas públicas para outro viewer e todas para o dono.
- `GetListDetailsQueryHandler` permite lista privada ao dono.
- `GetListDetailsQueryHandler` retorna `NotFound` (não `Forbidden`) para lista privada de outro viewer.
- `GetListBooksQueryHandler` aplica a mesma regra antes de listar livros.
- **Regressão de segurança (fix §3.4):** lista privada não é acessível por detail/books para não-dono; e os endpoints não são mais `[AllowAnonymous]` (chamador sem auth recebe 401). Trava o vazamento descrito em §0.

Adicionar ou ajustar testes de controller se o projeto já tiver esse padrão para rotas/auth.

### Frontend

Testes/smoke checks:

- renderizar `/profile`;
- renderizar `/users/:otherUserId`;
- alternar cada tab;
- conferir que `BookGridItem` recebe `editable=false` para outro usuário;
- conferir que `FollowButton` só aparece em outro perfil;
- conferir que `InteractionBar` continua aparecendo em atividade interactable.

Verificação manual com dados reais:

- usuário A com livros em todos os status;
- usuário A com lista pública e privada;
- usuário B visualizando A;
- usuário B não consegue ver a lista privada de A;
- usuário B consegue comentar/curtir atividade de A.

---

## 8. Edge cases e futuro

Preparar o desenho para estes casos, sem implementar tudo agora:

- usuário inexistente: mostrar error/not found.
- usuário sem livros/listas/activity: empty state por aba.
- username muda: não afeta v1 porque a rota é por `userId`.
- counters divergentes por atualização concorrente: invalidação/refetch resolve; não fazer otimismo em contadores de perfil.
- dono altera livro enquanto viewer navega: próxima refetch atualiza, e mutations alheias não existem.
- bibliotecas grandes: abas de livros e listas públicas são paginadas.
- perfis privados/bloqueio: `ViewerUserId` já deve chegar aos handlers novos para permitir política futura.

**Backlog pós-fase:**

- `/users/:username` com lookup/redirect.
- Perfil público deslogado.
- Query param de tab (`/users/:userId?tab=finished`) para deep links melhores.
- BFF/read model de perfil se a composição client-side ficar lenta.

---

## 9. Sequenciamento de entrega

Quebrar em dois PRs independentes — não juntar o fix de segurança dentro da feature grande, para não atrasar nem diluir a correção que mais importa:

- **PR A — Endurecer reads de lista (§3.4).** Pequeno, autocontido, **entregar primeiro**. Remove `[AllowAnonymous]`, adiciona viewer context + visibilidade nos handlers de list-detail/books, com o teste de regressão de segurança (§7). Fecha o IDOR descrito em §0 e não depende de nada do perfil rico.
- **PR B — Perfil público rico.** Endpoint `/stats` (+ aposentar `useLibraryCounts`), endpoint `/users/{userId}/lists`, `IUserListVisibilityPolicy`, shell compartilhado `ProfileExperience` e wrappers. Depende de A para a aba `Lists` respeitar visibilidade.
