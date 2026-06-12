// Deterministic placeholder color for a book cover, derived from a stable seed
// (the title — present in every view, so a given book keeps the same color
// across catalog, library and feed). Used by <BookCover/> when no real cover
// image is available, so a cover-less book looks intentional rather than broken.

export interface CoverColors {
  bg: string;
  text: string;
}

// Muted, book-spine-like colors, all dark enough for the shared warm-white text.
const PALETTE: CoverColors[] = [
  { bg: "#3f6212", text: "#f7fee7" }, // lime
  { bg: "#166534", text: "#ecfdf5" }, // green
  { bg: "#115e59", text: "#ccfbf1" }, // teal
  { bg: "#1e40af", text: "#dbeafe" }, // blue
  { bg: "#3730a3", text: "#e0e7ff" }, // indigo
  { bg: "#6b21a8", text: "#f3e8ff" }, // purple
  { bg: "#9f1239", text: "#ffe4e6" }, // rose
  { bg: "#9a3412", text: "#ffedd5" }, // orange
  { bg: "#854d0e", text: "#fef3c7" }, // amber
  { bg: "#44403c", text: "#f5f5f4" }, // stone
];

// djb2 string hash → stable unsigned int.
function hash(seed: string): number {
  let h = 5381;
  for (let i = 0; i < seed.length; i++) {
    h = (h * 33) ^ seed.charCodeAt(i);
  }
  return h >>> 0;
}

export function coverColors(seed: string): CoverColors {
  const key = seed.trim().toLowerCase();
  return PALETTE[hash(key) % PALETTE.length];
}
