import { cn } from "../../lib/utils";

interface AvatarProps {
  src?: string;
  alt?: string;
  fallback?: string;
  size?: "sm" | "md" | "lg" | "xl";
  className?: string;
}

const sizeClasses = {
  sm: "w-8 h-8 text-xs",
  md: "w-10 h-10 text-sm",
  lg: "w-16 h-16 text-lg",
  xl: "w-24 h-24 text-xl",
};

export function Avatar({ src, alt, fallback, size = "md", className }: AvatarProps) {
  if (src) {
    return (
      <img
        src={src}
        alt={alt || ""}
        className={cn("rounded-full object-cover", sizeClasses[size], className)}
      />
    );
  }

  const initials = fallback
    ? fallback.split(" ").map((n) => n[0]).join("").toUpperCase().slice(0, 2)
    : "?";

  return (
    <div
      className={cn(
        "rounded-full bg-green-100 text-green-800 flex items-center justify-center font-medium",
        sizeClasses[size],
        className
      )}
    >
      {initials}
    </div>
  );
}