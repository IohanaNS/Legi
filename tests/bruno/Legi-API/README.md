# Legi API Bruno Collection

Native Bruno collection for the four local Legi APIs:

- Identity: `http://localhost:5000`
- Catalog: `http://localhost:5112`
- Library: `http://localhost:5200`
- Social: `http://localhost:5300`

## Import

Open Bruno and select this folder as a collection:

```text
tests/bruno/Legi-API
```

Select the `Local` environment before sending requests.

## Suggested Smoke Flow

1. Run `00 Health`.
2. Run `01 Identity/Auth/01 Register` or `01 Identity/Auth/02 Login`.
3. Run `01 Identity/Auth/05 Register Second User` or `06 Login Second User` if you want to test follows.
4. Run Catalog book create/search/update requests.
5. Wait briefly for messaging projections, then run Library requests that depend on `bookId`.
6. Run Social requests that depend on `userId`, `followingUserId`, `listId`, and `postId`.
7. Run `99 Cleanup` only when you want to remove created test data.

Several create/login requests have post-response scripts that populate environment variables:

- `accessToken`, `refreshToken`, `userId`, `publicUserId`
- `secondAccessToken`, `secondRefreshToken`, `followingUserId`
- `bookId`, `authorSlug`, `tagSlug`
- `userBookId`, `listId`, `postId`, `listCommentId`, `postCommentId`

## Notes

- The default test users use static emails. If registration returns `409 Conflict`, run the login requests or change `testEmail` and `secondTestEmail` in `environments/Local.bru`.
- JSON enum request bodies use numeric values because the APIs do not register a JSON string enum converter. For Library requests: `ReadingStatus.Reading = 1`, `ProgressType.Page = 0`, `ProgressType.Percentage = 1`.
- The equivalent curl commands are in `curl-requests.md`.
