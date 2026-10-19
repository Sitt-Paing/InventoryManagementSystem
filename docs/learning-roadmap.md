# Inventory project: independent developer learning plan

Created: 9 October 2026. Owner: project developer. Stack: C#/.NET, Angular, SQL Server.

## Goal and realistic scope

မင်းရဲ့ junior experience ၁၀ လ + အလုပ်ချိန်အပြင် တစ်ပတ် ၁၅ နာရီကိုအခြေခံထားတယ်။
ရည်မှန်းချက်က AI ရေးပေးတဲ့ code ကိုလိုက်ထည့်တတ်ရုံမဟုတ်ဘဲ requirement ကနေ
rules, schema, API, UI, failure handling ကို ကိုယ်တိုင်စဉ်းစားရေးနိုင်ဖို့ဖြစ်တယ်။

12 weeks = approximately 180 focused hours. Three months is a first readiness checkpoint,
not a promise of a mid-level title or job. Readiness is measured by independent work,
debugging, and explaining tradeoffs. Weak areas get extra weeks; do not hide them by
adding more features. Enterprise inventory completeness takes longer than this plan.

12-week core: understand existing code, then ship email history, approval, purchasing
return, sales/issue, reservation, reports, Redis, permissions and deployment in small
vertical slices. A slice is one useful action through database → API → Angular, not
an entire enterprise module. Feature scope can shrink; understanding cannot be skipped.

## Project context to preserve

- Existing areas: products, suppliers, warehouses/locations, UOM, PO, receipts, stock
  transactions, authentication, pagination, barcode/printing/export, queued PO email.
- Previously practiced: LINQ, DTO projection, validation, tracking, stock reversal,
  receipt idempotency, partial receipt limits, DI and email background processing.
- Implemented/AI-assisted does not mean independently mastered. Verify each by rewriting.
- Stock follows the agreed storage unit: Rice 1 pcs = 5 kg, but receive/store/transfer
  10 pcs as 10 pcs. Do not silently switch balances to kg. For products with multiple
  accepted units, define normalization and display rules before implementation.
- Preserve existing flows and formatting outside the feature being changed.
- No automated tests are being requested or created by this plan. Initially use manual
  scenarios and builds. A later testing lesson is planned, but test implementation
  starts only when the user explicitly lifts the earlier “don't make tests now” request.
- Other projects mentioned in the old roadmap (CryptoDashboard/SarYeikMyaing) are optional
  context, not new work required by this inventory plan. SignalR can be learned here.

## Weekly time budget: 15 hours outside work

| Activity | Hours | Output |
|---|---:|---|
| Fundamentals and small exercises | 3 | Explain a concept + rewrite a small example |
| Feature design and implementation | 7 | One working vertical slice |
| Debugging and verification | 2 | Reproduction steps, evidence, manual scenarios |
| Closed-book reconstruction | 2 | Critical logic rewritten without AI/source |
| Interview explanation and weekly review | 1 | Recorded explanation + honest score |

Example: four weekday sessions of 90 minutes + two weekend sessions of 4.5 hours.
If work is busy, carry a week forward instead of rushing or skipping fundamentals.

## How each lesson works

1. State a real feature and give one worked example with concrete input/output.
2. Explain syntax before using it: what it means, why used here, and an alternative.
3. Developer writes a short design: rules, data, API, UI, errors and verification cases.
4. Developer implements a useful slice, not dozens of tiny prompted lines.
5. Tutor reviews the attempt: correctness first, then maintainability and tradeoffs.
6. Developer fixes the code and demonstrates normal, invalid and failure scenarios.
7. Close AI/source and rebuild the important logic in a scratch branch or console app.
8. Explain it in Burmese or English. Record unfamiliar terms in the glossary.

Use short checkpoints only where a decision matters. Avoid endless one-word quizzes.
Do not give the full solution before an attempt unless the user explicitly asks.
When the tutor implements a feature, assign a reconstruction task before calling it learned.

### AI independence rules

- Before asking AI, spend 25–40 focused minutes: reproduce, inspect types/error, try a
  hypothesis, consult documentation. Stop earlier for destructive operations or secrets.
- Ask first for a concept explanation, question, review or hint, not a whole handler.
- Hint ladder: clarify rule → point to relevant type/API → pseudocode → small fragment
  → full solution only when requested. Move up only as necessary.
