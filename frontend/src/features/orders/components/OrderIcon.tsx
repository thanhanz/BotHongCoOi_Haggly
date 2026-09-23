type IconKind = "stall" | "clock" | "check" | "copy" | "map" | "download" | "leaf" | "walk" | "bag";
const paths: Record<IconKind, string> = {
  stall: "M3 10h18L19 4H5l-2 6Zm2 0v10h14V10M9 20v-6h6v6M2 10c0 4 5 4 5 0 0 4 5 4 5 0 0 4 5 4 5 0 0 4 5 4 5 0",
  clock: "M12 8v5l3 2M9 2h6M5 4 3 6m16-2 2 2M21 13a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z",
  check: "m7 12 3 3 7-7M22 12a10 10 0 1 1-20 0 10 10 0 0 1 20 0Z",
  copy: "M9 3h11v14H9zM5 7H3v14h12v-2",
  map: "m3 5 6-2 6 2 6-2v16l-6 2-6-2-6 2V5Zm6-2v16m6-14v16",
  download: "M12 3v12m-4-4 4 4 4-4M5 16v5h14v-5",
  leaf: "M20 3C9 2 2 7 5 15s17 5 15-12ZM4 21 15 10",
  walk: "M14 3h.01M12 8l-3 6-4 2m7-8 4 5 4 1m-8-6 1 8 4 5m-4-5-5 5",
  bag: "M4 7h16l-2 14H6L4 7Zm4 0V5a4 4 0 0 1 8 0v2",
};
export function OrderIcon({ kind, className = "size-5" }: { kind: IconKind; className?: string }) {
  return <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className={className}><path d={paths[kind]} stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}
