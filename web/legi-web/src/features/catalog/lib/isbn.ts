export function normalizeIsbn(value: string) {
  return value.replace(/[-\s]/g, "").trim().toUpperCase();
}

export function isValidIsbn(value: string) {
  const isbn = normalizeIsbn(value);

  if (isbn.length === 10) {
    return isValidIsbn10(isbn);
  }

  if (isbn.length === 13) {
    return isValidIsbn13(isbn);
  }

  return false;
}

function isValidIsbn10(isbn: string) {
  let sum = 0;

  for (let index = 0; index < isbn.length; index += 1) {
    const char = isbn[index];
    const digit = index === 9 && char === "X" ? 10 : Number(char);

    if (!Number.isInteger(digit) || digit < 0 || digit > 10 || (digit === 10 && index !== 9)) {
      return false;
    }

    sum += (10 - index) * digit;
  }

  return sum % 11 === 0;
}

function isValidIsbn13(isbn: string) {
  if (!/^\d{13}$/.test(isbn)) {
    return false;
  }

  const sum = isbn
    .split("")
    .reduce((total, char, index) => total + Number(char) * (index % 2 === 0 ? 1 : 3), 0);

  return sum % 10 === 0;
}
