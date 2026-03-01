# Library Service — Decisões de Arquitetura

Documento vivo com as decisões de design do domínio Library, construído incrementalmente.

---

## 1. Aggregates Identificados

| Aggregate | Tipo | Justificativa |
|-----------|------|---------------|
| **UserBook** | Aggregate Root | Relação pessoal do usuário com um livro. Contém status, progresso, rating, wishlist. |
| **ReadingPost** | Aggregate Root | Promovido de entity filha para aggregate próprio. Posts de leitura são independentes entre si. |
| **UserList** | Aggregate Root | Listas personalizadas de livros. Contém `UserListItem` como entity filha. |
| **BookSnapshot** | Read Model | Cópia desnormalizada dos dados do Catalog (título, autores, capa, páginas). Atualizado via integration events. |

---

## 2. Decisões e Justificativas

### 2.1 ReadingPost promovido a Aggregate Root

**Problema:** No modelo original, `ReadingPost` era uma entity filha de `UserBook`. Isso obrigaria carregar todos os posts na memória ao adicionar um novo (um usuário pode ter centenas de posts sobre o mesmo livro).

**Teste do aggregate aplicado:** Não existe invariante de negócio que exija validar um ReadingPost olhando para outros ReadingPosts na mesma transação. Cada post é independente:

- "Post deve ter conteúdo OU progresso" → regra interna do post
- "Status muda para Reading ao postar com progresso" → reação ao evento, não invariante cross-post
- Não há regra de "máximo N posts" ou "um post por dia"

**Decisão:** ReadingPost é aggregate próprio com referência por ID ao UserBook.

**Desnormalização intencional:** ReadingPost armazena `UserId` e `BookId` diretamente (além de `UserBookId`) para evitar joins desnecessários em queries do Social/Feed.

**Coordenação de progresso:** Quando um post com progresso é criado, o handler do command carrega o `UserBook` e atualiza o `CurrentProgress` na mesma transação (consistência forte). Justificativa: estamos no mesmo bounded context e a atualização do progresso é expectativa imediata do usuário.

### 2.2 Wishlist como flag em UserBook

**Problema:** Wishlist deveria ser um conceito separado ou um atributo de UserBook?

**Decisão:** Wishlist é um `bool` dentro de UserBook. "Adicionar à biblioteca" significa reconhecer a existência do livro na vida do leitor — seja para ler futuramente (wishlist), lendo, lido, ou abandonado.

**Regra de domínio:** Quando o status muda para `Reading`, `Finished`, `Abandoned` ou `Paused`, o sistema automaticamente seta `Wishlist = false`. Wishlist só é válido com status `NotStarted`.

### 2.3 UserList contém seus itens (UserListItem como entity filha)

**Problema:** A lista deveria conter referências aos livros, ou o relacionamento seria externo?

**Decisão:** UserList contém `List<UserListItem>` como entities filhas.

**Justificativa — invariantes que exigem os itens:**
- Livro não pode estar duplicado na mesma lista
- Reordenação de livros requer acesso aos itens
- `BooksCount` deve ser consistente com a quantidade real

**Trade-off aceito:** Diferente de ReadingPosts (content de até 2000 chars), UserListItem é minúsculo (IDs + order + timestamp). Carregar 500 itens é trivial em memória.

### 2.4 ReadingPost pertence ao Library, não ao Social

**Problema:** ReadingPost tem aspectos sociais (likes, comments, feed). Deveria estar no Social?

**Teste aplicado:** "O conceito faz sentido sem a parte social?"
- ReadingPost sem Social → "registro pessoal da minha leitura" → **faz sentido** ✅
- ReadingPost sem Library → "post sobre um livro que não está na minha estante" → **não faz sentido** ❌

**Decisão:** ReadingPost é um conceito de leitura pessoal com efeitos colaterais sociais. O Library é a fonte de verdade. O Social consome posts via integration events para alimentar o feed e gerenciar interações (likes/comments).

