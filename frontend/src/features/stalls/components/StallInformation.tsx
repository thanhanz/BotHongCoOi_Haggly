"use client";

import { useCallback, useEffect, useState } from "react";
import { getPublicStallDetails, type PublicStallDetails } from "@/features/stalls/api";
import { ApiError } from "@/shared/api";
import { Avatar, AvatarFallback } from "@/shared/ui/avatar";
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
    void getPublicStallDetails(stallId, { signal: controller.signal }).then((result) => {
      setStall(result);
      setIsLoading(false);
    }).catch((requestError: unknown) => {
      if (!controller.signal.aborted) {
        setError(requestError);
        setIsLoading(false);
      }
    });
    return () => controller.abort();
  }, [stallId]);

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
          <div className="overflow-hidden rounded-modal border border-border-subtle bg-surface-raised shadow-card">
            <div className="min-h-40 border-b border-border-subtle bg-white px-md py-lg md:flex md:min-h-52 md:items-end md:px-lg">
              <div className="flex items-center gap-sm md:gap-md">
                <Avatar className="h-20 w-20 rounded-modal border-2 border-white bg-ready-background text-2xl shadow-card md:h-30 md:w-30 md:text-3xl">
                  <AvatarFallback >{stall.name.charAt(0).toLocaleUpperCase("vi")}</AvatarFallback>
                </Avatar>
                <div className="min-w-0">
                  <div className="mb-xs flex flex-wrap gap-xs text-sm text-foreground-secondary">
                    <span className="rounded-pill bg-surface-sunken px-sm py-2xs">
                      {stall.locationDescription || "Địa chỉ đang cập nhật"}
                    </span>
                    <span className="rounded-pill bg-surface-sunken px-sm py-2xs">
                      {stall.phoneNumber || "Số điện thoại đang cập nhật"}
                    </span>
                  </div>
                  <Typography as="h1" id="stall-heading" variant="headlineLgMobile" className="text-foreground-primary md:text-5xl">
                    {stall.name}
                  </Typography>
                  <Badge variant="tertiary" size="sm" className="mt-xs">{stall.code}</Badge>
                </div>
              </div>
            </div>
          </div>
        )}
      </Container>
    </section>
  );
}
