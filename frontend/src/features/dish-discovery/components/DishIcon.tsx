export function DishIcon({ className = "size-5", kind = "bowl" }: { className?: string; kind?: "bowl" | "search" | "stall" | "basket" | "leaf" }) {
  const paths = {
    bowl: "M3 11h18c0 5-4 9-9 9s-9-4-9-9Zm3 10h12M8 3c-2 2 2 3 0 5m4-6c-2 2 2 3 0 5m4-4c-2 2 2 3 0 5",
    search: "m21 21-5-5M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z",
    stall: "M3 10v11h18V10M2 10l2-7h16l2 7M2 10c0 3 5 3 5 0 0 3 5 3 5 0 0 3 5 3 5 0 0 3 5 3 5 0M9 21v-7h6v7",
    basket: "M3 9h18l-2 12H5L3 9Zm4 0 5-7 5 7M9 13v4m6-4v4",
    leaf: "M20 3C8 1 2 7 5 15s16 5 15-12ZM4 21 15 9",
  };
  return <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className={className}><path d={paths[kind]} stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}
