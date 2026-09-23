"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState, useTransition, type FormEvent } from "react";
import { ProductCard } from "@/features/product-listings/components";
import { ApiError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Input } from "@/shared/ui/input";
import { searchMarketplace, type MarketplaceSearchParams, type MarketplaceSearchResult } from "../api";
import { SearchPagination } from "./SearchPagination";

type SearchState =
  | { status: "loading" }
  | { status: "error"; invalidQuery: boolean }
  | { status: "success"; result: MarketplaceSearchResult };

export function MarketplaceSearch({ params }: { params: Required<MarketplaceSearchParams> }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [attempt, setAttempt] = useState(0);

  function navigate(next: Required<MarketplaceSearchParams>) {
    const query = new URLSearchParams({
      q: next.q,
      stallPage: String(next.stallPage),
      stallPageSize: String(next.stallPageSize),
      productPage: String(next.productPage),
      productPageSize: String(next.productPageSize),
    });
    startTransition(() => router.push(`/search?${query}`, { scroll: false }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const q = String(new FormData(event.currentTarget).get("q") ?? "").trim();
    if (!q) return;
    if (q === params.q && params.productPage === 1 && params.stallPage === 1) {
      setAttempt(value => value + 1);
      return;
    }
    navigate({ ...params, q, productPage: 1, stallPage: 1 });
  }

  return (
    <Container className="space-y-lg py-lg md:py-xl">
      <nav aria-label="Đường dẫn" className="flex gap-xs text-sm text-foreground-secondary">
        <Link href="/" className="rounded-control hover:text-brand-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary">Trang chủ</Link>
        <span aria-hidden="true">/</span>
        <span aria-current="page">Tìm kiếm</span>
      </nav>
      {!params.q ? (
        <div className="rounded-card border border-dashed border-border-prominent p-lg text-center">
          <h2 className="text-lg font-semibold">Bạn đang tìm gì hôm nay?</h2>
          <p className="mt-xs text-sm text-foreground-secondary">Nhập tên sản phẩm hoặc sạp để bắt đầu tìm kiếm.</p>
        </div>
      ) : (
        <SearchResults key={attempt} params={params} pending={pending} onNavigate={navigate} onRetry={() => setAttempt(value => value + 1)} />
      )}
    </Container>
  );
}

function SearchResults({ params, pending, onNavigate, onRetry }: {
  params: Required<MarketplaceSearchParams>;
  pending: boolean;
  onNavigate: (params: Required<MarketplaceSearchParams>) => void;
  onRetry: () => void;
}) {
  const [state, setState] = useState<SearchState>({ status: "loading" });
  const { q, stallPage, stallPageSize, productPage, productPageSize } = params;
  useEffect(() => {
    const controller = new AbortController();
    void searchMarketplace({ q, stallPage, stallPageSize, productPage, productPageSize }, { signal: controller.signal }).then(result => {
      if (!controller.signal.aborted) setState({ status: "success", result });
    }).catch(error => {
      if (!controller.signal.aborted) setState({ status: "error", invalidQuery: error instanceof ApiError && error.status === 400 });
    });
    return () => controller.abort();
  }, [q, stallPage, stallPageSize, productPage, productPageSize]);

  if (state.status === "loading") {
    return (
      <div role="status" className="space-y-lg">
        <p className="text-sm text-foreground-secondary">Đang tìm sạp và sản phẩm…</p>
        <div aria-hidden="true" className="space-y-lg animate-pulse motion-reduce:animate-none">
          <div className="h-28 rounded-card bg-ready-background" />
          <div className="grid grid-cols-1 gap-sm sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {Array.from({ length: 5 }, (_, index) => <div key={index} className="h-80 rounded-card bg-ready-background" />)}
          </div>
        </div>
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <div role="alert" className="rounded-card border border-border-prominent bg-surface-raised p-lg text-center">
        <h2 className="text-lg font-semibold">{state.invalidQuery ? "Từ khóa tìm kiếm chưa hợp lệ" : "Chưa tải được kết quả"}</h2>
        <p className="mt-xs text-sm text-foreground-secondary">
          {state.invalidQuery ? "Hãy nhập từ khóa có 2–100 ký tự có thể tìm kiếm, không chỉ gồm dấu câu hoặc khoảng trắng." : "Vui lòng kiểm tra kết nối và thử lại."}
        </p>
        {!state.invalidQuery && <Button className="mt-sm" onClick={onRetry}>Thử lại</Button>}
      </div>
    );
  }

  const { stalls, products } = state.result;
  return (
    <div className="space-y-xl" aria-busy={pending}>
      <p role="status" className="text-sm text-foreground-secondary">
        {pending ? "Đang chuyển trang…" : `Tìm thấy ${stalls.totalCount} sạp và ${products.totalCount} sản phẩm phù hợp.`}
      </p>
      <section aria-labelledby="search-stalls-heading">
        <div className="mb-sm flex items-baseline gap-xs">
          <h2 id="search-stalls-heading" className="text-xl font-semibold text-brand-primary">Sạp phù hợp</h2>
          <span className="font-data text-sm text-foreground-secondary">({stalls.totalCount})</span>
        </div>
        {stalls.items.length > 0 ? (
          <ul className="divide-y divide-border-subtle rounded-card border border-border-subtle bg-surface-raised">
            {stalls.items.map(stall => (
              <li key={stall.id}>
                <Link href={`/stalls/${encodeURIComponent(stall.id)}`} className="flex flex-wrap items-center gap-sm rounded-card p-sm transition-colors hover:bg-ready-background/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary md:px-md">
                  <span aria-hidden="true" className="flex size-11 shrink-0 items-center justify-center rounded-control bg-ready-background font-data text-lg font-bold text-brand-primary">{stall.name.charAt(0).toLocaleUpperCase("vi")}</span>
                  <div className="min-w-0 flex-1 basis-40">
                    <h3 className="break-words font-semibold text-brand-primary">{stall.name}</h3>
                    <p className="mt-1 break-words text-xs text-foreground-secondary">{stall.code}{stall.locationDescription ? ` · ${stall.locationDescription}` : ""}</p>
                  </div>
                  <span className="text-sm font-semibold text-brand-primary">Ghé sạp <span aria-hidden="true">→</span></span>
                </Link>
              </li>
            ))}
          </ul>
        ) : (
          <p className="rounded-card border border-dashed border-border-prominent p-md text-sm text-foreground-secondary">
            {stalls.totalCount ? "Trang này không có sạp. Hãy chọn một trang khác." : "Chưa tìm thấy sạp phù hợp. Bạn có thể xem các sản phẩm bên dưới hoặc thử tên sạp khác."}
          </p>
        )}
        {(stalls.totalPages > 1 || params.stallPage > 1) && (
          <SearchPagination label="sạp" {...stalls} disabled={pending} onPageChange={stallPage => onNavigate({ ...params, stallPage })} />
        )}
      </section>
      <section aria-labelledby="search-products-heading">
        <div className="mb-md flex flex-wrap items-baseline justify-between gap-xs">
          <div className="flex items-baseline gap-xs">
            <h2 id="search-products-heading" className="text-xl font-semibold text-brand-primary">Sản phẩm phù hợp</h2>
            <span className="font-data text-sm text-foreground-secondary">({products.totalCount})</span>
          </div>
          <p className="text-xs text-foreground-secondary">Ưu tiên kết quả phù hợp nhất</p>
        </div>
        {products.items.length > 0 ? (
          <div className="grid grid-cols-1 gap-sm sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {products.items.map(listing => <ProductCard key={listing.productStallId} listing={listing} />)}
          </div>
        ) : (
          <div className="rounded-card border border-dashed border-border-prominent p-lg text-center">
            <h3 className="text-lg font-semibold">{products.totalCount ? "Trang này không có sản phẩm" : "Chưa tìm thấy sản phẩm phù hợp"}</h3>
            <p className="mt-xs text-sm text-foreground-secondary">{products.totalCount ? "Chọn một trang khác để tiếp tục xem sản phẩm." : "Thử từ khóa khác hoặc ghé sạp phù hợp ở trên để xem hàng đang bán."}</p>
            <Link href="/products" className="mt-sm inline-block rounded-control text-sm font-semibold text-brand-primary underline underline-offset-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary">Xem tất cả sản phẩm</Link>
          </div>
        )}
        <SearchPagination
          label="sản phẩm"
          {...products}
          disabled={pending}
          onPageChange={productPage => onNavigate({ ...params, productPage })}
          onPageSizeChange={productPageSize => onNavigate({ ...params, productPageSize, productPage: 1 })}
        />
      </section>
    </div>
  );
}
