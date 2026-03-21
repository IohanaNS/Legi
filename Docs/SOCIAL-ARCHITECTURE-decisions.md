# Social Service — Decisões de Arquitetura

Documento vivo com as decisões de design do domínio Social, construído incrementalmente.

**Status de Implementação:** Domain ✅ | Application 📋 | Infrastructure 📋 | Api 📋

---

## 1. Definição do Bounded Context

### O que é o Social?

> **Social é o subdomínio que gerencia relacionamentos entre usuários e as interações sobre conteúdo produzido em outros contextos.**

O Social **não produz conteúdo original**. Ele *reage* a conteúdo. Quem produz conteúdo é o Library (posts de progresso, listas), o Catalog (reviews). O Social gerencia:

- **Relacionamentos** entre usuários (Follow)
- **Perfil público** do usuário (UserProfile)
- **Interações** sobre conteúdo (likes, comments)
- **Feed** de atividades (read model)

### Classificação de conceitos

| Conceito | Pertence ao Social? | Justificativa |
|----------|---------------------|---------------|
| Follow | ✅ Nativo | Não faz sentido em nenhum outro contexto |
| UserProfile (bio, avatar) | ✅ Nativo | Apresentação pública do usuário — Identity cuida de autenticação |
| Like | ✅ Nativo | Interação social sobre conteúdo |
| Comment | ✅ Nativo | Interação social sobre conteúdo |
| Feed / FeedItem | ✅ Nativo (read model) | Projeção cronológica de atividades |
| ReadingPost | ❌ Library | "Registro pessoal da leitura" faz sentido sem Social |
| UserList | ❌ Library | "Lista pessoal de livros" faz sentido sem Social |
| Review | ❌ Catalog | "Avaliação do livro" faz sentido sem Social |
| User (auth) | ❌ Identity | Credenciais e autenticação |

### O mesmo conceito, significados diferentes

O "usuário" aparece em múltiplos bounded contexts com representações distintas:

| Bounded Context | O que "usuário" significa | Dados que importam |
|----------------|--------------------------|---------------------|
| Identity | Credenciais de acesso | Email, Username, Password, Tokens |
| Library | O leitor | UserId (referência) |
| Catalog | O contribuidor | UserId (referência) |
| Social | A persona pública | Username, Bio, Avatar, Contadores |

---

## 2. Aggregates Identificados

| Aggregate | Tipo | Justificativa |
|-----------|------|---------------|
| **Follow** | Aggregate Root (magro) | Relacionamento direcional entre usuários. Sem pai natural, precisa de repositório próprio. |
| **UserProfile** | Aggregate Root (UserId como PK) | Perfil público do usuário. Dados editáveis + contadores. Fonte de verdade para bio, avatar. |
| **Like** | Aggregate Root (magro) | Interação independente sobre conteúdo. Sem invariantes cross-like. |
| **Comment** | Aggregate Root (magro, imutável) | Interação independente sobre conteúdo. Sem invariantes cross-comment. |
| **ContentSnapshot** | Read Model | Projeção enriquecida de conteúdo de outros contextos (autorização + contexto de exibição). |
| **FeedItem** | Read Model | Registro desnormalizado de atividades para o feed. |

---

## 3. Decisões e Justificativas

### 3.1 Separação Identity vs Social — Bio e Avatar saem do Identity

**Problema:** O Identity original continha Name, Bio e AvatarUrl junto com Email, Password e Tokens. Com o Social nascendo, onde moram os dados de apresentação pública?

**Teste aplicado:** "O Identity precisa disso para cumprir sua responsabilidade de autenticação?"
- Email → identificador de login → **fica** ✅
- Username → identificador de login e público → **fica** ✅
- PasswordHash → autenticação → **fica** ✅
- RefreshTokens → autenticação → **fica** ✅
- Name → apresentação pública → **sai** ❌
- Bio → apresentação pública → **sai** ❌
- AvatarUrl → apresentação pública → **sai** ❌

**Decisão:** Bio e AvatarUrl migram para `UserProfile` no Social. Name é removido — a rede identifica usuários por Username, não por nome real (padrão de redes de leitura como Goodreads/Skoob).

