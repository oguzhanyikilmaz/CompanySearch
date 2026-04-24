import clsx from "clsx";

type ScoreBadgeProps = {
  value?: number | null;
};

export function ScoreBadge({ value }: ScoreBadgeProps) {
  if (value === null || value === undefined) {
    return (
      <span className="inline-flex rounded-full border border-zinc-200 bg-zinc-50 px-2.5 py-1 text-xs font-medium text-zinc-500">
        Skor yok
      </span>
    );
  }

  return (
    <span
      className={clsx(
        "inline-flex rounded-full px-2.5 py-1 text-xs font-medium",
        value >= 80 && "bg-emerald-500/15 text-emerald-800",
        value >= 55 && value < 80 && "bg-amber-500/15 text-amber-800",
        value < 55 && "bg-rose-500/15 text-rose-800"
      )}
    >
      {value}/100
    </span>
  );
}
