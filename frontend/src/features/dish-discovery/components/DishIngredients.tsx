"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { getDishProposal, type DishProposal } from "../api";
import { ApiError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Checkbox } from "@/shared/ui/checkbox";
import { Container } from "@/shared/ui/container";
import { canSelect, minimumQuantity, money, searchHref } from "./dish-presentation";
import { DishIcon } from "./DishIcon";
import { IngredientRow, type IngredientChoice } from "./IngredientRow";

export function DishIngredients({ dishId, query }: { dishId: string; query: string }) {
  const [attempt, setAttempt] = useState(0);
  return <IngredientLoader key={`${dishId}:${attempt}`} dishId={dishId} query={query} onRetry={() => setAttempt(value => value + 1)} />;
}

function IngredientLoader({ dishId, query, onRetry }: { dishId: string; query: string; onRetry: () => void }) {
  const [proposal, setProposal] = useState<DishProposal>();
  const [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    void getDishProposal(dishId, { signal: controller.signal }).then(result => {
      if (!controller.signal.aborted) setProposal(result);
    }).catch(cause => {
      if (!controller.signal.aborted) setError(cause instanceof ApiError && cause.status === 404 ? "Món ăn này không còn có sẵn. Vui lòng chọn một món khác." : "Chưa tải được nguyên liệu. Vui lòng kiểm tra kết nối và thử lại.");
    });
    return () => controller.abort();
  }, [dishId]);
  if (proposal) return <IngredientReview proposal={proposal} query={query} />;
  return <Container className="py-12"><Link href={searchHref(query)} className="text-sm text-brand-primary underline">← Quay lại kết quả</Link><div className="mt-6 rounded-card border border-border-subtle bg-surface-raised p-10 text-center" role={error ? "alert" : "status"}><DishIcon className="mx-auto mb-4 size-10 text-brand-primary" /><p>{error || "Đang gom nguyên liệu từ các sạp…"}</p>{error && <Button className="mt-4" onClick={onRetry}>Thử lại</Button>}</div></Container>;
}

