# Frontend — Fase 01: Foundation (Auth + Camada de Dados)

Ordem de implementação para a Claude Code. Decisões transversais em `FRONTEND-INTEGRATION-decisions.md` (Doc 00); este doc é apenas execução da Fase 01.

**Convenção de linguagem:** código/identificadores em inglês; documentação em português.

**Status:** 📋 A implementar.

---

## 1. Objetivo

Estabelecer a fundação que destrava todas as fases seguintes: roteamento de API em dev, cliente HTTP com refresh automático, TanStack Query, autenticação completa (login/registro/logout), guard de rotas e correção do i18n.

**Não está no escopo:** trocar mocks por dados reais nas telas de feed/perfil/listas/explore. Após esta fase, essas páginas continuam com mock — mas agora **atrás de autenticação**. A única amarração com dado real é o nome de usuário no greeting do feed (passo 12), como prova de ponta a ponta.

**Pré-requisitos:** backend no ar (`docker-compose up`) para teste manual ao final.

---

## 2. Dependências (npm)

Adicionar em `web/legi-web/package.json` (`axios` já existe):

```
@tanstack/react-query           ^5
@tanstack/react-query-devtools  ^5
```

Rodar `npm install` na pasta `web/legi-web`.

---

## 3. Proxy de desenvolvimento — `vite.config.ts`

Em `npm run dev` não há nginx. Adicionar o bloco `server.proxy` espelhando o roteamento por prefixo (Doc 00 §3.5). **Não usar `rewrite`** — os serviços já servem sob `/api/v1/<serviço>/...`.

```ts
// dentro de defineConfig({ ... })
server: {
  proxy: {
    "/api/v1/identity": { target: "http://localhost:5000", changeOrigin: true },
    "/api/v1/catalog":  { target: "http://localhost:5112", changeOrigin: true },
    "/api/v1/library":  { target: "http://localhost:5200", changeOrigin: true },
    "/api/v1/social":   { target: "http://localhost:5300", changeOrigin: true },
  },
},
```

---

## 4. Armazenamento de sessão — `src/services/authStorage.ts` (novo)

Centraliza o acesso ao `localStorage` (Doc 00 §3.4). Tanto o interceptor quanto o `AuthContext` usam **só** este módulo — nenhum `localStorage` solto pelo código.

```ts
const ACCESS = "legi.accessToken";
const REFRESH = "legi.refreshToken";
const USER = "legi.user";

export interface StoredUser {
  userId: string;
  email: string;
  username: string;
}

export const authStorage = {
  getAccessToken: () => localStorage.getItem(ACCESS),
  getRefreshToken: () => localStorage.getItem(REFRESH),
  getUser: (): StoredUser | null => {
    const raw = localStorage.getItem(USER);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  },
  setSession: (s: { accessToken: string; refreshToken: string; user: StoredUser }) => {
    localStorage.setItem(ACCESS, s.accessToken);
    localStorage.setItem(REFRESH, s.refreshToken);
    localStorage.setItem(USER, JSON.stringify(s.user));
  },
  setTokens: (t: { accessToken: string; refreshToken: string }) => {
    localStorage.setItem(ACCESS, t.accessToken);
    localStorage.setItem(REFRESH, t.refreshToken);
  },
  clear: () => {
    localStorage.removeItem(ACCESS);
    localStorage.removeItem(REFRESH);
    localStorage.removeItem(USER);
  },
};
```

---

## 5. Tipos de auth — `src/features/auth/types.ts` (novo)

Espelham os DTOs do Identity. Login e registro devolvem o mesmo shape. O refresh devolve um subconjunto.

> ⚠️ **Verificar** o shape exato de `RefreshTokenResponse` no backend (`src/Legi.Identity.Application/Auth/Commands/RefreshToken/`). O design abaixo assume `{ token, refreshToken, expiresAt }` com rotação do refresh token. Ajustar se divergir.

```ts
export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

export interface RefreshResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
}

// login e register: refresh + dados do usuário
export interface AuthResponse extends RefreshResponse {
  userId: string;
  email: string;
  username: string;
}
```

---

## 6. Cliente HTTP — `src/services/http.ts` (novo)

O ponto mais delicado da fase. Uma instância axios única, `baseURL: "/api/v1"`. Request interceptor injeta o Bearer; response interceptor faz refresh em `401` e repete a requisição **uma vez**.

Três correções que não são óbvias e precisam estar aqui:

1. **Singleton de refresh** (`refreshPromise`): múltiplos `401` concorrentes compartilham **uma** chamada de refresh, em vez de disparar N.
2. **Pular endpoints de auth**: um `401` de `/identity/auth/login` é "credencial inválida", não "token expirado" — não deve acionar refresh (senão mascara o erro de login). Só tentamos refresh quando há refresh token e a chamada não é de auth.
3. **Refresh fora da instância `http`**: a chamada de refresh usa `axios` puro, para não reentrar neste interceptor e causar loop.