**Impacto no Identity (refatoração necessária):**
- Remover campos `Name`, `Bio`, `AvatarUrl` do User (domínio, schema, migrations)
- `UpdateProfileCommand` reduzido a: atualizar Email e/ou Username
- `GET /api/v1/identity/users/{userId}` (perfil público) **migra** para o Social
- Identity mantém apenas: registro, login, refresh, logout, dados de conta (me), delete

**Identity refatorado:**

```
User (Identity — pós-refatoração)
├── Id: Guid
├── Email: Email (VO)
├── Username: Username (VO)
├── PasswordHash: string
├── RefreshTokens: List<RefreshToken>
├── CreatedAt: DateTime
└── UpdatedAt: DateTime
```

**Fluxo de registro atualizado:**

```
1. POST /api/v1/identity/auth/register { email, username, password }
2. Identity cria User, publica UserRegisteredIntegrationEvent(UserId, Username)
3. Social consome evento, cria UserProfile com defaults:
   - Username = do evento
   - Bio = null
   - AvatarUrl = null
   - BannerUrl = null
   - Contadores = 0
```

### 3.2 Perfil Público/Privado — YAGNI no v1

**Problema:** A ideia original incluía `IsPublic` no perfil, com perfil privado restringindo acesso a conteúdo e exigindo aprovação para follow.

**Análise:** Sem sistema de notificações (planejado para Legi 2.0), o fluxo de "pedir para seguir" seria uma feature fantasma — existiria no backend mas seria invisível para o usuário. Maria nunca ficaria sabendo que Io pediu para segui-la.

**Decisão:** `IsPublic` fica fora do escopo v1. Todo perfil é público. Todo follow é auto-accept.

**Caminho para Legi 2.0:**
1. Adicionar `IsPublic: bool` ao UserProfile
2. Adicionar `FollowStatus` enum ao Follow (Pending, Accepted, Rejected, Cancelled)
3. Follow handler consulta `UserProfile.IsPublic` e decide: direto para Accepted ou Pending
4. Sistema de notificações notifica o alvo sobre solicitação pendente
5. O domínio do Follow já suporta as transições — a casca conceitual está documentada (seção 4.3)

### 3.3 Follow como Aggregate Root — magro e tudo bem

**Problema:** Follow sem estado (v1) é basicamente dois IDs e um timestamp. Precisa ser aggregate root?

**Teste de alternativas:**
- **Value Object?** Não — Follow tem identidade. Precisa ser encontrado e deletado individualmente.
- **Entity filha de UserProfile?** Não — carregar todos os follows de um usuário para adicionar um novo não escala. Um usuário com 5.000 follows forçaria carregar tudo na memória. **Mesmo problema que levou ReadingPost a ser promovido a aggregate no Library.**
- **Aggregate Root?** Tem identidade própria, precisa ser endereçável, precisa de repositório. Mas é magro.

**Decisão:** Follow é aggregate root magro. **Aggregate magro é okay** — nem todo aggregate precisa ser rico em comportamento. Vaughn Vernon recomenda aggregates pequenos como default.

**Hard delete no unfollow:** Follow é um relacionamento ativo — existe ou não existe. Não há valor de negócio em histórico ("Io seguiu Maria entre janeiro e março"). Se Io quiser seguir de novo, é um Follow novo.

### 3.4 Like e Comment como Aggregates independentes (não agrupados)

**Problema:** O modelo original do ARCHITECTURE.md tinha `PostInteractions` como aggregate agrupando likes e comments. Como modelar interações?

**Três abordagens avaliadas:**

| Abordagem | Descrição | Problema |
|-----------|-----------|----------|
| **A — Aggregate por tipo de conteúdo** | PostInteractions, ListInteractions, ReviewInteractions | Duplicação de código. Novo tipo = novo aggregate, repositório, handlers |
| **B — Aggregate genérico** | ContentInteractions agrupa likes e comments | Sem invariante cross-like/comment. Carregar 500 likes + 200 comments para adicionar 1 comment |
| **C — Aggregates independentes** | Like e Comment separados, cada um com TargetType + TargetId | Zero duplicação, escala para novos tipos, sem problema de carga |

**Decisão:** Abordagem C. Like e Comment são aggregates independentes.