**Fluxo de dados:**
- Library → Social: `ReadingPostCreatedIntegrationEvent` → Social cria FeedItem
- Social → Library: `ContentLikedIntegrationEvent` → Library incrementa LikesCount no ReadingPost

### 2.5 Soft Delete no UserBook

**Problema:** Quando o usuário remove um livro da biblioteca, o que acontece com os ReadingPosts e referências em UserLists?

**Decisão:** UserBook usa soft delete via `DeletedAt: DateTime?`.

**Comportamento ao remover:**
- UserBook recebe `DeletedAt = DateTime.UtcNow` (soft delete)
- ReadingPosts permanecem vinculados ao UserBook deletado (histórico preservado)
- UserListItems são removidos (hard delete) — livro sai de todas as listas automaticamente
- Social é notificado via integration event para ocultar feed items relacionados

**Re-adição do mesmo livro:** Cria um novo UserBook com novo Id (novo ciclo de leitura). O registro anterior permanece como histórico. Unique index filtrado no banco:
```sql
CREATE UNIQUE INDEX ix_user_books_user_book 
    ON user_books(user_id, book_id) 
    WHERE deleted_at IS NULL;
```

**Queries:** Global Query Filter no EF Core (`HasQueryFilter(ub => ub.DeletedAt == null)`) para filtrar deletados por padrão em todas as queries.

**DeletedAt não vai no SharedKernel.** Nem toda entity usa soft delete. Implementado diretamente no UserBook, mantendo o SharedKernel genérico (coerente com decisão 4.1 sobre Rating).

---

## 3. Modelo de Aggregates (visão geral)

```
UserBook (Aggregate Root)
├── Id: Guid
├── UserId: Guid
├── BookId: Guid
├── Status: ReadingStatus (enum)
├── CurrentProgress: Progress? (VO)
├── Wishlist: bool
├── Rating: Rating? (VO)
├── DeletedAt: DateTime? (soft delete)
├── AddedAt: DateTime
└── UpdatedAt: DateTime

ReadingPost (Aggregate Root)
├── Id: Guid
├── UserBookId: Guid (referência por ID)
├── UserId: Guid (desnormalizado)
├── BookId: Guid (desnormalizado)
├── Content: string? (max 2000)
├── Progress: Progress? (VO)
├── ReadingDate: Date
├── LikesCount: int (desnormalizado, fonte: Social)
├── CommentsCount: int (desnormalizado, fonte: Social)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

UserList (Aggregate Root)
├── Id: Guid
├── UserId: Guid
├── Name: string (2-50)
├── Description: string? (max 500)
├── IsPublic: bool (default false)
├── Items: List<UserListItem> (entity filha)
├── BooksCount: int (desnormalizado)
├── LikesCount: int (desnormalizado, fonte: Social)
├── CommentsCount: int (desnormalizado, fonte: Social)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

UserListItem (Entity — filha de UserList)
├── Id: Guid
├── UserBookId: Guid
├── Order: int
└── AddedAt: DateTime

BookSnapshot (Read Model)
├── BookId: Guid
├── Title: string
├── AuthorDisplay: string
├── CoverUrl: string?
├── PageCount: int?
└── UpdatedAt: DateTime
```

---

## 4. Value Objects

### 4.1 Rating

**Representação interna:** `int` de 1 a 10, representando meias-estrelas.

| Estrelas (exibição) | Valor interno |
|---------------------|---------------|
| 0.5 ★              | 1             |
| 1.0 ★              | 2             |
| 1.5 ★              | 3             |
| ...                 | ...           |
| 5.0 ★              | 10            |

**Por que `int` e não `decimal`:**
- Validação trivial: `value >= 1 && value <= 10`
- Sem problemas de precisão decimal
- `SMALLINT` no banco é mais eficiente
- Aritmética inteira no cálculo de médias, converte só na saída

**Propriedade pública:** `decimal Stars => Value / 2.0m` para exibição.

**Conversão API:** A API recebe/retorna estrelas (0.5-5.0). O command handler ou VO converte: `stars * 2 = valorInterno`.

