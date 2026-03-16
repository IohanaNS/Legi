import { Star } from "lucide-react";
import { cn } from "../../lib/utils";

interface StarRatingProps {
  rating: number;
  maxRating?: number;
  size?: number;
  showValue?: boolean;
  className?: string;
}

export function StarRating({
  rating,
  maxRating = 5,
  size = 14,
  showValue = true,
  className,
}: StarRatingProps) {
  return (
    <div className={cn("flex items-center gap-1", className)}>
      <div className="flex">
        {Array.from({ length: maxRating }, (_, i) => {
          const filled = i + 1 <= Math.floor(rating);
          const halfFilled = !filled && i < rating;

          return (
            <Star
              key={i}
              size={size}
              className={cn(
                filled
                  ? "fill-amber-400 text-amber-400"
                  : halfFilled
                    ? "fill-amber-400/50 text-amber-400"
                    : "fill-stone-200 text-stone-200"
              )}
            />
          );
        })}
      </div>
      {showValue && (
        <span className="text-xs text-stone-500 ml-0.5">{rating.toFixed(1)}</span>
      )}
    </div>
  );
}