O redirect ao expirar a sessão é **soft**: o interceptor chama um callback `onUnauthorized` que o `AuthProvider` registra para zerar o estado — o `RequireAuth` então redireciona naturalmente (sem `window.location`, preservando a SPA).

```ts
import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authStorage } from "./authStorage";

export const http = axios.create({ baseURL: "/api/v1" });

// Registrado pelo AuthProvider; chamado quando o refresh falha de vez.
let onUnauthorized: (() => void) | null = null;
export function setOnUnauthorized(fn: (() => void) | null) {
  onUnauthorized = fn;
}

http.interceptors.request.use((config) => {
  const token = authStorage.getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const refreshToken = authStorage.getRefreshToken();
  if (!refreshToken) throw new Error("No refresh token");

  // axios puro (não `http`) para não reentrar neste interceptor.
  const { data } = await axios.post("/api/v1/identity/auth/refresh", { refreshToken });
  authStorage.setTokens({ accessToken: data.token, refreshToken: data.refreshToken });
  return data.token;
}

http.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    const status = error.response?.status;
    const isAuthCall = original?.url?.includes("/identity/auth/");

    const shouldRefresh =
      status === 401 &&
      original &&
      !original._retried &&
      !isAuthCall &&
      !!authStorage.getRefreshToken();

    if (shouldRefresh) {
      original!._retried = true;
      try {
        refreshPromise ??= refreshAccessToken().finally(() => {
          refreshPromise = null;
        });
        const newToken = await refreshPromise;
        original!.headers.Authorization = `Bearer ${newToken}`;
        return http(original!);
      } catch {
        authStorage.clear();
        onUnauthorized?.();
      }
    }

    return Promise.reject(error);
  },
);
```

---

## 7. Funções de API de auth — `src/features/auth/api.ts` (novo)

```ts
import { http } from "../../services/http";
import type { AuthResponse, LoginRequest, RegisterRequest } from "./types";

export const authApi = {
  login: (body: LoginRequest) =>
    http.post<AuthResponse>("/identity/auth/login", body).then((r) => r.data),
  register: (body: RegisterRequest) =>
    http.post<AuthResponse>("/identity/auth/register", body).then((r) => r.data),
  logout: (refreshToken: string) =>
    http.post("/identity/auth/logout", { refreshToken }),
};
```

---

## 8. QueryClient — `src/app/queryClient.ts` (novo)

Defaults conservadores (Doc 00 §3.1).

```ts
import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});
```

---

## 9. Contexto de auth — `src/features/auth/AuthContext.tsx` (novo)

Fica **acima** do `RouterProvider` (para o `RequireAuth` dentro das rotas conseguir lê-lo) e **dentro** do `QueryClientProvider` (para o logout limpar o cache). Não navega diretamente — depende do `RequireAuth` reagir à mudança de estado.

`register` autentica na hora (o backend devolve tokens), então o fluxo pós-registro é idêntico ao login.

```tsx
import {
  createContext, useContext, useEffect, useMemo, useState, type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "./api";
import { authStorage, type StoredUser } from "../../services/authStorage";
import { setOnUnauthorized } from "../../services/http";
import type { AuthResponse, LoginRequest, RegisterRequest } from "./types";

interface AuthContextValue {
  user: StoredUser | null;
  isAuthenticated: boolean;
  login: (body: LoginRequest) => Promise<void>;
  register: (body: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function persist(res: AuthResponse): StoredUser {
  const user: StoredUser = { userId: res.userId, email: res.email, username: res.username };
  authStorage.setSession({ accessToken: res.token, refreshToken: res.refreshToken, user });
  return user;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(() => authStorage.getUser());
  const queryClient = useQueryClient();

  useEffect(() => {
    // Quando o refresh falha dentro do interceptor, derruba a sessão.
    setOnUnauthorized(() => setUser(null));
    return () => setOnUnauthorized(null);
  }, []);

  const login = async (body: LoginRequest) => {
    setUser(persist(await authApi.login(body)));
  };

  const register = async (body: RegisterRequest) => {
    setUser(persist(await authApi.register(body)));
  };

  const logout = async () => {
    const rt = authStorage.getRefreshToken();
    try {
      if (rt) await authApi.logout(rt);
    } catch {
      /* best-effort: a sessão local é derrubada de qualquer forma */
    }
    authStorage.clear();
    setUser(null);
    queryClient.clear();
  };

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: !!user, login, register, logout }),
    // login/register/logout fecham sobre setters estáveis; memoizar em `user` basta.
    [user], // eslint-disable-line react-hooks/exhaustive-deps
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
```

---

## 10. Guard de rota — `src/features/auth/RequireAuth.tsx` (novo)