**Não vai no SharedKernel.** Cada bounded context (Library, Catalog) define seu próprio Rating. O SharedKernel contém apenas infraestrutura genérica (BaseEntity, ValueObject, IDomainEvent, etc.), zero regras de negócio. Integration events carregam o valor primitivo (`int`), não o VO. Cada serviço é responsável por interpretar o valor no seu contexto.

### 4.2 Progress

**Composição:** `Value (int)` + `Type (ProgressType)`

**Validação interna (auto-contida):**
- `Value >= 0` (para ambos os tipos)
- Se `Type == Percentage`: `Value <= 100`

**Validação externa (não é responsabilidade do VO):**
- Se `Type == Page`: `Value <= PageCount` → validado pelo aggregate ou command handler, que tem acesso ao BookSnapshot.

**Justificativa:** Value Objects devem ser auto-contidos. Progress não conhece o contexto do livro (PageCount), então delega essa validação para quem tem o contexto.

## 5. Enums

### 5.1 ReadingStatus

```
NotStarted, Reading, Finished, Abandoned, Paused
```

**Sem state machine.** Todas as transições entre status são válidas. Justificativa: o usuário pode clicar acidentalmente em qualquer status e precisa poder corrigir livremente. A complexidade de uma state machine não se justifica para o caso de uso.

### 5.2 ProgressType

```
Page, Percentage
```

Usado em composição com o VO `Progress`.

## 6. Domain Events

Princípio aplicado: **YAGNI** — apenas eventos com pelo menos um consumidor identificado. Adicionar um evento futuro é aditivo (um `AddDomainEvent()` no método do aggregate), remover é destrutivo.

### Importante: Domain Events vs Integration Events

```
Domain Event (interno ao Library)
    → Handled no mesmo processo/transação
    → Dados magros (IDs)

Integration Event (cruza bounded contexts)
    → Publicado via mensageria (RabbitMQ)
    → Handler do domain event traduz → integration event com dados enriquecidos
```

Exemplo de fluxo:
```
1. UserBook.Rate(7) → adiciona UserBookRatedDomainEvent
2. Repository.SaveChanges() → persiste + despacha domain events
3. UserBookRatedDomainEventHandler → publica UserBookRatedIntegrationEvent para Catalog e Social
```

Domain events ficam no `Library.Domain`. Integration events e handlers ficam no `Library.Application`.

### 6.1 UserBook (5 eventos)

| Evento | Consumidores | Dados |
|--------|-------------|-------|
| `BookAddedToLibraryDomainEvent` | Social (feed) | UserId, BookId, Wishlist |
| `BookRemovedFromLibraryDomainEvent` | Social (ocultar feed items), UserList (hard delete dos items) | UserId, BookId, UserBookId |
| `ReadingStatusChangedDomainEvent` | Social (feed: "X começou a ler Y") | UserId, BookId, OldStatus, NewStatus |
| `UserBookRatedDomainEvent` | Catalog (recalcular média), Social (feed) | UserId, BookId, RatingValue, PreviousRatingValue |
| `UserBookRatingRemovedDomainEvent` | Catalog (recalcular média) | UserId, BookId, PreviousRatingValue |

**Nota:** `ReadingStatusChangedDomainEvent` carrega OldStatus e NewStatus para que o Social possa diferenciar transições (ex: "começou a ler" é mais relevante no feed que "pausou").

### 6.2 ReadingPost (2 eventos)

| Evento | Consumidores | Dados |
|--------|-------------|-------|
| `ReadingPostCreatedDomainEvent` | Social (feed) | PostId, UserBookId, UserId, BookId |
| `ReadingPostDeletedDomainEvent` | Social (remover do feed) | PostId, UserId, BookId |

**Cortado por YAGNI:** `ReadingPostUpdatedDomainEvent` — nenhum consumidor claro. O feed mostra snapshot do momento da criação; edição do post não precisa propagar. Adicionável no futuro sem refatoração.

### 6.3 UserList (1 evento)

