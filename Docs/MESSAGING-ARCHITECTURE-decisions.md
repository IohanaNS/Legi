# Mensageria (RabbitMQ + Outbox próprio) — Decisões de Arquitetura

Documento com as decisões de design para integração assíncrona entre bounded contexts via RabbitMQ, utilizando uma implementação própria do padrão Outbox.

**Status de Implementação:** 📋 Planejado

---

## 1. Visão Geral

### 1.1 O Problema

Os bounded contexts do Legi (Identity, Catalog, Library, Social) são isolados — cada um com seu próprio banco de dados, sua própria API, seu próprio domínio. Mas ações em um serviço frequentemente precisam provocar efeitos em outros:

- Um livro criado no Catalog precisa gerar um `BookSnapshot` no Library
- Um rating no Library precisa recalcular `average_rating` no Catalog
- Um post de progresso no Library precisa criar um `FeedItem` no Social
- A deleção de um usuário no Identity precisa limpar dados em todos os serviços

Sem mensageria, essas dependências criam acoplamento temporal (se o Catalog estiver fora, o Library falha), acoplamento de latência (o usuário espera todos os serviços responderem), e falhas em cascata (um serviço lento derruba os outros).

### 1.2 A Solução

Comunicação assíncrona via **integration events** publicados em um message broker (RabbitMQ). O produtor publica "algo aconteceu" e retorna imediatamente. Os consumidores processam no seu próprio tempo. Se um consumidor está fora, a mensagem fica na fila até ele voltar.

### 1.3 Workarounds Atuais (pré-mensageria)

| Workaround | Serviço | Descrição | Remoção |
|------------|---------|-----------|---------|
| BookSnapshot inline | Library | `AddBookToLibraryCommand` aceita campos opcionais e cria `BookSnapshot` inline quando não existe | Fase 2 |
| Stub domain event handlers | Social | `CommentCreatedDomainEventHandler`, `ContentLikedDomainEventHandler`, etc. — logging only, aguardando RabbitMQ | Fase 4 |
| ContentSnapshot/FeedItem inline | Social | Handlers de commands criam snapshots e feed items inline | Fase 4 |

---

## 2. Decisões Fundamentais

### 2.1 Comunicação assíncrona (eventos) vs síncrona (HTTP)

**Problema:** Como os serviços devem se comunicar entre si?

**Opções avaliadas:**

| Opção | Vantagens | Desvantagens |
|-------|-----------|--------------|
| **HTTP síncrono** | Simples, familiar, resposta imediata | Acoplamento temporal, latência acumulada, falhas em cascata |
| **Eventos assíncronos** | Desacoplamento temporal, baixa latência para o usuário, resiliência | Consistência eventual, complexidade de infraestrutura, debugging mais difícil |

**Decisão:** Eventos assíncronos para toda comunicação entre bounded contexts.

**Justificativa:** Nenhum dos fluxos entre serviços exige resposta síncrona. Quando o Library salva um rating, não precisa *esperar* o Catalog recalcular a média. Quando o Identity deleta um usuário, não precisa *esperar* todos os serviços limparem seus dados. A consistência eventual é aceitável em todos os casos identificados.

**Zero chamadas HTTP entre serviços.** Se um serviço precisa de dados de outro, usa snapshots locais (BookSnapshot, ContentSnapshot, UserProfile) mantidos atualizados via eventos. Isso elimina completamente a dependência em tempo de execução.

### 2.2 Implementação própria do Outbox + cliente RabbitMQ direto

**Problema:** Usar RabbitMQ diretamente (`RabbitMQ.Client`), uma abstração existente, ou implementar nossa própria infraestrutura de mensageria?

**Opções avaliadas:**

| Opção | Vantagens | Desvantagens |
|-------|-----------|--------------|
| **RabbitMQ.Client direto + outbox próprio** | Controle total, aprendizado profundo do padrão Outbox, zero dependências comerciais ou em risco, alinhado com o precedente do Mediator custom | Mais código a manter, ~1-2 semanas de plumbing antes do primeiro evento fluir |
| **MassTransit v8 (Apache 2.0)** | Outbox nativo, retry/dead-letter automáticos, topologia declarativa | v8 não tem suporte oficial para .NET 10. Suporte termina ao fim de 2026. Após isso, v9 (comercial) é o caminho oficial |
| **MassTransit v9 (comercial)** | Mantida ativamente, suporte oficial .NET 10 | Licença comercial. Custo desproporcional para projeto pessoal de aprendizado |
| **NServiceBus** | Maturidade, sagas robustas | Licença comercial |
| **Wolverine** | MIT, .NET 10 nativo, design code-first moderno | Modelo conceitual diferente (sem `IConsumer<T>`/`IBus`). Exigiria repensar boa parte do design de adapters |
| **OpenTransit (fork de MT v8)** | OSS, .NET 10 nativo | Projeto muito jovem (Nov 2025), mantenedor único, sem track record de produção |

**Decisão:** Implementação própria. Cliente: `RabbitMQ.Client` 7.x (oficial, MPL 2.0 / Apache 2.0, mantido pela equipe Broadcom RabbitMQ).

**Justificativa:**

- **Sustentabilidade.** Em 2025, MassTransit v9 transicionou para licença comercial. v8 OSS continua mantida apenas até fim de 2026 e não tem suporte oficial para .NET 10. Adotar v8 agora é construir sobre dependência em sunset; adotar v9 é pagar licença comercial em projeto pessoal.
- **Cliente RabbitMQ é estável.** `RabbitMQ.Client` é mantido oficialmente pela equipe RabbitMQ na Broadcom, dual-licenciado MPL 2.0 / Apache 2.0, sem risco de relicenciamento. É a base sobre a qual MassTransit, NServiceBus e Wolverine constroem.
- **Outbox próprio é viável.** O núcleo do padrão Outbox é pequeno (~300-500 linhas para uma implementação correta para Legi). A complexidade real do MassTransit não está no Outbox em si, mas em features secundárias (sagas, schedulers, request/response, distributed transactions) que Legi não usa.
- **Aprendizado profundo.** Implementar o Outbox força entender atomicidade transacional, deduplicação de inbox, locking concorrente, semântica de retry e ordering — conhecimento diretamente alinhado com o objetivo DDD/distributed systems do projeto.
- **Precedente no projeto.** O Mediator custom já estabelece o padrão de "implementar internamente o que pode ser entendido em poucas centenas de linhas, depender de bibliotecas externas para o que é genuinamente complexo". Outbox cai no primeiro grupo. Sagas distribuídas, se um dia forem necessárias, cairiam no segundo.

**Trade-off explícito:** Esta decisão custa ~1-2 semanas de plumbing inicial antes de o primeiro evento fluir, comparado a ~2-3 dias com MassTransit. É um custo real, aceito porque (a) este é um projeto de aprendizado, (b) o ROI em entendimento é alto, (c) elimina dependência em biblioteca em sunset ou comercial.

**Trabalho futuro (fora do escopo desta fase):** Se a implementação resultar limpa o suficiente, há intenção exploratória de extraí-la como biblioteca OSS. Esta possibilidade NÃO molda decisões de design nesta fase — o foco é "fazer funcionar para Legi". Generalizações, abstrações de pluggability, e API pública estável são preocupações deliberadamente adiadas. Ver seção 4.2 para a disciplina mantida (sem tipos Legi-específicos no `Legi.Messaging`) que mantém extração futura *possível* sem impor o custo de design-as-library agora.

### 2.3 Outbox Pattern: obrigatório

**Problema:** Como garantir que o banco de dados e o message broker fiquem consistentes?

**O dual-write problem:** Considere o fluxo de rating:

```
1. SaveChangesAsync() → UPDATE user_books SET rating = 7  → COMMIT ✅
2. PublishAsync() → enviar mensagem para RabbitMQ          → FALHA ❌ (rede, crash, timeout)
```

Resultado: o rating está salvo no banco do Library, mas o Catalog nunca recalcula a média. A inconsistência é **permanente** — não há mecanismo de auto-correção.

**Decisão:** Outbox Pattern obrigatório para toda publicação de integration events.

**Como funciona:**

```
┌─ Transação única no banco ─────────────────────────┐
│                                                      │
│  1. UPDATE user_books SET rating = 7                 │
│  2. INSERT INTO outbox_messages (payload, type...)   │
│                                                      │
│  COMMIT (ambos ou nenhum)                            │
└──────────────────────────────────────────────────────┘

Depois, independentemente:

┌─ Background Worker (OutboxDispatcher próprio) ────────┐
│                                                       │
│  1. SELECT ... FOR UPDATE SKIP LOCKED                 │
│     FROM outbox_messages                              │
│     WHERE processed_at IS NULL                        │
│     ORDER BY occurred_at LIMIT batch_size             │
│  2. Publish para RabbitMQ (com publisher confirms)    │
│  3. UPDATE outbox_messages SET processed_at = NOW()   │
│                                                       │
│  Se RabbitMQ estiver fora → retry com backoff         │
└───────────────────────────────────────────────────────┘
```

**Garantia:** Se a transação de domínio foi committed, a mensagem *eventualmente* será entregue. Se a transação falhou (rollback), a mensagem nunca existiu. Não há janela de inconsistência.

**Implementação:** Própria, no projeto `Legi.Messaging`. Componentes principais:

- `OutboxMessage` — entity EF Core mapeada para `outbox_messages` em cada DB de serviço
- `OutboxEventBus<TContext> : IEventBus` — escreve a mensagem no `DbContext` corrente; a inserção commita junto com as mudanças de domínio via `SaveChangesAsync` (ver decisão 2.5)
- `OutboxDispatcherWorker : BackgroundService` — polling do outbox a cada N ms, publica via `IRabbitMqPublisher`, marca como processado. Usa `FOR UPDATE SKIP LOCKED` para suportar múltiplas instâncias
- `IRabbitMqPublisher` — wrapper sobre `RabbitMQ.Client` com publisher confirms, persistent messages, declaração de topologia

