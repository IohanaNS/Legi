I want to implement a new feature: **Register New Book**.

Please inspect the existing codebase first and follow the current project architecture, naming conventions, UI patterns, routing conventions, validation style, API structure, and state management approach. Do not rewrite unrelated code.

## Feature goal

On the Explore page, authenticated users should be able to manually register a new book when the book is not found through the existing external API search.

This manually registered book must be saved using the same backend flow we already use when saving books found through external APIs, including the same catalog/book persistence behavior and any snapshot/read model/event flow currently used by the application.

## Requirements

### 1. Explore page

On the existing **Explore** page, add a button:

> Register new book

The button should be visible according to the existing authentication/UX rules in the project.

If the user clicks this button:

* if the user is authenticated, open a new page or modal for registering the book
* if the user is not authenticated, follow the existing app behavior for protected actions, such as redirecting to login or showing the existing login/auth message

Choose between a page or modal based on the existing project patterns.

## 2. Register book form

The register book UI must contain fields for the basic book information that the backend is already prepared to receive.

Use the same field names, types, and expectations that already exist in the backend DTO/command/request model for saving or creating books.

All fields are required.

The ISBN field must be validated and must contain a valid ISBN.

The form should include:

* book title
* author or authors
* ISBN
* any other required backend fields already expected by the existing book creation/save flow

Do not invent unnecessary fields unless the backend already requires them.

## 3. Validation

Handle validation on both frontend and backend.

Validation rules:

* all fields are required
* string fields cannot be empty or whitespace
* ISBN is required
* ISBN must be valid
* show clear validation messages in the UI
* backend must reject invalid requests even if frontend validation is bypassed

Use the existing validation patterns already present in the project.

## 4. Save behavior

When the user clicks save, the book must be saved using the same flow we already use when a book is selected from the external APIs in the Explore page.

This is important.

The manually registered book should go through the same backend path or shared service/application flow responsible for:

* saving the book in the catalog
* saving or updating book snapshots
* creating any required read models
* publishing or handling any events already used in the external API book save flow
* avoiding duplicated persistence logic

If there is already an application service, command handler, mapper, or workflow used when external API books are saved, reuse or extend it instead of creating a separate inconsistent flow.

## 5. Duplicate handling

Before creating a new book, the backend should follow the existing duplicate-prevention rules used for books.

For example, if the system already checks by ISBN, external ID, title, or another unique identifier, follow the current rule.

At minimum:

* do not create duplicate books with the same ISBN
* if a book with the same ISBN already exists, return or reuse the existing book according to the current backend behavior

## 6. After saving

After the book is saved successfully:

* show a success state/message using existing UI patterns
* redirect the user to the book detail page, or return to the Explore page with the created book available, depending on the existing UX convention
* make sure the newly registered book can be used anywhere a normal book from the external API flow can be used

The manually registered book should behave exactly like a book saved from external API search.

## 7. Backend expectations

Implement the backend changes needed for this feature according to the current architecture.

Likely areas to inspect:

* Explore/book search flow
* external API book save flow
* Catalog context
* Book creation command/handler
* Book DTOs/request models
* snapshot creation/update logic
* ISBN validation
* database mappings/migrations if needed
* tests

Do not create a parallel book-saving mechanism if the project already has a proper flow for this.

The goal is to reuse the existing book persistence pipeline.

## 8. Frontend expectations

Implement the frontend using existing components and patterns.

The UI should support:

* opening the register book form from the Explore page
* filling in all required book fields
* validating required fields
* validating ISBN
* saving the book
* showing loading, success, and error states
* canceling without saving
* redirecting/closing according to existing UX conventions

Reuse existing form components, buttons, validation components, API clients, hooks, and routing patterns where possible.

## 9. Authorization

Only authenticated users can register a new book.

The backend must enforce this. Do not rely only on hiding the button in the frontend.

If an unauthenticated request is made to the register book endpoint, it must be rejected according to the existing authorization behavior.

## 10. Acceptance criteria

The feature is complete when:

* the Explore page has a “Register new book” button
* authenticated users can open a page or modal to register a book manually
* unauthenticated users cannot register books
* the form contains the same required fields the backend expects
* all fields are obligatory
* ISBN validation works
* invalid requests are rejected by the backend
* saving the book uses the same flow as books saved from external API search
* snapshots/read models/events are handled consistently with the existing external API save flow
* duplicate books are not created for the same ISBN
* the manually registered book appears and behaves like any other catalog book
* relevant tests are added or updated
* unrelated code is not refactored unnecessarily

Before coding, briefly summarize the files/areas you plan to change.
