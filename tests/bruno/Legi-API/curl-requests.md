# Legi API curl Requests

Set these variables first:

```bash
export IDENTITY_URL=http://localhost:5000
export CATALOG_URL=http://localhost:5112
export LIBRARY_URL=http://localhost:5200
export SOCIAL_URL=http://localhost:5300
export ACCESS_TOKEN="<paste-access-token>"
export REFRESH_TOKEN="<paste-refresh-token>"
export SECOND_ACCESS_TOKEN="<paste-second-access-token>"
export SECOND_REFRESH_TOKEN="<paste-second-refresh-token>"
export USER_ID="00000000-0000-0000-0000-000000000000"
export PUBLIC_USER_ID="$USER_ID"
export FOLLOWING_USER_ID="00000000-0000-0000-0000-000000000000"
export BOOK_ID="00000000-0000-0000-0000-000000000000"
export AUTHOR_SLUG=""
export TAG_SLUG=""
export USER_BOOK_ID="00000000-0000-0000-0000-000000000000"
export REVIEW_ID="00000000-0000-0000-0000-000000000000"
export LIST_ID="00000000-0000-0000-0000-000000000000"
export POST_ID="00000000-0000-0000-0000-000000000000"
export LIST_COMMENT_ID="00000000-0000-0000-0000-000000000000"
export POST_COMMENT_ID="00000000-0000-0000-0000-000000000000"
```

## Health

```bash
curl -i "$IDENTITY_URL/health"
curl -i "$CATALOG_URL/health"
curl -i "$LIBRARY_URL/health"
curl -i "$SOCIAL_URL/health"
```

## Identity API

```bash
curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"bruno.user@example.com","username":"bruno_user","password":"Senha123!"}'

curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"emailOrUsername":"bruno.user@example.com","password":"Senha123!"}'

curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"bruno.second@example.com","username":"bruno_second","password":"Senha123!"}'

curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"emailOrUsername":"bruno.second@example.com","password":"Senha123!"}'

curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/refresh" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"

curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/logout" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"

curl -i -X POST "$IDENTITY_URL/api/v1/identity/auth/logout" \
  -H "Authorization: Bearer $SECOND_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$SECOND_REFRESH_TOKEN\"}"

curl -i "$IDENTITY_URL/api/v1/identity/users/me" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$IDENTITY_URL/api/v1/identity/users/$PUBLIC_USER_ID"

curl -i -X DELETE "$IDENTITY_URL/api/v1/identity/users/me" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$IDENTITY_URL/api/v1/identity/users/me" \
  -H "Authorization: Bearer $SECOND_ACCESS_TOKEN"
```

## Catalog API

```bash
curl -i "$CATALOG_URL/api/v1/catalog/books?searchTerm=clean%20code&pageNumber=1&pageSize=20&sortBy=Relevance&sortDescending=true"

curl -i "$CATALOG_URL/api/v1/catalog/books?authorSlug=$AUTHOR_SLUG&pageNumber=1&pageSize=20&sortBy=Title&sortDescending=false"

curl -i "$CATALOG_URL/api/v1/catalog/books?tagSlug=$TAG_SLUG&minRating=3.5&pageNumber=1&pageSize=20&sortBy=AverageRating&sortDescending=true"

curl -i "$CATALOG_URL/api/v1/catalog/books/$BOOK_ID"

curl -i -X POST "$CATALOG_URL/api/v1/catalog/books" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isbn":"9780132350884","title":"Clean Code","authors":["Robert C. Martin"],"synopsis":"A handbook of pragmatic software craftsmanship.","pageCount":464,"publisher":"Prentice Hall","coverUrl":"https://covers.openlibrary.org/b/isbn/9780132350884-L.jpg","tags":["software","engineering"]}'

curl -i -X PUT "$CATALOG_URL/api/v1/catalog/books/$BOOK_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Clean Code: A Handbook of Agile Software Craftsmanship","synopsis":"Updated via curl.","pageCount":464,"publisher":"Prentice Hall","coverUrl":"https://covers.openlibrary.org/b/isbn/9780132350884-L.jpg","authors":["Robert C. Martin"],"tags":["software","engineering","craftsmanship"]}'

curl -i -X DELETE "$CATALOG_URL/api/v1/catalog/books/$BOOK_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$CATALOG_URL/api/v1/catalog/authors/search?searchTerm=robert&limit=10"
curl -i "$CATALOG_URL/api/v1/catalog/authors/popular?limit=20"
curl -i "$CATALOG_URL/api/v1/catalog/tags/search?searchTerm=software&limit=10"
curl -i "$CATALOG_URL/api/v1/catalog/tags/popular?limit=20"
```

## Library API