Detalhes completos em seção 4.2 e 7.3.

### 2.4 Consistência eventual é aceitável

**Problema:** Com o outbox, existe um delay entre "salvei no banco" e "a mensagem chegou no consumidor". O sistema fica temporariamente inconsistente.

**Análise por fluxo:**

| Fluxo | Delay aceitável? | Justificativa |
|-------|-------------------|---------------|
| Catalog → Library (BookSnapshot) | ✅ Sim | BookSnapshot é pré-requisito para AddBookToLibrary. Se ainda não chegou, o handler retorna 404 — o usuário tenta novamente em segundos |
| Library → Catalog (rating) | ✅ Sim | AverageRating ser atrasado por 1-2 segundos é imperceptível |
| Library → Social (feed) | ✅ Sim | O feed ser atrasado por segundos é comportamento esperado em redes sociais |
| Social → Library (likes/comments count) | ✅ Sim | Contadores desnormalizados com delay de segundos são aceitáveis |
| Identity → todos (user deleted) | ✅ Sim | Limpeza de dados pode levar segundos, não afeta UX imediata |

**Decisão:** Consistência eventual é aceitável em 100% dos fluxos. Nenhum fluxo identificado requer consistência forte cross-service.

### 2.5 Dispatch de domain events: pré-save via Interceptor

**Problema:** O outbox transacional (decisão 2.3) exige que a escrita no `outbox_message` aconteça *dentro da mesma transação* que persiste as mudanças de domínio. Caso contrário, o dual-write problem retorna: a transação de domínio commita, mas o INSERT no outbox falha — e a mensagem nunca é enviada.

**Estado atual no Legi:** Os `DbContext`s fazem override de `SaveChangesAsync` e dispatcham domain events via `IMediator.Publish()` **após** `base.SaveChangesAsync()`. Esse padrão era correto enquanto não havia mensageria — os handlers apenas atualizavam read models locais no mesmo processo. Com integration events, ele quebra a garantia do outbox: o handler que chama `IEventBus.PublishAsync()` rodaria fora da transação de domínio.

**Opções avaliadas:**

| Opção | Como funciona | Avaliação |
|-------|---------------|-----------|
| **Manter dispatch pós-save; handler abre novo DbContext/transação** | Handler cria scope próprio, chama `IEventBus.PublishAsync`, faz `SaveChangesAsync` em transação separada | Reintroduz dual-write em menor escala: se a segunda transação falha, o estado de domínio está committed mas a mensagem não existe. Não resolve o problema fundamental — só o reduz |
| **Mover dispatch para ANTES de `base.SaveChangesAsync`** | Interceptor coleta domain events, chama Mediator, handlers escrevem integration events no outbox via `IEventBus`, tudo é persistido em uma única chamada a `base.SaveChangesAsync` | Atomicidade total — ou tudo commita, ou nada commita. Zero janela de inconsistência |

**Decisão:** Dispatch de domain events acontece **antes** de `base.SaveChangesAsync()`, implementado via `SaveChangesInterceptor`.

**Implementação: `DispatchDomainEventsInterceptor` no SharedKernel**

```csharp
public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;

    public DispatchDomainEventsInterceptor(IMediator mediator)
        => _mediator = mediator;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, ct);

        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private async Task DispatchDomainEventsAsync(DbContext ctx, CancellationToken ct)
    {
        // Loop: handlers podem modificar entities e levantar novos eventos
        while (true)
        {
            var entities = ctx.ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            if (entities.Count == 0) return;

            var events = entities.SelectMany(e => e.DomainEvents).ToList();
            entities.ForEach(e => e.ClearDomainEvents());

            foreach (var @event in events)
                await _mediator.Publish(@event, ct);
        }
    }
}
```

**Por que interceptor e não override de `SaveChangesAsync`:**

- **Separação de concerns:** o `DbContext` não precisa conhecer `IMediator`, domain events ou retry policy. Volta a ser um adaptador de persistência puro.
- **Reuso:** um único interceptor serve os 4 `DbContext`s (Identity, Catalog, Library, Social). Zero duplicação.
- **Testabilidade:** interceptor isolado é testável sem subir um `DbContext`.
- **Retry policy desacoplada:** hoje a retry policy de EF Core no `SaveChangesAsync` override coexiste confusamente com o dispatch. Com interceptor, cada concern vive no seu lugar.

**Registro:** Cada `DbContext` registra o interceptor em `AddDbContext`:

```csharp
services.AddScoped<DispatchDomainEventsInterceptor>();

services.AddDbContext<LibraryDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>());
});
```

**Loop de dispatch:** Um handler pode modificar outra entity que, por sua vez, levanta um novo domain event. O loop garante que todos os eventos sejam processados antes do commit. Sem loop, eventos levantados por handlers ficariam órfãos até o próximo `SaveChangesAsync` — quebrando a semântica de "eventos do ato atual".

**Impacto no refactor:** Os 4 `SaveChangesAsync` overrides existentes ficam mais simples. A coleta e o dispatch de domain events saem do override (ou o override é removido completamente, se só fazia isso). Ver seção 9 para o impacto por serviço.

### 2.6 Snapshots locais em cada consumidor (não denormalização em eventos)

**Problema:** Quando um consumidor precisa de dados "de display" (título do livro, cover, autor), há duas abordagens possíveis:

1. **Denormalizar em cada integration event** — o produtor enriquece o evento com título, cover, autor.
2. **Manter um read model local no consumidor** — o consumidor mantém seu próprio `BookSnapshot`, atualizado via `BookCreatedIntegrationEvent` / `BookUpdatedIntegrationEvent`.

**Análise concreta — o caso do `UserBookRatedIntegrationEvent`:**

Este evento é consumido por *dois* serviços:

- **Catalog** — recalcula `average_rating` no `Book`. Precisa apenas de `BookId, UserId, Rating, PreviousRating`.
- **Social** — cria `FeedItem (BookRated)` no feed. Precisa adicionalmente de `BookTitle`, `BookAuthorDisplay`, `BookCoverUrl` para renderizar o card sem joins externos.

Se denormalizamos o evento com dados do livro, o payload fica desalinhado com sua semântica ("usuário deu uma nota") e carrega dados inúteis para o Catalog. Se não denormalizamos, Social não consegue montar o `FeedItem` sem um read model local.

**Decisão:** Cada bounded context que precisa exibir dados do livro mantém seu próprio `BookSnapshot`, alimentado pelos eventos `BookCreatedIntegrationEvent` e `BookUpdatedIntegrationEvent` do Catalog.

**Consequência para Social:** Social ganha um `BookSnapshot` local (análogo ao que Library já tem), além dos consumers para `BookCreated` e `BookUpdated`.

**Consequência para os contratos de eventos Library → Social:** Os campos `BookTitle`, `BookAuthorDisplay`, `BookCoverUrl` saem de:

- `BookAddedToLibraryIntegrationEvent`
- `ReadingStatusChangedIntegrationEvent`
- `ReadingPostCreatedIntegrationEvent`

Os eventos ficam focados em sua semântica (quem, o quê, quando). O display é resolvido no Social via join com `BookSnapshot`.

**Vantagens:**

- **Eventos focados.** Cada evento carrega *exatamente* o que sua semântica sugere, nada mais.
- **Dados sempre frescos.** Quando o Catalog atualiza o cover de um livro, o `BookSnapshot` do Social é atualizado automaticamente via `BookUpdated`. Todos os `FeedItem`s daquele livro passam a exibir o cover novo, sem lógica de back-population.
- **Simetria.** Library e Social ambos mantêm `BookSnapshot` local — mesmo padrão, mesmo consumer, mesmo contrato.
- **Payload menor.** Eventos Library → Social ficam mais compactos.

**Custos:**

- Uma tabela a mais no Social (`book_snapshots`).
- Dois consumers a mais no Social (`BookCreated`, `BookUpdated`).
- Custo compensado pela eliminação de 3 campos redundantes em 3 eventos diferentes.

**Princípio geral:** Integration events carregam *semântica e IDs*. Display data é resolvida por read models locais alimentados pelos eventos do produtor dos dados em questão (o Catalog é quem "dita" os dados do livro). Evitar carregar display data em eventos de comportamento.

---

## 3. Arquitetura de Camadas

### 3.1 IEventBus no SharedKernel (Ports & Adapters)

**Problema:** A infraestrutura de mensageria (RabbitMQ, outbox, serialização) é uma preocupação de Infrastructure. A Application layer não pode depender dela sem violar a Dependency Rule. Mas a Application layer é onde os domain event handlers vivem — e eles precisam publicar integration events.

**Decisão:** Abstrair via interface no SharedKernel, implementar na infraestrutura.

```
SharedKernel:  IEventBus (interface — a "porta")
Messaging:     OutboxEventBus<TContext> (implementação — o "adaptador")
Application:   usa IEventBus sem saber que existe outbox, RabbitMQ, ou serialização
```

**Precedente no projeto:** Idêntico ao padrão do Mediator custom:

| Conceito | Interface (SharedKernel) | Implementação |
|----------|--------------------------|---------------|
| Mediator | `IMediator` | `Mediator` (SharedKernel) |
| Event Bus | `IEventBus` (novo) | `OutboxEventBus<TContext>` (Messaging) |
| Repositórios | `IUserBookRepository` | `UserBookRepository` (Infrastructure) |

```csharp
// Legi.SharedKernel/IEventBus.cs
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default)
        where T : class;
}
```

Um arquivo. Uma interface. Um método. Zero dependências.

