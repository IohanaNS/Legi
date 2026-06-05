import { useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { libraryApi } from "../../library/api";
import { libraryKeys } from "../../library/queryKeys";

interface AddToLibraryArgs {
  bookId: string;
  wishlist: boolean;
}

export function useAddToLibrary() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ bookId, wishlist }: AddToLibraryArgs) =>
      libraryApi.addBookToLibrary(bookId, wishlist),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: libraryKeys.all });
    },
  });
}

export const isAlreadyInLibrary = (error: unknown) =>
  isAxiosError(error) && error.response?.status === 409;

export const isMissingLibrarySnapshot = (error: unknown) =>
  isAxiosError(error) && error.response?.status === 404;