**Justificativa:** Não existe invariante que exija likes e comments na mesma transação. "Usuário só pode curtir uma vez" é regra do Like, não da relação entre Like e Comment. O problema de carga em memória da abordagem B é o **mesmo motivo** que levou ReadingPost a ser promovido a aggregate no Library.

### 3.5 Modelo unificado de interações — TargetType genérico

**Problema:** Likes e comments podem ser aplicados a posts, reviews e listas. Modelos separados por tipo ou modelo único?

**Análise:** No v1 (sem IsPublic), a regra de interação é uniforme — **qualquer usuário autenticado pode curtir/comentar qualquer conteúdo**. A mecânica é idêntica para todos os tipos.

**Decisão:** Modelo unificado com `InteractableType` enum. Like e Comment usam `TargetType + TargetId` polimórfico.

**Extensibilidade:** Adicionar um novo tipo de conteúdo interagível (ex: futuro "BookClub") requer apenas adicionar um valor ao enum. Nenhum aggregate, repositório ou handler novo.

**Preparação para Legi 2.0 (IsPublic):** Quando perfil público/privado chegar, a regra de permissão variará por *autor do conteúdo*, não por *tipo de conteúdo*. O modelo unificado suporta isso naturalmente — o handler consulta o owner via ContentSnapshot e verifica visibilidade.

### 3.6 Comentários são imutáveis

**Problema:** Comentários podem ser editados após publicação?

**Decisão:** Não. Comentários são imutáveis — só podem ser criados ou deletados.

**Justificativa:**
- Simplifica o modelo (sem `UpdateContent()`, sem `UpdatedAt`)
- Evita problemas de contexto — quando replies chegarem no v2, um comentário editado após receber respostas pode tornar as respostas sem sentido
- Segurança — evita que alguém edite um comentário para mudar seu significado após interações
- Padrão comum em redes sociais (Instagram, Reddit não permitem editar comments em todas as superfícies)

### 3.7 Quem pode deletar comentário

**Problema:** Só o autor do comentário pode deletar, ou o dono do conteúdo também?

**Decisão:** Ambos podem deletar.
- **Autor do comentário:** é dele, pode remover
- **Dono do conteúdo:** é *o espaço dele*, precisa poder moderar

**Implementação:** O aggregate `Comment` tem um método `Delete()` sem parâmetros — ele não sabe quem está executando. A verificação de permissão é feita no **command handler**:

```
actorId == comment.UserId → autor do comentário → permite
actorId == contentSnapshot.OwnerId → dono do conteúdo → permite
nenhum dos dois → ForbiddenException
```

Isso segue o princípio estabelecido no Follow (seção 3.8): **o aggregate protege invariantes de domínio, o handler protege precondições de contexto.**

### 3.8 Separação de responsabilidades: aggregate vs handler

**Princípio firmado durante o design do Follow e aplicado consistentemente:**

> O aggregate é *ignorante* sobre o mundo externo. Ele conhece apenas seu próprio estado e suas invariantes internas.

| Responsabilidade | Quem resolve | Exemplo |
|-----------------|--------------|---------|
| Invariantes de estado | Aggregate | "Não pode aceitar Follow que não está Pending" |
| Autorização (quem executa) | Handler | "Só o dono do perfil pode aceitar" |
| Dados externos (snapshots, outros aggregates) | Handler | "Perfil alvo é público?" |
| Unicidade cross-aggregate | Handler + Banco | "Follow duplicado?" |

### 3.9 ContentSnapshot — projeção enriquecida para desacoplamento

**Problema original:** O handler de `DeleteCommentCommand` precisa verificar quem é o dono do conteúdo alvo. Mas o conteúdo (post, review, lista) vive em outro bounded context. Como o Social descobre o owner?

**Três opções avaliadas:**

| Opção | Descrição | Problema |
|-------|-----------|----------|
| **1 — Chamada cross-service** | Handler chama Library em runtime | Acoplamento. Se Library cair, Social não pode deletar comments |
| **2 — Snapshot local** | Tabela local com dados projetados do conteúdo | Mais uma tabela + sync via eventos. Mas desacoplamento total |
| **3 — Desnormalizar no Comment** | Gravar ContentOwnerId dentro do Comment | Sem mecanismo de atualização. Mistura dados |