```bash
curl -i "$LIBRARY_URL/api/v1/library?status=Reading&wishlist=false&page=1&pageSize=20" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$LIBRARY_URL/api/v1/library?search=clean%20code&page=1&pageSize=20" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# My UserBook for a book (200) or 204 if not in library
curl -i "$LIBRARY_URL/api/v1/library/by-book/$BOOK_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X POST "$LIBRARY_URL/api/v1/library" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"bookId\":\"$BOOK_ID\",\"wishlist\":false}"

curl -i -X PATCH "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status":1,"wishlist":false,"progressValue":42,"progressType":0}'

curl -i -X PUT "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID/rating" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":4.5}'

curl -i -X DELETE "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID/rating" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID/posts?page=1&pageSize=20" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X POST "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID/posts" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"Initial reading note from curl.","progressValue":42,"progressType":0,"readingDate":"2026-06-05"}'

curl -i -X PUT "$LIBRARY_URL/api/v1/library/posts/$POST_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"Updated reading note from curl.","progressValue":58,"progressType":0}'

curl -i -X DELETE "$LIBRARY_URL/api/v1/library/posts/$POST_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# Write a book review (sets rating + creates a ReviewCreated activity). Returns reviewId.
curl -i -X POST "$LIBRARY_URL/api/v1/library/$USER_BOOK_ID/reviews" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"A genuinely thoughtful review from curl.","stars":4.5,"isSpoiler":true}'

curl -i "$LIBRARY_URL/api/v1/library/lists" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$LIBRARY_URL/api/v1/library/lists/search?search=software&page=1&pageSize=20"
curl -i "$LIBRARY_URL/api/v1/library/lists/$LIST_ID"
curl -i "$LIBRARY_URL/api/v1/library/lists/$LIST_ID/books?page=1&pageSize=20"

curl -i -X POST "$LIBRARY_URL/api/v1/library/lists" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Curl Smoke List","description":"Created by curl.","isPublic":true}'

curl -i -X PATCH "$LIBRARY_URL/api/v1/library/lists/$LIST_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Curl Smoke List Updated","description":"Updated by curl.","isPublic":true}'

curl -i -X POST "$LIBRARY_URL/api/v1/library/lists/$LIST_ID/books" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"userBookId\":\"$USER_BOOK_ID\"}"

curl -i -X DELETE "$LIBRARY_URL/api/v1/library/lists/$LIST_ID/books/$USER_BOOK_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$LIBRARY_URL/api/v1/library/lists/$LIST_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

## Social API

```bash
curl -i "$SOCIAL_URL/api/v1/social/users/$PUBLIC_USER_ID"

curl -i "$SOCIAL_URL/api/v1/social/feed?page=1&pageSize=20" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$SOCIAL_URL/api/v1/social/users/$PUBLIC_USER_ID/activity?page=1&pageSize=20"

# Reviews written for a book (ReviewCreated activities) — book details page
curl -i "$SOCIAL_URL/api/v1/social/books/$BOOK_ID/reviews?page=1&pageSize=20" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X POST "$SOCIAL_URL/api/v1/social/follows" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"followingId\":\"$FOLLOWING_USER_ID\"}"

curl -i -X DELETE "$SOCIAL_URL/api/v1/social/follows/$FOLLOWING_USER_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$SOCIAL_URL/api/v1/social/users/$FOLLOWING_USER_ID/followers?page=1&pageSize=20"
curl -i "$SOCIAL_URL/api/v1/social/users/$PUBLIC_USER_ID/following?page=1&pageSize=20"

curl -i -X POST "$SOCIAL_URL/api/v1/social/lists/$LIST_ID/likes" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$SOCIAL_URL/api/v1/social/lists/$LIST_ID/likes" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$SOCIAL_URL/api/v1/social/lists/$LIST_ID/comments?page=1&pageSize=20"

curl -i -X POST "$SOCIAL_URL/api/v1/social/lists/$LIST_ID/comments" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"List comment created from curl."}'

curl -i -X POST "$SOCIAL_URL/api/v1/social/posts/$POST_ID/likes" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$SOCIAL_URL/api/v1/social/posts/$POST_ID/likes" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$SOCIAL_URL/api/v1/social/posts/$POST_ID/comments?page=1&pageSize=20"

curl -i -X POST "$SOCIAL_URL/api/v1/social/posts/$POST_ID/comments" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"Post comment created from curl."}'

# Review interactions (InteractableType.Review)
curl -i -X POST "$SOCIAL_URL/api/v1/social/reviews/$REVIEW_ID/likes" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$SOCIAL_URL/api/v1/social/reviews/$REVIEW_ID/likes" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i "$SOCIAL_URL/api/v1/social/reviews/$REVIEW_ID/comments?page=1&pageSize=20"

curl -i -X POST "$SOCIAL_URL/api/v1/social/reviews/$REVIEW_ID/comments" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"content":"Review comment created from curl."}'

curl -i -X DELETE "$SOCIAL_URL/api/v1/social/comments/$LIST_COMMENT_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

curl -i -X DELETE "$SOCIAL_URL/api/v1/social/comments/$POST_COMMENT_ID" \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```