**Sobre `OutboxEventBus<TContext>`:** A implementação é genérica sobre o `DbContext` do serviço (`IdentityDbContext`, `LibraryDbContext`, etc.) porque cada serviço tem sua própria tabela de outbox no seu próprio banco — a inserção da mensagem precisa acontecer no DbContext correto para participar da transação de domínio. A registração na DI é feita uma vez por serviço, em sua Infrastructure layer.

### 3.2 IIntegrationEvent herda INotification

**Problema:** No lado do consumidor, quando uma mensagem chega do RabbitMQ, como ela chega até o handler de negócio na Application layer?

**Decisão:** Integration events implementam `INotification` (do SharedKernel), permitindo dispatch via Mediator existente.

```csharp
// Legi.Contracts/IIntegrationEvent.cs
public interface IIntegrationEvent : INotification { }
```

**Fluxo no consumidor:**

```
RabbitMQ entrega mensagem
    │
    ▼
IntegrationEventConsumer<T> (Messaging — adaptador genérico)
    │ recebe evento, chama IMediator.Publish(evento)
    ▼
INotificationHandler<T> (Application — lógica de negócio)
    │ processa o evento (cria BookSnapshot, atualiza contador, etc.)
    ▼
Fim
```

**Justificativa:** A Application layer já sabe trabalhar com `INotificationHandler<T>` para domain events. Reusar o mesmo mecanismo para integration events significa zero conceitos novos na Application layer. O handler nem sabe se foi invocado por um domain event local ou por uma mensagem do RabbitMQ — e isso é intencional.

**Distinção semântica:** `INotificationHandler<UserBookRatedDomainEvent>` = handler de evento interno. `INotificationHandler<UserBookRatedIntegrationEvent>` = handler de evento externo. O `IIntegrationEvent` marker torna grep/find-usages significativo.

### 3.3 Pipeline do consumidor: RabbitMQ → Inbox → Mediator → Handler

**Problema:** Quando uma mensagem chega do RabbitMQ no serviço consumidor, ela precisa percorrer um caminho até virar uma chamada a um `INotificationHandler<T>` na Application layer. Esse caminho precisa: deserializar o tipo correto, deduplicar (inbox), publicar via Mediator, ack apenas em sucesso.

**Decisão:** Um pipeline em três camadas, todo dentro de `Legi.Messaging`, sem MassTransit:

```
RabbitMQ
   │
   ▼ (mensagem chega no canal)
RabbitMqConsumerHost : BackgroundService
   │ - 1 consumer host por integration event type registrado
   │ - mantém connection/channel ao RabbitMQ
   │ - desserializa o payload via Type column
   │ - delega para o IntegrationEventDispatcher
   ▼
IntegrationEventDispatcher
   │ - 1. checa InboxMessages: se MessageId já processado, ack e retorna
   │ - 2. abre scope DI, resolve IMediator
   │ - 3. await mediator.Publish(integrationEvent)
   │ - 4. INSERT INTO inbox_messages (na transação do handler)
   │ - 5. ack a mensagem no RabbitMQ
   ▼
INotificationHandler<TIntegrationEvent>
   │ - lógica de negócio na Application layer
   │ - serviço NÃO sabe que veio de RabbitMQ — é só um handler de notification
```

**Por que separar Host e Dispatcher:**

- `RabbitMqConsumerHost` cuida de conectividade, lifecycle de canal, deserialização. Conhece RabbitMQ.
- `IntegrationEventDispatcher` cuida de inbox/dedup, scope de DI, dispatch via Mediator. **Não conhece RabbitMQ** — recebe um objeto já deserializado e um `MessageId`. Isso permite testar o pipeline de consumo sem broker (basta chamar o dispatcher diretamente).

**Resultado:** Uma única classe `RabbitMqConsumerHost<T>` genérica serve para todos os integration events. A Application layer continua escrevendo apenas `INotificationHandler<T>` — exatamente o mesmo padrão dos domain event handlers. Zero conceitos novos na Application.

**Inbox é parte essencial do pipeline.** Sem deduplicação, redeliveries do RabbitMQ (causadas por crashes, timeouts, network blips) processariam o mesmo evento múltiplas vezes. A entrada na `inbox_messages` é feita na *mesma transação* que o handler usa para suas mudanças — se o handler falha, a mensagem volta para a fila e será reprocessada (idempotência por construção).

### 3.4 Fluxo completo: Domain Event → Integration Event → Consumer

Exemplo concreto: Library publica rating, Catalog consome.

**Lado do produtor (Library):**

```
1. UserBook.Rate(7)
   └── adiciona UserBookRatedDomainEvent à coleção de domain events

2. CommandHandler chama LibraryDbContext.SaveChangesAsync()

3. DispatchDomainEventsInterceptor.SavingChangesAsync (ANTES do commit)
   │ Loop de dispatch:
   ├── Coleta domain events de todas entities trackadas
   ├── IMediator.Publish(UserBookRatedDomainEvent)
   │
   └── UserBookRatedDomainEventHandler (Application)
       ├── Traduz: DomainEvent → IntegrationEvent
       └── await IEventBus.PublishAsync(UserBookRatedIntegrationEvent)
           └── OutboxEventBus<LibraryDbContext>.PublishAsync()
               ├── Serializa o evento (System.Text.Json)
               ├── Cria OutboxMessage com Id, Type, Payload, OccurredAt
               └── _ctx.OutboxMessages.Add(msg)
                   (pendente no ChangeTracker do mesmo DbContext)

4. base.SaveChangesAsync() executa:
   ├── UPDATE user_books SET rating = 7
   ├── INSERT INTO outbox_messages (...)
   └── COMMIT (ambos atômicos)

5. OutboxDispatcherWorker (BackgroundService no serviço Library)
   ├── Polling: SELECT ... FOR UPDATE SKIP LOCKED
   │           FROM outbox_messages
   │           WHERE processed_at IS NULL
   │           ORDER BY occurred_at LIMIT batch_size
   ├── IRabbitMqPublisher.PublishAsync (com publisher confirms)
   │   └── Aguarda ACK do broker
   └── UPDATE outbox_messages SET processed_at = NOW()
```

**Lado do consumidor (Catalog):**

```
6. RabbitMqConsumerHost<UserBookRatedIntegrationEvent> (Messaging, BackgroundService)
   ├── Recebe mensagem do canal RabbitMQ
   ├── Lê headers: MessageId, Type
   ├── Deserializa Payload via System.Text.Json
   └── delega para IntegrationEventDispatcher

7. IntegrationEventDispatcher
   ├── Abre scope DI
   ├── SELECT 1 FROM inbox_messages WHERE id = MessageId
   │   ├── Já existe → ack no RabbitMQ, retorna (idempotência)
   │   └── Não existe → continua
   ├── IMediator.Publish(UserBookRatedIntegrationEvent)
   │
   └── UserBookRatedIntegrationEventHandler (Application)
       ├── Carrega Book do repositório
       ├── book.RecalculateRating(rating, previousRating)
       ├── _ctx.InboxMessages.Add(new InboxMessage(MessageId, NOW()))
       └── await bookRepository.UpdateAsync(book) → SaveChangesAsync()
           (UPDATE books + INSERT inbox_messages na mesma transação)

8. IntegrationEventDispatcher
   └── Ack no RabbitMQ (mensagem removida da fila)
```

**Garantia de atomicidade:** Passos 3 e 4 acontecem dentro da mesma transação do EF Core. Se `base.SaveChangesAsync()` falha (constraint, lock, crash), o ROLLBACK descarta *tanto* a mudança no `user_books` *quanto* a linha do `outbox_message`. Se commita, ambos commitam juntos. Não há janela onde um existe sem o outro. Ver decisão 2.5 para detalhes do `DispatchDomainEventsInterceptor`.

---

## 4. Novos Projetos

### 4.1 Legi.Contracts

Contém **apenas** os records (contratos) de integration events. Sem lógica, sem behavior — apenas data shapes.

**Dependências:** `Legi.SharedKernel` (para `INotification` via `IIntegrationEvent`)

**Referenciado por:** Todos os projetos `.Application` e `.Infrastructure` que publicam ou consomem eventos.

```
Legi.Contracts/
├── IIntegrationEvent.cs
├── Identity/
│   ├── UserRegisteredIntegrationEvent.cs
│   ├── UserDeletedIntegrationEvent.cs
│   └── UsernameChangedIntegrationEvent.cs
├── Catalog/
│   ├── BookCreatedIntegrationEvent.cs
│   └── BookUpdatedIntegrationEvent.cs
├── Library/
│   ├── UserBookRatedIntegrationEvent.cs
│   ├── UserBookRatingRemovedIntegrationEvent.cs
│   ├── BookAddedToLibraryIntegrationEvent.cs
│   ├── ReadingStatusChangedIntegrationEvent.cs
│   ├── ReadingPostCreatedIntegrationEvent.cs
│   ├── ReadingPostDeletedIntegrationEvent.cs
│   ├── UserListCreatedIntegrationEvent.cs
│   └── UserListDeletedIntegrationEvent.cs
└── Social/
    ├── ContentLikedIntegrationEvent.cs
    ├── ContentUnlikedIntegrationEvent.cs
    ├── ContentCommentedIntegrationEvent.cs
    └── CommentDeletedIntegrationEvent.cs
```

**Organização por serviço de origem.** O produtor define o contrato. Os consumidores adaptam-se a ele. Ao debuggar "de onde vem esse evento?", o folder responde imediatamente.

### 4.2 Legi.Messaging

Infraestrutura de mensageria própria do Legi. Contém o Outbox, Inbox, dispatcher worker, wrapper RabbitMQ, e helpers de DI.

