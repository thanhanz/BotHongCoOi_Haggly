"use client";

import type { ProposalIngredient, ProposalListing } from "../api";
import { Button } from "@/shared/ui/button";
import { Checkbox } from "@/shared/ui/checkbox";
import { Select } from "@/shared/ui/select";
import { canSelect, listingName, minimumQuantity, money, quantityFormat, unitLabel } from "./dish-presentation";
import { DishIcon } from "./DishIcon";

export interface IngredientChoice {
  ingredient: ProposalIngredient;
  listing: ProposalListing | null;
  quantity: number;
  selected: boolean;
}

const unavailableLabels = {
  NO_PRODUCT: "Chưa có sản phẩm phù hợp",
  NO_ACTIVE_LISTING: "Chưa có sạp đang bán",
  OUT_OF_STOCK: "Tạm hết hàng",
  AVAILABLE: "Chưa đủ lượng mua tối thiểu",
};

export function IngredientRow({ choice, onChange }: { choice: IngredientChoice; onChange: (patch: Partial<IngredientChoice>) => void }) {
  const { ingredient, listing, quantity, selected } = choice;
  const available = listing !== null && canSelect(listing);
  const options = [...new Map([ingredient.selectedListing, ...ingredient.alternatives].filter((item): item is ProposalListing => item !== null).map(item => [item.inventoryItemId, item])).values()];
  const step = listing ? minimumQuantity(listing) : 1;
  const changeQuantity = (amount: number) => onChange({ quantity: Number(amount.toFixed(6)) });
  return (
    <div className="grid grid-cols-[auto_minmax(0,1fr)] items-start gap-3 p-4 sm:p-5">
      <Checkbox aria-label={`Chọn ${ingredient.name}`} checked={selected} disabled={!available} onChange={event => onChange({ selected: event.target.checked })} />
      <div className="flex min-w-0 flex-wrap items-center gap-4">
        <div aria-hidden="true" className="hidden size-12 shrink-0 items-center justify-center rounded-control border border-brand-primary/10 bg-ready-background text-brand-primary sm:flex"><DishIcon kind="leaf" className="size-6" /></div>
        <div className="min-w-0 flex-1 basis-48">
          <p className="mb-1 font-data text-[11px] font-semibold uppercase tracking-wide text-brand-secondary">{ingredient.name}</p>
          <h3 className="text-sm font-semibold sm:text-base">{listing ? listingName(listing) : ingredient.name}</h3>
          {listing && <p className="mt-1 flex flex-wrap gap-x-3 font-data text-sm"><span>{money.format(listing.currentUnitPrice)}/{unitLabel(listing.sellingUnit)}</span><span className="text-brand-primary">Còn {quantityFormat.format(listing.availableQuantity)} {unitLabel(listing.sellingUnit)}</span></p>}
          {!available && <p className="mt-1 text-sm text-state-warning">{unavailableLabels[ingredient.status]}</p>}
          {options.length > 1 || (!listing && options.length > 0) ? <Select aria-label={`Đổi sản phẩm cho ${ingredient.name}`} value={listing?.inventoryItemId ?? ""} containerClassName="mt-2" className="max-w-full text-xs" onChange={event => {
            const next = options.find(item => item.inventoryItemId === event.target.value);
            if (next) onChange({ listing: next, quantity: minimumQuantity(next), selected: canSelect(next) });
          }}>
            {!listing && <option value="">Chọn sản phẩm thay thế</option>}
            {options.map(item => <option key={item.inventoryItemId} value={item.inventoryItemId} disabled={!canSelect(item)}>{listingName(item)} · {item.stallName} · {money.format(item.currentUnitPrice)}/{unitLabel(item.sellingUnit)}{canSelect(item) ? "" : " · Không đủ hàng"}</option>)}
          </Select> : available && <p className="mt-2 text-xs text-foreground-secondary">Sản phẩm đề xuất từ sạp</p>}
        </div>
        {listing && available && <div className="flex w-full flex-wrap items-center justify-between gap-3 sm:w-auto">
          <div role="group" aria-label={`Số lượng ${ingredient.name}`} className="flex items-center rounded-control border border-border-prominent bg-ready-background/40">
            <Button variant="ghost" size="icon" aria-label={`Giảm ${ingredient.name}`} disabled={quantity <= step} onClick={() => changeQuantity(Math.max(step, quantity - step))}>−</Button>
            <span className="min-w-16 px-1 text-center font-data text-sm font-semibold">{quantityFormat.format(quantity)} {unitLabel(listing.sellingUnit)}</span>
            <Button variant="ghost" size="icon" aria-label={`Tăng ${ingredient.name}`} disabled={Number((quantity + step).toFixed(6)) > listing.availableQuantity} onClick={() => changeQuantity(Math.min(listing.availableQuantity, quantity + step))}>+</Button>
          </div>
          <strong className="min-w-20 text-right font-data text-sm tabular-nums">{money.format(listing.currentUnitPrice * quantity)}</strong>
          <Button variant="ghost" size="icon" disabled={!selected} aria-label={`Bỏ chọn ${ingredient.name}`} onClick={() => onChange({ selected: false })}><svg aria-hidden="true" viewBox="0 0 24 24" fill="none" className="size-4"><path d="M4 7h16M9 7V4h6v3m3 0-1 13H7L6 7m4 4v5m4-5v5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" /></svg></Button>
        </div>}
      </div>
    </div>
  );
}