```tsx
import { Navigate, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "./AuthContext";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }
  return <>{children}</>;
}
```

---

## 11. Páginas de login e registro

> ⚠️ **Verificar** o estilo de export de `Button`/`Card` em `components/ui/` (default vs named) e ajustar os imports. O JSX abaixo é representativo — a Claude Code aplica os componentes/utilitários de UI já existentes para o visual.

### `src/features/auth/components/LoginPage.tsx` (novo)

```tsx
import { useState } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { isAxiosError } from "axios";
import { useAuth } from "../AuthContext";
import { Button } from "../../../components/ui/Button";
import { Card } from "../../../components/ui/Card";

export default function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/feed";

  const [emailOrUsername, setEmailOrUsername] = useState("");
  const [password, setPassword] = useState("");

  const mutation = useMutation({
    mutationFn: () => login({ emailOrUsername, password }),
    onSuccess: () => navigate(from, { replace: true }),
  });

  const errorMessage = mutation.isError
    ? isAxiosError(mutation.error) && mutation.error.response?.status === 401
      ? t("auth.invalidCredentials")
      : t("auth.genericError")
    : null;

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <Card className="w-full max-w-sm p-6 space-y-4">
        <h1 className="text-xl font-semibold">{t("auth.loginTitle")}</h1>
        <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); mutation.mutate(); }}>
          <input
            className="w-full rounded-md border px-3 py-2"
            placeholder={t("auth.emailOrUsername")}
            value={emailOrUsername}
            onChange={(e) => setEmailOrUsername(e.target.value)}
            autoComplete="username"
          />
          <input
            type="password"
            className="w-full rounded-md border px-3 py-2"
            placeholder={t("auth.password")}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
          {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}
          <Button type="submit" disabled={mutation.isPending} className="w-full">
            {t("auth.signIn")}
          </Button>
        </form>
        <p className="text-sm text-center">
          {t("auth.noAccount")}{" "}
          <Link to="/register" className="text-green-700">{t("auth.signUp")}</Link>
        </p>
      </Card>
    </div>
  );
}
```

### `src/features/auth/components/RegisterPage.tsx` (novo)

Análoga à `LoginPage`, com **três** campos (`email`, `username`, `password`) e chamando `register(...)`. Mesmo tratamento de erro (mas um `409` deve mostrar mensagem de "já existe" — usar `auth.genericError` no v1 ou adicionar uma chave dedicada). Link para `/login`. `onSuccess` navega para `/feed`.

---

## 12. Composição da árvore e rotas

### `src/app/App.tsx` (editar)