**Dependências:**
- `RabbitMQ.Client` (MPL 2.0 / Apache 2.0)
- `Microsoft.EntityFrameworkCore` (apenas abstrações — providers ficam em cada serviço)
- `Microsoft.Extensions.Hosting.Abstractions` (para `BackgroundService`)
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Legi.SharedKernel`
- `Legi.Contracts`

**Referenciado por:** Todos os projetos `.Infrastructure`.

```
Legi.Messaging/
├── Outbox/
│   ├── OutboxMessage.cs                      ← entity
│   ├── OutboxMessageConfiguration.cs         ← EF Core fluent config
│   ├── OutboxEventBus.cs                     ← IEventBus impl, genérico sobre TContext
│   ├── OutboxDispatcherWorker.cs             ← BackgroundService que faz polling
│   └── OutboxOptions.cs                      ← settings (poll interval, batch size, max attempts)
├── Inbox/
│   ├── InboxMessage.cs                       ← entity
│   ├── InboxMessageConfiguration.cs          ← EF Core fluent config
│   └── IntegrationEventDispatcher.cs         ← dedup + dispatch via Mediator
├── RabbitMq/
│   ├── IRabbitMqPublisher.cs                 ← interface (publish para o broker)
│   ├── RabbitMqPublisher.cs                  ← impl com publisher confirms
│   ├── RabbitMqConsumerHost.cs               ← BackgroundService genérico por evento
│   ├── RabbitMqConnectionFactory.cs          ← gerencia connection/channel lifecycle
│   ├── RabbitMqTopology.cs                   ← declaração de exchanges/queues
│   └── RabbitMqSettings.cs                   ← connection settings (host, port, credentials)
├── Serialization/
│   └── IntegrationEventSerializer.cs         ← System.Text.Json + Type column resolver
├── DependencyInjection/
│   ├── MessagingExtensions.cs                ← AddLegiMessaging<TContext>(...)
│   ├── ProducerExtensions.cs                 ← AddOutboxProducer<TContext>(...)
│   └── ConsumerExtensions.cs                 ← AddIntegrationEventConsumer<T>(...)
└── Legi.Messaging.csproj
```

**Disciplina de extração futura (ver 2.2):** Nada neste projeto referencia tipos Legi-específicos (UserBook, BookSnapshot, etc.). O único acoplamento ao "Legi" é o nome do projeto e o uso do `IIntegrationEvent` marker — ambos triviais de renomear se a extração para biblioteca OSS for executada no futuro.

### 4.3 Alterações no SharedKernel

**Duas adições:**

**1. `IEventBus.cs`** — porta de saída para publicação de integration events:

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default)
        where T : class;
}
```

**2. `DispatchDomainEventsInterceptor.cs`** — `SaveChangesInterceptor` que dispatcha domain events *antes* do commit, garantindo atomicidade com o outbox (ver decisão 2.5).

**Dependência nova no SharedKernel:** Se o interceptor viver no SharedKernel, é preciso referenciar `Microsoft.EntityFrameworkCore` (para os tipos `SaveChangesInterceptor`, `DbContextEventData`, `InterceptionResult`, `DbContext`).

**Alternativa:** Criar um projeto separado `Legi.SharedKernel.Persistence` que referencie o SharedKernel + EF Core, isolando essa dependência. Este projeto seria referenciado apenas pelas camadas Infrastructure.

**Escolha recomendada:** Manter o interceptor em `SharedKernel` e aceitar a dependência de EF Core. Justificativas: (a) EF Core é a base abstrata de persistência compartilhada por todo o projeto — não é uma dependência "estranha"; (b) domain event dispatch é uma preocupação genérica de DDD, não específica de mensageria; (c) criar um projeto separado para um único arquivo é overengineering nesta fase. A dependência é do pacote base `Microsoft.EntityFrameworkCore` apenas — sem Npgsql, sem providers. A claim de "zero dependências externas" do SharedKernel se torna "apenas EF Core abstrato".

---

## 5. Grafo de Dependências (com novos projetos)

```
Legi.SharedKernel (+ IEventBus)          Legi.Contracts
    ↑                                         ↑
    │                                         │
    ├── Legi.{Service}.Domain                 │ (referencia para contratos)
    │       ↑                                 │
    │       │                                 │
    │       ├── Legi.{Service}.Application ───┘
    │       │       ↑
    │       │       │
    │       │       ├── Legi.{Service}.Infrastructure ← Legi.Messaging
    │       │       │       ↑
    │       │       │       │
    │       │       │       └── Legi.{Service}.Api
```

**Regra de dependência preservada:**
- Domain depende apenas do SharedKernel (sem mudanças)
- Application depende de Domain + SharedKernel + Contracts (novo: Contracts)
- Infrastructure depende de Application + Messaging (novo: Messaging)
- API depende de Infrastructure (sem mudanças conceituais)

---

## 6. Contratos de Integration Events

### 6.1 Princípios de Design

**Integration events carregam dados primitivos, não Value Objects.** Cada bounded context define seus próprios VOs. O evento carrega `int Rating` (1-10), não `Rating` (VO). O consumidor interpreta o valor no seu contexto. Coerente com a decisão 4.1 do LIBRARY-ARCHITECTURE-decisions.md.

**Integration events são imutáveis.** Records em C#, sem setters, sem métodos de negócio. São DTOs de fronteira.

**Dados enriquecidos.** O consumidor não pode consultar o produtor. O evento carrega tudo que o consumidor precisa para processar sem fazer chamadas HTTP de volta.

**Versionamento aditivo.** Adicionar campos com defaults é backward-compatible. Remover campos é breaking change. Em monorepo, isso é validado em compile-time.

### 6.2 Identity → Todos

```csharp
// Criado quando um novo usuário se registra. Carrega o username escolhido
// no registro — a obrigatoriedade no domínio do Identity garante que sempre
// está presente.
public record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Username,
    string Email,
    DateTime RegisteredAt
) : IIntegrationEvent;

// Criado quando um usuário deleta sua conta
public record UserDeletedIntegrationEvent(
    Guid UserId,
    DateTime DeletedAt
) : IIntegrationEvent;

// Criado quando o username é alterado pelo próprio usuário (após registro)
public record UsernameChangedIntegrationEvent(
    Guid UserId,
    string NewUsername
) : IIntegrationEvent;
```

**Consumidores:**

| Evento | Consumidor | Efeito |
|--------|------------|--------|
| `UserRegistered` | Social | Cria `UserProfile` com username e email |
| `UserDeleted` | Catalog | Atualiza `created_by` para referência genérica |
| `UserDeleted` | Library | Deleta `user_books`, `reading_posts`, `user_lists` |
| `UserDeleted` | Social | Deleta `UserProfile`, `Follows`, `Likes`, `Comments`, `FeedItems` |
| `UsernameChanged` | Social | Atualiza `UserProfile.Username` |

### 6.3 Catalog → Library, Social

```csharp
// Criado quando um livro é adicionado ao catálogo
public record BookCreatedIntegrationEvent(
    Guid BookId,
    string Title,
    List<string> Authors,
    string AuthorDisplay,
    string? CoverUrl,
    int? PageCount
) : IIntegrationEvent;

// Criado quando dados de um livro são atualizados
public record BookUpdatedIntegrationEvent(
    Guid BookId,
    string Title,
    List<string> Authors,
    string AuthorDisplay,
    string? CoverUrl,
    int? PageCount
) : IIntegrationEvent;
```

**Consumidores:**

| Evento | Consumidor | Efeito |
|--------|------------|--------|
| `BookCreated` | Library | Cria `BookSnapshot` |
| `BookCreated` | Social | Cria `BookSnapshot` (read model local — ver decisão 2.6) |
| `BookUpdated` | Library | Atualiza `BookSnapshot` (upsert) |
| `BookUpdated` | Social | Atualiza `BookSnapshot` (upsert) |

### 6.4 Library → Catalog

```csharp
// Criado quando o usuário dá ou atualiza um rating
public record UserBookRatedIntegrationEvent(
    Guid BookId,
    Guid UserId,
    int Rating,            // valor primitivo 1-10 (meias-estrelas)
    int? PreviousRating    // null se é o primeiro rating
) : IIntegrationEvent;

// Criado quando o usuário remove um rating
public record UserBookRatingRemovedIntegrationEvent(
    Guid BookId,
    Guid UserId,
    int PreviousRating     // valor que foi removido
) : IIntegrationEvent;
```

**Consumidores:**

| Evento | Consumidor | Efeito |
|--------|------------|--------|
| `UserBookRated` | Catalog | Recalcula `average_rating` e `ratings_count` no `Book` |
| `UserBookRatingRemoved` | Catalog | Recalcula `average_rating` e `ratings_count` no `Book` |

### 6.5 Library → Social

```csharp
// Criado quando o usuário adiciona um livro à biblioteca
// Nota: dados do livro (título, autor, cover) são resolvidos pelo Social
// via seu próprio BookSnapshot local. Ver decisão 2.6.
public record BookAddedToLibraryIntegrationEvent(
    Guid UserBookId,
    Guid UserId,
    Guid BookId,
    bool Wishlist,
    DateTime AddedAt
) : IIntegrationEvent;

// Criado quando o status de leitura muda
public record ReadingStatusChangedIntegrationEvent(
    Guid UserId,
    Guid BookId,
    string OldStatus,      // string do enum, não o enum (fronteira entre contextos)
    string NewStatus,
    DateTime ChangedAt
) : IIntegrationEvent;

// Criado quando o usuário publica um post de progresso
public record ReadingPostCreatedIntegrationEvent(
    Guid PostId,
    Guid UserId,
    Guid BookId,
    string? Content,
    int? ProgressValue,
    string? ProgressType,   // "Page" | "Percentage" | null
    DateTime CreatedAt
) : IIntegrationEvent;

// Criado quando um post de progresso é deletado
public record ReadingPostDeletedIntegrationEvent(
    Guid PostId,
    Guid UserId
) : IIntegrationEvent;

// Criado quando o usuário dá ou atualiza um rating (Social cria FeedItem)
// Nota: UserBookRatedIntegrationEvent (seção 6.4) é o mesmo evento consumido
// tanto pelo Catalog quanto pelo Social — RabbitMQ fanout entrega em filas separadas

// Criado quando o usuário cria uma lista
public record UserListCreatedIntegrationEvent(
    Guid ListId,
    Guid UserId,
    string Name,
    bool IsPublic,
    DateTime CreatedAt
) : IIntegrationEvent;

// Criado quando uma lista é deletada
public record UserListDeletedIntegrationEvent(
    Guid ListId,
    Guid UserId
) : IIntegrationEvent;
```