- Documentation/IntelliSense are allowed during independent coding. Memorizing every
  API name is unnecessary; understand types, control flow and business decisions.
- Never paste credentials. Do not experiment with production stock or supplier emails.
- After AI help, explain every important line and rewrite the critical part after
  48 hours. Repeat after one week. Copying with renamed variables does not count.
- Each week has at least one 60-minute coding session with no AI and no existing
  project implementation open. Record where you were stuck rather than concealing it.

## Baseline: start here before a new module

In one 90-minute session, without AI-generated code:

- Write a paged product query with search, price filter, deterministic sorting and DTO.
- Explain why Count happens after filtering but before Skip/Take.
- Find a product, change its name, save it; explain tracking vs a DTO/read-only query.
- Trace Send Email from Angular to Outbox, RabbitMQ, SMTP and status response.
- Write a C# class with a private-set status and two guarded transition methods.
- Use breakpoints/watch to explain one failure and one null/empty-collection case.

Score with the rubric below. This is a diagnostic, not a pass/fail exam. Concepts you
cannot explain become week-one exercises. Do not mark all existing features as mastered.

## 12-week roadmap

### Week 1 — C# foundations through email and products

Feature: understand and reconstruct the existing email state machine; no broker rewrite.

Lessons: variables, types, expressions, if/else, loops, methods, arguments/return values,
scope/shadowing, naming, string interpolation, Trim/IsNullOrWhiteSpace, decimal vs int,
Guid, DateTime/UTC, nullable value/reference types, null, empty lists, ?. and ??.
Then class/struct/record, value/reference semantics, access modifiers, get/set/private
set, constructors, enum, static, const/readonly, partial and using/namespace.

Practice: build a small EmailOutbox-like class from memory. Decide whether a method
returns a value or changes state; reject sending an already-sent email. Explain why
nullable dates are useful and why records suit a request but do not automatically
make nested objects deeply immutable.

Done: explain each property/method without reading; implement 3 transitions correctly.

### Week 2 — OOP, DI, async and email history

Feature: PO email history endpoint + Angular history list with status and attempts.
Reuse tenant filtering; do not expose SMTP passwords or raw sensitive errors.

Lessons: encapsulation, abstraction, inheritance, polymorphism, interface vs abstract
class, composition; SRP and dependency inversion with concrete examples. DI constructor
injection and Transient/Scoped/Singleton; worker scope and DbContext lifetime.
Task, async/await, cancellation, exceptions, using/IDisposable, I/O vs CPU, Task vs
thread, why .Result/async void are problematic. Generics and List<T>.

Practice: explain why IEmailService separates transport from business rules; sketch a
fake transport in a learning exercise without adding a test suite. Add a typed DTO and
history query yourself. Implement loading/empty/error states, then manual status refresh.

Done: add a history slice without copying another handler wholesale; explain disposal
and why API-hosted worker stops when the API stops.

### Week 3 — HTTP, ASP.NET, EF and purchase approval

Feature: minimal PO Draft → Submitted → Approved/Rejected workflow. Define whether
only Approved POs can be received; do not silently invalidate historical receipts.

Lessons: request pipeline, middleware order, routing, model binding/[FromQuery], DTOs,
FluentValidation pipeline, business vs input validation, consistent exception handling.
HTTP methods/status codes including 200/201/202/204/400/401/403/404/409/422/500.
EF relationships/FKs, migrations Up/Down/snapshot, tracked entities and SaveChanges.

Practice: write allowed transition table, actor rules and failure responses before code.
First implement one submit action end-to-end; add approval after it works.

Done: invalid transition rejected in backend, useful UI errors, old data migration
reviewed, request traced with breakpoints. Explain optimistic concurrency as next concern.

### Week 4 — Transactions, concurrency and receipt correctness

Feature: protect approval/receiving from concurrent requests using a justified concurrency
strategy; reconcile receipt reversal behavior and outstanding PO quantities.

Lessons: IQueryable vs IEnumerable, deferred execution, Select/Include, Any/First/Single,
AsNoTracking, cancellation; grouping receipt lines by PO item ID; ACID, commit/rollback,
isolation levels, optimistic concurrency/rowversion, unique constraints and deadlocks.
Explain database constraints vs UI button disabling. Never claim all changes wait for
Commit: SaveChanges writes within a transaction; Commit makes that transaction durable.

