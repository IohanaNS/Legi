export const socialKeys = {
  all: ["social"] as const,
  profile: (userId: string) => [...socialKeys.all, "profile", userId] as const,
};