function IngredientReview({ proposal, query }: { proposal: DishProposal; query: string }) {
  const [choices, setChoices] = useState<IngredientChoice[]>(() => proposal.ingredients.map(ingredient => {
    const listing = ingredient.selectedListing;
    return { ingredient, listing, quantity: listing ? minimumQuantity(listing) : 0, selected: ingredient.status === "AVAILABLE" && listing !== null && canSelect(listing) };
  }));
  const selectAllRef = useRef<HTMLInputElement>(null);
  const available = choices.filter(choice => choice.listing && canSelect(choice.listing));
  const selected = available.filter(choice => choice.selected);
  const allSelected = available.length > 0 && selected.length === available.length;
  useEffect(() => {
    if (selectAllRef.current) selectAllRef.current.indeterminate = selected.length > 0 && !allSelected;
  }, [selected.length, allSelected]);

  const groups = new Map<string, { name: string; choices: IngredientChoice[]; subtotal: number; selectedCount: number }>();
  for (const choice of choices) {
    if (!choice.listing || !canSelect(choice.listing)) continue;
    const { stallId, stallName, currentUnitPrice } = choice.listing;
    const group = groups.get(stallId) ?? { name: stallName, choices: [], subtotal: 0, selectedCount: 0 };
    group.choices.push(choice);
    if (choice.selected) { group.subtotal += currentUnitPrice * choice.quantity; group.selectedCount++; }
    groups.set(stallId, group);
  }
  const unavailable = choices.filter(choice => !choice.listing || !canSelect(choice.listing));
  const selectedGroups = [...groups.entries()].filter(([, group]) => group.selectedCount > 0);
  const total = selectedGroups.reduce((sum, [, group]) => sum + group.subtotal, 0);
  // One inventory listing can fulfill more than one canonical ingredient.
  const inventoryTotals = new Map<string, { quantity: number; available: number }>();
  for (const choice of selected) {
    const listing = choice.listing!;
    const previous = inventoryTotals.get(listing.inventoryItemId);
    inventoryTotals.set(listing.inventoryItemId, { quantity: (previous?.quantity ?? 0) + choice.quantity, available: listing.availableQuantity });
  }
  const exceedsStock = [...inventoryTotals.values()].some(item => Number(item.quantity.toFixed(6)) > item.available);
  function updateChoice(id: string, patch: Partial<IngredientChoice>) {
    setChoices(current => current.map(choice => choice.ingredient.canonicalIngredientId === id ? { ...choice, ...patch } : choice));
  }
  const renderRow = (choice: IngredientChoice) => <IngredientRow key={choice.ingredient.canonicalIngredientId} choice={choice} onChange={patch => updateChoice(choice.ingredient.canonicalIngredientId, patch)} />;

  return (
    <div className="py-6 md:py-10">
      <Container className="max-w-none px-4 md:px-6 xl:px-8">
        <div className="overflow-hidden rounded-modal border border-border-prominent bg-surface-canvas shadow-card">
          <header className="flex flex-wrap items-center justify-between gap-5 border-b border-border-prominent bg-surface-raised p-5 md:px-7 md:py-6">
            <div className="min-w-0 flex-1 basis-80"><p className="mb-2 inline-flex items-center gap-2 rounded-pill bg-awaiting-pickup-background px-3 py-1 font-data text-xs text-awaiting-pickup-text"><DishIcon className="size-3.5" /> Gợi ý từ món đã chọn</p><h1 className="text-2xl font-bold tracking-tight md:text-3xl">Nguyên liệu cho {proposal.dishName}</h1><p className="mt-2 text-sm text-foreground-secondary"><strong className="text-brand-primary">{choices.length} nguyên liệu</strong> · {available.length} có sẵn · từ {groups.size} sạp</p></div>
            <div className="max-w-60 rounded-card border border-brand-primary/10 bg-ready-background/50 px-4 py-3 text-xs text-foreground-secondary"><strong className="block text-sm text-brand-primary">Tự chọn lượng cho bữa ăn</strong><p className="mt-1">Bắt đầu từ lượng mua tối thiểu của sạp. Điều chỉnh theo nhu cầu của bạn.</p></div>
            <Link href={searchHref(query)} className="inline-flex min-h-11 items-center gap-3 text-sm text-foreground-secondary underline decoration-border-prominent underline-offset-4 hover:text-brand-primary">Quay lại kết quả <span aria-hidden="true" className="flex size-10 items-center justify-center rounded-full border border-border-prominent bg-ready-background text-xl">×</span></Link>
          </header>
          <div className="grid items-start gap-6 p-4 md:p-6 lg:grid-cols-[minmax(0,2fr)_minmax(20rem,1fr)]">
            <section aria-label="Chọn nguyên liệu" className="min-w-0 space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-3 rounded-card border border-border-prominent bg-surface-raised px-4 py-3"><Checkbox ref={selectAllRef} label={`Chọn tất cả ${available.length} nguyên liệu có sẵn`} checked={allSelected} disabled={!available.length} onChange={event => {
                const checked = event.target.checked;
                setChoices(current => current.map(choice => ({ ...choice, selected: checked && !!choice.listing && canSelect(choice.listing) })));
              }} /><span aria-live="polite" className="rounded-badge bg-ready-background px-2 py-1 font-data text-xs">{selected.length}/{choices.length} món được lấy</span></div>
              {choices.length === 0 && <div className="rounded-card border border-dashed border-border-prominent bg-surface-raised p-8 text-center"><h2 className="font-semibold">Chưa có danh sách nguyên liệu</h2><p className="mt-2 text-sm text-foreground-secondary">Bạn có thể quay lại và chọn món khác.</p></div>}
              {[...groups.entries()].map(([id, group]) => <section key={id} aria-label={group.name} className="overflow-hidden rounded-card border border-border-prominent bg-surface-raised"><div className="flex flex-wrap items-center justify-between gap-2 border-b border-border-prominent bg-ready-background/50 px-4 py-3"><h2 className="flex items-center gap-2 text-sm font-semibold uppercase text-brand-primary"><DishIcon kind="stall" className="size-4 shrink-0" />{group.name}</h2><span className="rounded border border-border-subtle bg-surface-raised px-2 py-1 text-xs">{group.selectedCount} món lấy ở sạp này</span></div><div className="divide-y divide-border-subtle">{group.choices.map(renderRow)}</div></section>)}
              {unavailable.length > 0 && <section className="overflow-hidden rounded-card border border-border-prominent bg-surface-raised"><h2 className="border-b border-border-subtle bg-preparing-background px-4 py-3 text-sm font-semibold text-preparing-text">Cần tìm thêm · {unavailable.length} nguyên liệu</h2><div className="divide-y divide-border-subtle">{unavailable.map(renderRow)}</div></section>}
            </section>
            <aside aria-label="Tóm tắt giỏ nguyên liệu" className="min-w-0 rounded-card border border-border-prominent bg-surface-raised p-5 shadow-card md:p-6 lg:sticky lg:top-6">
              <h2 className="text-xl font-bold">Tóm tắt giỏ nguyên liệu</h2><p className="mt-1 border-b border-border-subtle pb-4 text-sm text-foreground-secondary"><strong className="text-brand-primary">{selected.length}/{choices.length} món</strong> đã chọn · {selectedGroups.length} sạp · Tự nhận tại sạp</p>
              <h3 className="mb-3 mt-5 font-data text-xs font-semibold uppercase tracking-wide text-foreground-secondary">Theo từng sạp lấy hàng</h3>
              <div className="space-y-2">{selectedGroups.map(([id, group]) => <div key={id} className="flex items-baseline justify-between gap-3 rounded-control bg-ready-background/45 px-3 py-2 text-sm"><span className="min-w-0">{group.name} ({group.selectedCount} món)</span><strong className="shrink-0 font-data tabular-nums">{money.format(group.subtotal)}</strong></div>)}{!selected.length && <p className="py-4 text-sm text-foreground-secondary">Chọn nguyên liệu để xem chi phí dự kiến.</p>}</div>
              <div className="mt-5 border-t border-dashed border-border-prominent pt-4"><p className="flex justify-between gap-3 text-sm"><span>Tạm tính ({selected.length} nguyên liệu)</span><span className="font-data">{money.format(total)}</span></p><div className="mt-4 flex flex-wrap items-baseline justify-between gap-2 border-t border-border-prominent pt-4"><strong>Tổng dự kiến</strong><output aria-label="Tổng chi phí dự kiến" className="font-data text-3xl font-bold text-brand-primary">{money.format(total)}</output></div></div>
              {selected.length > 0 && <p role="status" className={`mt-5 rounded-card border p-3 text-sm ${exceedsStock ? "border-state-warning/25 bg-preparing-background text-preparing-text" : "border-brand-primary/20 bg-ready-background text-brand-primary"}`}>{exceedsStock ? "Một sản phẩm đang dùng cho nhiều nguyên liệu vượt lượng còn tại sạp. Hãy giảm số lượng hoặc chọn sản phẩm thay thế." : "Các nguyên liệu đã chọn có hàng theo thông tin vừa tải từ sạp."}</p>}
              {unavailable.length > 0 && <p className="mt-3 text-xs text-foreground-secondary">Còn {unavailable.length} nguyên liệu chưa có hàng phù hợp, chưa tính vào tổng.</p>}
              <div className="mt-8 space-y-3 border-t border-border-subtle pt-5">
                <Button size="lg" disabled aria-describedby="dish-actions-note" className="h-auto min-h-12 w-full whitespace-normal py-3 text-sm"><DishIcon kind="basket" className="size-5 shrink-0" />Thêm {selected.length} món vào giỏ ({money.format(total)})</Button>
                <Button variant="outline" size="lg" disabled aria-describedby="dish-actions-note" className="w-full text-sm">Mua ngay &amp; tạo đơn</Button>
                <p id="dish-actions-note" className="text-center text-xs text-foreground-secondary">Thêm vào giỏ và tạo đơn sẽ sớm khả dụng.</p>
                <p className="text-center text-xs leading-relaxed text-foreground-secondary">Giá và tồn kho chưa được giữ trước. Tổng trên chỉ là chi phí nguyên liệu dự kiến.</p>
                <div className="flex items-start gap-2 rounded-control border border-brand-primary/10 bg-ready-background/60 p-3 text-xs"><DishIcon kind="stall" className="mt-0.5 size-4 shrink-0 text-brand-secondary" /><p><strong>Tự nhận tại {selectedGroups.length} sạp.</strong> Danh sách được gom theo từng sạp để bạn dễ kiểm tra trước khi đi chợ.</p></div>
              </div>
            </aside>
          </div>
        </div>
      </Container>
    </div>
  );
}
