import { useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { catalogApi } from "../api";
import { catalogKeys } from "../queryKeys";
import type { CreateBookRequest } from "../types";

interface ConflictProblemDetails {
  existingBookId?: string;
}

export function useCreateBook() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (body: CreateBookRequest) => catalogApi.createBook(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: catalogKeys.all });
    },
  });
}

export function getExistingBookIdFromConflict(error: unknown) {
  if (!isAxiosError<ConflictProblemDetails>(error) || error.response?.status !== 409) {
    return null;
  }

  return error.response.data?.existingBookId ?? null;
}

export const isCreateBookConflict = (error: unknown) =>
  isAxiosError(error) && error.response?.status === 409;