**Decisão:** Opção 2 — ContentSnapshot. Mesmo padrão já usado no projeto: BookSnapshot no Library (projeção do Catalog), UserProfile recebendo Username do Identity.

**Justificativa do senior:** Consistência de padrão num codebase é mais valiosa que a solução "perfeita" isolada. Quando alguém novo entra no projeto e vê BookSnapshot no Library, ContentSnapshot no Social, UserProfile recebendo Username do Identity — entende imediatamente: "dados de outro contexto viram snapshots locais". É vocabulário arquitetural.

**Modelo enriquecido (não mínimo):** Inicialmente o ContentSnapshot continha apenas OwnerId para autorização. Após análise de cenários reais de uso (página de comentários, página de likes, futuras notificações), decidiu-se enriquecer o snapshot com dados de contexto:

- **Cenário: página de comentários** — o usuário clica em "3 comentários" no feed. A página precisa mostrar *sobre o quê* as pessoas estão comentando (texto do post, livro, quem postou). Com snapshot mínimo, o frontend teria que chamar Library (dados do post) + Social (comentários) — duas chamadas cross-service para uma tela. Com snapshot enriquecido, é uma única chamada ao Social.
- **Cenário: página de likes** — mesma lógica, precisa de contexto visual.
- **Cenário: futuras notificações** — "X comentou no seu post sobre Duna" requer título do livro e dados do owner.

**LikesCount e CommentsCount NÃO estão no ContentSnapshot.** Likes e comments vivem no mesmo banco do Social — contadores são consultados em tempo real via `COUNT(*)` com índice em `(target_type, target_id)`. Desnormalizar contadores aqui criaria escrita duplicada (atualizar Like + atualizar ContentSnapshot + enviar integration event pro Library) sem benefício, já que a consulta local é trivial. Regra: **desnormalizar contadores apenas quando os dados vivem em serviço diferente.**

**`UpdateOwner` para sincronização:** Quando Identity notifica mudança de username/avatar, o handler atualiza OwnerUsername e OwnerAvatarUrl em todos os ContentSnapshots daquele usuário. Dados do livro não têm método de update por ora — metadata de livro muda raramente (YAGNI).

**`ContentPreview` truncado na criação:** O snapshot armazena os primeiros ~200 caracteres do conteúdo do post/review. A truncagem é feita na entidade (constante `MaxContentPreviewLength = 200`), garantindo o invariante independente de quem cria o snapshot.

**Workaround temporário:** Assim como BookSnapshot no Library, criação inline até RabbitMQ existir.

### 3.10 UserProfile com UserId como PK (não herda BaseEntity)

**Problema:** UserProfile tem relação 1:1 com o usuário. Deve ter `Id: Guid` próprio (herdando de BaseEntity) ou usar `UserId` como PK?

**Decisão:** `UserId` como PK diretamente, sem herança de BaseEntity.

**Justificativa:** Mesmo padrão de BookSnapshot no Library — BookId é PK, não herda BaseEntity. UserProfile não faz sentido sem o usuário, nunca existirá profile órfão, e toda query é por UserId. A diferença é que UserProfile tem comportamento de aggregate (diferente de BookSnapshot que é read model puro), mas a decisão de PK é independente disso.

### 3.11 FollowersCount e FollowingCount — dados nativos do Social

**Problema:** Onde moram os contadores de seguidores? No Identity (via snapshot) ou no Social?

**Decisão:** No UserProfile do Social. São dados **nativos** — produzidos e mantidos pelo Social, não projetados de outro contexto.

**Justificativa:** UserSnapshot (se existisse) misturaria "dados que eu recebi de fora" (username) com "dados que eu produzo aqui" (contadores). São origens diferentes, ciclos de atualização diferentes. O UserProfile já é aggregate do Social, então os contadores vivem naturalmente nele.

### 3.12 Feed — fan-out on read, não fan-out on write

**Problema:** Como implementar o feed de atividades?

**Fan-out on write (push):** Quando Carlos posta, cria FeedItem *para cada seguidor*. Leitura rápida (SELECT por userId), escrita cara (10.000 seguidores = 10.000 inserts).

