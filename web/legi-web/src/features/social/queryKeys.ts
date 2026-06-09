export const socialKeys = {
  all: ["social"] as const,
  profile: (userId: string) => [...socialKeys.all, "profile", userId] as const,
  userSearches: () => [...socialKeys.all, "userSearch"] as const,
  userSearch: (usernamePrefix: string, limit: number) =>
    [...socialKeys.userSearches(), usernamePrefix, limit] as const,
};

export const feedKeys = {
  all: ["feed"] as const,
  list: () => [...feedKeys.all, "list"] as const,
  activity: (userId: string) => [...feedKeys.all, "activity", userId] as const,
  bookReviews: (bookId: string) => [...feedKeys.all, "bookReviews", bookId] as const,
};

export const interactionKeys = {
  comments: (resource: string, id: string) => ["comments", resource, id] as const,
  followers: (userId: string) => ["followers", userId] as const,
  following: (userId: string) => ["following", userId] as const,
};
