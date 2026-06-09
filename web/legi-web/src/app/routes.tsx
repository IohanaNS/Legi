import { createBrowserRouter, Navigate } from "react-router-dom";
import Layout from "./Layout";
import { RequireAuth } from "../features/auth/RequireAuth";
import LoginPage from "../features/auth/components/LoginPage";
import RegisterPage from "../features/auth/components/RegisterPage";
import FeedPage from "../features/social/components/FeedPage";
import UserProfilePage from "../features/social/components/UserProfilePage";
import ExplorePage from "../features/catalog/components/ExplorePage";
import BookDetailsPage from "../features/catalog/components/BookDetailsPage";
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
      { path: "books/:bookId", element: <BookDetailsPage /> },
      { path: "lists", element: <ListsPage /> },
      { path: "wishlist", element: <WishlistPage /> },
      { path: "profile", element: <ProfilePage /> },
      { path: "users/:userId", element: <UserProfilePage /> },
    ],
  },
]);