**Fan-out on read (pull):** Quando Ana abre o feed, busca atividades recentes de quem ela segue. Escrita zero, leitura faz join com follows.

**Decisão:** Fan-out on read via tabela `activities`.

**Justificativa:** O Legi não é Twitter. A escala esperada é dezenas ou centenas de seguidores por usuário. O custo do join é trivial para 20 items com índices adequados. Não precisa de tabela fan-out que multiplica dados.

**Query do feed:**

```sql
SELECT a.* FROM activities a
INNER JOIN follows f ON f.following_id = a.actor_id
WHERE f.follower_id = @me
ORDER BY a.created_at DESC
LIMIT 20
```

### 3.13 FeedItem como read model desnormalizado

**Problema:** FeedItem precisa de dados de múltiplos contextos para renderizar o feed (nome do usuário, título do livro, capa, etc). Como resolver?

**Decisão:** FeedItem é um **read model totalmente desnormalizado** no momento da escrita. Quando o integration event chega, o handler cria o FeedItem já com todos os dados necessários para exibição.

**Nota de nomenclatura:** Nomeado `FeedItem` em vez de `Activity` para evitar colisão com `System.Diagnostics.Activity` do .NET.

**Justificativa:** O feed é a tela mais acessada do app. Toda abertura do Legi bate no feed. A leitura precisa ser um SELECT simples, sem joins com UserProfile, ContentSnapshot, etc.

**Dados potencialmente desatualizados:** Se o usuário muda o avatar, activities antigas mostram o avatar antigo. Isso é aceitável — o feed é inerentemente temporal. "Carlos postou isso às 15h" é um fato histórico. Opcionalmente, um batch update pode atualizar via handler de `UsernameChangedIntegrationEvent`, mas não é obrigatório para v1.

### 3.14 LikesCount e CommentsCount no feed — consulta em tempo real, não desnormalizado

**Problema:** Cada item do feed mostra contagem de likes e comments. Desnormalizar no FeedItem ou consultar em tempo real?

**Decisão:** Consulta em tempo real via subquery.

**Justificativa:** Diferente do Library (onde LikesCount é desnormalizado porque likes vivem em outro serviço), no Social **likes e comments estão no mesmo banco** que as activities. Não há razão para desnormalizar quando `COUNT(*)` com índice em `(target_type, target_id)` é suficiente para 20 items.

**Vantagens sobre desnormalização:**
- Sempre atualizado (sem race conditions de contadores)
- Não precisa de dois updates por like (um no Like + um no FeedItem)
- FeedItem não fica poluído com contadores de tipos que não são interagíveis

**Distinção conceitual:** O FeedItem é um registro histórico de algo que aconteceu. O Like é uma interação com o *conteúdo original* (post, review, lista) — não com o FeedItem. Quando alguém curte, está curtindo o *post do Carlos*, não o "fato de Carlos ter postado".

**Query do feed com contadores:**

```sql
SELECT
    a.*,
    (SELECT COUNT(*) FROM likes l
     WHERE l.target_id = a.reference_id
     AND l.target_type = a.target_type) as likes_count,
    (SELECT COUNT(*) FROM comments c
     WHERE c.target_id = a.reference_id
     AND c.target_type = a.target_type) as comments_count
FROM activities a
INNER JOIN follows f ON f.following_id = a.actor_id
WHERE f.follower_id = @me
ORDER BY a.created_at DESC
LIMIT 20
```

---

## 4. Modelo de Aggregates (visão geral)

