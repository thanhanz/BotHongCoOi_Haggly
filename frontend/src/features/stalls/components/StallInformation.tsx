"use client";

import { useCallback, useEffect, useState } from "react";
import { getPublicStallDetails, type PublicStallDetails } from "@/features/stalls/api";
import { ApiError } from "@/shared/api";
import { Badge } from "@/shared/ui/badge";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Typography } from "@/shared/ui/typography";

export function StallInformation({ stallId }: { stallId: string }) {
  const [stall, setStall] = useState<PublicStallDetails>();
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>();

  const loadStall = useCallback(async (signal?: AbortSignal) => {
    setIsLoading(true);
    setError(undefined);

    try {
      setStall(await getPublicStallDetails(stallId, { signal }));
    } catch (requestError) {
      if (!signal?.aborted) setError(requestError);
    } finally {
      if (!signal?.aborted) setIsLoading(false);
    }
  }, [stallId]);

  useEffect(() => {
    const controller = new AbortController();
    void loadStall(controller.signal);
    return () => controller.abort();
  }, [loadStall]);

  return (
    <section aria-labelledby="stall-heading" className="py-xl md:py-2xl">
      <Container>
        {isLoading && (
          <div role="status" aria-label="Đang tải thông tin sạp" className="animate-pulse rounded-modal border border-border-subtle bg-surface-raised p-md shadow-card">
            <div className="h-5 w-24 rounded bg-surface-sunken" />
            <div className="mt-sm h-10 w-2/3 rounded bg-surface-sunken" />
            <div className="mt-sm h-5 w-1/2 rounded bg-surface-sunken" />
          </div>
        )}

        {!isLoading && error !== undefined && (
          <div role="alert" className="rounded-modal border border-border-subtle bg-surface-raised p-md text-center shadow-card">
            <Typography as="h1" id="stall-heading" variant="headlineSm">
              {error instanceof ApiError && error.status === 404 ? "Không tìm thấy sạp" : "Chưa tải được thông tin sạp"}
            </Typography>
            <Typography className="mt-xs text-foreground-secondary">
              {error instanceof ApiError && error.status === 404
                ? "Sạp không tồn tại hoặc hiện không hoạt động."
                : "Vui lòng kiểm tra kết nối rồi thử lại."}
            </Typography>
            {!(error instanceof ApiError && error.status === 404) && (
              <Button className="mt-sm" onClick={() => void loadStall()}>Thử lại</Button>
            )}
          </div>
        )}

        {!isLoading && !error && stall && (
          <div className="overflow-hidden rounded-modal border border-brand-primary/15 bg-surface-raised shadow-card">
            <div className="bg-brand-primary px-md py-lg text-foreground-inverse md:px-lg">
              <Badge variant="tertiary" size="sm">{stall.code}</Badge>
              <Typography as="h1" id="stall-heading" variant="headlineLgMobile" className="mt-sm text-foreground-inverse md:text-5xl">
                {stall.name}
              </Typography>
            </div>
            <div className="grid gap-sm p-md text-foreground-secondary md:grid-cols-2 md:px-lg">
              <div>
                <Typography as="p" variant="labelSm" className="text-brand-secondary">Vị trí</Typography>
                <Typography className="mt-2xs">{stall.locationDescription || "Đang cập nhật"}</Typography>
              </div>
              <div>
                <Typography as="p" variant="labelSm" className="text-brand-secondary">Liên hệ</Typography>
                <Typography className="mt-2xs">{stall.phoneNumber || "Đang cập nhật"}</Typography>
              </div>
            </div>
          </div>
        )}
      </Container>
    </section>
  );
}