Practice: two simultaneous requests competing for remaining PO quantity. Predict the
result, reproduce manually and fix the invariant. Preserve original transactions and
record reversal references; review legacy links rather than guessing repairs.

Done: no over-receipt/negative balance/duplicate accepted request in selected scenarios.
Explain why ProductId is insufficient when the same product occupies separate PO lines.

### Week 5 — Purchase returns and document invariants

Feature: Draft → Posted purchase return linked to original receipt lines; quantity
cannot exceed the remaining returnable quantity. Posted corrections use reversal.

Lessons: domain vs application vs infrastructure responsibilities, pure calculations
vs DB lookups, composition/Strategy only where useful, aggregate boundaries, immutability
of posted facts. Decimal rounding policy and unit consistency.

Practice: design header/items and linkage, list invariants, calculate returnable quantity,
implement one return posting action + UI. Explain stock impact with 10 received and
3 returned; do not implement a full finance/refund system this week.

Done: repeated products, prior returns, consumed stock and duplicate posting considered.

### Week 6 — Angular/TypeScript through customer + sales order

Feature: Customer master and a minimal draft Sales Order with lines; backend + UI.

Lessons: TS types/interfaces/unions, nullability, arrays/functions, object mutation;
components, services/DI, inputs/outputs, routing/params/guards, lifecycle, OnPush,
reactive forms/FormArray, validation, reusable components and lazy loading.
Signals/computed for local derived state; avoid effect for ordinary derivations.

Practice: create quantity/price lines and a derived total, validate both frontend and
backend, explain that UI validation and guards are not security boundaries.

Done: developer can build a typed list/form/detail slice from a written API contract.

### Week 7 — Reservation and stock issue; RxJS

Feature: reserve stock for a sales order, release on cancel, post an issue. Start with
one warehouse and partial fulfillment only if time permits.

Lessons: physical vs reserved vs available quantities, race conditions, state transitions,
atomic updates. Observable/Subject/BehaviorSubject, subscription cleanup, map/tap/filter,
switchMap/mergeMap/concatMap/exhaustMap, catchError/finalize, debounceTime,
distinctUntilChanged, combineLatest/forkJoin, takeUntil and cancellation.

Practice: search with debounce + switchMap; prevent duplicate submission with a justified
operator. Two orders compete for the final 5 pcs. Issue reserved stock exactly once.

Done: explain why on-hand 50 and reserved 10 means available 40, not on-hand 40;
reservation alone creates no physical stock movement. Manual concurrency cases pass.

### Week 8 — SQL and useful inventory reports

Feature: stock balance + PO outstanding quantity report; stock ledger as-of-date if
source history is sufficient. Document old data gaps instead of inventing balances.

Lessons: SELECT, INNER/LEFT JOIN, GROUP BY/HAVING, aggregates, CASE, subquery/CTE,
window functions introduction, PK/FK/unique constraints, normalization, indexes,
composite index order, execution plans, scan vs seek, N+1 and pagination.
View vs table: a normal view stores a query definition, not a materialized snapshot.
Use a view only when it improves reusable reporting; it is not required for every report.

Practice: write SQL by hand first, then LINQ equivalent. Count filtered records, project
only needed fields, stable ordering, export contract. Measure before adding an index.

Done: reconcile a report against known transactions; explain joins and generated SQL.

### Week 9 — Redis with a measured caching use case

Feature: cache a slow dashboard summary/report with TTL and company-specific keys.
Use live database checks for transactional available stock/permissions; stale cache
must not approve an issue or expose another company’s data.

Lessons: cache-aside, memory vs distributed cache, serialization, TTL, invalidation,
cache stampede, hit/miss measurement, stale reads, fallback when Redis is unavailable.
Learn Decorator by wrapping a query service only if the design benefits.

Practice: record baseline, add cache, measure hit behavior; update stock and demonstrate
the documented freshness policy. Test key separation manually for two companies.

Done: Redis outage still allows the uncached read; explain cost and why caching was chosen.

### Week 10 — Permissions, security and audit

Feature: Product.Read, PO.Approve, Stock.Issue and Report.View policies; maker-checker
for approval; audit trail for one important action.

Lessons: authentication vs authorization, Identity/password hashing, JWT structure,
claims, cookies, refresh expiration/revocation; 401 vs 403; server policies, tenant and
warehouse access; CSRF, XSS, SQL injection, CORS, HTTPS and secret storage.

