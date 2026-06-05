// Legi web-frontend UI driver.
// Drives the running React app at http://localhost:3000 with Playwright
// (chromium is already vendored in web/legi-web/node_modules).
//
// Usage (run from repo root):
//   node .claude/skills/run-legi/ui-driver.mjs login              # register+login, screenshot /feed
//   node .claude/skills/run-legi/ui-driver.mjs shot /login out.png        # raw (unauth) screenshot of a path
//   node .claude/skills/run-legi/ui-driver.mjs authed /explore out.png    # register+login, then screenshot an auth-gated route
//
// Screenshots land in /tmp/legi-shots/ by default.
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

const REPO = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
// playwright lives in the web app's node_modules, not at repo root.
const require = createRequire(path.join(REPO, 'web/legi-web/package.json'));
const { chromium } = require('playwright');

const WEB = process.env.LEGI_WEB || 'http://localhost:3000';
const IDENTITY = process.env.LEGI_IDENTITY || 'http://localhost:5000';
const SHOTS = process.env.LEGI_SHOTS || '/tmp/legi-shots';
fs.mkdirSync(SHOTS, { recursive: true });

const cmd = process.argv[2] || 'login';

async function api(p, body) {
  const r = await fetch(IDENTITY + p, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return { status: r.status, json: await r.json().catch(() => ({})) };
}

async function rawShot(routePath, out) {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1280, height: 900 } });
  const resp = await p.goto(WEB + routePath, { waitUntil: 'networkidle', timeout: 30000 });
  await p.waitForTimeout(800);
  await p.screenshot({ path: out, fullPage: true });
  console.log(`shot ${routePath} -> ${out} (status=${resp && resp.status()}, landed=${p.url()})`);
  await b.close();
}

// Register a fresh user and drive the login form. Returns { b, p, u } with the
// browser landed on /feed — caller is responsible for b.close().
async function login() {
  const u = `uidrv_${Date.now()}`;
  const cred = { email: `${u}@example.com`, username: u, password: 'Senha123!' };
  const reg = await api('/api/v1/identity/auth/register', cred);
  if (reg.status !== 201 && reg.status !== 409) {
    throw new Error(`register failed: ${reg.status} ${JSON.stringify(reg.json)}`);
  }
  console.log(`registered ${u} (status=${reg.status})`);

  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1280, height: 900 } });
  await p.goto(WEB + '/login', { waitUntil: 'networkidle', timeout: 30000 });
  await p.getByPlaceholder(/mail|usu/i).fill(u);
  await p.getByPlaceholder(/senha|password/i).fill(cred.password);
  await p.getByRole('button', { name: /entrar|sign in/i }).click();
  await p.waitForURL('**/feed', { timeout: 15000 });
  await p.waitForTimeout(1000);
  return { b, p, u };
}

async function loginFlow() {
  const { b, p, u } = await login();
  const out = path.join(SHOTS, 'feed.png');
  await p.screenshot({ path: out, fullPage: true });
  console.log(`logged in as ${u} -> ${p.url()} -> ${out}`);
  await b.close();
}

// Log in, navigate to an auth-gated route, and screenshot it.
async function authedShot(routePath, out) {
  const { b, p, u } = await login();
  await p.goto(WEB + routePath, { waitUntil: 'networkidle', timeout: 30000 });
  await p.waitForTimeout(1000);
  await p.screenshot({ path: out, fullPage: true });
  console.log(`authed (${u}) ${routePath} -> ${out} (landed=${p.url()})`);
  await b.close();
}

if (cmd === 'login') {
  await loginFlow();
} else if (cmd === 'shot') {
  const routePath = process.argv[3] || '/login';
  const out = process.argv[4] || path.join(SHOTS, 'shot.png');
  await rawShot(routePath, out);
} else if (cmd === 'authed') {
  const routePath = process.argv[3] || '/explore';
  const out = process.argv[4] || path.join(SHOTS, 'authed.png');
  await authedShot(routePath, out);
} else {
  console.error(`unknown command: ${cmd} (use: login | shot <path> <out> | authed <path> <out>)`);
  process.exit(1);
}
