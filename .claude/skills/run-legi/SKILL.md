---
name: run-legi
description: Build, launch, and drive the Legi book-social app — the full Docker stack (4 .NET APIs + 4 Postgres + RabbitMQ + React web). Use to run/start Legi, smoke-test the APIs, log into the web UI, or screenshot the frontend.
---

# Run Legi

Legi is a multi-service .NET 10 / DDD app (Identity, Catalog, Library, Social) behind a
React web frontend. Everything runs as one Docker Compose stack. There are **two driver
surfaces**, both used below:

- **API driver** — the committed Bruno collection at `tests/bruno/Legi-API`, run with the
  `bru` CLI. Chained register→login→create-book→add-to-library→social flow across all 4 services.
- **UI driver** — `.claude/skills/run-legi/ui-driver.mjs` (Playwright). Registers a user via
  the API, drives the real login form, and screenshots the authenticated feed.

All paths below are relative to the repo root (`<unit>/`).

## Prerequisites

Already present in this container; install only if missing:

```bash
docker --version          # Docker + Compose v2
bru --version             # @usebruno/cli  (npm i -g @usebruno/cli)
```

Playwright + its chromium are vendored in `web/legi-web/node_modules` — the UI driver
resolves them from there, so no separate browser install is needed.

## Build & launch (the stack)

First run needs a JWT secret in `.env` (already configured in this container):

```bash
cp -n .env.example .env   # then set Jwt__Secret=$(openssl rand -base64 32)
```

Bring the stack up. Use `--build` the first time or after code changes; plain `up -d` is
idempotent and just (re)starts the already-built containers:

```bash
docker compose up -d --build   # cold start / after changes
docker compose up -d           # idempotent restart (what this session ran)
docker compose ps              # all 10 containers should be Up/healthy
```

APIs apply EF Core migrations on startup. When healthy:

```bash
for p in 5000 5112 5200 5300; do printf "%s->%s " "$p" "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:$p/health)"; done; echo
curl -s -o /dev/null -w "web:3000 -> %{http_code}\n" http://localhost:3000/
```

Expected: `5000->200 5112->200 5200->200 5300->200` and `web:3000 -> 200`.

Ports: web `3000`, identity `5000`, catalog `5112`, library `5200`, social `5300`,
RabbitMQ UI `15672` (`legi`/`legi_dev`). Each API also serves `/swagger`.

## Run — API driver (agent path)

Run the **whole** Bruno collection in a single invocation so post-response scripts can chain
`accessToken`/`bookId`/`userBookId` between requests. The `--delay` lets async RabbitMQ
projections (Catalog→Library `BookSnapshot`) land before dependent requests fire:

```bash
cd tests/bruno/Legi-API && bru run . --env Local -r --delay 800
```

Expect `Requests 63 (63 Passed)`. With the delay, `User Books/03 Add Book To Library`
returns **201** (proves cross-service messaging works). Run a single area instead:

```bash
cd tests/bruno/Legi-API && bru run "00 Health" --env Local
```

Curl equivalents live in `tests/bruno/Legi-API/curl-requests.md`.

## Run — UI driver (agent path)

Registers a fresh user, logs in through the browser form, screenshots the authenticated
feed to `/tmp/legi-shots/`:

```bash
node .claude/skills/run-legi/ui-driver.mjs login           # -> /tmp/legi-shots/feed.png
node .claude/skills/run-legi/ui-driver.mjs shot /login /tmp/legi-shots/login.png
```

`login` prints the username and lands on `http://localhost:3000/feed`. Open the PNG to
confirm the sidebar (Mural/Explorar/Listas/Lista de Desejos/Perfil) and feed rendered.

## Run — human path

`http://localhost:3000` in a browser (root redirects to `/login`; register, then land on
`/feed`). Useless headless — use the UI driver instead. For local non-Docker dev, see
`README.md` Opção 2 (`dotnet run` per API + `yarn dev` in `web/legi-web`).

## Test (source, not the running app)

```bash
dotnet test Legi.sln --settings tests/.runsettings   # unit tests; integration suites skip without *_TEST_DB
```

## Gotchas

- **Bruno vars only chain within ONE `bru run`.** Running folders as separate invocations
  loses `accessToken` → every authed Catalog/Library/Social request 401s. Run `bru run .`
  (the whole collection) for the full flow.
- **Eventual consistency.** A book created in Catalog isn't immediately addable in Library —
  the `BookSnapshot` arrives via RabbitMQ. Without `--delay`, `Add Book To Library` → 404;
  with `--delay 800` it → 201.
- **Collection ordering quirk.** `Lists/07 Add Book To List`, `Reading Posts/02 Create
  Reading Post`, and the Social like/comment requests stay 404 even on a clean run, because
  the collection creates `userBookId`/`postId` in *later* folders than it consumes them.
  Not a stack bug. Every request still reports "Passed" — Bruno counts any HTTP response as
  a pass (there are no status assertions).
- **Re-running register → 409.** The static `bruno.user@example.com` already exists; the
  collection's Login step recovers. The UI driver sidesteps this by using a timestamped user.
- **Playwright lives in `web/legi-web/node_modules`,** not at repo root. `ui-driver.mjs`
  resolves it via `createRequire` against `web/legi-web/package.json` — run it with `node`
  from the repo root, not from the skill dir.
- **Web root redirects to `/login`** (and `/` → `/feed` once authed). Screenshot `/login`
  for the unauthenticated page, or use `login` for the authed feed.

## Troubleshooting

- `bru: command not found` → `npm install -g @usebruno/cli`.
- UI driver `Cannot find package 'playwright'` → you ran it with cwd inside the skill dir or
  node_modules is missing; run from repo root and ensure `cd web/legi-web && yarn` has run.
- Health endpoint not 200 / containers restarting → `docker compose logs <service>`; most
  often a missing `Jwt__Secret` in `.env`.
- `waitForURL('**/feed')` times out in the UI driver → the Identity API is down or login
  failed; check `curl http://localhost:5000/health` and the register status the driver prints.
