"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { getCategories, type Category } from "@/features/categories/api";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Typography } from "@/shared/ui/typography";

const CATEGORY_PAGE_SIZE = 6;

interface CategoryCardProps {
  category: Category;
  href: string;
  isSelected?: boolean;
  compact?: boolean;
}

function CategoryCard({ category, href, isSelected = false, compact = false }: CategoryCardProps) {
  const appearance = isSelected
    ? "border-brand-primary bg-brand-primary text-foreground-inverse"
    : compact
      ? "border-brand-primary/10 bg-surface-raised hover:border-brand-primary"
      : "border-brand-primary/10 bg-ready-background hover:border-brand-primary/30";

  return (
    <Link
      href={href}
      aria-current={isSelected ? "page" : undefined}
      scroll={false}
      className={`group flex items-center justify-center border text-center transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary focus-visible:ring-offset-2 ${compact ? "min-h-10 shrink-0 whitespace-nowrap rounded-pill px-sm py-xs shadow-none" : "min-h-32 flex-col gap-xs rounded-card p-sm shadow-card hover:-translate-y-0.5 hover:shadow-md"} ${appearance}`}
    >
      <span
        aria-hidden="true"
        className={`${compact ? "hidden" : "flex"} size-14 items-center justify-center rounded-full bg-surface-sunken bg-cover bg-center font-data text-xl font-bold text-brand-primary ring-1 ring-border-subtle transition-transform group-hover:scale-105`}
        style={category.imageUrl ? { backgroundImage: `url(${JSON.stringify(category.imageUrl)})` } : undefined}
      >
        {!category.imageUrl && category.name.charAt(0).toLocaleUpperCase("vi")}
      </span>
      <Typography
        as="span"
        variant="labelMd"
        className={`line-clamp-2 ${isSelected ? "text-foreground-inverse" : "text-foreground-primary"}`}
      >
        {category.name}
      </Typography>
    </Link>
  );
}

function CategorySkeleton() {
  return (
    <div
      aria-hidden="true"
      className="min-h-32 animate-pulse rounded-card border border-border-subtle bg-surface-raised p-sm"
    >
      <div className="mx-auto mt-xs size-14 rounded-full bg-surface-sunken" />
      <div className="mx-auto mt-sm h-4 w-20 rounded bg-surface-sunken" />
    </div>
  );
}

interface CategorySectionProps {
  stallId?: string;
  selectedCategoryId?: string;
}

function categoryHref(categoryId: string, stallId?: string): string {
  const query = `categoryId=${encodeURIComponent(categoryId)}`;
  return stallId ? `/stalls/${encodeURIComponent(stallId)}?${query}` : `/products?${query}`;
}

function categoryParams(stallId?: string) {
  return {
    ...(stallId ? { stallId } : {}),
    page: 1,
    pageSize: CATEGORY_PAGE_SIZE,
  };
}

export function CategorySection({ stallId, selectedCategoryId }: CategorySectionProps = {}) {
  const [categories, setCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(false);

  const loadCategories = useCallback(async () => {
    setIsLoading(true);
    setError(false);

    try {
      const result = await getCategories(
        categoryParams(stallId),
      );
      setCategories(result.items);
    } catch {
      setError(true);
    } finally {
      setIsLoading(false);
    }
  }, [stallId]);

  useEffect(() => {
    const controller = new AbortController();
    void getCategories(
      categoryParams(stallId),
      { signal: controller.signal },
    ).then((result) => {
      setCategories(result.items);
      setIsLoading(false);
    }).catch(() => {
      if (!controller.signal.aborted) {
        setError(true);
        setIsLoading(false);
      }
    });
    return () => controller.abort();
  }, [stallId]);

  return (
    <section id="categories" aria-labelledby="categories-heading" className={stallId ? "pb-lg" : "py-xl md:py-2xl"}>
      <Container>
        <div className={stallId ? "" : "rounded-modal border border-border-prominent bg-surface-container p-sm md:p-md"}>
          <div className={stallId ? "sr-only" : "mb-md flex items-end justify-between gap-md"}>
            <div>
              <Typography as="p" variant="labelMd" className="mb-2xs text-brand-secondary">
                Mua theo nhu cầu
              </Typography>
              <Typography as="h2" id="categories-heading" variant="headlineMd">
                Khám phá danh mục
              </Typography>
            </div>
          </div>

          {isLoading && (
            <div role="status" aria-label="Đang tải danh mục" className="grid grid-cols-2 gap-sm sm:grid-cols-3 lg:grid-cols-6">
              {Array.from({ length: 6 }, (_, index) => <CategorySkeleton key={index} />)}
            </div>
          )}

          {!isLoading && error && (
            <div role="alert" className="rounded-card border border-border-subtle bg-surface-raised p-md text-center">
              <Typography as="h3" variant="titleMd">Chưa tải được danh mục</Typography>
              <Typography className="mt-2xs text-foreground-secondary">Vui lòng kiểm tra kết nối rồi thử lại.</Typography>
              <Button className="mt-sm" onClick={() => void loadCategories()}>Thử lại</Button>
            </div>
          )}

          {!isLoading && !error && categories.length === 0 && (
            <div className="rounded-card bg-surface-sunken p-md text-center">
              <Typography as="p" className="text-foreground-secondary">Danh mục đang được cập nhật.</Typography>
            </div>
          )}

          {!isLoading && !error && categories.length > 0 && (
            <div className={stallId ? "flex flex-nowrap gap-xs overflow-x-auto pb-xs" : "grid grid-cols-2 gap-sm sm:grid-cols-3 lg:grid-cols-6"}>
              {stallId && (
                <Link
                  href={`/stalls/${encodeURIComponent(stallId)}`}
                  scroll={false}
                  aria-current={!selectedCategoryId ? "page" : undefined}
                  className={`inline-flex min-h-10 shrink-0 items-center whitespace-nowrap rounded-pill border px-sm font-data text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-primary ${!selectedCategoryId ? "border-brand-primary bg-brand-primary text-foreground-inverse" : "border-border-prominent bg-surface-raised text-foreground-primary hover:border-brand-primary"}`}
                >
                  Tất cả sản phẩm
                </Link>
              )}
              {categories.map((category) => (
                <CategoryCard
                  key={category.id}
                  category={category}
                  href={categoryHref(category.id, stallId)}
                  isSelected={selectedCategoryId === category.id}
                  compact={Boolean(stallId)}
                />
              ))}
            </div>
          )}
        </div>
      </Container>
    </section>
  );
}
