import { useState } from "react";
import { Star } from "lucide-react";
import { cn } from "../../lib/utils";

interface StarRatingInputProps {
  /** Current value in stars (0.5–5.0, half-star steps), or 0 when unset. */
  value: number;
  onChange: (value: number) => void;
  size?: number;
  disabled?: boolean;
  className?: string;
}

/**
 * Interactive star rating selector with half-star granularity (0.5–5.0) and
 * hover preview. Clicking the left half of a star selects `n - 0.5`, the right
 * half selects `n`. Used by the review form and the "your rating" widget.
 */
export function StarRatingInput({
  value,
  onChange,
  size = 28,
  disabled = false,
  className,
}: StarRatingInputProps) {
  const [hover, setHover] = useState(0);
  const active = hover || value;

  return (
    <div className={cn("flex items-center gap-1", className)} onMouseLeave={() => setHover(0)}>
      {Array.from({ length: 5 }, (_, i) => {
        const full = i + 1;
        const half = i + 0.5;
        // Fill fraction for this star slot: 1 (full), 0.5 (half), or 0 (empty).
        const fill = active >= full ? 1 : active >= half ? 0.5 : 0;

        return (
          <div
            key={full}
            className={cn("relative", disabled && "cursor-not-allowed")}
            style={{ width: size, height: size }}
          >
            {/* Empty background star */}
            <Star size={size} className="absolute inset-0 fill-transparent text-stone-300 dark:text-stone-600" />
            {/* Filled overlay, clipped to the fill fraction */}
            {fill > 0 && (
              <div className="absolute inset-0 overflow-hidden" style={{ width: size * fill }}>
                <Star size={size} className="fill-amber-400 text-amber-400" />
              </div>
            )}
            {/* Two click/hover targets: left half and right half */}
            {!disabled && (
              <>
                <button
                  type="button"
                  aria-label={`${half}`}
                  onMouseEnter={() => setHover(half)}
                  onClick={() => onChange(half)}
                  className="absolute left-0 top-0 z-10 h-full w-1/2 cursor-pointer"
                />
                <button
                  type="button"
                  aria-label={`${full}`}
                  onMouseEnter={() => setHover(full)}
                  onClick={() => onChange(full)}
                  className="absolute right-0 top-0 z-10 h-full w-1/2 cursor-pointer"
                />
              </>
            )}
          </div>
        );
      })}
    </div>
  );
}
