# Mensageria (RabbitMQ + Outbox próprio) — Decisões de Arquitetura

Documento com as decisões de design para integração assíncrona entre bounded contexts via RabbitMQ, utilizando uma implementação própria do padrão Outbox.

**Status de Implementação:** ✅ **Fases 1–6 CONCLUÍDAS e runtime-verified** (6D.4 feed-drift recompute adiado YAGNI; `CausationId` descartado — seria sempre null, nenhum consumer republica; ver nota em 6C). Resiliência: DLX/retry/parking + classificação transitório-vs-poison (6A/6B); observabilidade: correlação MessageId + métricas OTel + `/health` (6C); auditoria de idempotência dos 19 consumers + 3 gates de replay + `JsonStringEnumConverter` (6E); ops: `--migrate` step + flag, retenção outbox/inbox, `--reconcile-ratings` (6D). Pipeline completo: outbox/inbox sobre RabbitMQ.Client, sem MassTransit (decisão 2.2).

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

### 1.3 Workarounds Atuais (pré-mensageria) — ✅ todos removidos

| Workaround | Serviço | Descrição | Status |
|------------|---------|-----------|--------|
| BookSnapshot inline | Library | `AddBookToLibraryCommand` criava `BookSnapshot` inline quando ausente | ✅ Removido (Fase 2/4A) — o handler agora **lê** o snapshot populado por `BookCreated`/`BookUpdated`; lança 404 se ainda não chegou |
| Stub domain event handlers | Social | `CommentCreatedDomainEventHandler`, `ContentLikedDomainEventHandler`, etc. — logging only | ✅ Removido (Fase 4D) — convertidos em tradutores reais publicando via `IEventBus` |
| ContentSnapshot/FeedItem inline | Social | Command handlers criavam snapshots e feed items inline | ✅ Removido (Fase 4C) — `FeedItem`/`ContentSnapshot` agora criados pelos integration event handlers; sem criação inline nos command handlers |

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
- **Snapshot único, atualizável.** O `BookSnapshot` do Social é mantido fresco via `BookUpdated` — quando o Catalog atualiza o cover, o snapshot reflete o novo valor para *futuras* leituras de display.
- **Simetria.** Library e Social ambos mantêm `BookSnapshot` local — mesmo padrão, mesmo consumer, mesmo contrato.
- **Payload menor.** Eventos Library → Social ficam mais compactos.

**Custos:**

- Uma tabela a mais no Social (`book_snapshots`).
- Dois consumers a mais no Social (`BookCreated`, `BookUpdated`).
- Custo compensado pela eliminação de 3 campos redundantes em 3 eventos diferentes.

**Princípio geral:** Integration events carregam *semântica e IDs*. Display data é resolvida por read models locais alimentados pelos eventos do produtor dos dados em questão (o Catalog é quem "dita" os dados do livro). Evitar carregar display data em eventos de comportamento.

#### 2.6.1 `BookSnapshot` é fonte de lookup em *write-time*, não join em *read-time*

**Contradição corrigida (revisão Fase 4).** Uma versão anterior desta seção afirmava que, ao atualizar o cover via `BookUpdated`, "todos os `FeedItem`s daquele livro passam a exibir o cover novo, sem lógica de back-population". **Isso era falso** e contradizia o design real do `FeedItem`.

`FeedItem` é um read model **totalmente desnormalizado** (fan-out on read): carrega suas próprias colunas `BookTitle`, `BookAuthor`, `BookCoverUrl`, gravadas no momento da criação do item, justamente para que a query do feed seja um `SELECT` sem joins. O mesmo vale para `ContentSnapshot`. Esses valores **não** são resolvidos por join contra `BookSnapshot` em tempo de query.

**Papel real do `BookSnapshot` no Social:** é a **fonte de lookup que o handler de evento consulta no momento da escrita** para *assar* (bake) título/autor/cover dentro do novo `FeedItem`/`ContentSnapshot`. Sem ele, os 3 campos teriam que voltar para os eventos Library → Social — exatamente o que 2.6 elimina. O `BookSnapshot` justifica sua existência como fonte de write-time, não como tabela de join.

**Consequência aceita explicitamente:** um `BookUpdated` posterior atualiza o `BookSnapshot`, mas **não** reescreve os `FeedItem`s/`ContentSnapshot`s já criados — eles mantêm os valores do momento da criação (staleness). Para um tracker de leitura isso é negligenciável: título e autor praticamente nunca mudam, e um cover trocado é puramente cosmético. Se algum dia importar, um recompute periódico (Fase 6) reconcilia o drift. A alternativa (dropar as 3 colunas do `FeedItem` e fazer join com `BookSnapshot` na `FeedItemReadRepository`) foi **rejeitada** porque anula a razão de existir do `FeedItem` (leitura sem joins) e introduz custo de query em troca de frescor que o domínio não exige.

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
   ├── _ctx.Set<InboxMessage>().Add(new InboxMessage(MessageId, NOW()))   ← dispatcher stage a inbox row
   ├── IMediator.Publish(UserBookRatedIntegrationEvent)
   │   │
   │   └── UserBookRatedIntegrationEventHandler (Application)  [Fase 5, Opção B]
   │       ├── Carrega Book (ausente → nack-com-requeue transitório, §8.3)
   │       ├── upsert BookRating(BookId, UserId, Rating)        ← row por-usuário
   │       ├── recompute AVG/COUNT das rows daquele book
   │       └── book.RecalculateRating(avg5, count)              ← staging; SEM SaveChangesAsync
   │
   └── await _ctx.SaveChangesAsync()   ← o DISPATCHER faz o único commit
       (UPSERT book_ratings + UPDATE books + INSERT inbox_messages na mesma transação)

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
│   └── ReadingPostDeletedIntegrationEvent.cs
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
// Criado quando um livro é adicionado ao catálogo. Consumidores (Library, Social)
// usam para criar BookSnapshot local. AuthorDisplay não é carregado — cada
// consumidor faz a junção dos autores conforme sua convenção de display.
public record BookCreatedIntegrationEvent(
    Guid BookId,
    string Isbn,
    string Title,
    List<string> Authors,
    string? CoverUrl,
    int? PageCount
) : IIntegrationEvent;

