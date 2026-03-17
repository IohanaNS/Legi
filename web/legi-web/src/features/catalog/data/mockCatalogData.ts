import type { Book, Genre } from "../types";

export const recommendedBooks: Book[] = [
  { id: "1", title: "O Nome do Vento", author: "Patrick Rothfuss", rating: 4.8, genres: ["Fantasia"] },
  { id: "2", title: "Orgulho e Preconceito", author: "Jane Austen", rating: 4.7, genres: ["Romance", "Clássico"] },
  { id: "3", title: "Garota Exemplar", author: "Gillian Flynn", rating: 4.4, genres: ["Thriller", "Mistério"] },
  { id: "4", title: "Fundação", author: "Isaac Asimov", rating: 4.7, genres: ["Ficção Científica"] },
  { id: "5", title: "O Código Da Vinci", author: "Dan Brown", rating: 4.0, genres: ["Thriller", "Mistério"] },
];

export const allBooks: Book[] = [
  ...recommendedBooks,
  { id: "6", title: "Duna", author: "Frank Herbert", rating: 4.7, genres: ["Ficção Científica"] },
  { id: "7", title: "Cem Anos de Solidão", author: "Gabriel García Márquez", rating: 4.9, genres: ["Realismo Mágico", "Clássico"] },
  { id: "8", title: "It — A Coisa", author: "Stephen King", rating: 4.3, genres: ["Terror", "Suspense"] },
  { id: "9", title: "1984", author: "George Orwell", rating: 4.6, genres: ["Ficção Científica", "Clássico"] },
  { id: "10", title: "A Arte da Guerra", author: "Sun Tzu", rating: 4.2, genres: ["Não-Ficção", "História"] },
  { id: "11", title: "Sapiens", author: "Yuval Noah Harari", rating: 4.5, genres: ["Não-Ficção", "História"] },
  { id: "12", title: "Dom Casmurro", author: "Machado de Assis", rating: 4.4, genres: ["Clássico", "Ficção Histórica"] },
];

export const genres: Genre[] = [
  { id: "1", name: "Fantasia", nameKey: "genres.fantasy" },
  { id: "2", name: "Romance", nameKey: "genres.romance" },
  { id: "3", name: "Thriller", nameKey: "genres.thriller" },
  { id: "4", name: "Mistério", nameKey: "genres.mystery" },
  { id: "5", name: "Ficção Científica", nameKey: "genres.scifi" },
  { id: "6", name: "Clássico", nameKey: "genres.classic" },
  { id: "7", name: "Terror", nameKey: "genres.horror" },
  { id: "8", name: "Autoajuda", nameKey: "genres.selfHelp" },
  { id: "9", name: "História", nameKey: "genres.history" },
  { id: "10", name: "Não-Ficção", nameKey: "genres.nonFiction" },
  { id: "11", name: "Realismo Mágico", nameKey: "genres.magicalRealism" },
  { id: "12", name: "Ficção Histórica", nameKey: "genres.historicalFiction" },
];