```
Follow (Aggregate Root — magro)
├── Id: Guid
├── FollowerId: Guid
├── FollowingId: Guid
├── CreatedAt: DateTime
├── Factory: Create(followerId, followingId)
│     → valida: followerId != followingId
└── Hard delete no unfollow (sem soft delete)

UserProfile (Aggregate Root — UserId como PK)
├── UserId: Guid (PK, não herda BaseEntity)
├── Username: string (snapshot do Identity)
├── Bio: string? (max 500)
├── AvatarUrl: string?
├── BannerUrl: string?
├── FollowersCount: int (≥ 0, nativo do Social)
├── FollowingCount: int (≥ 0, nativo do Social)
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
├── Métodos:
│   ├── UpdateBio(string?)
│   ├── UpdateAvatar(string?)
│   ├── UpdateBanner(string?)
│   ├── UpdateUsername(string)
│   ├── IncrementFollowers() / DecrementFollowers()
│   └── IncrementFollowing() / DecrementFollowing()

Like (Aggregate Root — magro)
├── Id: Guid
├── UserId: Guid
├── TargetType: InteractableType
├── TargetId: Guid
├── CreatedAt: DateTime
├── Factory: Create(userId, targetType, targetId)
└── Invariante: unicidade (userId + targetType + targetId) no banco
    Hard delete no unlike

Comment (Aggregate Root — magro, imutável)
├── Id: Guid
├── UserId: Guid
├── TargetType: InteractableType
├── TargetId: Guid
├── Content: string (1-500)
├── CreatedAt: DateTime
├── Factory: Create(userId, targetType, targetId, content)
└── Sem edição — só cria ou deleta
    Hard delete
    Deletável pelo autor OU pelo dono do conteúdo alvo

ContentSnapshot (Read Model — PK composta, enriquecido)
├── TargetType: InteractableType
├── TargetId: Guid
├── (PK composta: TargetType + TargetId)
├── OwnerId: Guid
├── OwnerUsername: string (snapshot do Identity)
├── OwnerAvatarUrl: string? (snapshot do Identity)
├── BookTitle: string? (do Catalog/Library)
├── BookAuthor: string? (do Catalog/Library)
├── BookCoverUrl: string? (do Catalog/Library)
├── ContentPreview: string? (primeiros ~200 chars do post/review)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

FeedItem (Read Model — desnormalizado)
├── Id: Guid
├── ActorId: Guid
├── ActorUsername: string
├── ActorAvatarUrl: string?
├── ActivityType: ActivityType
├── TargetType: InteractableType? (null se não é interagível)
├── ReferenceId: Guid (id do post, review, lista, etc)
├── BookTitle: string?
├── BookAuthor: string?
├── BookCoverUrl: string?
├── Data: string? (JSON — progresso, rating, texto do post)
└── CreatedAt: DateTime
```

---

## 5. Enums

### 5.1 InteractableType

```
Post | Review | List
```

Usado por Like, Comment e ContentSnapshot para identificar o tipo de conteúdo alvo. Extensível — adicionar novo tipo é adicionar um valor ao enum.

### 5.2 ActivityType

```
ProgressPosted | BookFinished | BookStarted | BookRated | ReviewCreated | ListCreated
```

Identifica o tipo de ação registrada no feed.

---

## 6. Domain Events

Princípio aplicado: **YAGNI** — apenas eventos com pelo menos um consumidor identificado.

### 6.1 Follow (2 eventos)

| Evento | Consumidores | Dados |
|--------|-------------|-------|
| `FollowCreatedDomainEvent` | UserProfile (incrementa FollowersCount do followed + FollowingCount do follower) | FollowerId, FollowingId |
| `FollowRemovedDomainEvent` | UserProfile (decrementa contadores) | FollowerId, FollowingId |

### 6.2 Like (2 eventos)

| Evento | Consumidores | Dados |
|--------|-------------|-------|
| `ContentLikedDomainEvent` | Library (incrementa LikesCount no post/lista) via integration event | UserId, TargetType, TargetId |
| `ContentUnlikedDomainEvent` | Library (decrementa LikesCount) via integration event | UserId, TargetType, TargetId |

### 6.3 Comment (2 eventos)

| Evento | Consumidores | Dados |
|--------|-------------|-------|
| `CommentCreatedDomainEvent` | Library (incrementa CommentsCount) via integration event | CommentId, UserId, TargetType, TargetId |
| `CommentDeletedDomainEvent` | Library (decrementa CommentsCount) via integration event | CommentId, UserId, TargetType, TargetId |

### 6.4 UserProfile (0 eventos)

Nenhum domain event identificado com consumidor. YAGNI.

### 6.5 Resumo

**Total: 6 domain events.** Cada um com pelo menos um consumidor claro.

| Aggregate | Eventos | Lista |
|-----------|---------|-------|
| Follow | 2 | Created, Removed |
| Like | 2 | Liked, Unliked |
| Comment | 2 | Created, Deleted |
| UserProfile | 0 | — |