**Consumidores:**

| Evento | Consumidor | Efeito |
|--------|------------|--------|
| `BookAddedToLibrary` | Social | Cria `FeedItem` (BookStarted) se não é wishlist |
| `ReadingStatusChanged` | Social | Cria `FeedItem` (BookFinished se NewStatus = "Finished") |
| `ReadingPostCreated` | Social | Cria `ContentSnapshot` (Post) + `FeedItem` (ProgressPosted) |
| `ReadingPostDeleted` | Social | Remove `ContentSnapshot` + `Likes` + `Comments` + `FeedItem` |
| `UserBookRated` | Social | Cria `FeedItem` (BookRated) |
| `UserListCreated` | Social | Cria `ContentSnapshot` (List) + `FeedItem` (ListCreated) |
| `UserListDeleted` | Social | Remove `ContentSnapshot` + `Likes` + `Comments` + `FeedItem` |

### 6.6 Social → Library

```csharp
// Criado quando um conteúdo é curtido
public record ContentLikedIntegrationEvent(
    string TargetType,     // "Post" | "List"
    Guid TargetId,
    Guid UserId
) : IIntegrationEvent;

// Criado quando um curtida é removida
public record ContentUnlikedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid UserId
) : IIntegrationEvent;

// Criado quando um comentário é adicionado
public record ContentCommentedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid CommentId,
    Guid UserId
) : IIntegrationEvent;

// Criado quando um comentário é deletado
public record CommentDeletedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid CommentId
) : IIntegrationEvent;
```

**Consumidores:**

| Evento | Consumidor | Efeito |
|--------|------------|--------|
| `ContentLiked` | Library | Incrementa `LikesCount` em `ReadingPost` ou `UserList` |
| `ContentUnliked` | Library | Decrementa `LikesCount` |
| `ContentCommented` | Library | Incrementa `CommentsCount` em `ReadingPost` ou `UserList` |
| `CommentDeleted` | Library | Decrementa `CommentsCount` |

### 6.7 Resumo de Eventos

**Total: 17 integration events.**

| Origem | Eventos | Destino(s) |
|--------|---------|------------|
| Identity | 3 (Registered, Deleted, UsernameChanged) | Social, Catalog, Library |
| Catalog | 2 (BookCreated, BookUpdated) | Library, Social |
| Library | 8 (BookAdded, StatusChanged, PostCreated, PostDeleted, Rated, RatingRemoved, ListCreated, ListDeleted) | Catalog, Social |
| Social | 4 (ContentLiked, ContentUnliked, ContentCommented, CommentDeleted) | Library |

### 6.8 Divergências resolvidas com ARCHITECTURE.md

O ARCHITECTURE.md seção 6.2 foi escrito antes dos documentos de arquitetura detalhados (Library e Social). As seguintes atualizações são necessárias:

| Item | ARCHITECTURE.md (antigo) | Decisão atualizada | Motivo |
|------|--------------------------|---------------------|--------|
| `UserBookRatingRemovedIntegrationEvent` | Ausente | Adicionado | Library.Domain tem `UserBookRatingRemovedDomainEvent` com consumidor no Catalog |
| `ReadingPostCreatedIntegrationEvent` | Dados mínimos (sem book data) | Mantido lean; dados do livro resolvidos via `BookSnapshot` no Social | Decisão 2.6: snapshots locais em vez de denormalização em eventos |
| `UserListCreatedIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | SOCIAL-ARCHITECTURE-decisions.md seção 7.1 define como incoming |
| `UserListDeletedIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | Idem |
| `ContentUnlikedIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | SOCIAL-ARCHITECTURE-decisions.md seção 7.2 define como outgoing |
| `CommentDeletedIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | Idem |
| `UserRegisteredIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | Social precisa criar UserProfile no registro |
| `UsernameChangedIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | Social precisa atualizar UserProfile.Username |
| `ReadingStatusChangedIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | Social precisa para FeedItem de BookFinished |
| `BookAddedToLibraryIntegrationEvent` | Ausente no ARCHITECTURE.md | Adicionado | Social precisa para FeedItem de BookStarted |
| Campos de denormalização em Library→Social | `BookTitle/AuthorDisplay/CoverUrl` nos eventos | Removidos de 3 eventos (ver 2.6) | Social passa a manter `BookSnapshot` local alimentado por `BookCreated`/`BookUpdated` |
| Consumidores de `BookCreated`/`BookUpdated` | Apenas Library | Library + Social | Consequência direta de 2.6 |

---

## 7. Infraestrutura

### 7.1 Docker Compose

```yaml
rabbitmq:
  image: rabbitmq:3-management-alpine
  container_name: legi-rabbitmq
  ports:
    - "5672:5672"        # AMQP protocol
    - "15672:15672"      # Management UI (http://localhost:15672)
  environment:
    RABBITMQ_DEFAULT_USER: legi
    RABBITMQ_DEFAULT_PASS: legi_dev
  volumes:
    - rabbitmq_data:/var/lib/rabbitmq
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
```

**Porta 15672** expõe o Management UI — dashboard web para monitorar exchanges, queues, mensagens em trânsito, consumers ativos. Essencial para debugging.

### 7.2 Configuração por Serviço (appsettings)

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "legi",
    "Password": "legi_dev"
  }
}
```

Options pattern via `RabbitMqSettings`:

```csharp
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "legi";
    public string Password { get; set; } = "legi_dev";
}
```

### 7.3 Configuração de Mensageria por Serviço

**Centralizado no `Legi.Messaging`:**

```csharp
// Legi.Messaging/DependencyInjection/MessagingExtensions.cs
public static class MessagingExtensions
{
    /// <summary>
    /// Registra a infraestrutura completa de mensageria do Legi para um serviço:
    /// IEventBus (producer), OutboxDispatcherWorker (worker), conexão RabbitMQ.
    /// Consumers são registrados separadamente via AddIntegrationEventConsumer&lt;T&gt;.
    /// </summary>
    public static IServiceCollection AddLegiMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));

        // Producer side
        services.AddScoped<IEventBus, OutboxEventBus<TDbContext>>();

        // RabbitMQ infrastructure (singleton — connection é caro)
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddSingleton<IntegrationEventSerializer>();

        // Outbox dispatcher
        services.AddHostedService<OutboxDispatcherWorker<TDbContext>>();

        return services;
    }

    /// <summary>
    /// Registra um consumer host para um integration event específico.
    /// Cada chamada cria um BackgroundService dedicado àquele tipo.
    /// </summary>
    public static IServiceCollection AddIntegrationEventConsumer<TEvent>(
        this IServiceCollection services)
        where TEvent : class, IIntegrationEvent
    {
        services.AddHostedService<RabbitMqConsumerHost<TEvent>>();
        return services;
    }
}
```

**Uso por serviço (exemplo: Library):**

```csharp
// Legi.Library.Infrastructure/DependencyInjection.cs
public static IServiceCollection AddLibraryInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... DbContext, repositories existentes ...

    // Registra outbox + RabbitMQ + dispatcher worker
    services.AddLegiMessaging<LibraryDbContext>(configuration);

    // Library CONSOME eventos de Social, Catalog, Identity
    services.AddIntegrationEventConsumer<ContentLikedIntegrationEvent>();
    services.AddIntegrationEventConsumer<ContentUnlikedIntegrationEvent>();
    services.AddIntegrationEventConsumer<ContentCommentedIntegrationEvent>();
    services.AddIntegrationEventConsumer<CommentDeletedIntegrationEvent>();
    services.AddIntegrationEventConsumer<BookCreatedIntegrationEvent>();
    services.AddIntegrationEventConsumer<BookUpdatedIntegrationEvent>();
    services.AddIntegrationEventConsumer<UserDeletedIntegrationEvent>();

    // Library PUBLICA via IEventBus nos domain event handlers
    // (não precisa registrar nada extra — IEventBus já vem do AddLegiMessaging)

    return services;
}
```

