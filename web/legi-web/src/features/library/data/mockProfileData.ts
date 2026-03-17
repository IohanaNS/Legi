import type { UserProfile, UserBook, UserList } from "../types";

export const userProfile: UserProfile = {
  id: "1",
  name: "Ana Silva",
  username: "anasilva",
  bio: "Apaixonada por fantasia e literatura clássica. Lendo pelo menos 2 livros por mês desde 2019.",
  genres: ["Fantasia", "Romance", "Clássico", "Mistério"],
  isVerified: true,
  stats: {
    booksRead: 143,
    followers: 312,
    following: 89,
  },
};

export const userBooks: UserBook[] = [
  {
    id: "1",
    bookId: "1",
    title: "O Nome do Vento",
    author: "Patrick Rothfuss",
    rating: 4.8,
    status: "reading",
    progress: 67,
  },
  {
    id: "2",
    bookId: "2",
    title: "Orgulho e Preconceito",
    author: "Jane Austen",
    rating: 4.7,
    status: "finished",
  },
  {
    id: "3",
    bookId: "3",
    title: "Cem Anos de Solidão",
    author: "Gabriel García Márquez",
    rating: 5.0,
    status: "finished",
  },
  {
    id: "4",
    bookId: "4",
    title: "1984",
    author: "George Orwell",
    rating: 4.6,
    status: "finished",
  },
  {
    id: "5",
    bookId: "5",
    title: "Dom Casmurro",
    author: "Machado de Assis",
    rating: 4.4,
    status: "finished",
  },
  {
    id: "6",
    bookId: "6",
    title: "Duna",
    author: "Frank Herbert",
    rating: 4.7,
    status: "finished",
  },
  {
    id: "7",
    bookId: "7",
    title: "Fundação",
    author: "Isaac Asimov",
    rating: 4.7,
    status: "paused",
  },
  {
    id: "8",
    bookId: "8",
    title: "O Hobbit",
    author: "J.R.R. Tolkien",
    rating: 4.5,
    status: "abandoned",
  },
];

export const userLists: UserList[] = [
  {
    id: "1",
    name: "Fantasia Épica",
    description: "Os melhores livros de fantasia que já li ou quero ler.",
    visibility: "public",
    bookCount: 4,
    coverUrls: [],
  },
  {
    id: "2",
    name: "Leituras de 2026",
    description: "Todos os livros que li este ano.",
    visibility: "public",
    bookCount: 3,
    coverUrls: [],
  },
  {
    id: "3",
    name: "Clássicos Imperdíveis",
    description: "Lista privada dos clássicos que todo mundo precisa ler na vida.",
    visibility: "private",
    bookCount: 4,
    coverUrls: [],
  },
  {
    id: "4",
    name: "Para ler no Verão",
    description: "Livros leves e envolventes para aproveitar nas férias.",
    visibility: "public",
    bookCount: 3,
    coverUrls: [],
  },
];