| Evento | Consumidores | Dados |
|--------|-------------|-------|
| `UserListDeletedDomainEvent` | Social (limpar likes/comments) | ListId, UserId |

**Cortados por YAGNI:**
- `UserListCreatedDomainEvent` — criar lista vazia não é fato social relevante
- `UserListUpdatedDomainEvent` — renomear lista não tem consumidor
- `BookAddedToListDomainEvent` — barulhento demais no feed (usuário organizando 20 livros gera 20 eventos). Custo de adicionar depois é baixo: um `AddDomainEvent()` no método `UserList.AddBook()`
- `BookRemovedFromListDomainEvent` — nenhum consumidor

### 6.4 Resumo

**Total: 8 domain events.** Cada um com pelo menos um consumidor claro.

| Aggregate | Eventos | Lista |
|-----------|---------|-------|
| UserBook | 5 | Added, Removed, StatusChanged, Rated, RatingRemoved |
| ReadingPost | 2 | Created, Deleted |
| UserList | 1 | Deleted |

---

## 7. Regras de Negócio

### 7.1 UserBook

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Unicidade** | Um usuário só pode ter um UserBook ativo (não-deletado) por BookId | Banco (unique index filtrado) + handler |
| **BookId válido** | O BookId deve referenciar um BookSnapshot existente | Command handler |
| **Wishlist auto-reset** | Ao mudar status para Reading, Finished, Abandoned ou Paused, Wishlist é setado para `false` automaticamente | Aggregate (método ChangeStatus) |
| **Finished auto-progress** | Ao marcar como Finished, se existir progresso, converter para Percentage 100%. Se não existir, criar Progress(100, Percentage) | Aggregate (método ChangeStatus) |
| **Rating independente** | Rating é nullable, pode ser adicionado/removido a qualquer momento, independente do status | Aggregate |
| **Soft delete** | Remoção marca DeletedAt. Posts preservados. Items de listas removidos (hard delete) | Aggregate (método Remove) |

### 7.2 ReadingPost

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Conteúdo obrigatório** | Deve ter Content OU Progress (ou ambos). Não pode ser totalmente vazio | Aggregate (construtor) |
| **Content max length** | Máximo 2000 caracteres | Aggregate + FluentValidation |
| **Progress page** | Value >= 0 | Value Object (Progress) |
| **Progress percentage** | Value entre 0 e 100 | Value Object (Progress) |
| **Progress vs PageCount** | Se type = Page, value <= PageCount do livro | Command handler (acessa BookSnapshot) |
| **UserBook válido** | Deve referenciar UserBook existente e não-deletado | Command handler |
| **Ownership** | Só o dono do UserBook pode criar posts nele | Command handler |

### 7.3 UserList

| Regra | Descrição | Onde é validada |
|-------|-----------|-----------------|
| **Nome** | 2-50 caracteres | Aggregate + FluentValidation |
| **Nome único por usuário** | Case-insensitive. "Ficção Científica" e "ficção científica" são a mesma lista | Command handler (consulta repositório) |
| **Descrição** | Máximo 500 caracteres, opcional | Aggregate + FluentValidation |
| **Máximo de listas** | 100 listas por usuário | Command handler (consulta repositório) |
| **Sem duplicata** | Mesmo UserBookId não pode estar duas vezes na mesma lista | Aggregate (método AddBook) |
| **Visibilidade padrão** | `IsPublic = false` | Aggregate (construtor) |

### 7.4 Rating (Value Object)

| Regra | Descrição |
|-------|-----------|
| **Range** | Valor interno entre 1 e 10 (meias-estrelas) |
| **Conversão** | API recebe 0.5-5.0, internamente armazena 1-10 |
| **Validação** | `value >= 1 && value <= 10` |

### 7.5 Progress (Value Object)

| Regra | Descrição |
|-------|-----------|
| **Value** | `>= 0` para ambos os tipos |
| **Percentage** | `<= 100` |
| **Page** | Sem limite superior no VO (validação externa contra PageCount) |
| **Consistência** | Value e Type são sempre definidos juntos (nunca parcial) |