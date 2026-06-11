type LogoVariant = "default" | "cream";

interface LogoProps {
  /** "default" = dark-green pages (light backgrounds); "cream" = cream pages (dark/green backgrounds). */
  variant?: LogoVariant;
  className?: string;
}

/**
 * BukiHub brand mark — a green book spine forming an "L" with fanned pages.
 * Source: "LEGI DOCS/BukiHub Design System/logo/exports".
 */
export function Logo({ variant = "default", className }: LogoProps) {
  const pages = variant === "cream" ? "#eaecdc" : "#1c3a24";
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="52 40 196 360"
      role="img"
      aria-label="BukiHub"
      className={className}
    >
      <path fill="#1c9c54" d="M62 48 H120 V392 L91 366 L62 392 Z" />
      <path fill={pages} d="M120 48 A104 78 0 0 1 120 204 L120 184 A56 58 0 0 0 120 68 Z" />
      <path fill={pages} d="M120 188 A118 82 0 0 1 120 352 L120 330 A64 62 0 0 0 120 208 Z" />
    </svg>
  );
}
