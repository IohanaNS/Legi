import { createBrowserRouter, Navigate } from "react-router-dom";
import Layout from "./Layout";
import { RequireAuth } from "../features/auth/RequireAuth";
import LoginPage from "../features/auth/components/LoginPage";
import RegisterPage from "../features/auth/components/RegisterPage";
import FeedPage from "../features/social/components/FeedPage";
import UserProfilePage from "../features/social/components/UserProfilePage";
import FollowListPage from "../features/social/components/FollowListPage";
import ExplorePage from "../features/catalog/components/ExplorePage";
import BookDetailsPage from "../features/catalog/components/BookDetailsPage";
import RegisterBookPage from "../features/catalog/components/RegisterBookPage";
import ListsPage from "../features/library/components/ListsPage";
import ListEditorPage from "../features/library/components/ListEditorPage";
import ListDetailPage from "../features/library/components/ListDetailPage";
import WishlistPage from "../features/library/components/WishlistPage";
import ProfilePage from "../features/library/components/ProfilePage";
import ReadBooksPage from "../features/library/components/ReadBooksPage";

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
      { path: "books/new", element: <RegisterBookPage /> },
      { path: "books/:bookId", element: <BookDetailsPage /> },
      { path: "lists", element: <ListsPage /> },
      { path: "lists/new", element: <ListEditorPage /> },
      { path: "lists/:listId", element: <ListDetailPage /> },
      { path: "lists/:listId/edit", element: <ListEditorPage /> },
      { path: "wishlist", element: <WishlistPage /> },
      { path: "profile", element: <ProfilePage /> },
      { path: "users/:userId", element: <UserProfilePage /> },
      { path: "users/:userId/read", element: <ReadBooksPage /> },
      { path: "users/:userId/followers", element: <FollowListPage mode="followers" /> },
      { path: "users/:userId/following", element: <FollowListPage mode="following" /> },
    ],
  },
]);
