"use client";

import { Button } from "@/shared/ui/button";
import { Select } from "@/shared/ui/select";

interface SearchPaginationProps {
  label: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  disabled: boolean;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (pageSize: number) => void;
}

export function SearchPagination({
  label, page, pageSize, totalCount, totalPages, disabled, onPageChange, onPageSizeChange,
}: SearchPaginationProps) {
  const pageSizes = [...new Set([12, 20, 40, 60, pageSize])].sort((a, b) => a - b);
  // Keep the selector bounded even when the API reports thousands of pages.
  const pages = totalPages <= 200
    ? Array.from({ length: totalPages }, (_, index) => index + 1)
    : [...new Set([1, page - 2, page - 1, page, page + 1, page + 2, totalPages])]
      .filter(value => value >= 1 && value <= totalPages)
      .sort((a, b) => a - b);
  const first = totalCount > 0 && page <= totalPages ? (page - 1) * pageSize + 1 : 0;
  const last = first ? Math.min(page * pageSize, totalCount) : 0;

  return (
    <nav aria-label={`Phân trang ${label}`} className="mt-md flex flex-wrap items-center justify-between gap-sm border-t border-border-subtle pt-sm">
      <p className="text-sm text-foreground-secondary">
        {first ? `${first}–${last} / ${totalCount} ${label}` : `${totalCount} ${label}`}
      </p>
      <div className="flex flex-wrap items-center gap-xs">
        {onPageSizeChange && (
          <Select aria-label={`Số ${label} mỗi trang`} value={pageSize} disabled={disabled} onChange={event => onPageSizeChange(Number(event.target.value))}>
            {pageSizes.map(value => <option key={value} value={value}>{value} / trang</option>)}
          </Select>
        )}
        {totalPages === 0 && page > 1 && (
          <Button variant="outline" disabled={disabled} onClick={() => onPageChange(1)}>Về trang đầu</Button>
        )}
        {totalPages > 0 && (
          <>
            <Button variant="outline" disabled={disabled || page <= 1} onClick={() => onPageChange(Math.min(page - 1, totalPages))} aria-label={`Trang ${label} trước`}>Trước</Button>
            <Select aria-label={`Chọn trang ${label}`} value={page} disabled={disabled || (totalPages === 1 && page === 1)} onChange={event => onPageChange(Number(event.target.value))}>
              {page > totalPages && <option value={page}>Trang {page} (không có kết quả)</option>}
              {pages.map(value => <option key={value} value={value}>Trang {value} / {totalPages}</option>)}
            </Select>
            <Button variant="outline" disabled={disabled || page >= totalPages} onClick={() => onPageChange(page + 1)} aria-label={`Trang ${label} sau`}>Sau</Button>
          </>
        )}
      </div>
    </nav>
  );
}