**`appsettings.json` adicionado:**

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "legi",
    "Password": "legi_dev"
  },
  "Outbox": {
    "PollingIntervalMs": 1000,
    "BatchSize": 50,
    "MaxAttempts": 5
  }
}
```

### 7.4 Topologia de Exchanges e Queues

A topologia é declarada explicitamente no código (não inferida automaticamente). Vive em `RabbitMqTopology.cs` no `Legi.Messaging`.

**Padrão adotado:**

Para cada integration event `T`:
- **Exchange:** `legi.events.{event-fqn-lowercase}` (tipo `fanout`, durable)
    - Ex: `legi.events.legi.contracts.library.userbookratedintegrationevent`
- **Queue por consumidor:** `{service}.{event-name-kebab}` (durable)
    - Ex: `catalog.user-book-rated`, `social.user-book-rated`
- **Binding:** exchange → queue (sem routing key — fanout)

Quando o producer publica, a mensagem vai para o exchange. O exchange entrega para *todas* as queues vinculadas. Se dois serviços (Catalog e Social) consomem o mesmo evento, cada um tem sua própria queue — ambos recebem independentemente.

**Declaração:**
- Producer: declara o exchange ao publicar (idempotente).
- Consumer host: declara o exchange + sua queue + binding ao iniciar (idempotente).

**Dead-letter:** Para v1, mensagens que falham `MaxAttempts` no consumer são logadas e a `delivery tag` é `nack`-ed sem requeue, descartando a mensagem. Não há dead-letter exchange dedicado nesta fase — adicionável em hardening (Fase 6).

### 7.5 Outbox e Inbox — Tabelas no Banco

Cada serviço tem suas próprias duas tabelas, no seu próprio banco. Schema definido pelas EF Core configurations no `Legi.Messaging`.

**`outbox_messages`:**

| Coluna | Tipo | Notas |
|--------|------|-------|
| `id` | `uuid` (PK) | gerado pelo producer; usado como `MessageId` no broker |
| `type` | `varchar(500)` | FQN do tipo de evento, para deserialização no consumer |
| `payload` | `jsonb` | evento serializado (System.Text.Json) |
| `occurred_at` | `timestamptz` | quando o domain event ocorreu |
| `processed_at` | `timestamptz NULL` | quando o broker confirmou recepção; null = pendente |
| `attempts` | `integer NOT NULL DEFAULT 0` | contador de tentativas de publicação |
| `next_retry_at` | `timestamptz NOT NULL DEFAULT NOW()` | quando esta linha fica elegível para a próxima tentativa; default `NOW()` para que linhas novas sejam imediatamente elegíveis |
| `error` | `text NULL` | última mensagem de erro (para diagnóstico) |

**Índices:**
- `(NextRetryAt, OccurredAt)` parcial `WHERE ProcessedAt IS NULL` — query do dispatcher (`WHERE processed_at IS NULL AND attempts < max AND next_retry_at <= now() ORDER BY occurred_at`) é otimizada para escanear apenas mensagens pendentes elegíveis em ordem cronológica.

**Política de retry (gerenciada pelo dispatcher, ver decisão 8.2):** Em falha de publicação, dispatcher incrementa `attempts`, registra `error`, e define `next_retry_at = now() + backoff(attempts)`. A próxima iteração do polling pega a linha de volta apenas quando `next_retry_at` já passou. Quando `attempts >= MaxAttempts`, o dispatcher para de incrementar — a linha fica como poison pendente para diagnóstico manual (filtrada pelo `attempts < max` na query).

**`inbox_messages`:**

| Coluna | Tipo | Notas |
|--------|------|-------|
| `id` | `uuid` (PK) | `MessageId` recebido do broker |
| `type` | `varchar(500)` | FQN do tipo (diagnóstico) |
| `processed_at` | `timestamptz NOT NULL` | quando foi processado com sucesso |

**Sem índices secundários** — todas as consultas são `WHERE id = ?`, satisfeitas pela PK.

**Migrations:** Cada serviço tem sua própria migration `AddOutboxAndInbox` no seu DbContext, gerada via `dotnet ef migrations add AddOutboxAndInbox`. As entity configurations vêm do `Legi.Messaging` e são aplicadas via `modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboxMessageConfiguration).Assembly)` ou registradas explicitamente no `OnModelCreating` do DbContext.

**Cleanup de outbox/inbox:** Linhas com `processed_at` antigas crescem sem limite. Em produção, há de se ter um job de housekeeping que deleta linhas com `processed_at < NOW() - retention`. Para v1, não implementado — anotado em Fase 6 (hardening).

---

## 8. Idempotência e Resiliência

### 8.1 Consumers devem ser idempotentes

**Problema:** At-least-once delivery significa que uma mensagem pode ser processada mais de uma vez (retry após timeout, redelivery após crash). O consumer deve produzir o mesmo resultado se executado múltiplas vezes com a mesma mensagem.

**Estratégias por tipo de operação:**

| Operação | Estratégia de idempotência |
|----------|---------------------------|
| Criar entidade (BookSnapshot, FeedItem) | Upsert (`AddOrUpdateAsync`) — se já existe, atualiza |
| Incrementar contador (LikesCount) | Check-before-increment: verificar se o Like já existe antes de incrementar |
| Deletar entidade | `DELETE WHERE id = @id` é naturalmente idempotente (deletar o que não existe = no-op) |
| Recalcular valor (average_rating) | Recalcular do zero é idempotente por natureza |

**Inbox como deduplicação primária:** Cada `IntegrationEventDispatcher` consulta `inbox_messages` antes de invocar o handler. Se a mensagem já foi processada (matching `MessageId`), ela é silenciosamente ignorada — ack imediato no broker, sem invocar handler. Se nova, o handler executa, e o `INSERT INTO inbox_messages` faz parte da mesma transação que as mudanças do handler. Isso garante: ou ambos commitam (handler logic + inbox row) ou nenhum (rollback completo). Na próxima entrega da mesma mensagem, o handler não roda de novo.

**Idempotência defensiva ainda importa:** Mesmo com inbox, é boa prática que os handlers sejam idempotentes em si. A inbox não protege contra (a) clock skew em casos extremos, (b) falhas após o INSERT mas antes do ack ao broker (gerando reentrega que será capturada pela inbox, mas ainda é processamento perdido). Os padrões na tabela acima continuam aplicáveis.

**Convenção: integration event handlers NÃO chamam `SaveChangesAsync`.** Diferente de command handlers (que comitam o trabalho do request) ou domain event handlers (que rodam dentro do `SaveChangesAsync` do request), integration event handlers são invocados pelo `IntegrationEventDispatcher` fora de qualquer request. O dispatcher é dono do ciclo: adiciona o `InboxMessage`, invoca o handler via `IMediator.Publish`, e então chama `SaveChangesAsync` uma única vez. Isso garante que o INSERT na inbox commite atomicamente com as mudanças do handler. Se o handler chamasse `SaveChangesAsync` por conta própria, a inbox row commitaria separadamente — quebrando a atomicidade que protege contra processamento duplicado.

### 8.2 Retry Policy

**Producer side (outbox dispatcher):**

```
Tentativa 1: imediata
Tentativa 2: +1 segundo
Tentativa 3: +5 segundos
Tentativa 4: +30 segundos
Tentativa 5: +60 segundos
Tentativa 6+: marca outbox row com error + para de tentar (poison)
```

Implementado no `OutboxDispatcherWorker`. `MaxAttempts` configurável (default 5). Após exceder, a row permanece no outbox com `error` preenchido e `attempts >= max` — para diagnóstico manual; o dispatcher pula essas rows nas próximas iterações.

**Consumer side (RabbitMQ delivery):**

Quando o handler do consumer falha, a mensagem é `nack`-ed *com* requeue. RabbitMQ a reentrega na próxima janela de consumo. Em v1, **não há limite explícito de tentativas no lado do consumer** — uma mensagem permanentemente quebrada redeliverá indefinidamente.

**Justificativa para v1:** projeto pessoal, logs sob observação ativa. Loop de redelivery é loud e visível, o que acelera diagnóstico de bugs em handlers. Contar tentativas exigiria header customizado ou uso de quorum queues — complexidade adiada.

**Suficiente para:** falhas transitórias de banco (lock timeout, connection pool), broker temporariamente indisponível, picos de carga, deploys sequenciais.

**Não resolve:** bugs no código, dados corrompidos, violações de constraint. Esses casos manifestam como redelivery loop visível em log. Quando observado, requer correção do handler ou descarte manual da mensagem (purge da queue no Management UI). Dead-letter exchange dedicado e contagem de attempts são hardening de Fase 6.

### 8.3 Ordering

**Não há garantia de ordenação** entre mensagens diferentes. Se Library publica `BookAddedToLibrary` e depois `ReadingPostCreated` no mesmo segundo, Social pode receber na ordem inversa, porque:

- Múltiplas instâncias do dispatcher worker podem publicar em paralelo (com `FOR UPDATE SKIP LOCKED`, cada uma pega rows diferentes em ordens não previsíveis).
- O RabbitMQ entrega FIFO *dentro de uma queue*, mas se um consumer tem prefetch > 1 e processa em paralelo, a ordem é perdida.

**Impacto no Legi:** Baixo. Cada consumer é independente. Um FeedItem de "post criado" não depende de um FeedItem de "livro adicionado" já existir. O único caso sensível é `UserDeleted` — mas a limpeza é idempotente (deletar o que não existe é no-op).

**Se no futuro for necessário ordering por aggregate:** opções são (a) particionar por `UserId` no payload e usar consumer single-instance por partição, (b) usar headers do AMQP para priorização. Não implementar agora — YAGNI.

---

## 9. Impacto em Serviços Existentes

**Refactor comum a todos os serviços (precede qualquer adição de handlers):** A dispatch de domain events é movida do override de `SaveChangesAsync` para o novo `DispatchDomainEventsInterceptor` no SharedKernel (ver decisão 2.5). O override dos 4 `DbContext`s passa a ser simplificado ou removido. Cada `DbContext` registra o interceptor via `AddDbContext` → `AddInterceptors(...)`. Esta refatoração é pré-requisito para que o outbox funcione corretamente; é executada na Fase 1 antes de qualquer integration event fluir.

### 9.1 Identity

| Mudança | Descrição |
|---------|-----------|
| Nova dependência | `Legi.Contracts` na Application layer |
| Novos domain events | `UserRegisteredDomainEvent`, `UserDeletedDomainEvent`, `UsernameChangedDomainEvent` (se não existirem) |
| Novos domain event handlers | 3 handlers que traduzem domain → integration events via `IEventBus` |
| Infrastructure | `AddLegiMessaging<IdentityDbContext>()` sem consumers (Identity só publica) |
| Outbox | Migration para tabelas de outbox no `identity` DB |

**Nota sobre `UserRegisteredDomainEvent`:** Identity já emite `UserRegisteredDomainEvent` no domínio. O novo handler na Application traduz para `UserRegisteredIntegrationEvent` e publica via `IEventBus`.

### 9.2 Catalog

| Mudança | Descrição |
|---------|-----------|
| Nova dependência | `Legi.Contracts` na Application layer |
| Novos domain event handlers (outgoing) | `BookCreatedDomainEventHandler` → publica `BookCreatedIntegrationEvent`. `BookUpdatedDomainEventHandler` → publica `BookUpdatedIntegrationEvent` |
| Novos notification handlers (incoming) | `UserBookRatedIntegrationEventHandler` (recalcula rating). `UserBookRatingRemovedIntegrationEventHandler` (recalcula rating). `UserDeletedIntegrationEventHandler` (limpa `created_by`) |
| Infrastructure | `AddLegiMessaging<CatalogDbContext>()` com consumers registrados |
| Outbox | Migration para tabelas de outbox no `catalog` DB |

### 9.3 Library

| Mudança | Descrição |
|---------|-----------|
| Nova dependência | `Legi.Contracts` na Application layer |
| Novos domain event handlers (outgoing) | 8 handlers traduzindo domain events → integration events via `IEventBus` |
| Novos notification handlers (incoming) | `BookCreatedIntegrationEventHandler` (cria BookSnapshot). `BookUpdatedIntegrationEventHandler` (atualiza BookSnapshot). `ContentLikedIntegrationEventHandler` (incrementa LikesCount). `ContentUnlikedIntegrationEventHandler` (decrementa). `ContentCommentedIntegrationEventHandler` (incrementa CommentsCount). `CommentDeletedIntegrationEventHandler` (decrementa). `UserDeletedIntegrationEventHandler` (deleta dados) |
| Remoção workaround | Remover campos `BookTitle`, `BookAuthorDisplay`, `BookCoverUrl`, `BookPageCount` do `AddBookToLibraryCommand` |
| Infrastructure | `AddLegiMessaging<LibraryDbContext>()` com consumers registrados |
| Outbox | Migration para tabelas de outbox no `library` DB |

### 9.4 Social

| Mudança | Descrição |
|---------|-----------|
| Nova dependência | `Legi.Contracts` na Application layer |
| **Novo read model local** | **`BookSnapshot` (análogo ao de Library) — ver decisão 2.6. Entity, EF configuration, migration. Resolve display data (título, autor, cover) para FeedItems sem depender de denormalização em eventos** |
| Novos domain event handlers (outgoing) | 4 handlers (`ContentLiked`, `ContentUnliked`, `ContentCommented`, `CommentDeleted`) substituindo stubs |
| Novos notification handlers (incoming) | `UserRegisteredIntegrationEventHandler` (cria UserProfile). `UserDeletedIntegrationEventHandler` (deleta tudo). `UsernameChangedIntegrationEventHandler` (atualiza username). `BookCreatedIntegrationEventHandler` (cria BookSnapshot). `BookUpdatedIntegrationEventHandler` (upsert BookSnapshot). 7 handlers de Library events (FeedItem/ContentSnapshot creation — resolvem dados do livro via BookSnapshot). |
| Remoção stubs | Substituir domain event handlers stub por versões que publicam integration events |
| Infrastructure | `AddLegiMessaging<SocialDbContext>()` com consumers registrados |
| Outbox | Migration para tabelas de outbox no `social` DB |

---

## 10. Fases de Implementação

### Fase 1 — Foundation (sem eventos de negócio, pipeline próprio validado ponta-a-ponta)

**Objetivo:** Infraestrutura própria de mensageria pronta, dispatch pré-save refatorado, smoke test end-to-end passando.

Esta fase é dividida em sub-fases para revisão incremental. Cada sub-fase é um marco de revisão antes de seguir.

**1A — Abstrações compartilhadas (SharedKernel)**

| Tarefa | Descrição |
|--------|-----------|
| 1A.1 | Adicionar `IEventBus` ao SharedKernel |
| 1A.2 | Criar `DispatchDomainEventsInterceptor` no SharedKernel (ver decisão 2.5) |
| 1A.3 | Adicionar pacote `Microsoft.EntityFrameworkCore` (apenas abstração) ao SharedKernel |
| 1A.4 | Compilar — nenhum teste deve quebrar (interceptor ainda não registrado em DbContext) |

**1B — Contratos (Legi.Contracts)**

| Tarefa | Descrição |
|--------|-----------|
| 1B.1 | Criar projeto `Legi.Contracts` (.NET 10 class library) |
| 1B.2 | Adicionar `IIntegrationEvent : INotification` |

**1C — Outbox producer (Legi.Messaging — parte 1)**

| Tarefa | Descrição |
|--------|-----------|
| 1C.1 | Criar projeto `Legi.Messaging` (.NET 10 class library) com pacotes: `RabbitMQ.Client`, `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions` |
| 1C.2 | Implementar `OutboxMessage` (entity) e `OutboxMessageConfiguration` (EF Core fluent) |
| 1C.3 | Implementar `IntegrationEventSerializer` (System.Text.Json + type FQN resolver) |
| 1C.4 | Implementar `OutboxEventBus<TContext> : IEventBus` — escreve `OutboxMessage` no DbContext corrente |
| 1C.5 | Implementar `OutboxOptions` (settings) |

**1D — RabbitMQ wrapper (Legi.Messaging — parte 2)**

| Tarefa | Descrição |
|--------|-----------|
| 1D.1 | Implementar `RabbitMqSettings` |
| 1D.2 | Implementar `RabbitMqConnectionFactory` (gerencia conexão singleton) |
| 1D.3 | Implementar `RabbitMqTopology` (declaração idempotente de exchange/queue/binding) |
| 1D.4 | Implementar `IRabbitMqPublisher` + `RabbitMqPublisher` (com publisher confirms, persistent messages) |

**1E — Dispatcher worker (Legi.Messaging — parte 3)**

| Tarefa | Descrição |
|--------|-----------|
| 1E.1 | Implementar `OutboxDispatcherWorker<TContext> : BackgroundService` com polling, `FOR UPDATE SKIP LOCKED`, retry com backoff, marca poison após `MaxAttempts` |

**1F — Inbox + consumer (Legi.Messaging — parte 4)**

| Tarefa | Descrição |
|--------|-----------|
| 1F.1 | Implementar `InboxMessage` + `InboxMessageConfiguration` |
| 1F.2 | Implementar `IntegrationEventDispatcher` (dedup via inbox + Mediator publish) |
| 1F.3 | Implementar `RabbitMqConsumerHost<TEvent> : BackgroundService` (consumer host genérico) |

**1G — DI extensions (Legi.Messaging — parte 5)**

| Tarefa | Descrição |
|--------|-----------|
| 1G.1 | Implementar `MessagingExtensions.AddLegiMessaging<TContext>` |
| 1G.2 | Implementar `MessagingExtensions.AddIntegrationEventConsumer<TEvent>` |

**1H — Refactor de DbContexts (todos os 4 serviços)**

| Tarefa | Descrição |
|--------|-----------|
| 1H.1 | Remover dispatch de domain events do `SaveChangesAsync` override de cada DbContext |
| 1H.2 | Registrar `DispatchDomainEventsInterceptor` via `AddDbContext` em cada serviço |
| 1H.3 | Garantir que **todos os testes existentes continuam passando** |

**1I — Wiring no Identity + smoke test**

| Tarefa | Descrição |
|--------|-----------|
| 1I.1 | Adicionar RabbitMQ ao docker-compose com Management UI exposto (porta 15672) |
| 1I.2 | Atualizar `appsettings.json` (e `appsettings.Development.json`) do Identity com seções `RabbitMq` e `Outbox` |
| 1I.3 | Chamar `AddLegiMessaging<IdentityDbContext>("identity", configuration)` na DI do Identity |
| 1I.4 | Adicionar `modelBuilder.ApplyMessagingConfigurations()` em `IdentityDbContext.OnModelCreating` |
| 1I.5 | Migration `AddOutboxAndInbox` no Identity (`dotnet ef migrations add AddOutboxAndInbox`) |
| 1I.6 | Adicionar `UserRegisteredIntegrationEvent` em `Legi.Contracts/Identity/` (record com `UserId`, `Username`, `Email`, `RegisteredAt`) — código de produção, ficará permanente |
| 1I.7 | Adicionar `UserRegisteredDomainEventHandler` em `Legi.Identity.Application` (traduz domain event → integration event, chama `IEventBus.PublishAsync`) — código de produção, ficará permanente |
| 1I.8 | Adicionar `UserRegisteredIntegrationEventHandler` em `Legi.Identity.Application` (apenas log; existe para validar consumo no smoke test e como canário em produção) |
| 1I.9 | Chamar `AddIntegrationEventConsumer<UserRegisteredIntegrationEvent, IdentityDbContext>()` na DI do Identity (auto-consumo para validar pipeline) |
| 1I.10 | **Teste manual:** subir docker-compose, registrar um usuário via endpoint existente do Identity, observar logs. Verificar Management UI: exchange criado, queue `identity.user-registered` criada, mensagem entregue. Verificar tabelas: outbox row com `processed_at` preenchido, inbox row criada |
| 1I.11 | **Teste de integração automatizado:** TestContainers (RabbitMQ + Postgres), executar fluxo de registro de usuário, esperar (com timeout) inbox row aparecer, validar atomicidade outbox/inbox |

**Justificativa de escolha do fluxo de teste:** Em vez de criar um `PingIntegrationEvent` descartável, o smoke test usa o fluxo real de registro de usuário (`UserRegisteredDomainEvent` → `UserRegisteredIntegrationEvent`). Isto significa que o "código de smoke test" é, na verdade, código de produção que será permanente — exigido pela Fase 5 (eventos de Identity para Library/Social). Zero código diagnóstico para apagar depois; a Fase 1 já entrega a primeira meia-flecha do produto real.

**Entregável da Fase 1:** Pipeline próprio de mensageria funcionando ponta-a-ponta. Dispatch de domain events refatorado em todos os serviços. Smoke test (manual + automatizado) passando. Primeiro evento de negócio (`UserRegistered`) já fluindo do Identity para si mesmo — pronto para Fase 5 ligar Library e Social como consumidores adicionais.

### Fase 2 — Primeiro evento end-to-end (Catalog → Library: BookCreated)

**Objetivo:** Validar o fluxo completo com um caso real.

| Tarefa | Descrição |
|--------|-----------|
| 2.1 | Adicionar domain event handler em Catalog: `BookCreatedDomainEvent` → `BookCreatedIntegrationEvent` |
| 2.2 | Adicionar domain event handler em Catalog: `BookUpdatedDomainEvent` → `BookUpdatedIntegrationEvent` |
| 2.3 | Adicionar notification handler em Library: `BookCreatedIntegrationEvent` → cria `BookSnapshot` |
| 2.4 | Adicionar notification handler em Library: `BookUpdatedIntegrationEvent` → atualiza `BookSnapshot` |
| 2.5 | Configurar outbox em `CatalogDbContext`, gerar migration |
| 2.6 | Registrar consumers em Library's Infrastructure |
| 2.7 | **Testar:** criar livro no Catalog → BookSnapshot aparece no Library |
| 2.8 | **Remover workaround:** BookSnapshot inline do `AddBookToLibraryCommand` |

**Entregável:** BookSnapshots mantidos automaticamente via eventos. Workaround removido.

### Fase 3 — Identity events (UserDeleted → todos)

**Objetivo:** Cascata de deleção de usuário.

| Tarefa | Descrição |
|--------|-----------|
| 3.1 | Identity publica `UserRegisteredIntegrationEvent`, `UserDeletedIntegrationEvent`, `UsernameChangedIntegrationEvent` |
| 3.2 | Social consome `UserRegistered` (cria UserProfile), `UserDeleted` (deleta tudo), `UsernameChanged` (atualiza) |
| 3.3 | Library consome `UserDeleted` (deleta user_books, reading_posts, user_lists) |
| 3.4 | Catalog consome `UserDeleted` (atualiza created_by) |

**Entregável:** Lifecycle do usuário propagado automaticamente.

### Fase 4 — Library ↔ Social (eventos bidirecionais)

**Objetivo:** Feed e interações sociais funcionais.

| Tarefa | Descrição |
|--------|-----------|
| 4.1 | Library publica 8 integration events (BookAdded, StatusChanged, PostCreated, PostDeleted, Rated, RatingRemoved, ListCreated, ListDeleted) |
| 4.2 | Social consome todos → cria FeedItems e ContentSnapshots |
| 4.3 | Social publica 4 integration events (ContentLiked, ContentUnliked, ContentCommented, CommentDeleted) |
| 4.4 | Library consome → atualiza LikesCount, CommentsCount |
| 4.5 | **Remover stubs** em Social's Application layer |

**Entregável:** Sistema social completo. Feed funcional. Contadores atualizados via eventos.

### Fase 5 — Library → Catalog (ratings)

**Objetivo:** Average rating calculado via eventos.

| Tarefa | Descrição |
|--------|-----------|
| 5.1 | Library publica `UserBookRatedIntegrationEvent` e `UserBookRatingRemovedIntegrationEvent` |
| 5.2 | Catalog consome → recalcula `average_rating` e `ratings_count` no `Book` |

**Entregável:** Ratings no catálogo mantidos automaticamente.

### Fase 6 — Hardening

**Objetivo:** Resiliência e observabilidade.

| Tarefa | Descrição |
|--------|-----------|
| 6.1 | Verificar idempotência de todos os consumers |
| 6.2 | Configurar retry policies por consumer (se algum precisar de tuning diferente) |
| 6.3 | Testar cenários de falha: RabbitMQ down, consumer crash, mensagem duplicada |
| 6.4 | Documentar runbook para _error queues |

**Entregável:** Sistema resiliente a falhas transitórias.

---

## 11. Estrutura Final de Projetos

```
src/
├── Legi.SharedKernel/
│   ├── (existente)
│   ├── IEventBus.cs                              ← NOVO
│   └── DispatchDomainEventsInterceptor.cs        ← NOVO (ver 2.5)
│
├── Legi.Contracts/                        ← NOVO PROJETO
│   ├── IIntegrationEvent.cs
│   ├── Identity/
│   │   ├── UserRegisteredIntegrationEvent.cs
│   │   ├── UserDeletedIntegrationEvent.cs
│   │   └── UsernameChangedIntegrationEvent.cs
│   ├── Catalog/
│   │   ├── BookCreatedIntegrationEvent.cs
│   │   └── BookUpdatedIntegrationEvent.cs
│   ├── Library/
│   │   ├── UserBookRatedIntegrationEvent.cs
│   │   ├── UserBookRatingRemovedIntegrationEvent.cs
│   │   ├── BookAddedToLibraryIntegrationEvent.cs
│   │   ├── ReadingStatusChangedIntegrationEvent.cs
│   │   ├── ReadingPostCreatedIntegrationEvent.cs
│   │   ├── ReadingPostDeletedIntegrationEvent.cs
│   │   ├── UserListCreatedIntegrationEvent.cs
│   │   └── UserListDeletedIntegrationEvent.cs
│   └── Social/
│       ├── ContentLikedIntegrationEvent.cs
│       ├── ContentUnlikedIntegrationEvent.cs
│       ├── ContentCommentedIntegrationEvent.cs
│       └── CommentDeletedIntegrationEvent.cs
│
├── Legi.Messaging/                                ← NOVO PROJETO
│   ├── Outbox/
│   │   ├── OutboxMessage.cs
│   │   ├── OutboxMessageConfiguration.cs
│   │   ├── OutboxEventBus.cs
│   │   ├── OutboxDispatcherWorker.cs
│   │   └── OutboxOptions.cs
│   ├── Inbox/
│   │   ├── InboxMessage.cs
│   │   ├── InboxMessageConfiguration.cs
│   │   └── IntegrationEventDispatcher.cs
│   ├── RabbitMq/
│   │   ├── IRabbitMqPublisher.cs
│   │   ├── RabbitMqPublisher.cs
│   │   ├── RabbitMqConsumerHost.cs
│   │   ├── RabbitMqConnectionFactory.cs
│   │   ├── RabbitMqTopology.cs
│   │   └── RabbitMqSettings.cs
│   ├── Serialization/
│   │   └── IntegrationEventSerializer.cs
│   └── DependencyInjection/
│       ├── MessagingExtensions.cs
│       ├── ProducerExtensions.cs
│       └── ConsumerExtensions.cs
│
├── Legi.Identity.Domain/                          (sem mudanças)
├── Legi.Identity.Application/
│   └── (EventHandlers/ novos — traduzem domain → integration)
├── Legi.Identity.Infrastructure/
│   └── (DependencyInjection atualizado com AddLegiMessaging)
├── Legi.Identity.Api/                             (sem mudanças)
│
├── Legi.Catalog.Domain/                           (sem mudanças)
├── Legi.Catalog.Application/
│   └── (EventHandlers/ novos — outgoing + incoming)
├── Legi.Catalog.Infrastructure/
│   └── (DependencyInjection atualizado com AddLegiMessaging + consumers)
├── Legi.Catalog.Api/                              (sem mudanças)
│
├── Legi.Library.Domain/                           (sem mudanças)
├── Legi.Library.Application/
│   └── (EventHandlers/ novos — outgoing + incoming; remoção workaround)
├── Legi.Library.Infrastructure/
│   └── (DependencyInjection atualizado com AddLegiMessaging + consumers)
├── Legi.Library.Api/                              (remoção dos campos de workaround do request DTO)
│
├── Legi.Social.Domain/                            (sem mudanças)
├── Legi.Social.Application/
│   └── (EventHandlers/ — stubs substituídos por publishers; novos incoming handlers)
├── Legi.Social.Infrastructure/
│   └── (DependencyInjection atualizado com AddLegiMessaging + consumers)
└── Legi.Social.Api/                               (sem mudanças)
```

---

## 12. Referências de Pacotes NuGet

| Projeto | Pacote | Versão | Licença |
|---------|--------|--------|---------|
| `Legi.SharedKernel` | `Microsoft.EntityFrameworkCore` | 10.x | MIT |
| `Legi.Messaging` | `RabbitMQ.Client` | 7.x | MPL 2.0 / Apache 2.0 (dual) |
| `Legi.Messaging` | `Microsoft.EntityFrameworkCore` | 10.x | MIT |
| `Legi.Messaging` | `Microsoft.Extensions.Hosting.Abstractions` | 10.x | MIT |
| `Legi.Messaging` | `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.x | MIT |
| `Legi.Messaging` | `Microsoft.Extensions.Logging.Abstractions` | 10.x | MIT |

**Nota:** `RabbitMQ.Client` é mantido oficialmente pela equipe RabbitMQ na Broadcom. Dual-licenciado MPL 2.0 / Apache 2.0 — escolha do consumidor; ambas permitem uso comercial sem royalties. Sem risco de relicenciamento (precedente: nem o Spring/Pivotal nem a Broadcom mudaram esta licença em 15+ anos).

**Decisão consciente:** Não usar nenhuma biblioteca de mensageria de alto nível (MassTransit, NServiceBus, Wolverine). Ver decisão 2.2 para o raciocínio completo.

---

## 13. Atualização do ARCHITECTURE.md

Após a implementação, o ARCHITECTURE.md seção 6 deve ser atualizado para:

1. Refletir os 17 integration events (não apenas 6)
2. Adicionar seção sobre outbox pattern
3. Adicionar `Legi.Contracts` e `Legi.Messaging` na estrutura de projetos (seção 7.1)
4. Atualizar o diagrama de bounded contexts para incluir RabbitMQ
5. Adicionar RabbitMQ ao docker-compose documentation
6. Marcar "Mensageria: RabbitMQ" como ✅ Implementado na stack tecnológica