```tsx
import { RouterProvider } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { queryClient } from "./queryClient";
import { AuthProvider } from "../features/auth/AuthContext";
import { router } from "./routes";

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

### `src/app/routes.tsx` (editar)

Adicionar `/login` e `/register` (públicas, fora do `Layout`) e envolver a subárvore do `Layout` em `RequireAuth`. Os `children` permanecem como estão.

```tsx
import { createBrowserRouter, Navigate } from "react-router-dom";
import Layout from "./Layout";
import { RequireAuth } from "../features/auth/RequireAuth";
import LoginPage from "../features/auth/components/LoginPage";
import RegisterPage from "../features/auth/components/RegisterPage";
import FeedPage from "../features/social/components/FeedPage";
import ExplorePage from "../features/catalog/components/ExplorePage";
import ListsPage from "../features/library/components/ListsPage";
import WishlistPage from "../features/library/components/WishlistPage";
import ProfilePage from "../features/library/components/ProfilePage";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  { path: "/register", element: <RegisterPage /> },
  {
    path: "/",
    element: (
      <RequireAuth>
        <Layout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <Navigate to="/feed" replace /> },
      { path: "feed", element: <FeedPage /> },
      { path: "explore", element: <ExplorePage /> },
      { path: "lists", element: <ListsPage /> },
      { path: "wishlist", element: <WishlistPage /> },
      { path: "profile", element: <ProfilePage /> },
    ],
  },
]);
```

### Amarração real do greeting (prova de ponta a ponta)

No `FeedPage`, trocar o nome mockado do greeting pelo username real da sessão — a única troca de dado real desta fase:

```tsx
const { user } = useAuth();
// ...
t("feed.greeting", { username: user?.username ?? "" })
```

O `Layout` (sidebar com "Sair") deve chamar `logout()` do `useAuth` no botão de logout, e exibir `@{user?.username}` no rodapé.

---

## 13. i18n

### `src/i18n/locales/en.json` (editar)

- `feed.greeting`: `"Hi, {{username}} 👋"` (troca a variável `name` → `username`).
- Adicionar a seção `auth`:

```json
"auth": {
  "loginTitle": "Sign in",
  "registerTitle": "Create account",
  "emailOrUsername": "Email or username",
  "email": "Email",
  "username": "Username",
  "password": "Password",
  "signIn": "Sign in",
  "signUp": "Sign up",
  "noAccount": "Don't have an account?",
  "haveAccount": "Already have an account?",
  "invalidCredentials": "Invalid email/username or password.",
  "genericError": "Something went wrong. Please try again."
}
```

### `src/i18n/locales/pt-BR.json` (editar)

Traduzir as seções que ainda estão em inglês e ajustar o greeting:

```json
"feed": {
  "title": "Mural",
  "greeting": "Olá, {{username}} 👋",
  "subtitle": "Veja o que seus amigos estão lendo",
  "readingNow": "Lendo agora",
  "progress": "Progresso",
  "pagesOf": "{{current}} de {{total}} páginas",
  "updateProgress": "Atualizar progresso",
  "updatedProgress": "atualizou o progresso em",
  "finished": "terminou",
  "startedReading": "começou a ler",
  "suggestionsForYou": "Sugestões para você",
  "booksRead": "{{count}} livros lidos",
  "trendingThisWeek": "Em alta esta semana",
  "yourGenres": "Seus gêneros"
},
"explore": {
  "title": "Explorar",
  "subtitle": "Descubra seu próximo livro favorito",
  "searchPlaceholder": "Buscar por título, autor ou gênero...",
  "recommendedForYou": "Recomendados para você",
  "filterByGenre": "Filtrar por gênero",
  "booksFound": "{{count}} livros encontrados",
  "sortBy": {
    "bestRated": "Melhor avaliados",
    "mostRecent": "Mais recentes",
    "mostPopular": "Mais populares"
  }
},
"genres": {
  "fantasy": "Fantasia",
  "romance": "Romance",
  "thriller": "Suspense",
  "mystery": "Mistério",
  "scifi": "Ficção Científica",
  "classic": "Clássico",
  "horror": "Terror",
  "selfHelp": "Autoajuda",
  "history": "História",
  "nonFiction": "Não-ficção",
  "magicalRealism": "Realismo Mágico",
  "historicalFiction": "Ficção Histórica"
}
```

Adicionar a mesma seção `auth`, traduzida:

```json
"auth": {
  "loginTitle": "Entrar",
  "registerTitle": "Criar conta",
  "emailOrUsername": "E-mail ou usuário",
  "email": "E-mail",
  "username": "Usuário",
  "password": "Senha",
  "signIn": "Entrar",
  "signUp": "Cadastrar",
  "noAccount": "Não tem uma conta?",
  "haveAccount": "Já tem uma conta?",
  "invalidCredentials": "E-mail/usuário ou senha inválidos.",
  "genericError": "Algo deu errado. Tente novamente."
}
```

> Nota: `feed.yourGenres` fica traduzida mas a **seção** "Seus gêneros" é removida da UI nesta rodada (Doc 00 §3.7). A chave permanece para o v2.

---

## 14. Critérios de aceitação (gate)

Com o backend no ar (`docker-compose up`) e `npm run dev`:

1. `npm install` e `npm run build` (`tsc -b`) passam sem erro de tipo.
2. Acessar `/feed` deslogado → redireciona para `/login`.
3. **Registrar** um usuário novo → cai autenticado em `/feed`, e o greeting mostra `@username` real (não "Ana Silva").
4. **Logout** (botão na sidebar) → volta para `/login`; reacessar `/feed` redireciona de novo.
5. **Login** com o usuário criado → entra no feed.
6. **Credencial inválida** no login → mostra "E-mail/usuário ou senha inválidos" (e **não** um erro genérico de refresh — confirma a correção do passo 6.2).
7. **Refresh automático:** logado, apagar `legi.accessToken` no DevTools (manter `legi.refreshToken`), navegar para uma rota que faça chamada autenticada → a chamada deve refazer via refresh e suceder, sem deslogar. *(Observação: no v1 as telas ainda são mock; este teste fica mais direto na Fase 02, quando houver uma chamada autenticada real. Registrar como verificação pendente se não houver chamada protegida ainda.)*
8. Trocar o idioma para pt-BR → feed/explore aparecem traduzidos.

---

## 15. Decisões desta fase (resumo)

- **`localStorage`** para access + refresh token (Doc 00 §3.4) — backend devolve tokens no corpo.
- **Singleton de refresh** + **skip de endpoints de auth** + **refresh fora da instância `http`** (passo 6) — evitam, respectivamente, refresh em paralelo, mascaramento de erro de login, e loop de interceptor.
- **Redirect soft** via callback `onUnauthorized` (sem `window.location`).
- **Refresh reativo** (no `401`), não proativo por `expiresAt` — proativo fica para depois se necessário (YAGNI).
- **Greeting** é a única amarração de dado real; o resto das telas segue mock até suas fases.
```