// Criado quando dados de um livro são atualizados
public record BookUpdatedIntegrationEvent(
    Guid BookId,
    string Isbn,
    string Title,
    List<string> Authors,
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
    int RemovedRating      // half-star int removido; traceability/log — Catalog IGNORA sob Opção B
                           // (delete da row BookRating é por (BookId, UserId)). Ver Fase 5.
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

// ─────────────────────────────────────────────────────────────────────────
// REMOVIDO (decisão Fase 4 — listas fora do feed):
//   UserListCreatedIntegrationEvent / UserListDeletedIntegrationEvent
//
// LIBRARY-ARCHITECTURE-decisions.md §6.3 já havia cortado
// UserListCreatedDomainEvent por YAGNI ("criar lista vazia não é fato social
// relevante"). Honramos essa decisão: listas NÃO entram no feed. Sem
// FeedItem(ListCreated) e sem ContentSnapshot(List) → UserListDeleted também
// não tem nada para limpar no Social, então seu consumer Social seria no-op.
// Ambos os integration events ficam sem consumidor e são DROPADOS.
//
// UserListDeletedDomainEvent permanece no Library (não tem efeito cross-service
// na Fase 4). Reversível no futuro sem refactor: adicionar
// UserListCreatedDomainEvent em UserList.Create() + translator + consumer.
// Social já tem ActivityType.ListCreated e suporte a ContentSnapshot(List)
// definidos, então a reativação é barata.
//
// CONSEQUÊNCIA (ver bloco 4E, decisão Opção A): sem o handler de
// UserListCreated, nenhum ContentSnapshot(List) é criado → listas são
// NÃO-interagíveis no v1 (curtir/comentar exige snapshot). Reativar lista
// como interagível-mas-fora-do-feed = handler snapshot-only (Opção B).
// ─────────────────────────────────────────────────────────────────────────
```

**Consumidores:**

| Evento | Consumidor | Efeito |
|--------|------------|--------|
| `BookAddedToLibrary` | Social | Cria `FeedItem` (BookStarted) se não é wishlist |
| `ReadingStatusChanged` | Social | Cria `FeedItem` (BookFinished se NewStatus = "Finished") |
| `ReadingPostCreated` | Social | Cria `ContentSnapshot` (Post) + `FeedItem` (ProgressPosted) |
| `ReadingPostDeleted` | Social | Remove `ContentSnapshot` + `Likes` + `Comments` + `FeedItem` |
| `UserBookRated` | Social | Cria `FeedItem` (BookRated) |

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

**Total: 15 integration events.** *(Revisão Fase 4: `UserListCreated`/`UserListDeleted` dropados — listas fora do feed, ver 6.5. Era 17.)*

| Origem | Eventos | Destino(s) |
|--------|---------|------------|
| Identity | 3 (Registered, Deleted, UsernameChanged) | Social, Catalog, Library |
| Catalog | 2 (BookCreated, BookUpdated) | Library, Social |
| Library | 6 (BookAdded, StatusChanged, PostCreated, PostDeleted, Rated, RatingRemoved) | Catalog, Social |
| Social | 4 (ContentLiked, ContentUnliked, ContentCommented, CommentDeleted) | Library |

### 6.8 Divergências resolvidas com ARCHITECTURE.md

O ARCHITECTURE.md seção 6.2 foi escrito antes dos documentos de arquitetura detalhados (Library e Social). As seguintes atualizações são necessárias:

| Item | ARCHITECTURE.md (antigo) | Decisão atualizada | Motivo |
|------|--------------------------|---------------------|--------|
| `UserBookRatingRemovedIntegrationEvent` | Ausente | Adicionado | Library.Domain tem `UserBookRatingRemovedDomainEvent` com consumidor no Catalog |
| `ReadingPostCreatedIntegrationEvent` | Dados mínimos (sem book data) | Mantido lean; dados do livro resolvidos via `BookSnapshot` no Social | Decisão 2.6: snapshots locais em vez de denormalização em eventos |
| `UserListCreatedIntegrationEvent` | Ausente no ARCHITECTURE.md | **Dropado (Fase 4)** | Library §6.3 cortou o domain event por YAGNI; listas fora do feed (ver 6.5) |
| `UserListDeletedIntegrationEvent` | Ausente no ARCHITECTURE.md | **Dropado (Fase 4)** | Sem FeedItem/ContentSnapshot de lista para limpar → consumer Social seria no-op |
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

**Dead-letter (Fase 6 6A/6B — implementado):** cada work queue tem `x-dead-letter-exchange` apontando para uma **retry queue** (TTL fixo, default 30s) que dead-letteria de volta ao work (backoff flat). Em falha o host faz `nack(requeue:false)` → caminho de retry; a contagem de tentativas vem do header `x-death` (sem aritmética custom). Ao exceder o cap, a mensagem é **publicada no parking** (`legi.parking.{service}` → `{queue}.error`, sem consumer) e ack-ada — terminal, sem loop infinito. `TransientMessagingException` (§8.3) recebe budget generoso (`MaxTransientAttempts`); exceções genéricas parkam rápido (`MaxConsumerAttempts`). Park **não** escreve inbox row, então re-drive manual do error queue reprocessa exactly-once. *(Histórico: uma versão anterior afirmava "nack sem requeue, descarta após MaxAttempts" — isso nunca existiu; o comportamento v1 pré-6A era nack-com-requeue infinito.)*

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
| Recalcular valor (average_rating) | Recompute-from-rows é convergente/auto-curável — **mas só porque** o Catalog passa a guardar rows `BookRating` por-usuário (Fase 5, Opção B). É a partir daí que esta linha vira verdade: o handler faz upsert/delete da row e recalcula `AVG`/`COUNT`, então uma mensagem perdida/duplicada não corrompe a média permanentemente. Sem essas rows (estado anterior à 5B) não haveria fonte local completa para recalcular — a inbox seria o único guard. |

**Inbox como deduplicação primária:** Cada `IntegrationEventDispatcher` consulta `inbox_messages` antes de invocar o handler. Se a mensagem já foi processada (matching `MessageId`), ela é silenciosamente ignorada — ack imediato no broker, sem invocar handler. Se nova, o handler executa, e o `INSERT INTO inbox_messages` faz parte da mesma transação que as mudanças do handler. Isso garante: ou ambos commitam (handler logic + inbox row) ou nenhum (rollback completo). Na próxima entrega da mesma mensagem, o handler não roda de novo.

**Idempotência defensiva ainda importa:** Mesmo com inbox, é boa prática que os handlers sejam idempotentes em si. A inbox não protege contra (a) clock skew em casos extremos, (b) falhas após o INSERT mas antes do ack ao broker (gerando reentrega que será capturada pela inbox, mas ainda é processamento perdido). Os padrões na tabela acima continuam aplicáveis.

**Convenção: integration event handlers NÃO chamam `SaveChangesAsync`.** Diferente de command handlers (que comitam o trabalho do request) ou domain event handlers (que rodam dentro do `SaveChangesAsync` do request), integration event handlers são invocados pelo `IntegrationEventDispatcher` fora de qualquer request. O dispatcher é dono do ciclo: adiciona o `InboxMessage`, invoca o handler via `IMediator.Publish`, e então chama `SaveChangesAsync` uma única vez. Isso garante que o INSERT na inbox commite atomicamente com as mudanças do handler. Se o handler chamasse `SaveChangesAsync` por conta própria, a inbox row commitaria separadamente — quebrando a atomicidade que protege contra processamento duplicado.

**Exceção: operações bulk com `ExecuteUpdateAsync` / `ExecuteDeleteAsync`.** Para limpezas em cascata (ex: `UserDeleted` em múltiplas tabelas), handlers podem usar bulk operations diretamente em vez de stage no change tracker. Bulk operations commitam imediatamente, **antes** do `SaveChangesAsync` do dispatcher que persiste a inbox row — isso viola a atomicidade que 8.1 normalmente garante. É **aceitável apenas se** o handler for completamente idempotente:

- **Deletes filtrados** (`WHERE x = @value`): rodar duas vezes deleta zero rows na segunda. ✅
- **Updates convergentes para valor fixo** (`SET column = @newValue WHERE x = @value`): rodar duas vezes deixa o mesmo estado final. ✅
- **Decrementos/incrementos pareados com delete da fonte** (`SET column = column - 1 WHERE x IN (SELECT ... FROM source WHERE ...)`) **seguidos** de `DELETE FROM source WHERE ...` no mesmo handler: a primeira execução decrementa e deleta a fonte; a segunda execução vê a fonte vazia, o subquery retorna zero rows, nada é decrementado. **Idempotente apenas como conjunto, e apenas na ordem correta — o delete da fonte deve vir DEPOIS do decremento.** Documentar a ordem no handler.
- **Inserts, incrementos sem delete pareado, transições de estado**: NÃO idempotentes — devem usar o padrão de change-tracker para que o dispatcher commit atomicamente. ❌

Custo observável: entre as bulk-writes do handler e o `SaveChangesAsync` do dispatcher, o sistema pode estar em estado parcialmente atualizado se o processo crashar. A próxima redelivery converge para o estado correto via idempotência. Hardening da Fase 6 pode introduzir transação explícita ao redor do handler para eliminar essa janela.

**Outro custo: bulk operations bypassam domain events.** Um `ExecuteDeleteAsync` em `UserBooks` não dispara `BookRemovedFromLibraryDomainEvent`. Para `UserDeleted` isso é desejado (Social tem sua própria subscrição direta ao `UserDeletedIntegrationEvent`, não precisamos que Library faça fanout de N eventos). Mas o handler deve documentar isso explicitamente para o próximo leitor.

#### 8.1.1 Contadores no Library (Fase 4) — idempotência depende *exclusivamente* da inbox

Os handlers de `ContentLiked`/`ContentUnliked`/`ContentCommented`/`CommentDeleted` no Library ajustam `LikesCount`/`CommentsCount` em `ReadingPost`/`UserList`. Diferente de outros consumers idempotentes (recompute de rating, deletes filtrados), aqui **não há estado de domínio local que torne o incremento naturalmente idempotente**: as rows de `Like`/`Comment` vivem no Social, não no Library. O Library não tem como fazer "check-before-increment" — não enxerga o `Like`. Logo, a **inbox é a única defesa contra contagem dupla**.

**Regra obrigatória:** esses handlers usam o **caminho do change-tracker** (carregar o aggregate, chamar `IncrementLikes()`/`DecrementLikes()`/etc., e deixar o `IntegrationEventDispatcher` fazer o único `SaveChangesAsync`). É o inverso da exceção bulk-ops de 8.1: usar `ExecuteUpdateAsync` aqui **quebraria** a idempotência, porque o update commitaria *antes* da inbox row, abrindo a janela de double-count que a atomicidade inbox+handler existe para fechar. Anotar essa proibição no próprio handler.

**Aggregate alvo:** `TargetType` do evento mapeia `"Post"` → `ReadingPost`, `"List"` → `UserList`; `TargetId` é o `PostId`/`ListId`. Aggregate não encontrado (ex: post deletado concorrentemente) → no-op idempotente (o contador é irrelevante se o conteúdo sumiu).

#### 8.1.2 Handlers de deleção no Social NÃO re-emitem eventos de contador

Quando o Social processa `ReadingPostDeleted` e purga os `Like`s/`Comment`s daquele conteúdo, ele **não** deve publicar `ContentUnliked`/`CommentDeleted` de volta para o Library. O post já foi deletado no Library — seus contadores foram embora junto. Re-emitir decrementaria um contador que não existe mais. O bypass de domain events das bulk operations (acima) entrega esse comportamento de graça (o `ExecuteDeleteAsync` nos `Like`s/`Comment`s não levanta `ContentUnlikedDomainEvent`), mas o handler deve comentar a intenção para que ninguém "conserte" isso no futuro. *(Listas estão fora do feed na Fase 4, ver 6.5 — quando reativadas, a mesma regra vale para `UserListDeleted`.)*

#### 8.1.3 Handlers de criação de `FeedItem` no Social (Fase 4C) — idempotência também depende da inbox

`FeedItem.Create(...)` gera um `Guid` novo a cada chamada e não há chave natural para deduplicar (não é um upsert por `(TargetType, TargetId)` como o `ContentSnapshot`). Logo, uma redelivery do mesmo integration event criaria um **FeedItem duplicado** se não fosse a inbox. Mesma regra de 8.1.1: os handlers de criação **stage** o trabalho (sem `SaveChangesAsync`) para que a inbox row commite atomicamente com o insert do FeedItem; a redelivery bate na inbox e é pulada.

`ContentSnapshot` é upsert por `(TargetType, TargetId)`, então é naturalmente idempotente — mas ainda assim segue o staging para atomicidade com a inbox. O handler de deleção (4C.4) é naturalmente idempotente (deletar o que não existe é no-op), também via staging.

**No-op ainda precisa ackar.** O handler de `ReadingStatusChanged` só cria FeedItem quando `NewStatus == "Finished"`; nos demais casos é no-op. A infra de consumer **deve** commitar a inbox row mesmo quando o handler não muda nenhuma entidade — caso contrário a mensagem nunca é marcada como processada e entra em loop de redelivery. Verificar que o caminho no-op persiste a inbox row (item de housekeeping herdado da Fase 3: "inbox dedup silent-on-skip").

#### 8.1.4 Matriz de auditoria de idempotência (Fase 6 6E.1) — 19 consumers

Cada consumer cai em uma de três classes. **A dedup por MessageId vive no `IntegrationEventDispatcher` compartilhado** — provada uma vez por serviço pelos gates de replay (Library 4F.2, Catalog 5D.3, Social 6E.2), válida para todos. A coluna abaixo registra a defesa *adicional* (ou a falta dela) de cada handler.

| Serviço | Consumer | Efeito | Base de idempotência |
|---------|----------|--------|----------------------|
| Identity | `UserRegistered` | self-consumption (smoke/diagnóstico) | inbox |
| Catalog | `UserDeleted` | anonimiza `created_by` (`ExecuteUpdate`) | **convergente** (update filtrado → valor fixo) + inbox |
| Catalog | `UserBookRated` | upsert `BookRating` + recompute média | **convergente** (upsert por `(BookId,UserId)` + recompute-from-rows, Opção B) + inbox |
| Catalog | `UserBookRatingRemoved` | delete `BookRating` + recompute | **convergente** (delete-by-key + recompute) + inbox |
| Library | `BookCreated` / `BookUpdated` | upsert `BookSnapshot` | **convergente** (upsert por `BookId`) + inbox |
| Library | `UserDeleted` | hard-delete user_books/lists/posts (`ExecuteDelete`) | **convergente** (delete filtrado) + inbox — gate 6E.3 |
| Library | `ContentLiked`/`ContentUnliked`/`ContentCommented`/`CommentDeleted` | ±`LikesCount`/`CommentsCount` no `ReadingProgress` | **só-inbox** (§8.1.1 — sem estado local p/ check-before-increment) — gate 4F.2 |
| Social | `UserRegistered` | cria `UserProfile` (StageCreateIfMissing) | **convergente** (no-op se já existe) + inbox |
| Social | `UserDeleted` | purga follows/likes/comments/feed | **convergente** (deletes filtrados) + inbox |
| Social | `BookCreated` / `BookUpdated` | upsert `BookSnapshot` | **convergente** (upsert por `BookId`) + inbox |
| Social | `BookAddedToLibrary` / `ReadingStatusChanged` / `UserBookRated` | cria `FeedItem` | **só-inbox** (§8.1.3 — `FeedItem` sem chave natural) — gate 6E.2 |
| Social | `ReadingPostCreated` | cria `FeedItem` (só-inbox) + `ContentSnapshot` (upsert) | **só-inbox** p/ o FeedItem; ContentSnapshot convergente |
| Social | `ReadingPostDeleted` | purga ContentSnapshot/likes/comments/FeedItem por PostId | **convergente** (delete-by-target) + inbox |

*Transitório (§8.3), ortogonal às classes acima:* os handlers de feed do Social e os de rating do Catalog lançam `TransientMessagingException` quando um lookup local (`UserProfile`/`BookSnapshot`/`Book`) ainda não chegou → retry com budget generoso (6B), não park. Não é uma classe de idempotência — é a política de retry para uma pré-condição que vai se resolver.

**Conclusão da auditoria:** nenhum consumer depende de algo além de (inbox) e/ou (convergência natural). Os dois caminhos só-inbox sem chave natural (contadores do Library, FeedItem do Social) têm gates de replay dedicados; as cascatas bulk que commitam fora da transação da inbox (UserDeleted) têm o gate 6E.3. Cobertura completa.

#### 8.1.5 Evolução de schema / contratos (Fase 6 6E.4–6E.5)

- **Enums no fio:** o `IntegrationEventSerializer` registra `JsonStringEnumConverter` — qualquer campo enum futuro serializa como nome, não ordinal. Hoje nenhum contrato carrega enum (usam strings na fronteira, §6.5), então adotar isso **não exigiu drenar filas** (não há mensagens int-encoded). Se um contrato ganhar um enum no futuro, o nome é estável a renomeações de membros adjacentes — mas **reordenar/renomear membros do enum** continua sendo breaking p/ mensagens em voo.
- **Type rename/move = breaking:** a discriminação de tipo é por *assembly-qualified name* gravado na coluna `Type` do outbox e no header. Renomear/mover um contrato (namespace, assembly, classe) quebra a desserialização de qualquer mensagem já produzida (`Type.GetType` retorna null → handler lança → retry → eventualmente park). Mitigação aceita p/ o modelo monorepo single-deploy do Legi: drenar filas antes de tais renomeações, ou manter um shim do nome antigo. Documentado no docstring de `IntegrationEventSerializer`.

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

Quando o handler do consumer falha (a partir da Fase 6 6A/6B): `nack(requeue:false)` → a mensagem é dead-lettered para a **retry queue** (TTL fixo), reentregue ao work após o TTL, e ao exceder o cap de tentativas (`MaxConsumerAttempts` p/ genérico, `MaxTransientAttempts` p/ `TransientMessagingException`) é desviada ao **parking** (error queue, terminal). Contagem via `x-death`. *Não* há mais loop infinito de redelivery. (Antes da 6A o host fazia `nack` **com** requeue sem limite — redelivery indefinido.)

**Justificativa para v1:** projeto pessoal, logs sob observação ativa. Loop de redelivery é loud e visível, o que acelera diagnóstico de bugs em handlers. Contar tentativas exigiria header customizado ou uso de quorum queues — complexidade adiada.

**Suficiente para:** falhas transitórias de banco (lock timeout, connection pool), broker temporariamente indisponível, picos de carga, deploys sequenciais.

**Não resolve:** bugs no código, dados corrompidos, violações de constraint. Esses casos manifestam como redelivery loop visível em log. Quando observado, requer correção do handler ou descarte manual da mensagem (purge da queue no Management UI). Dead-letter exchange dedicado e contagem de attempts são hardening de Fase 6.

### 8.3 Ordering

**Não há garantia de ordenação** entre mensagens diferentes. Se Library publica `BookAddedToLibrary` e depois `ReadingPostCreated` no mesmo segundo, Social pode receber na ordem inversa, porque:

- Múltiplas instâncias do dispatcher worker podem publicar em paralelo (com `FOR UPDATE SKIP LOCKED`, cada uma pega rows diferentes em ordens não previsíveis).
- O RabbitMQ entrega FIFO *dentro de uma queue*, mas se um consumer tem prefetch > 1 e processa em paralelo, a ordem é perdida.

**Impacto no Legi:** Baixo. Cada consumer é independente. Um FeedItem de "post criado" não depende de um FeedItem de "livro adicionado" já existir. O único caso sensível é `UserDeleted` — mas a limpeza é idempotente (deletar o que não existe é no-op).

**Se no futuro for necessário ordering por aggregate:** opções são (a) particionar por `UserId` no payload e usar consumer single-instance por partição, (b) usar headers do AMQP para priorização. Não implementar agora — YAGNI.

**Caso concreto introduzido na Fase 4 — snapshot local ausente quando um evento Library → Social chega.** Os handlers do Social que criam `FeedItem`/`ContentSnapshot` fazem **dois lookups locais**: `UserProfile` (por `UserId`, para `ActorUsername`/`ActorAvatarUrl`) e `BookSnapshot` (por `BookId`, para título/autor/cover, ver 2.6.1). Como não há ordering cross-event, qualquer um dos dois pode ainda não existir: `UserProfile` é criado por `UserRegistered` (Identity → Social) e `BookSnapshot` por `BookCreated` (Catalog → Social); ambos podem chegar depois do evento do Library.

**Regra:** **qualquer** snapshot ausente (UserProfile ou BookSnapshot) é tratado como **condição transitória**. O handler lança, a mensagem é `nack`-ed *com* requeue, e o RabbitMQ reentrega. Quando o evento que cria o snapshot faltante chegar, a reentrega seguinte tem sucesso. Coerente com a filosofia de "redelivery loop loud e visível" (8.2) — emitir log de warning identificando qual snapshot (`UserId` ou `BookId`) faltava, para diagnosticar loop genuíno. Em monorepo saudável, janela de subsegundos.

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
| Novos notification handlers (incoming) | `UserRegisteredIntegrationEventHandler` (cria UserProfile). `UserDeletedIntegrationEventHandler` (deleta tudo). `UsernameChangedIntegrationEventHandler` (atualiza username). `BookCreatedIntegrationEventHandler` (cria BookSnapshot). `BookUpdatedIntegrationEventHandler` (upsert BookSnapshot). **5** handlers de Library events (FeedItem/ContentSnapshot — resolvem dados do livro via BookSnapshot; listas fora do feed, ver 6.5). |
| Remoção stubs | Substituir domain event handlers stub por versões que publicam integration events |
| Infrastructure | `AddLegiMessaging<SocialDbContext>()` com consumers registrados |
| Outbox | Migration para tabelas de outbox no `social` DB |

---

## 10. Fases de Implementação

### Fase 1 — Foundation (sem eventos de negócio, pipeline próprio validado ponta-a-ponta) ✅ CONCLUÍDA

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

### Fase 2 — Primeiro evento end-to-end (Catalog → Library: BookCreated) ✅ CONCLUÍDA

> **Reconciliação (Fase 4A):** apesar de marcada concluída, apenas a metade `BookCreated` estava de fato ligada ponta-a-ponta. O pipeline `BookUpdated` (tarefas 2.2/2.4 — `BookUpdatedDomainEvent`, `Book.RaiseUpdatedEvent()`, `BookUpdatedDomainEventHandler` no Catalog, contrato `BookUpdatedIntegrationEvent`, consumer no Library) não existia e foi construído durante a 4A, junto com o consumer do Social. Agora sim a Fase 2 está completa para ambos os eventos. *Ponto em aberto:* confirmar que `RaiseUpdatedEvent()` só dispara quando algum campo de fato mudou (evitar evento espúrio em update no-op).

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

### Fase 3 — Identity events (UserRegistered + UserDeleted → todos) ✅ CONCLUÍDA

**Objetivo:** Lifecycle do usuário propagado automaticamente — criação de UserProfile no Social, e cascata de deleção em todos os serviços.

**Escopo ajustado:** `UsernameChanged` foi **removido desta fase**. Identity não tem fluxo de rename hoje (sem `UpdateProfileCommand`, sem mutator em `User`, sem `UsernameChangedDomainEvent`). Adicionar o evento exigiria construir a feature inteira primeiro. Os mutators do lado consumidor já existem e esperando (`UserProfile.UpdateUsername`, `ContentSnapshot.UpdateOwner`) — quando a feature de rename for construída, o plumbing cross-service é a parte fácil. Fase 3 cobre **UserRegistered + UserDeleted apenas**.

**Sub-fases:**

| Sub-fase | Descrição | Status |
|----------|-----------|--------|
| 3A | Identity publica `UserDeletedIntegrationEvent` (contract + translator; `UserDeletedDomainEvent` já existia, raised em `DeleteAccountCommandHandler`) | ✅ |
| 3B | Preconditions do Social (project refs, registro de `INotificationHandler<>`, `ApplyMessagingConfigurations`, `AddLegiMessaging<SocialDbContext>`, migration outbox/inbox, appsettings) | ✅ |
| 3C | Catalog consome `UserDeleted` → anonimiza `created_by` | ✅ |
| 3D | Library consome `UserDeleted` → hard-delete de `user_books`, `user_lists`, `reading_posts` | ✅ |
| 3E | Social consome `UserRegistered` (cria UserProfile) + `UserDeleted` (purge completo) | ✅ |
| 3F | Teste end-to-end: registro → cria conteúdo nos 3 serviços → delete → verificar cascata | ✅ |
| 3G | Teste de dedup: re-publicar UserDeleted, verificar que os 3 consumers pulam via inbox | ✅ |

**Idempotência dependente de ordem — provada empiricamente (3G).** Dois testes distintos: (A) redelivery COM inbox dedup → handler pulado silenciosamente, contadores intactos — prova que o *ledger* funciona. (B) redelivery com a inbox row APAGADA, forçando o purger a re-rodar de fato → contadores permaneceram 0/0 porque o delete de `Follows` da primeira execução esvaziou as subqueries de decremento da segunda — prova que o *handler em si* é idempotente, independente do ledger. Teste B é a prova real; Teste A sozinho só provaria o inbox.

**Decisões por consumidor:**

**Catalog (3C) — anonimizar, não deletar.** `Book.CreatedByUserId` passou de `Guid` para `Guid?` (propriedade nullable; `Book.Create` ainda exige non-null — não se cria livro sem saber quem adicionou). Adicionado `Book.AnonymizeCreator()` como caminho de domínio canônico, mas o handler de produção usa bulk update via `IBookRepository.AnonymizeCreatorsAsync` (`ExecuteUpdateAsync SET CreatedByUserId = null WHERE CreatedByUserId = @userId`). Livros persistem (outros usuários têm eles em suas bibliotecas); só perdem a atribuição de criador. `BookCreatedDomainEvent.CreatedByUserId` permanece `Guid` non-null — é um fato histórico do momento da criação, nunca null. Migration: `ALTER COLUMN created_by_user_id DROP NOT NULL`. Idempotente (filtro casa zero rows no rerun).

**Library (3D) — hard-delete tudo.** Soft-delete existe para recuperabilidade voltada ao usuário (remover um livro e re-adicionar). No instante em que o usuário some, não há mais "voltado ao usuário" — e erasure estilo GDPR implica hard-delete na deleção de conta. Três bulk deletes via repository methods (`DeleteAllForUserAsync` em `IUserBookRepository`, `IReadingProgressRepository`, `IUserListRepository`): `reading_posts`, `user_lists` (cascata DB-level para `user_list_items`), `user_books`. O delete de `user_books` usa **`IgnoreQueryFilters()`** — sem isso o filtro global `DeletedAt == null` excluiria rows já soft-deletadas, deixando órfãos. Bypassa domain events de propósito (não queremos fanout de N `BookRemovedDomainEvent`; Social tem sua própria subscrição direta a `UserDeleted`). Idempotente individualmente.

**Social (3E) — purge coordenado via `IUserDataPurger`.** A cascata toca 7 tabelas + ajuste de contadores. Não é persistência de aggregate — é purge coordenado de read models. A forma certa é um **serviço** (`IUserDataPurger` em Application/Common/Interfaces, `UserDataPurger` em Infrastructure/Persistence), não repository methods espalhados. O serviço retorna um `UserPurgeResult` record com contagens para observability.

A purga tem **idempotência dependente de ordem**: os decrementos de contadores (`FollowersCount`/`FollowingCount` em perfis de OUTROS usuários) devem rodar **antes** do delete de `Follows`. Em redelivery, as rows de `Follows` já foram deletadas → subqueries dos decrementos retornam vazio → nada é decrementado duas vezes. Reordenar quebraria a idempotência.

Cleanup indireto (likes/comments/feed-items SOBRE o conteúdo do usuário) não carrega o owner id — usa `ContentSnapshot` como chave de join (`OwnerId`). Adicionado índice `ix_content_snapshots_owner_id` (3E migration). Batched por target type para evitar N round-trips. Aceita race teórico (like inserido entre query e delete) — órfão aponta para conteúdo que será deletado de qualquer forma; recompute periódico (Fase 6) corrige drift.

**`UserRegistered` no Social — `StageCreateIfMissingAsync`, não overwrite.** Diferente do `StageAddOrUpdateAsync` do BookSnapshot (que sobrescreve, porque BookUpdated traz dados mais frescos), o handler de UserProfile faz **no-op se já existe**. O username de `UserRegistered` é o de registro — não pode sobrescrever um rename posterior. `UserProfile.Create` promovido de `internal` para `public` (factory de aggregate deve ser público para a Application chamar). Social ignora `Email` do evento — UserProfile não tem campo email; o evento carrega Email para outros consumidores (ex: notification service futuro), e cada consumidor projeta o que precisa.

**Convenção bulk-ops + 8.1:** `ExecuteDeleteAsync`/`ExecuteUpdateAsync` são commit-imediato, fora do change tracker. A regra "handlers não chamam SaveChangesAsync" (8.1) não se aplica a elas — são a exceção documentada em 8.1. Mas o handler de `UserRegistered` escreve via change tracker (`StageCreateIfMissingAsync`), então *esse* não chama SaveChangesAsync — o dispatcher commita inbox row + profile juntos.

**Cut-over dev:** usuários registrados antes de 3E estar no ar não recebem UserProfile retroativo. Política dev: wipe-and-re-register. Backfill scripts (Social.UserProfile, Library.BookSnapshot para usuários pré-messaging) ficam para quando o projeto se aproximar de produção.

**Entregável:** Lifecycle do usuário propagado automaticamente — UserProfile criado no registro, dados removidos/anonimizados na deleção, através de Catalog + Library + Social, todos via fanout do mesmo `UserDeletedIntegrationEvent`.

### Fase 4 — Library ↔ Social (eventos bidirecionais)

**Objetivo:** Feed e interações sociais funcionais — a fase em que o feed "ganha vida". É a maior fase até aqui; dividida em sub-fases para revisão incremental, cada uma um marco antes de seguir.

**Decisões de design que esta fase força (revisar antes de implementar):**

1. **`FeedItem`/`ContentSnapshot` permanecem desnormalizados; `BookSnapshot` é fonte de write-time, não join.** Ver decisão 2.6.1. Consequência aceita: book metadata fica stale em itens já criados após `BookUpdated`.
2. **`BookSnapshot` ausente em evento Library → Social = condição transitória → nack-com-requeue.** Ver 8.3 (caso concreto Fase 4).
3. **Contadores no Library dependem exclusivamente da inbox; caminho change-tracker é obrigatório.** Ver 8.1.1. Handlers de deleção no Social não re-emitem eventos de contador. Ver 8.1.2.

**Sub-fases:**

**4A — `BookSnapshot` no Social (pré-requisito)** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 4A.1 | Entity `BookSnapshot` no Social.Domain (análogo ao de Library) + EF configuration + migration | ✅ |
| 4A.2 | Repository com `StageAddOrUpdateAsync` (upsert idempotente, mesmo padrão do Library) | ✅ |
| 4A.3 | `BookCreatedIntegrationEventHandler` (cria snapshot) + `BookUpdatedIntegrationEventHandler` (upsert) na Application | ✅ |
| 4A.4 | Registrar consumers `BookCreated` + `BookUpdated` na DI do Social | ✅ |
| 4A.5 | **Teste:** criar livro no Catalog → `BookSnapshot` aparece no Social | ✅ (no e2e da 4C/4F) |

*Por que primeiro:* desbloqueia 4C (handlers não resolvem display data sem ele). Espelha a Fase 2 mecanicamente — baixo risco, bom aquecimento.

*Status 4A:* ✅ código completo + **runtime-verified** (o e2e da 4C/4F confirmou o `BookSnapshot` aparecendo no Social a partir do `BookCreated`). **Bônus:** 4A fechou um gap da Fase 2 — o pipeline `BookUpdated` (publisher no Catalog + consumer no Library + consumer no Social) não existia e foi construído aqui (ver nota na Fase 2).

**4B — Publishers do Library (outgoing)** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 4B.1 | Adicionar contratos faltantes em `Legi.Contracts/Library/` | ✅ |
| 4B.2 | **5** domain event handlers traduzindo domain → integration via `IEventBus` | ✅ |
| 4B.3 | **Teste:** translator publica 1 integration event por ação (9 testes; outbox-write/atomicidade real fica para o gate de runtime) | ✅ |

*Notas de implementação 4B:*
- Nome real da entidade de post é `ReadingProgress` (tabela `reading_posts`); o domain event de criação é `ReadingProgressCreatedDomainEvent`, mas o contrato cross-context é `ReadingPostCreatedIntegrationEvent` (split interno/externo documentado no handler). Não afeta 4C — Social só vê o contrato. Débito de naming (alinhar `ReadingPost`/`ReadingProgress`) adiado para pós-Fase-4.
- **Bug pré-existente corrigido:** `UserBookRatedDomainEvent` tinha o parâmetro/propriedade `userBookId`, mas `UserBook.Rate(...)` passava `UserId` nesse slot. Renomeado para `userId` (valor em runtime já era UserId; só o nome estava mentindo). Nenhum consumer precisa de UserBookId aqui.
- `ReadingPostDeletedIntegrationEvent` ficou `(PostId, UserId)` — `BookId` dropado (purge é por PostId; YAGNI).
- `BookAddedToLibraryIntegrationEvent` usa `OccurredOn` do domain event como `AddedAt`.

*Escopo (dois cortes YAGNI conscientes):*
- **`UserBookRatingRemoved` adiado para a Fase 5.** Único consumidor é o Catalog (Fase 5). Publicá-lo agora gera outbox rows para zero queues vinculadas — inofensivo, mas inútil. `UserBookRated` *é* construído agora (Social precisa para o FeedItem `BookRated`); Fase 5 só adiciona o consumer do Catalog no mesmo exchange fanout.
- **`UserListCreated`/`UserListDeleted` dropados** — listas fora do feed (ver 6.5). Library §6.3 já havia cortado `UserListCreatedDomainEvent`. Não há publisher de lista na Fase 4.

*Alinhamento de campos (domain events enriquecidos aditivamente):* os domain events do Library não carregam tudo que os integration events exigem. Verificar a forma real de cada um e alinhar, enriquecendo o domain event de forma aditiva quando faltar campo (em vez de dropar dado):
- `BookAddedToLibraryDomainEvent`: integration event precisa de `UserBookId` + `AddedAt` (o evento de `Remove()` já carrega o `Id`, então é consistente adicionar ao de Added).
- `ReadingPostCreatedDomainEvent`: faltam `Content`, `ProgressValue`, `ProgressType`, `CreatedAt`.
- `UserBookRatedDomainEvent`: carrega `Rating` (value objects) → mapear para `int`/`int?` via `rating.Value` no translator.
- `ReadingStatusChangedDomainEvent`: status como enum → `.ToString()` na fronteira (§6.5).

**4C — Social consome eventos do Library (incoming) — o núcleo "feed ganha vida"** ✅ CONCLUÍDA (runtime-verified)

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 4C.1 | `BookAddedToLibraryIntegrationEventHandler` → `FeedItem` (BookStarted) se não wishlist; sem ContentSnapshot (não interagível); `TargetType=null`, `ReferenceId=BookId` | ✅ |
| 4C.2 | `ReadingStatusChangedIntegrationEventHandler` → `FeedItem` (BookFinished) **só se** `NewStatus == "Finished"`; senão no-op | ✅ |
| 4C.3 | `ReadingPostCreatedIntegrationEventHandler` → `ContentSnapshot` (Post) + `FeedItem` (ProgressPosted) | ✅ |
| 4C.4 | `ReadingPostDeletedIntegrationEventHandler` → purga `ContentSnapshot` + `Like`s + `Comment`s + `FeedItem` via load+`RemoveRange` (atômico com inbox; sem re-emitir, 8.1.2) | ✅ |
| 4C.5 | `UserBookRatedIntegrationEventHandler` → `FeedItem` (BookRated); `Data = {"rating": <half-star int ÷ 2>}` | ✅ |
| 4C.6 | Dois lookups locais (`UserProfile` + `BookSnapshot`) via helper `FeedLookups`; qualquer um ausente → throw → nack transitório (8.3) | ✅ |
| 4C.7 | Staging (sem `SaveChangesAsync`); `Stage*` adicionados aos repos Feed/ContentSnapshot/Like/Comment (load + RemoveRange, nunca ExecuteDelete) | ✅ |
| 4C.8 | 5 consumers registrados na DI do Social; no-op acka (dispatcher sempre commita a inbox row — **silent-on-skip RESOLVIDO**) | ✅ |
| 4C.9 | 16 testes (Social.Application.Tests) + **runtime e2e** (docker, RabbitMQ + outbox/inbox real) — feed de B mostrou todas as atividades de A com username/título/autor/cover corretos, purge do post deletado confirmado | ✅ |

**Gate de runtime PASSOU** — este e2e fecha 4A, 4B e 4C de uma vez. Infra adicionada para viabilizar: serviço `social-api` no docker-compose + Dockerfile + rota nginx `/api/v1/social/` + `Database.Migrate()` idempotente no startup das 4 APIs.
**Housekeeping resolvido:** "no-op precisa ackar" / "inbox dedup silent-on-skip" — confirmado que o dispatcher sempre commita a inbox row mesmo sem mudanças de entidade.
**Item Fase 6:** `Database.Migrate()` no startup corre risco de race com múltiplas instâncias — mover para step separado no hardening.

*Listas:* sem handlers de `UserListCreated`/`UserListDeleted` (ver 6.5). `ActivityType.ListCreated` e suporte a `ContentSnapshot(List)` ficam definidos-mas-ociosos até reativação futura.

**4D — Social publica interações (outgoing) + retira stubs** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 4D.1 | 4 stubs convertidos em tradutores (classes mantidas como subscrições) publicando via `IEventBus`; `TargetType.ToString()` na fronteira; sem `SaveChangesAsync` | ✅ |
| 4D.2 | Contratos em `Legi.Contracts/Social/` (TargetType+TargetId; UserId/CommentId só traceability); `CommentCreated` → `ContentCommentedIntegrationEvent` | ✅ |
| 4D.3 | 5 testes de tradutor (PublishAsync 1×, shape correto, VerifyNoOtherCalls) | ✅ |

**4E — Library consome interações (incoming) — fecha o loop** ✅ CONCLUÍDA

> **Decisão (Fase 4, Opção A): listas são NÃO-interagíveis no v1.** Curtir/comentar exige um `ContentSnapshot` do alvo (o command handler lança `NotFound` sem ele). O snapshot de lista só era criado pelo handler de `UserListCreated`, que foi dropado (ver 6.5). Logo, nenhuma lista pode ser curtida/comentada, e `ContentLiked`/`ContentCommented` só carregam `TargetType = "Post"` na prática. Consequências aceitas: `UserList.LikesCount`/`CommentsCount` ficam dormentes (métodos e colunas mantidos, prontos para reativação); o sort de `SearchPublicAsync` deixa de usar `LikesCount` (que seria sempre 0) e passa a ordenar por `BooksCount`/`CreatedAt`. Reativar = Opção B (handler snapshot-only para `UserListCreated`, sem FeedItem) — fora de escopo agora.

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 4E.1 | 4 handlers (`ContentLiked`/`ContentUnliked`/`ContentCommented`/`CommentDeleted`) ajustando `LikesCount`/`CommentsCount` **só para `Post` → `ReadingProgress`** via caminho change-tracker (carrega tracked, muta, dispatcher commita; **nunca** `ExecuteUpdate`/`SaveChanges` — decisão 8.1.1). Guard comum em `InteractionTargetResolver` | ✅ |
| 4E.2 | `TargetType` ≠ `"Post"` (List/Review) → log warning + no-op (acka; não pode ocorrer legitimamente). Aggregate não encontrado (post deletado) → no-op **terminal** (ack, **não** requeue — conteúdo sumiu é estado permanente, diferente do snapshot ausente da 4C) | ✅ |
| 4E.3 | Corrigir `UserListReadRepository.SearchPublicAsync`: remover `LikesCount` do `OrderBy` (sempre 0) → ordenar por `BooksCount desc, CreatedAt desc`. Manter `LikesCount` no DTO (0, inofensivo). | ✅ |
| 4E.4 | Registrar os 4 consumers na DI do Library (`AddLegiMessaging<LibraryDbContext>` já existe) | ✅ |
| 4E.5 | **Teste:** like no Social → `LikesCount` incrementa no `ReadingProgress`; decrement com floor em 0; `TargetType=List` no-op; post inexistente no-op; sem `SaveChanges`/`ExecuteUpdate` nos handlers (12 testes). Idempotência (replay) no 4F. | ✅ |

*Status 4E:* ✅ código completo (12 testes unitários: contador ±1, floor em 0, no-op de List, no-op terminal de post inexistente). `GetByIdAsync` confirmado como load **tracked** (sem `AsNoTracking`). Gate de idempotência inbox-only validado no 4F.2.

**4F — End-to-end + reconciliação de docs** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 4F.1 | Teste e2e: usuário A posta progresso → seguidor B vê no feed → B curte → contador ↑ no Library → B comenta → contador ↑ → A deleta o post → Social purga feed/snapshot/likes/comments | ✅ |
| 4F.2 | Teste de dedup (replay de cada evento, verificar inbox skip) | ✅ |
| 4F.3 | Atualizar `ARCHITECTURE.md` §6; marcar Fase 4 ✅ | ✅ |

**Gate de runtime 4F PASSOU (docker, RabbitMQ + Postgres reais):**

*4F.1 — loop completo.* Feed de B mostrou as 4 atividades de A (ProgressPosted/BookRated/BookFinished/BookStarted) com username/título/autor/cover corretos, `Data` correto, newest-first. Contadores via endpoint do Library: like → `LikesCount` 1, comment → `CommentsCount` 1, unlike → `LikesCount` 0 (floor segura). **Sem drift:** contagens em tempo real do feed do Social == contadores desnormalizados do Library (0 likes / 1 comment). Delete do post → Social purgou feed item + ContentSnapshot + comments. **§8.1.2 provado consultando as tabelas direto:** outbox do Social emitiu 0× `CommentDeleted` (só 1× cada de Liked/Unliked/Commented); inbox do Library consumiu 0× `CommentDeleted`. Nenhuma exceção / redelivery preso.

*4F.2 — gate §8.1.1 (o que de fato importa).* Mecanismo: teste de integração (`tests/Legi.Library.Integration.Tests/InboxReplayDedupTests`) dirigindo o `IntegrationEventDispatcher<LibraryDbContext>` real contra o Postgres do compose (bypassa só o transporte RabbitMQ). Mesmo `MessageId` 2× → `LikesCount` moveu **exatamente 1×** + **exatamente 1 inbox row**; um `MessageId` **distinto** com o mesmo evento moveu de novo (→ 2) — provando que o guard é a inbox row, **não** sorte/timing. Mesmo padrão para `ContentCommented` → dedup é **event-agnostic** (vive no dispatcher). Testes são `[SkippableFact]` + `Skip.If` em `LIBRARY_TEST_DB` (pacote `Xunit.SkippableFact`; xUnit 2.9.3 não tem `Assert.Skip`), então a suíte default segue verde e sem Docker.

*Infra/housekeeping:* `social-api` adicionado ao docker-compose (Dockerfile + rota nginx `/api/v1/social/`); `Database.Migrate()` idempotente no startup das 4 APIs (ver item de hardening Fase 6 sobre race com múltiplas instâncias). Stub `UnitTest1.cs` removido de Library.Application.Tests.

**Edge case a confirmar (decisão de produto, não bloqueante):** transição `Wishlist → Reading` não produz FeedItem hoje (apenas `BookAdded`-não-wishlist → `BookStarted`, e `→ Finished` → `BookFinished` estão mapeados). Se um item "começou a ler" for desejado nessa transição, é uma adição pequena ao handler de `ReadingStatusChanged` (4C.2).

**Entregável:** ✅ Sistema social completo. Feed funcional com dados de livro corretos. Contadores de like/comment atualizados via eventos, idempotentes sob redelivery. Stubs removidos.

### Fase 5 — Library → Catalog (ratings) ✅ CONCLUÍDA (runtime-verified)

**Objetivo:** `Book.AverageRating` + `RatingsCount` mantidos automaticamente via eventos. `UserBookRatedIntegrationEvent` já é publicado desde a Fase 4 (Social consome para o FeedItem `BookRated`); a Fase 5 adiciona o **segundo consumer** (Catalog) no mesmo exchange fanout, mais o publisher de `UserBookRatingRemoved` que ficou adiado da 4B.

**Decisões de design (travadas 2026-06-04, via ddd-architecture-advisor):**

1. **Como o Catalog recalcula = Opção B (rows por-usuário).** Nova projeção leve `BookRating(BookId, UserId, Rating)` no Catalog — modelada como projeção (igual a `BookSnapshot`/`ContentSnapshot`), **não** um aggregate root; `Book` continua o único aggregate root. Os handlers fazem upsert/delete da row e então **recalculam** `AVG`/`COUNT` sobre as rows daquele book → `Book.RecalculateRating(decimal avg5, int count)`.
   - *Por que B e não A (coluna `RatingsSum` incremental + delta, dependente só da inbox):* `AverageRating` é o número mais visível/ordenável do catálogo (um `LikesCount` com drift é cosmético; uma média com drift mente para o usuário). B é **convergente/auto-curável** — uma mensagem perdida ou duplicada não corrompe permanentemente a média, o próximo evento recalcula a partir da verdade. Faz a afirmação de §8.1 ("recompute idempotente") ser *verdadeira* em vez de meia-verdade. Custo: uma tabela + write repo + EF config + migration (forma já construída ×3: BookSnapshot, BookSnapshot Social, ContentSnapshot).
   - *Consequência:* o Catalog ignora `PreviousRating`; o contrato de remoção precisa só de `BookId + UserId` (campo `RemovedRating` mantido para traceability/log e viabilidade de um futuro flip para Opção A). `PreviousRating` permanece em `UserBookRated` — Social usa, e removê-lo seria breaking change sem ganho.

2. **`Book` não encontrado num evento de rating (race rate-antes-de-`BookCreated`) = nack-com-requeue transitório** (espelha o snapshot-ausente da 4C, ver §8.3), **não** no-op terminal. Um rating para um book que o Catalog ainda não criou é a mesma janela de ordering da 4C; logar warning com `BookId`.

3. **Idempotência (inverso da regra 4E §8.1.1):** aqui upsert/delete + recompute é convergente, então a **inbox é defesa-em-profundidade, não o único guard**. Os handlers fazem staging (sem `SaveChangesAsync`); o dispatcher commita a row do `BookRating` + a inbox row atomicamente. (Em §8.1.1 os contadores do Library dependiam *exclusivamente* da inbox porque não havia estado local; aqui há.)

**Sub-fases:**

**5A — Contrato + publisher do Library (a metade adiada da 4B)** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 5A.1 | Adicionar `UserBookRatingRemovedIntegrationEvent(BookId, UserId, RemovedRating)` em `Legi.Contracts/Library/` (campo `RemovedRating` = half-star int; Catalog ignora sob Opção B) | ✅ |
| 5A.2 | `UserBookRatingRemovedDomainEventHandler` (translator em `Legi.Library.Application/UserBooks/EventHandlers/`) espelhando o de `UserBookRated`: mapeia `OldRating.Value` → `RemovedRating`, publica via `IEventBus`; sem `SaveChangesAsync`; auto-registrado pelo reflection scan | ✅ |
| 5A.3 | Confirmar que `UserBook.RemoveRating()` levanta `UserBookRatingRemovedDomainEvent` carregando o rating pré-remoção (verificar `UserBook.cs`) | ✅ (só levanta quando `CurrentRating` != null; captura `oldRating` antes de limpar) |
| 5A.4 | **Teste:** translator publica 1 integration event por remoção, shape correto, `VerifyNoOtherCalls` | ✅ |

**5B — Domínio + persistência de rating no Catalog** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 5B.1 | Projeção `BookRatingEntity` (persistence entity em `Catalog.Infrastructure`, padrão `AuthorEntity`/`TagEntity` — Catalog tem split domínio/persistência; chave natural `(BookId, UserId)`, `Rating` half-star 1-10). `BookRatingConfiguration` → tabela `book_ratings` | ✅ |
| 5B.2 | **`Book.RecalculateRating(decimal newAverage, int totalRatings)` já existe e tem a assinatura certa** (valida 0-5, arredonda 2dp, levanta `BookRatingRecalculatedDomainEvent`) — sem mudança no domínio. Pseudo-código errado do doc corrigido na §6.4/fluxo. Rounding já coberto 6× em `BookTests`. | ✅ |
| 5B.3 | `IBookRatingRepository` (Domain) + `BookRatingAggregate` (record com `FromHalfStarRatings` puro/testável — conversão half-star→0-5). Impl `BookRatingRepository` faz **load tracked → muta no change-tracker → recompute in-memory** (não SQL `AVG`, pois a row staged não é visível a query SQL antes do commit); sem `SaveChangesAsync`. `DbSet<BookRatingEntity>` + DI. | ✅ |
| 5B.4 | Migration `AddBookRatings` (só cria `book_ratings`, PK composta `(book_id, user_id)`; sem drift) | ✅ |
| 5B.5 | Testes: `FromHalfStarRatings` — 0 rows→(0,0); `[7,9]`→(4.0, 2); single 10→5.0 / 1→0.5; `[7,8,8]`→~3.833 (round 2dp = 3.83). (4 testes; Catalog.Domain 62→66) | ✅ |

**5C — Consumers no Catalog + DI** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 5C.1 | `UserBookRatedIntegrationEventHandler` (Catalog): `GetByIdAsync` (Book tracked) → `StageRatingAsync` (upsert + recompute in-memory) → `book.RecalculateRating(avg5, count)`. **Staging, sem `SaveChangesAsync`** (dispatcher commita Book + BookRating + inbox, §8.1) | ✅ |
| 5C.2 | `UserBookRatingRemovedIntegrationEventHandler` (Catalog): `StageRatingRemovalAsync` (delete por `(BookId,UserId)`, no-op se ausente; último rating → recompute (0,0)) → `RecalculateRating`. Staging. | ✅ |
| 5C.3 | `Book` não encontrado → `throw InvalidOperationException` → **nack-com-requeue transitório** (decisão 2, §8.3); warning com `BookId` | ✅ |
| 5C.4 | 2 consumers registrados na DI do Catalog (`using Legi.Contracts.Library`). Social `UserBookRated` **não regride** — queue independente no mesmo fanout, sem mudança no Social | ✅ |
| 5C.5 | XML-doc da regra de idempotência em cada handler (inverso do §8.1.1): "upsert/delete convergente; recompute das rows; inbox é defesa-em-profundidade, não o único guard; staging via change-tracker; nunca ExecuteUpdate/SaveChanges". Watch-out resolvido: `BookRatingRecalculatedDomainEvent` **não tem handler** no Catalog → dispatch pre-save é no-op. | ✅ |

**5D — Testes + gate de runtime** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 5D.1 | Testes de handler (Catalog.Application.Tests, +6 → 90): handler aplica o agregado recomputado no Book tracked + passa args corretos ao repo + throw em book-não-encontrado (rated e removed). A sequência re-rate/remove/remove-last (que depende da recompute do repo) é coberta no teste de integração com DB real (5D.3). | ✅ |
| 5D.2 | **Runtime e2e (docker):** rate 4.0★ → Catalog `AverageRating` 4.00 / count 1; re-rate 5.0★ → 5.00 / count **estável** 1; remove → 0.00 / 0. Social **ainda** mostra FeedItem `BookRated` (atividade de A; não regrediu 4C). Sem exceções de rating / redelivery preso. | ✅ |
| 5D.3 | **Gate de idempotência (§8.1.1-style):** `tests/Legi.Catalog.Integration.Tests/RatingRecomputeIntegrationTests` dirige o `IntegrationEventDispatcher<CatalogDbContext>` real contra o Postgres do compose. (a) mesmo `MessageId` 2× → recompute **1×** + 1 inbox row; `MessageId` distinto (outro user) move de novo. (b) sequência rate→re-rate→remove→remove-last. (c) **convergência Opção B:** dup com `MessageId` distinto (mesmo rating, escapando a inbox) converge ao mesmo valor. `[SkippableFact]` em `CATALOG_TEST_DB`. **3 testes verdes contra Postgres real.** | ✅ |
| 5D.4 | Docs reconciliados: Fase 5 ✅; `ARCHITECTURE.md` §6 fluxo Library→Catalog marcado implementado. | ✅ |

**Fora de escopo:** `ReviewsCount`/eventos de review (fluxo separado); backfill de ratings pré-Fase-5 (gap de cold-start aceito → job de reconciliação é Fase 6); dropar `PreviousRating` de `UserBookRated` (Social precisa); dead-letter/attempt-counting nas queues (Fase 6).

**Entregável:** Ratings no catálogo mantidos automaticamente, convergentes e idempotentes sob redelivery.


### Fase 6 — Hardening

**Objetivo:** Tornar o sistema outbox/inbox-sobre-RabbitMQ.Client (sem MassTransit, decisão 2.2) production-leaning: limitar e desviar mensagens poison sem perder at-least-once + idempotência, observabilidade mínima viável para operador solo, corrigir o race de migrate-on-startup, reconciliação de drift, e fechar formalmente a auditoria de idempotência.

**Estado verificado (o sketch de 4 linhas estava desatualizado — corrigido):**
- ✅ **Retry do producer/outbox JÁ ESTÁ PRONTO** — `OutboxDispatcherWorker` tem backoff (1/5/30/60s), `MaxAttempts`, marcação poison, `NextRetryAt` (ver §8.2). O antigo "6.2 retry policies" é producer-side e já entregue; só falta o lado do consumer.
- ⚠️ **O gap real é o consumer:** `RabbitMqConsumerHost` faz `nack(requeue:true)` em **toda** exceção → poison redeliveria para sempre. Sem DLX, sem cap de tentativas, sem parking. (O texto de §7.4 que dizia "descarta após MaxAttempts" estava errado — corrigido.)
- ⚠️ **OpenTelemetry referenciado em `Legi.Messaging.csproj` mas SEM wiring** — zero métricas, zero health checks.
- ✅ Recuperação de conexão (`AutomaticRecoveryEnabled`/`TopologyRecoveryEnabled`) e isolamento de canal já corretos — não são gap.

**Decisões travadas (2026-06-04, via ddd-architecture-advisor):**
1. **Deploy = single-instance docker-compose.** O race do migrate-on-startup (6D) é latente-não-ativo → corrige, mas urgência menor; 6A/6B é a parte load-bearing.
2. **Retry do consumer = TTL fixo único (backoff flat, ex. 30s)**, não filas de retry escalonadas/exponenciais. Simplificação documentada, não TODO.
3. **Métricas = wire `Meter` + OTel hosting + `/health` + correlação de logs; DEFERIR backend de exporter** (console / Management UI / `/health` é a superfície operacional do dia-um).

**Sub-fases (ordem recomendada: 6A/6B → 6C → 6E → 6D):**

**6A — Topologia de retry/DLX no consumer** ✅ CONCLUÍDA (runtime-verified)

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 6A.1 | `RabbitMqTopology`: helpers `RetryExchangeNameFor`/`RetryQueueNameFor`/`ParkingExchangeNameFor`/`ErrorQueueNameFor` (por serviço) | ✅ |
| 6A.2 | Consumer host declara: work queue com `x-dead-letter-exchange` (→ retry), retry queue (`x-message-ttl` + DLX de volta ao work, keyed por nome de queue p/ não fan-out), error queue no parking (sem consumer). Declares idempotentes | ✅ |
| 6A.3 | `nack(requeue:false)` (dead-letter → retry). Contagem via header `x-death` (`ConsumerRetryPolicy.GetRejectedDeathCount`, sem aritmética custom). Malformed MessageId → parking (publish + ack), não drop | ✅ |
| 6A.4 | `RetryTtlMs`/`MaxConsumerAttempts`/`MaxTransientAttempts` em `MessagingHostingOptions`, **bindáveis da seção `Messaging`** do config (default 30s/5/50) | ✅ |

*Gate PASSOU (docker, 2026-06-04):* poison injetado em `catalog.user-deleted` (TTL 2s/cap 2 via override) → observado **work → retry (t+2s) → error/parking (t+6s)**, estável (sem loop), `x-parked-reason` + `x-death` no header da mensagem parked, e **0 inbox rows** para o MessageId (re-drive reprocessaria). Log: "attempt 1/2; retrying" → "exhausted retry budget after 2 attempt(s) (transient=False); parking".

**6B — Classificação transitório-vs-poison (interlock de segurança do 6A)** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 6B.1 | `TransientMessagingException` em **`Legi.SharedKernel`** (visível a Application=throw e Messaging=catch; SharedKernel já referenciado por ambos) | ✅ |
| 6B.2 | Consumer host ramifica: `TransientMessagingException` → `MaxTransientAttempts` (50); genérico → `MaxConsumerAttempts` (5) → parking rápido. Decisão pura em `ConsumerRetryPolicy.Decide` (unit-testada) | ✅ |
| 6B.3 | Throws de snapshot/book-ausente reescritos p/ `TransientMessagingException`: Social `FeedLookups` (UserProfile+BookSnapshot), Catalog `UserBookRated`/`UserBookRatingRemoved` (book-not-found) | ✅ |
| 6B.4 | Park **não** escreve inbox (host faz publish→parking + ack, dispatcher nunca commita) — confirmado no gate (0 inbox rows). Regra "no-op success ainda escreve inbox" (§8.1.3) intacta | ✅ |

*Gate:* 11 testes unitários `ConsumerRetryPolicy` (budgets transitório-vs-genérico + parse de `x-death`) + os testes de handler atualizados p/ `ThrowsAsync<TransientMessagingException>` (Social 4C, Catalog 5D) + o gate docker do 6A (poison genérico parka no cap baixo; `transient=False` no log confirma o branch). Budget transitório generoso coberto por unit (parka só em `MaxTransientAttempts`).
**6A+6B saíram juntos** — interlock cumprido.

**6C — Observabilidade (mínimo viável para operador solo)** ✅ CONCLUÍDA (runtime-verified)

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 6C.1 | Correlação por `MessageId`: consumer host abre log scope `MessageId:{}/MessageType:{}` (message-template, renderiza com `IncludeScopes`) cobrindo dispatch + handlers. Publisher já loga MessageId. **`CausationId` ADIADO** (ver nota abaixo) | ✅ (MessageId) / ⏳ (CausationId) |
| 6C.2 | `MessagingMetrics` (Meter `Legi.Messaging`): counters `consumed`/`failed`/`parked`/`redelivered` (tagged por `event`), incrementados no consumer host. Outbox backlog via health check (mesma query; não duplicado como métrica) | ✅ |
| 6C.3 | `AddOpenTelemetry().WithMetrics(AddMeter + AddConsoleExporter)` em `AddLegiMessaging`. Console exporter só — backend OTLP/Prometheus adiado (decisão: console + `/health`) | ✅ |
| 6C.4 | `RabbitMqHealthCheck` (snapshot NÃO-bloqueante da conexão — nunca tenta conectar, p/ não pendurar `/health`) + `OutboxBacklogHealthCheck<TContext>` (Degraded acima de `OutboxBacklogThreshold`). `AddHealthChecks` em `AddLegiMessaging`; `MapHealthChecks("/health")` nas 4 APIs | ✅ |
| 6C.5 | §7.4/§8.2 já refletem a superfície real (DLX/parking + métricas/health) | ✅ |

*Gate PASSOU (docker, 2026-06-04):* `/health` Healthy/200 nas APIs; rabbitmq parado → **Unhealthy/503 em ~0.2s** (não-bloqueante); log scope renderiza `=> MessageId:… MessageType:…` na linha do handler; OTel console exporter emite `legi.messaging.consumed` tagged `event:UserRegisteredIntegrationEvent`.

> **`CausationId` NÃO será implementado (decisão 2026-06-04).** Razão definitiva, descoberta ao auditar o grafo de eventos: **nenhum consumer (integration-event handler) publica evento downstream.** Todo `IEventBus.PublishAsync` parte de um `INotificationHandler<…DomainEvent>` (lado produtor, disparado no SaveChanges de um comando HTTP). O grafo é sempre de um salto — `comando HTTP → domain event → integration event → consumido (terminal)` — então nunca há "mensagem B emitida por processar mensagem A". `CausationId` (que existe justamente para encadear efeito→causa entre mensagens) seria **estruturalmente sempre null**: uma coluna morta + 4 migrations + plumbing ambiente para zero dado. Isso é infra especulativa → não construir. A correlação por `MessageId` (6C, log scope) é a superfície real de tracing. `CausationId` vira um add trivial (~15min: estampar o ambiente do dispatcher) **no dia em que** um consumer republicar (saga/fan-out) — não antes.

**6D — Migrate-step, retenção outbox/inbox, reconciliação de drift** ✅ CONCLUÍDA (6D.4 adiado, ver nota)

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 6D.1 | Modo `--migrate` (migra e sai) + flag `RunMigrationsOnStartup` (default **true**) nos 4 Program.cs. Single-instance migra no startup como antes; multi-replica roda o step `--migrate` e seta a flag false → sem race. Verificado: `catalog-api --migrate` migra e sai 0 (não sobe o server) | ✅ |
| 6D.2 | `RetentionCleaner` (core testável) + `RetentionCleanupWorker<TContext>` (hosted, intervalo `RetentionIntervalMinutes`): deleta outbox processado + inbox consumido com `ProcessedAt < now - RetentionDays` (default 7d). **Mantém poison** (`ProcessedAt == null`) automaticamente. Registrado no `AddLegiMessaging`. Gate de integração verde | ✅ |
| 6D.3 | `BookRatingReconciler` (recompute-from-rows idempotente; `ReconcileAllAsync`/`ReconcileBookAsync`, no-op quando já correto) + CLI `catalog-api --reconcile-ratings`. Gate de integração: drift curado → rerun no-op. CLI verificado (sai 0, "N book(s) corrected") | ✅ |
| 6D.4 | Comando de recompute de drift feed/snapshot (órfãos de like/comment; FeedItem stale) | ⏸️ **ADIADO (YAGNI)** |

*Gate:* `--migrate`/`--reconcile-ratings` smoke (docker, saem 0 sem subir server) + integração (retenção mantém poison/recent e deleta old-processed; reconciler cura drift e é no-op no rerun). Suíte default verde e Docker-free.

> **6D.4 adiado conscientemente (YAGNI).** O recompute de drift feed/snapshot endereça (a) órfãos like/comment de um race "teórico, aceito" (§ cleanup indireto do UserDeleted) e (b) staleness de FeedItem que o próprio design declara "negligenciável" (2.6.1). O conselho do advisor era explícito: reconciliação "gatilho manual... só agendar se drift for observado". A auditoria 8.1.4 acabou de provar que todo consumer é idempotente/convergente, então fontes de drift são mínimas e **nenhuma foi observada**. Construir essa ferramenta agora seria especulativo. O reconciler de rating (6D.3) foi feito porque a média é o número mais visível e tinha gap de cold-start real. Fica documentado como follow-up se/quando drift de feed aparecer.

**6E — Auditoria de idempotência + gates faltantes + evolução de schema** ✅ CONCLUÍDA

| Tarefa | Descrição | Status |
|--------|-----------|--------|
| 6E.1 | Matriz de auditoria dos 19 consumers × {só-inbox / convergente / transitório}, com justificativa por linha → **§8.1.4**. Conclusão: cobertura completa, nada depende de algo além de inbox e/ou convergência | ✅ |
| 6E.2 | Gate replay/dedup p/ **feed do Social** (`tests/Legi.Social.Integration.Tests`): mesmo MessageId 2× → **exatamente 1 FeedItem** + 1 inbox row; MessageId distinto → 2º FeedItem (prova que sem chave natural a inbox é o único guard, §8.1.3) | ✅ |
| 6E.3 | Gate replay/convergência p/ **cascata UserDeleted** (em `Legi.Library.Integration.Tests`): seed → UserDeleted 2× mesmo MessageId → dados purgados + 1 inbox row; MessageId distinto → converge (deleta 0, estado intacto) | ✅ |
| 6E.4 | `JsonStringEnumConverter` em `IntegrationEventSerializer.Options` + 3 testes round-trip (Legi.Messaging.Tests). **Sem drain necessário:** nenhum contrato carrega enum hoje (strings na fronteira, §6.5), então não há mensagem int-encoded em voo. Ver §8.1.5 | ✅ |
| 6E.5 | Hazard de rename/move de tipo (AQN) documentado em §8.1.5 + docstring do serializer | ✅ |

*Por que só 2 gates novos (não um por handler):* a dedup vive no **dispatcher** compartilhado, não por-handler → provada uma vez por serviço (Library 4F.2, Catalog 5D.3; Identity/Social usam o mesmo dispatcher genérico). O que NÃO é propriedade do dispatcher é a *convergência* dos handlers de recompute/bulk → daí os 2 gates específicos (feed sem chave natural; cascata bulk fora da inbox).
*Gate:* testes de integração (6E.2/6E.3 replay real) + round-trip unit (6E.4 enum) + revisão de doc (matriz). Sem gate docker.

**Fora de escopo (cortes explícitos):** MassTransit/Wolverine (decisão 2.2); quorum queues; filas de retry exponenciais escalonadas; ordering por-agregado (§8.3 YAGNI); backend de exporter de tracing/dashboards (métricas wired, backend adiado); cron de reconciliação (gatilho manual até drift); leader election (migrator do compose basta); warnings de HttpClient BaseAddress (não-messaging); tuning de prefetch/QoS (`PrefetchCount=10` fica — mas notar que prefetch>1 + entrega não-ordenada de §8.3 = ordering por-agregado segue não-garantido, aceito).

**Entregável:** Sistema resiliente a falhas transitórias, com poison desviado para parking (sem loop infinito), observável (correlação + métricas + `/health`), migrações sem race, e idempotência formalmente auditada.

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
│   │   └── ReadingPostDeletedIntegrationEvent.cs
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

## 13. Atualização do ARCHITECTURE.md ✅ RECONCILIADO (Fase 4F)

`ARCHITECTURE.md §6` foi reconciliado e agora é o panorama dos fluxos; **este doc é a fonte de verdade** dos contratos/idempotência/ordering (§6 aponta para cá, em vez de duplicar — foi a duplicação que causou o drift anterior). Estado atual:

1. ✅ Integration events: **14 arquivos** em `Legi.Contracts` (Identity 2, Catalog 2, Library 5, Social 4, + `PingIntegrationEvent` diagnóstico; `IIntegrationEvent.cs` é o marker, não conta). *(O "17" de uma versão antiga incluía os `UserListCreated/Deleted` e `UsernameChanged` que foram dropados/não construídos.)*
2. ✅ Outbox/inbox descrito (§6 aponta para as decisões 2.5 / 8.1 daqui).
3. ✅ `Legi.Contracts` e `Legi.Messaging` presentes (ver §11 aqui).
4. ⏳ Diagrama de bounded contexts com RabbitMQ — opcional, não bloqueante.
5. ✅ RabbitMQ no docker-compose (+ `social-api` adicionado na Fase 4F).
6. Marcar "Mensageria: RabbitMQ" como ✅ Implementado na stack tecnológica