"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { getDishProposal, type CommonDishCandidate, type DishProposal } from "../api";
import { canSelect, dishImage, minimumQuantity, money } from "./dish-presentation";
import { DishIcon } from "./DishIcon";

export function DishCard({ dish, query }: { dish: CommonDishCandidate; query: string }) {
  const [proposal, setProposal] = useState<DishProposal>();
  const [failed, setFailed] = useState(false);
  const [imageFailed, setImageFailed] = useState(false);
  useEffect(() => {
    const controller = new AbortController();
    void getDishProposal(dish.dishId, { signal: controller.signal }).then(result => {
      if (!controller.signal.aborted) setProposal(result);
    }).catch(() => { if (!controller.signal.aborted) setFailed(true); });
    return () => controller.abort();
  }, [dish.dishId]);
  const available = proposal?.ingredients.filter(item => item.status === "AVAILABLE" && item.selectedListing && canSelect(item.selectedListing)) ?? [];
  const stalls = [...new Map(available.map(item => [item.selectedListing!.stallId, item.selectedListing!.stallName])).values()];
  const total = available.reduce((sum, item) => sum + item.selectedListing!.currentUnitPrice * minimumQuantity(item.selectedListing!), 0);
  const image = dishImage(dish.name);
  return (
    <Link href={`/common-dishes/${encodeURIComponent(dish.dishId)}?q=${encodeURIComponent(query)}`} className="group flex h-full flex-col overflow-hidden rounded-card border border-border-prominent bg-surface-raised shadow-card transition hover:-translate-y-1 hover:border-brand-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary">
      <div className="relative flex h-52 items-center justify-center overflow-hidden bg-ready-background text-brand-primary">
        {image && !imageFailed ? <Image src={image} alt={`Ảnh minh họa ${dish.name}`} fill unoptimized sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw" className="object-cover transition duration-300 group-hover:scale-105" onError={() => setImageFailed(true)} /> : <DishIcon className="size-24 opacity-35" />}
        <div className="absolute inset-0 bg-linear-to-t from-brand-primary via-brand-primary/10 to-transparent" />
        <div className="absolute inset-x-0 bottom-0 p-4 text-foreground-inverse">
          <p className="font-data text-xs uppercase tracking-wide text-white/80">{dish.category || "Gợi ý bữa cơm nhà"}</p>
          <h3 className="mt-1 text-xl font-bold">{dish.name}</h3>
        </div>
      </div>
      <div className="flex flex-1 flex-col gap-4 p-5">
        {proposal ? <>
          <div className="flex flex-wrap items-center justify-between gap-2 rounded-control border border-brand-primary/15 bg-ready-background px-3 py-2 text-xs text-brand-primary">
            <span>{available.length}/{proposal.ingredients.length} nguyên liệu có sẵn</span><span className="rounded bg-surface-raised px-2 py-1">Tại {stalls.length} sạp</span>
          </div>
          <div className="text-xs text-foreground-secondary"><p className="mb-2 flex items-center gap-1"><DishIcon kind="stall" className="size-3.5" /> Sạp gom nguyên liệu dự kiến:</p>
            <div className="flex flex-wrap gap-1">{stalls.slice(0, 3).map(stall => <span key={stall} className="rounded bg-ready-background px-2 py-1">{stall}</span>)}{stalls.length > 3 && <span>+{stalls.length - 3} sạp</span>}{stalls.length === 0 && <span>Chưa có sạp phù hợp.</span>}</div>
          </div>
          <div className="mt-auto flex flex-wrap items-baseline justify-between gap-2 border-t border-border-subtle pt-3"><span className="text-xs text-foreground-secondary">Tạm tính theo lượng tối thiểu:</span><strong className="font-data text-lg text-brand-primary">{available.length ? `~${money.format(total)}` : "Chưa có giá"}</strong></div>
        </> : <p role="status" className="min-h-24 text-sm text-foreground-secondary">{failed ? "Xem chi tiết để tải lại nguyên liệu và giá tại sạp." : "Đang tìm nguyên liệu tại các sạp…"}</p>}
        <span className="mt-auto flex min-h-11 items-center justify-center gap-2 rounded-control border border-brand-primary/20 bg-ready-background px-3 text-center font-data text-sm font-semibold text-brand-primary transition group-hover:bg-brand-primary group-hover:text-white">Xem nguyên liệu{proposal ? ` (${proposal.ingredients.length} món)` : ""} <span aria-hidden="true">→</span></span>
      </div>
    </Link>
  );
}