---

## 7. Integration Events

### 7.1 Incoming (outros → Social)

| Origem | Evento | Efeito no Social |
|--------|--------|-------------------|
| Identity | `UserRegisteredIntegrationEvent(UserId, Username)` | Cria UserProfile com defaults |
| Identity | `UserDeletedIntegrationEvent(UserId)` | Deleta UserProfile, Follows, Likes, Comments, FeedItems |
| Identity | `UsernameChangedIntegrationEvent(UserId, NewUsername)` | Atualiza UserProfile.Username |
| Library | `ReadingPostCreatedIntegrationEvent(PostId, UserId, BookId, Content?, Progress?, ...)` | Cria ContentSnapshot (Post) + FeedItem (ProgressPosted ou BookFinished) |
| Library | `ReadingPostDeletedIntegrationEvent(PostId)` | Remove ContentSnapshot + Likes + Comments + FeedItem |
| Library | `BookAddedToLibraryIntegrationEvent(UserBookId, UserId, BookId, ...)` | Cria FeedItem (BookStarted) |
| Library | `ReadingStatusChangedIntegrationEvent(UserId, BookId, OldStatus, NewStatus)` | Cria FeedItem (BookFinished, se aplicável) |
| Library | `UserBookRatedIntegrationEvent(BookId, UserId, Rating)` | Cria FeedItem (BookRated) |
| Library | `UserListCreatedIntegrationEvent(ListId, UserId, Name)` | Cria ContentSnapshot (List) + FeedItem (ListCreated) |
| Library | `UserListDeletedIntegrationEvent(ListId)` | Remove ContentSnapshot + Likes + Comments + FeedItem |
| Catalog | `ReviewCreatedIntegrationEvent(ReviewId, UserId, BookId, ...)` | Cria ContentSnapshot (Review) + FeedItem (ReviewCreated) |
| Catalog | `ReviewDeletedIntegrationEvent(ReviewId)` | Remove ContentSnapshot + Likes + Comments + FeedItem |

### 7.2 Outgoing (Social → outros)

| Evento | Consumidor |
|--------|------------|
| `ContentLikedIntegrationEvent(TargetType, TargetId, UserId)` | Library (LikesCount em ReadingPost/UserList) |
| `ContentUnlikedIntegrationEvent(TargetType, TargetId)` | Library (LikesCount) |
| `ContentCommentedIntegrationEvent(TargetType, TargetId, CommentId, UserId)` | Library (CommentsCount) |
| `CommentDeletedIntegrationEvent(TargetType, TargetId, CommentId)` | Library (CommentsCount) |

### 7.3 Workaround temporário (pré-RabbitMQ)

Assim como o BookSnapshot no Library, integration events não existem em runtime até a mensageria ser implementada. Snapshots e FeedItems serão criados via workaround inline nos handlers, com documentação explícita para remoção futura.

---

## 8. Regras de Negócio

### 8.1 Follow

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Auto-follow** | Usuário não pode seguir a si mesmo | Aggregate (factory method Create) |
| **Unicidade** | Par (FollowerId, FollowingId) é único | Handler + Banco (unique constraint) |
| **Hard delete** | Unfollow remove o registro. Sem soft delete | Handler chama DeleteAsync |

### 8.2 UserProfile

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Bio max length** | Máximo 500 caracteres | Aggregate + FluentValidation |
| **AvatarUrl** | URL válida ou null | Aggregate + FluentValidation |
| **BannerUrl** | URL válida ou null | Aggregate + FluentValidation |
| **Contadores ≥ 0** | FollowersCount e FollowingCount nunca negativos | Aggregate (Decrement lança se < 0) |
| **Um por usuário** | Criado via integration event no registro | Banco (UserId como PK) |

### 8.3 Like

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Unicidade** | Usuário só pode curtir o mesmo conteúdo uma vez | Handler + Banco (unique constraint: userId + targetType + targetId) |
| **Conteúdo existente** | TargetId deve referenciar ContentSnapshot existente | Handler |
| **Hard delete** | Unlike remove o registro | Handler chama DeleteAsync |

