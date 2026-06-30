FROM node:22-alpine AS build
WORKDIR /app
# The repo tracks yarn.lock (Yarn Classic, version pinned via package.json
# "packageManager"); package-lock.json is gitignored. Corepack honors the pin.
COPY package.json yarn.lock ./
RUN corepack enable && yarn install --frozen-lockfile
COPY . .
ARG VITE_TURNSTILE_SITE_KEY=
ENV VITE_TURNSTILE_SITE_KEY=$VITE_TURNSTILE_SITE_KEY
ARG VITE_GOOGLE_CLIENT_ID=
ENV VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID
RUN yarn build

# Unprivileged nginx: runs as uid 101 (nginx), never root. It listens on 8080 (a
# non-root process cannot bind <1024) — see nginx.conf and the compose port mapping.
FROM nginxinc/nginx-unprivileged:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
