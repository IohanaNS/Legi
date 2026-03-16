import { createBrowserRouter, Navigate } from "react-router-dom";
import Layout from "./Layout";
import FeedPage from "../features/social/components/FeedPage";
import ExplorePage from "../features/catalog/components/ExplorePage";
import ListsPage from "../features/library/components/ListsPage";
import WishlistPage from "../features/library/components/WishlistPage";
import ProfilePage from "../features/library/components/ProfilePage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Layout />,
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