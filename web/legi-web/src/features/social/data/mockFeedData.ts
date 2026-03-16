import type { FeedPost, FeedUser, TrendingBook } from "../types";

export const currentUser: FeedUser = {
  id: "1",
  name: "Ana Silva",
  username: "anasilva",
};

export const currentlyReading = {
  bookTitle: "O Nome do Vento",
  bookAuthor: "Patrick Rothfuss",
  progress: 67,
  currentPage: 443,
  totalPages: 662,
};

export const feedPosts: FeedPost[] = [
  {
    id: "1",
    user: {
      id: "2",
      name: "Carlos Mendes",
      username: "carlosm",
    },
    type: "progress_update",
    bookTitle: "Duna",
    bookAuthor: "Frank Herbert",
    progress: 58,
    currentPage: 348,
    totalPages: 600,
    comment: "Que livro viciante! A construção do mundo é simplesmente absurda. Não consigo parar de ler.",
    likes: 14,
    comments: 3,
    createdAt: "3h atrás",
  },
  {
    id: "2",
    user: {
      id: "3",
      name: "Julia Fernandes",
      username: "juliaf",
    },
    type: "finished",
    bookTitle: "Cem Anos de Solidão",
    bookAuthor: "Gabriel García Márquez",
    rating: 5,
    comment: "Uma obra-prima absoluta. García Márquez consegue transformar o impossível em cotidiano com uma maestria incomparável.",
    likes: 47,
    comments: 9,
    createdAt: "14h atrás",
  },
  {
    id: "3",
    user: {
      id: "4",
      name: "Pedro Costa",
      username: "pedroc",
    },
    type: "started_reading",
    bookTitle: "Garota Exemplar",
    bookAuthor: "Gillian Flynn",
    comment: "Começando depois de muitas recomendações. Vamos ver se é tudo isso mesmo!",
    likes: 8,
    comments: 2,
    createdAt: "22h atrás",
  },
];

export const suggestedUsers: FeedUser[] = [
  { id: "5", name: "Marina Santos", username: "marinasantos", booksRead: 56 },
  { id: "6", name: "Rafael Oliveira", username: "rafaoliveira", booksRead: 178 },
];

export const trendingBooks: TrendingBook[] = [
  { id: "1", title: "O Nome do Vento", author: "Patrick Rothfuss", rating: 4.8 },
  { id: "2", title: "Orgulho e Preconceito", author: "Jane Austen", rating: 4.7 },
  { id: "3", title: "Garota Exemplar", author: "Gillian Flynn", rating: 4.4 },
  { id: "4", title: "Duna", author: "Frank Herbert", rating: 4.7 },
];

export const userGenres = ["Fantasia", "Romance", "Clássico", "Mistério"];