### 8.4 Comment

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Content obrigatório** | 1-500 caracteres | Aggregate (factory) + FluentValidation |
| **Imutável** | Sem edição após criação | Aggregate (sem método de update) |
| **Conteúdo existente** | TargetId deve referenciar ContentSnapshot existente | Handler |
| **Deleção por autor** | Autor do comentário pode deletar | Handler (actorId == comment.UserId) |
| **Deleção por dono do conteúdo** | Dono do conteúdo alvo pode deletar comentários no seu conteúdo | Handler (actorId == contentSnapshot.OwnerId) |
| **Hard delete** | Deleção remove o registro | Handler chama DeleteAsync |

---

## 9. Repository Interfaces

### 9.1 Write Repositories (Domain)

```
IFollowRepository
├── GetByIdAsync(Guid id)
├── GetByPairAsync(Guid followerId, Guid followingId)
├── AddAsync(Follow follow)
├── DeleteAsync(Follow follow)

IUserProfileRepository
├── GetByUserIdAsync(Guid userId)
├── AddAsync(UserProfile profile)
├── UpdateAsync(UserProfile profile)
├── DeleteAsync(UserProfile profile)

ILikeRepository
├── GetByIdAsync(Guid id)
├── GetByUserAndTargetAsync(Guid userId, InteractableType type, Guid targetId)
├── AddAsync(Like like)
├── DeleteAsync(Like like)

ICommentRepository
├── GetByIdAsync(Guid id)
├── AddAsync(Comment comment)
├── DeleteAsync(Comment comment)

IContentSnapshotRepository
├── GetByTargetAsync(InteractableType type, Guid targetId)
├── AddOrUpdateAsync(ContentSnapshot snapshot)
├── DeleteByTargetAsync(InteractableType type, Guid targetId)

IFeedItemRepository
├── AddAsync(FeedItem feedItem)
├── DeleteByReferenceAsync(Guid referenceId)
├── DeleteByActorAsync(Guid actorId)
```

### 9.2 Read Repositories (Application — a definir na fase de Application design)

Queries do feed, listagem de seguidores/seguindo, comentários por conteúdo, etc. Serão definidas quando atacarmos a camada Application.

---

## 10. O que ficou fora do v1 (Legi 2.0)

| Feature | Motivo | Caminho de implementação |
|---------|--------|--------------------------|
| **IsPublic (perfil público/privado)** | Sem sistema de notificação, privacidade é ilusória | Adicionar `IsPublic` ao UserProfile, ligar check no handler de Follow e interações |
| **Follow com estado (Pending/Accepted/Rejected)** | Depende de IsPublic + notificações | Adicionar `FollowStatus` enum, transições no aggregate, handlers consultam visibilidade |
| **Sistema de notificações** | Complexidade de infra (push, email, in-app) | Novo bounded context ou módulo transversal |
| **Reply em comentários** | Complexidade de UI e modelagem (árvore de comments) | Comment ganha `ParentId?`, queries recursivas |
| **Like em comentários** | Depende de replies pra fazer sentido completo | Expandir InteractableType ou modelo separado |
| **Filtro de conteúdo ofensivo** | Requer NLP ou API externa | Behavior na pipeline ou serviço externo |

---

## 11. Impacto em Outros Serviços

### 11.1 Identity (refatoração necessária)

- **Remover:** campos `Name`, `Bio`, `AvatarUrl` do User
- **Reduzir:** `UpdateProfileCommand` → apenas Email e/ou Username
- **Remover:** `GET /api/v1/identity/users/{userId}` (perfil público migra pro Social)
- **Adicionar evento:** `UsernameChangedIntegrationEvent` (quando username é atualizado)
- **Manter:** registro, login, refresh, logout, `GET /me`, `PATCH /me`, `DELETE /me`

### 11.2 Library (sem mudança estrutural)

- Já possui `LikesCount` e `CommentsCount` desnormalizados em ReadingPost e UserList
- Já emite domain events necessários (ReadingPostCreated, UserListDeleted, etc.)
- Integration events outgoing precisam ser implementados quando RabbitMQ chegar

### 11.3 Catalog (sem mudança estrutural)

- BookReview (planejado) precisará emitir ReviewCreated/ReviewDeleted integration events
- Sem impacto nos componentes já implementados
