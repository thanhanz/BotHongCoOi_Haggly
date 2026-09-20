"use client";

import { useCallback, useEffect, useState } from "react";
import {
  getProductListings,
  type ProductListing,
  type ProductListingParams,
} from "@/features/product-listings/api";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Typography } from "@/shared/ui/typography";
import { ProductCard } from "./ProductCard";

interface ProductListingSectionProps {
  categoryId?: string;
  stallId?: string;
  title?: string;
  useDemoData?: boolean;
  pageSize?: number;
  showNegotiation?: boolean;
  showStall?: boolean;
}

const PAGE_SIZE = 20;

const DEMO_LISTINGS: ProductListing[] = [
  {
    productId: "00000000-0000-0000-0000-000000000101",
    productStallId: "00000000-0000-0000-0000-000000000201",
    inventoryItemId: "00000000-0000-0000-0000-000000000301",
    productName: "Cà chua beef Đà Lạt",
    displayName: "Cà chua mọng nước",
    imageUrl: null,
    stallId: "00000000-0000-0000-0000-000000000401",
    stallName: "Sạp Rau Sạch Cô Ba",
    stallCode: "Sạp 12B",
    currentUnitPrice: 45000,
    sellingUnit: 0,
    minimumOrderQuantity: 0.5,
    availableQuantity: 18.5,
    isNegotiable: true,
  },
  {
    productId: "00000000-0000-0000-0000-000000000102",
    productStallId: "00000000-0000-0000-0000-000000000202",
    inventoryItemId: "00000000-0000-0000-0000-000000000302",
    productName: "Xà lách xoăn",
    displayName: null,
    imageUrl: null,
    stallId: "00000000-0000-0000-0000-000000000402",
    stallName: "Rau Vườn Nhà Út",
    stallCode: "Sạp 08A",
    currentUnitPrice: 28000,
    sellingUnit: 0,
    minimumOrderQuantity: 0.5,
    availableQuantity: 12,
    isNegotiable: false,
  },
  {
    productId: "00000000-0000-0000-0000-000000000103",
    productStallId: "00000000-0000-0000-0000-000000000203",
    inventoryItemId: "00000000-0000-0000-0000-000000000303",
    productName: "Cam sành miền Tây",
    displayName: "Cam sành ngọt mọng",
    imageUrl: null,
    stallId: "00000000-0000-0000-0000-000000000403",
    stallName: "Trái Cây Chị Năm",
    stallCode: "Sạp 21C",
    currentUnitPrice: 38000,
    sellingUnit: 0,
    minimumOrderQuantity: 1,
    availableQuantity: 25,
    isNegotiable: true,
  },
  {
    productId: "00000000-0000-0000-0000-000000000104",
    productStallId: "00000000-0000-0000-0000-000000000204",
    inventoryItemId: "00000000-0000-0000-0000-000000000304",
    productName: "Hành lá tươi",
    displayName: null,
    imageUrl: null,
    stallId: "00000000-0000-0000-0000-000000000404",
    stallName: "Sạp Cô Tư",
    stallCode: "Sạp 05D",
    currentUnitPrice: 8000,
    sellingUnit: 3,
    minimumOrderQuantity: 1,
    availableQuantity: 40,
    isNegotiable: false,
  },
  {
    productId: "00000000-0000-0000-0000-000000000105",
    productStallId: "00000000-0000-0000-0000-000000000205",
    inventoryItemId: "00000000-0000-0000-0000-000000000305",
    productName: "Chuối già Nam Mỹ",
    displayName: "Chuối chín tự nhiên",
    imageUrl: null,
    stallId: "00000000-0000-0000-0000-000000000405",
    stallName: "Trái Cây Dì Sáu",
    stallCode: "Sạp 17A",
    currentUnitPrice: 32000,
    sellingUnit: 0,
    minimumOrderQuantity: 1,
    availableQuantity: 16,
    isNegotiable: true,
  },
];

function buildParams(categoryId?: string, stallId?: string, pageSize = PAGE_SIZE): ProductListingParams {
  return {
    categoryId,
    stallId,
    sort: "home",
    page: 1,
    pageSize,
  };
}