Practice: inspect existing auth flow before rewriting. Try forbidden actions directly
against API, not just through hidden buttons. Demonstrate cross-company ID rejection.

Done: server enforces roles/permissions and tenant ownership; explain the refresh/CSRF
requests seen in DevTools and distinguish them from polling or duplicate business calls.

### Week 11 — RabbitMQ depth, observability and testing concepts

Feature: improve diagnostics/health of existing email flow; optional authenticated
SignalR status notification replacing polling after the basic flow is understood.

Lessons: producer/consumer, exchange/routing/queue, durable vs persistent, confirmations
vs ACK, redelivery, at-least-once, idempotency, poison messages/DLQ concepts, outbox,
bounded backoff, cancellation, thread safety and scope. Explain why SMTP cannot provide
an atomic DB+delivery commit. Learn structured logging, correlation IDs and health checks.

Practice: diagram the crash points; stop broker and restart; duplicate a queue ID;
wrong password; disconnect before send vs uncertain send. Use only own test inbox.
SignalR: hub/groups/authentication/reconnect; company-scoped events, no email secrets.

Testing lesson: unit vs integration vs end-to-end, AAA, mocking boundaries, xUnit,
WebApplicationFactory, test pyramid; choose business invariants worth testing.
Keep manual verification while the no-tests instruction remains active. Once explicitly
authorized, automate important cases with a real relational database for constraints
and transactions; avoid EF InMemory as proof of SQL concurrency correctness.

Done: explain failure behavior honestly, distinguish receipt acceptance from inbox
delivery, and diagnose with IDs/logs rather than asking AI to guess.

### Week 12 — Deployment and independent interview checkpoint

Feature: Dockerfile/Compose for selected services, environment config and health checks;
CI restore/build and, only if authorized, tests. Prepare deployment steps and rollback.
Do not publish externally merely because it is written in this plan.

Lessons: image/container, ports/volumes/networks, localhost inside a container, environment
variables/secrets, migration deployment, backups/restore drill, logs, reverse proxy basics,
Git branches/commits/PR/review and conflict resolution. Clean/Layered/Vertical Slice,
CQRS/Mediator, Repository/Unit of Work and when extra abstraction adds no value.

Practice: a new developer can run the app from instructions; no credentials in Git.
Explain stateless API, worker lifecycle, DB, cache and queue in a 10-minute walkthrough.

Done: 90-minute independent feature + debugging task, then review the readiness rubric.

## Fundamentals coverage checklist

Mark only after explaining AND using the concept in a new example.

- [ ] C# control flow, types, scope, methods, null handling and collections.
- [ ] class/struct/record/partial/static; const/readonly; reference/value semantics
  without the misleading rule “every value type is always on the stack.”
- [ ] OOP four principles, interface/abstract class, composition, SOLID tradeoffs.
- [ ] Generics, IEnumerable/IQueryable, lambdas, extension methods, Func/Action,
  delegates/events (small stock-alert exercise; do not add patterns for decoration).
- [ ] LINQ filters/projection/grouping/order/pagination; execution timing.
- [ ] Exceptions, disposal, Task/await/WhenAll/cancellation, concurrency basics.
  Do not run parallel queries on the same DbContext. ConfigureAwait concept if relevant.
- [ ] ASP.NET pipeline, routing/model binding/DI/validation/error handling/HTTP.
- [ ] EF relationships/tracking/migrations/generated SQL/transactions/concurrency.
- [ ] SQL joins/aggregation/CTE/indexes/plans/ACID and reconciliation.
- [ ] TypeScript, Angular components/forms/routing/lifecycle/DI/state/OnPush.
- [ ] RxJS operators/subscription ownership and Signals vs Observable use cases.
- [ ] Auth/authz/tenancy/CSRF/XSS/CORS/secrets and direct API permission checks.
- [ ] RabbitMQ/outbox/retry/idempotency and realistic delivery guarantees.
- [ ] Redis/freshness/invalidation/fallback and measured performance.
- [ ] Testing concepts, Git, Docker/CI, logs/health/deployment/rollback.
- [ ] Architecture/pattern tradeoffs and basic system-design explanation.

## Enterprise backlog: preserved, not promised within 12 weeks

