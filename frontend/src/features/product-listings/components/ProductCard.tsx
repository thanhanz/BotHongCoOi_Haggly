"use client";

import { useState } from "react";
import Link from "next/link";
import type { ProductListing } from "@/features/product-listings/api";
import { PRODUCT_UNITS, type ProductUnit } from "@/features/products/api";
import { Button } from "@/shared/ui/button";
import { Typography } from "@/shared/ui/typography";

const UNIT_LABELS: Record<ProductUnit, string> = {
  [PRODUCT_UNITS.KG]: "kg",
  [PRODUCT_UNITS.GRAM]: "g",
  [PRODUCT_UNITS.PIECE]: "cái",
  [PRODUCT_UNITS.BUNCH]: "bó",
  [PRODUCT_UNITS.BOX]: "hộp",
  [PRODUCT_UNITS.PACK]: "gói",
  [PRODUCT_UNITS.LITER]: "lít",
  [PRODUCT_UNITS.OTHER]: "đơn vị",
};

const currencyFormatter = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

function decimalPlaces(value: number): number {
  const decimal = value.toString().split(".")[1];
  return decimal?.length ?? 0;
}

function normalizeQuantity(value: number, step: number): number {
  return Number(value.toFixed(Math.max(decimalPlaces(step), 2)));
}

export function ProductCard({ listing }: { listing: ProductListing }) {
  const step = listing.minimumOrderQuantity > 0 ? listing.minimumOrderQuantity : 1;
  const canOrder = listing.availableQuantity >= step;
  const [quantity, setQuantity] = useState(String(canOrder ? step : 0));
  const name = listing.displayName?.trim() || listing.productName;

  const numericQuantity = Number(quantity) || 0;
  const clampQuantity = (value: number) => normalizeQuantity(
    Math.min(listing.availableQuantity, Math.max(step, value)),
    step,
  );
  const decrease = () => setQuantity(String(clampQuantity(numericQuantity - step)));
  const increase = () => setQuantity(String(clampQuantity(numericQuantity + step)));
  const normalizeTypedQuantity = () => {
    if (!canOrder) {
      setQuantity("0");
      return;
    }

    setQuantity(String(clampQuantity(numericQuantity)));
  };

  return (
    <article className="flex h-full flex-col overflow-hidden rounded-card border border-border-subtle bg-surface-raised shadow-card">
      <div className="relative aspect-[5/3] overflow-hidden bg-ready-background">
        {listing.imageUrl ? (
          <div
            role="img"
            aria-label={name}
            className="size-full bg-cover bg-center transition-transform duration-300 hover:scale-105"
            style={{ backgroundImage: `url(${JSON.stringify(listing.imageUrl)})` }}
          />
        ) : (
          <div className="flex size-full items-center justify-center font-data text-4xl font-bold text-brand-primary/45" aria-label={`Chưa có ảnh cho ${name}`}>
            {name.charAt(0).toLocaleUpperCase("vi")}
          </div>
        )}

        {listing.isNegotiable && (
          <span className="absolute left-2 top-2 rounded-pill bg-brand-secondary px-xs py-2xs font-data text-[11px] font-semibold text-white">
            Có thể trả giá
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col p-xs">
        <Link
          href={`/stalls/${encodeURIComponent(listing.stallId)}`}
          className="mb-2xs flex items-center gap-2xs rounded-control text-xs text-foreground-secondary hover:text-brand-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary"
        >
          <span className="rounded-badge bg-ready-background px-xs py-2xs font-data font-semibold text-brand-primary">
            {listing.stallCode}
          </span>
          <span className="truncate">{listing.stallName}</span>
        </Link>

        <Typography as="h3" variant="labelLg" className="line-clamp-2 min-h-12">
          {name}
        </Typography>

        <div className="mt-2xs flex items-baseline gap-2xs">
          <Typography as="span" variant="priceTabular" className="text-brand-primary">
            {currencyFormatter.format(listing.currentUnitPrice)}
          </Typography>
          <span className="text-sm text-foreground-secondary">/ {UNIT_LABELS[listing.sellingUnit]}</span>
        </div>

        <Typography as="p" variant="bodySm" className={`mt-2xs ${canOrder ? "text-foreground-secondary" : "text-state-error"}`}>
          {canOrder ? `Còn ${listing.availableQuantity} ${UNIT_LABELS[listing.sellingUnit]}` : "Tạm hết hàng"}
        </Typography>

        <div className="mt-auto pt-xs">
          <div className="mb-xs flex h-9 items-center justify-between rounded-control border border-border-prominent bg-surface-canvas">
            <button
              type="button"
              onClick={decrease}
              disabled={!canOrder || numericQuantity <= step}
              aria-label={`Giảm số lượng ${name}`}
              className="size-9 text-lg text-brand-primary disabled:opacity-35"
            >
              −
            </button>
            <label className="flex min-w-0 flex-1 items-center justify-center gap-1 font-data text-sm font-semibold">
              <span className="sr-only">Số lượng {name}</span>
              <input
                type="number"
                inputMode="decimal"
                min={step}
                max={listing.availableQuantity}
                step={step}
                value={quantity}
                disabled={!canOrder}
                onChange={(event) => setQuantity(event.target.value)}
                onBlur={normalizeTypedQuantity}
                className="min-w-0 flex-1 bg-transparent text-center font-data text-sm font-semibold tabular-nums outline-none disabled:opacity-35"
              />
              <span className="shrink-0 text-xs text-foreground-secondary">{UNIT_LABELS[listing.sellingUnit]}</span>
            </label>
            <button
              type="button"
              onClick={increase}
              disabled={!canOrder || numericQuantity + step > listing.availableQuantity}
              aria-label={`Tăng số lượng ${name}`}
              className="size-9 text-lg text-brand-primary disabled:opacity-35"
            >
              +
            </button>
          </div>

          <Button disabled size="sm" className="w-full" title="Tính năng giỏ hàng sẽ được bổ sung sau">
            Thêm vào giỏ
          </Button>
        </div>
      </div>
    </article>
  );
}
