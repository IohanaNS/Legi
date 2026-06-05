import { useQuery } from "@tanstack/react-query";
import { libraryApi } from "../api";
import { libraryKeys } from "../queryKeys";

export function useLists() {
  return useQuery({ queryKey: libraryKeys.lists(), queryFn: libraryApi.getLists });
}
