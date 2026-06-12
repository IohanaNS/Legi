import { useState } from "react";
import { BookOpen } from "lucide-react";
import { cn } from "../../lib/utils";
import { coverColors } from "../../lib/coverColor";

interface BookCoverProps {
  title: string;
  /** Author display string, shown on the generated placeholder. */
  author?: string;
  coverUrl?: string | null;
  /**
   * Sizing + rounding from the caller. Defaults to filling the parent
   * (`h-full w-full`); pass explicit dimensions for fixed thumbnails.
   */
  className?: string;
  alt?: string;
}

/**
 * Renders a book cover image, falling back to a deterministic generated
 * placeholder (title + author typeset on a color derived from the title) when
 * there is no cover URL or the image fails to load. The placeholder scales with
 * the cover's width via container queries, so it reads well from tiny
 * thumbnails up to the details-page hero — a cover-less book looks intentional,
 * never like a broken image.
 *
 * Fills its parent by default; overlays (badges, progress) stay as absolute
 * siblings layered on top.
 */
export function BookCover({ title, author, coverUrl, className, alt }: BookCoverProps) {
  const [failed, setFailed] = useState(false);
  const showImage = coverUrl && !failed;

  return (
    <div
      className={cn(
        "relative h-full w-full overflow-hidden bg-stone-200 dark:bg-dark-raised",
        className,
      )}
    >
      {showImage ? (
        <img
          src={coverUrl}
          alt={alt ?? title}
          className="h-full w-full object-cover"
          onError={() => setFailed(true)}
        />
      ) : (
        <Placeholder title={title} author={author} />
      )}
    </div>
  );
}

function Placeholder({ title, author }: { title: string; author?: string }) {
  const { bg, text } = coverColors(title);

  return (
    <div
      className="@container absolute inset-0"
      style={{ backgroundColor: bg, color: text }}
      aria-label={title}
    >
      {/* Spine stripe to read as a book rather than a colored box. */}
      <span className="absolute inset-y-0 left-0 w-[4%] bg-black/20" />

      {/* Typeset title/author — only when the cover is wide enough to read. */}
      <div className="absolute inset-0 hidden flex-col justify-center px-[9%] py-[8%] @[68px]:flex">
        <p className="text-[11cqw] font-semibold leading-[1.15] line-clamp-5">{title}</p>
        {author && <p className="mt-[3%] text-[7.5cqw] leading-tight opacity-80 line-clamp-2">{author}</p>}
      </div>

      {/* Tiny thumbnails: just a glyph, text would be unreadable. */}
      <div className="absolute inset-0 flex items-center justify-center opacity-70 @[68px]:hidden">
        <BookOpen className="h-1/3 w-1/3" />
      </div>
    </div>
  );
}