Implement after core readiness, one end-to-end workflow at a time. Each item requires
a design + data rules + API/UI + verification; a checkbox is not permission to auto-build.

| Feature group | Later slices | Main learning |
|---|---|---|
| Purchasing | PR, amount-based approval, quotations, invoice matching | lifecycle, comparison, relational queries |
| Receiving | QC accept/reject/hold, receiving location, putaway | stock states, locations, invariant design |
| Transfers | request/approve/dispatch/in-transit/receive; 20 sent, 18 received reconciliation | multi-step transactions, partial receipt |
| Sales | picking, packing, barcode checks, delivery note, proof of delivery, backorder | reservation, workflow and UI |
| Sales returns | sellable/damaged/repair disposition | traceability and reversible stock effects |
| Stock counts | blind/cycle/full count, cutoff or freeze, variance approval, adjustment reasons | snapshots, concurrency, auditing |
| Detailed stock | location/bin, lot/batch, serial, expiry, FIFO/FEFO | schema and allocation rules |
| Governance | document posting/reversal everywhere, warehouse access, period lock | permissions and immutable history |
| Valuation | choose weighted average first or FIFO, landed cost | precise arithmetic, costing history |
| Planning | reorder/safety stock, supplier lead time, low-stock alerts | reporting and background work |
| Reports | valuation, expiry, slow/dead stock, sales backlog, in-transit, count variance | SQL and measured performance |
| Integration | accounting export/integration, separate email worker host, bounce tracking | contracts and external failure handling |

Do not treat a stock balance view as a complete ledger. Reports need reliable historical
movements, opening balances and defined sign/date/unit conventions.

## Feature design worksheet — copy before coding

```text
Feature and user:
Smallest useful action:
Inputs / outputs with example numbers:
Business invariants:
Allowed status transitions and actor permissions:
Entity fields, FKs, unique constraints and indexes:
Company/warehouse scope:
Quantity/unit/rounding/date rules:
Endpoint, DTO and status/error contract:
UI loading / empty / validation / failure behavior:
Transaction boundary and concurrency conflict:
Duplicate request behavior:
Failure after each write/external call:
Manual verification cases and expected DB result:
Migration/backward compatibility/rollback concerns:
Alternatives rejected and why:
What I still do not understand:
```

## Readiness rubric and review log

Score each area 0–3: 0 cannot explain; 1 recognizes/copies; 2 implements with docs
and small hints; 3 independently implements, debugs and explains tradeoffs.

Areas: C#, OOP/DI, async, HTTP/ASP.NET, EF/SQL, business invariants/concurrency,
Angular/TS, RxJS/state, auth/security, queue/cache, verification/debugging, Git/deployment.

Target: all core coding/data/security areas at least 2, several at 3, and no critical
flow understood only by copying. This is a personal benchmark, not a universal hiring rule.

Independent checkpoints at weeks 4, 8 and 12:

- Implement a small new requirement with docs but no AI-written solution.
- Diagnose a supplied bug using debugger/network/SQL evidence.
- Explain what can fail and how data stays consistent.
- Walk through a feature in English for interview practice; Burmese explanation first
  is acceptable when learning, but do not replace understanding with memorized English.
- Show reviewed commits and actual working behavior, not only technology names.

| Week/date | Feature shipped | Concepts explained | AI help used | Rewrite result | Remaining gap |
|---|---|---|---|---|---|
| Baseline | | | | | |
| 1 | | | | | |
| 2 | | | | | |
| 3 | | | | | |
| 4 | | | | | |
| 5 | | | | | |
| 6 | | | | | |
| 7 | | | | | |
| 8 | | | | | |
| 9 | | | | | |
| 10 | | | | | |
| 11 | | | | | |
| 12 | | | | | |

## Next session

Start the baseline, then Week 1. First closed-book exercise: define a pending email
class and implement BeginSend/Complete/Fail with valid transitions. Explain the
constructor, property types, private set, enum, nullability and exceptions you used.
Do not add another infrastructure tool to avoid this exercise.

Tutor handoff: use this file as the curriculum, inspect actual current code before
claiming a feature exists, track gaps in the review log, and teach a useful feature
slice with syntax explanations. Ask for the developer’s design/attempt before giving
the full implementation; honor explicit requests to implement and follow them with
reconstruction practice. Keep weekly milestones flexible and evidence-based.
