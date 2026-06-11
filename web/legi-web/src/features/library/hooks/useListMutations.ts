import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";
import type { SaveUserListBody } from "../types";

export function useListDetail(listId: string, enabled = true) {
  return useQuery({
    queryKey: libraryKeys.listDetail(listId),
    queryFn: () => libraryApi.getListDetail(listId),
    enabled,
  });
}

export function useListBooks(listId: string, enabled = true) {
  return useQuery({
    queryKey: libraryKeys.listBooks(listId),
    queryFn: () => libraryApi.getListBooks(listId),
    enabled,
  });
}

export function useCreateList() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveUserListBody) => libraryApi.createList(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: libraryKeys.lists() }),
  });
}

export function useUpdateList(listId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveUserListBody) => libraryApi.updateList(listId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: libraryKeys.lists() });
      qc.invalidateQueries({ queryKey: libraryKeys.listDetail(listId) });
      qc.invalidateQueries({ queryKey: libraryKeys.listBooks(listId) });
    },
  });
}

export function useDeleteList() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (listId: string) => libraryApi.deleteList(listId),
    onSuccess: () => qc.invalidateQueries({ queryKey: libraryKeys.lists() }),
  });
}