function ProductCardSkeleton() {
  return (
    <div aria-hidden="true" className="overflow-hidden rounded-card border border-border-subtle bg-surface-raised shadow-card">
      <div className="aspect-[4/3] animate-pulse bg-ready-background" />
      <div className="grid gap-xs p-sm">
        <div className="h-4 w-2/3 animate-pulse rounded bg-surface-sunken" />
        <div className="h-6 w-full animate-pulse rounded bg-surface-sunken" />
        <div className="h-5 w-1/2 animate-pulse rounded bg-surface-sunken" />
        <div className="h-10 animate-pulse rounded-control bg-surface-sunken" />
      </div>
    </div>
  );
}

export function ProductListingSection({
  categoryId,
  stallId,
  title = "Sản phẩm hôm nay",
  useDemoData = true,
  pageSize = PAGE_SIZE,
  showNegotiation = true,
  showStall = true,
}: ProductListingSectionProps) {
  const [listings, setListings] = useState<ProductListing[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(false);
  const showDemoData = useDemoData && !isLoading && (error || listings.length === 0);
  const displayedListings = showDemoData ? DEMO_LISTINGS : listings;

  const loadListings = useCallback(async () => {
    setIsLoading(true);
    setError(false);
    try {
      const result = await getProductListings(buildParams(categoryId, stallId, pageSize));
      setListings(result.items);
    } catch {
      setError(true);
    } finally {
      setIsLoading(false);
    }
  }, [categoryId, pageSize, stallId]);

  useEffect(() => {
    const controller = new AbortController();
    void getProductListings(buildParams(categoryId, stallId, pageSize), { signal: controller.signal }).then((result) => {
      setListings(result.items);
      setIsLoading(false);
    }).catch(() => {
      if (!controller.signal.aborted) {
        setError(true);
        setIsLoading(false);
      }
    });
    return () => controller.abort();
  }, [categoryId, pageSize, stallId]);

  return (
    <section aria-labelledby="product-listings-heading" className="pb-xl md:pb-2xl">
      <Container>
        <div className="mb-md">
          <Typography as="p" variant="labelMd" className="mb-2xs text-brand-secondary">Tươi ngon mỗi ngày</Typography>
          <Typography as="h2" id="product-listings-heading" variant="headlineMd">{title}</Typography>
        </div>

        {isLoading && (
          <div role="status" aria-label="Đang tải sản phẩm" className="grid grid-cols-1 gap-sm sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {Array.from({ length: Math.min(pageSize, 10) }, (_, index) => <ProductCardSkeleton key={index} />)}
          </div>
        )}

        {!isLoading && error && (
          <div role="alert" className="rounded-card border border-border-subtle bg-surface-raised p-md text-center">
            <Typography as="h3" variant="titleMd">Chưa tải được sản phẩm</Typography>
            <Typography className="mt-2xs text-foreground-secondary">
              {useDemoData ? "Đang hiển thị dữ liệu minh họa. " : ""}Vui lòng kiểm tra kết nối rồi thử lại.
            </Typography>
            <Button className="mt-sm" onClick={() => void loadListings()}>Thử lại</Button>
          </div>
        )}

        {!isLoading && !error && listings.length === 0 && useDemoData && (
          <div className="mb-md rounded-card bg-surface-sunken p-sm text-center text-foreground-secondary">
            Chưa có sản phẩm thật. Dưới đây là dữ liệu minh họa giao diện.
          </div>
        )}

        {!isLoading && !error && listings.length === 0 && !useDemoData && (
          <div className="rounded-card bg-surface-sunken p-md text-center text-foreground-secondary">
            Sạp chưa có sản phẩm đang bán.
          </div>
        )}

        {!isLoading && displayedListings.length > 0 && (
          <div className={`${error ? "mt-md " : ""}grid grid-cols-1 gap-sm sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5`}>
            {displayedListings.map((listing) => (
              <ProductCard
                key={listing.productStallId}
                listing={listing}
                showNegotiation={showNegotiation}
                showStall={showStall}
              />
            ))}
          </div>
        )}
      </Container>
    </section>
  );
}
