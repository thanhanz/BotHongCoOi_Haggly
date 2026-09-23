"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/features/identity/components";
import { getPublicStallDetails, type PublicStallDetails } from "@/features/stalls/api";
import { ApiError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import { Container } from "@/shared/ui/container";
import { getOrderDetails, type Order } from "../api";
import { OrderIcon } from "./OrderIcon";
import { OrderStallCard } from "./OrderStallCard";
import { OrderSummary } from "./OrderSummary";
import { fulfillmentLabels, orderLabels } from "./order-presentation";
import styles from "./orders.module.css";

export function OrderDetails({ orderId }: { orderId: string }) {
  const { session, isReady } = useAuth();
  const router = useRouter();
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    if (isReady && !session) router.replace(`/login?returnTo=${encodeURIComponent(`/orders/${orderId}`)}`);
  }, [isReady, session, router, orderId]);
  if (!isReady || !session) return <OrderLoading />;
  if (!session.roles.some(role => role.toUpperCase() === "BUYER")) {
    return <OrderMessage message="Tài khoản này không có quyền xem đơn hàng người mua." />;
  }
  return <OrderLoader key={`${orderId}:${session.userId}:${attempt}`} orderId={orderId} onRetry={() => setAttempt(value => value + 1)} />;
}

function OrderLoader({ orderId, onRetry }: { orderId: string; onRetry: () => void }) {
  const [order, setOrder] = useState<Order>();
  const [stalls, setStalls] = useState<Record<string, PublicStallDetails>>({});
  const [error, setError] = useState("");
  const [stallWarning, setStallWarning] = useState(false);
  useEffect(() => {
    const controller = new AbortController();
    void getOrderDetails(orderId, { signal: controller.signal }).then(async result => {
      if (controller.signal.aborted) return;
      setOrder(result);
      const ids = [...new Set(result.fulfillments.map(group => group.stallId))];
      const results = await Promise.allSettled(ids.map(id => getPublicStallDetails(id, { signal: controller.signal })));
      if (controller.signal.aborted) return;
      const details: Record<string, PublicStallDetails> = {};
      results.forEach((result, index) => { if (result.status === "fulfilled") details[ids[index]] = result.value; });
      setStalls(details);
      setStallWarning(results.some(result => result.status === "rejected"));
    }).catch(cause => {
      if (controller.signal.aborted) return;
      const status = cause instanceof ApiError ? cause.status : undefined;
      setError(status === 404 ? "Không tìm thấy đơn hàng này." : status === 403 ? "Bạn không có quyền xem đơn hàng này." : status === 401 ? "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." : status === 400 ? "Mã đơn hàng không hợp lệ." : "Chưa tải được đơn hàng. Vui lòng kiểm tra kết nối và thử lại.");
    });
    return () => controller.abort();
  }, [orderId]);
  if (error) return <OrderMessage message={error} onRetry={onRetry} />;
  if (!order) return <OrderLoading />;
  return <OrderReview order={order} stalls={stalls} stallWarning={stallWarning} onRefresh={onRetry} />;
}

function OrderReview({ order, stalls, stallWarning, onRefresh }: {
  order: Order; stalls: Record<string, PublicStallDetails>; stallWarning: boolean; onRefresh: () => void;
}) {
  const [notice, setNotice] = useState("");
  const groups = order.fulfillments;
  const stallCount = new Set(groups.map(group => group.stallId)).size;
  const completed = order.status === 6;
  const cancelled = order.status === 7;
  const nameFor = (id: string) => stalls[id]?.name ?? `Sạp ${id.slice(0, 8)}`;

  return <div className={styles.page}>
    <div className="bg-ready-background/60">
      <Container>
        <nav aria-label="Đường dẫn" className="flex flex-wrap items-center gap-2 py-4 text-xs font-semibold text-foreground-secondary">
          <Link href="/products" className="flex items-center gap-2 text-brand-primary hover:underline"><OrderIcon kind="stall" className="size-4" />Chợ mua sắm</Link><span aria-hidden="true">›</span><span className="break-all">Đơn #{order.orderNo}</span><span aria-hidden="true">›</span><span className="text-brand-secondary">Tự nhận trực tiếp</span>
          <span className="rounded-pill bg-surface-raised px-3 py-1 text-brand-primary sm:ml-auto">{orderLabels[order.status] ?? "Đang cập nhật"}</span>
        </nav>
        {!cancelled && <ol aria-label="Tiến trình đơn hàng" className="grid grid-cols-3 gap-3 py-5 pb-7">
          {["Gom giỏ các sạp", "Theo dõi đơn tại sạp", "Nhận hàng tại sạp"].map((label, index) => <li key={label} aria-current={index === (completed ? 2 : 1) ? "step" : undefined} className={`flex items-center gap-3 ${index === 2 && !completed ? "text-foreground-secondary" : "text-brand-primary"}`}><span className={`flex size-8 shrink-0 items-center justify-center rounded-full font-data font-bold ${index === 2 && !completed ? "bg-ready-background" : "bg-brand-primary text-white"}`}>{index === 0 || completed ? "✓" : index + 1}</span><div><p className="font-data text-[10px] font-bold uppercase tracking-wider">Bước {index + 1}</p><p className="text-xs font-semibold sm:text-sm">{label}</p></div></li>)}
        </ol>}
      </Container>
    </div>
    <Container className="py-8 md:py-10">
      {notice && <p role="status" className="mb-4 rounded-control bg-ready-background p-3 text-sm text-brand-primary">{notice}</p>}
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.42fr)_minmax(0,1fr)]">
        <section aria-labelledby="pickup-title" className="min-w-0">
          <div className="mb-7 flex flex-wrap items-center justify-between gap-3">
            <div><h1 id="pickup-title" className="text-2xl font-bold tracking-tight text-brand-primary md:text-[28px]">Danh Sách Sạp Cần Ghé Lấy</h1><p className="mt-1 text-xs leading-6 text-foreground-secondary">Đơn hàng gồm <strong>{stallCount} sạp độc lập.</strong> Ghé từng sạp để nhận món tươi ngon.</p></div>
            <span className="inline-flex items-center gap-2 rounded-pill bg-ready-background px-4 py-2 text-xs font-semibold text-brand-primary"><OrderIcon kind="bag" className="size-4" />Tự nhận tại sạp</span>
          </div>
          {cancelled && <p role="status" className="mb-5 rounded-card bg-state-error-surface p-4 text-sm text-state-error">Đơn hàng đã hủy.{order.cancellationReason && ` ${order.cancellationReason}`}</p>}
          {stallWarning && <p role="status" className="mb-5 rounded-control bg-preparing-background p-3 text-sm">Một số thông tin sạp chưa tải được. Bạn vẫn có thể xem chi tiết đơn và tải lại để cập nhật.</p>}
          <div className="space-y-6">
            {groups.map(group => <OrderStallCard key={group.id} group={group} stall={stalls[group.stallId]} currency={order.currency} onNotice={setNotice} />)}
            {!groups.length && <Card className="p-8 text-center"><OrderIcon kind="bag" className="mx-auto mb-3 size-10 text-brand-primary" /><h2 className="font-semibold">Đơn hàng chưa có sạp nhận hàng</h2><p className="mt-2 text-sm text-foreground-secondary">Tải lại để kiểm tra thông tin mới nhất.</p></Card>}
          </div>
          <div className="mt-6 flex gap-4 rounded-card bg-brand-primary p-5 text-white"><span className="flex size-12 shrink-0 items-center justify-center rounded-full bg-white/10"><OrderIcon kind="leaf" className="size-6" /></span><div><h2 className="text-sm font-bold">Văn Hóa Chợ Thật · Hàng Tươi Tận Mắt</h2><p className="mt-1 text-xs leading-6 text-white/80">Khi ghé sạp, hãy kiểm tra độ tươi và số lượng cùng tiểu thương trước khi nhận hàng. Bớt chút đỉnh vui vẻ, ấm lòng cả đôi bên!</p></div></div>
        </section>
        <aside aria-label="Tóm tắt và hướng dẫn nhận hàng" className="min-w-0 space-y-6">
          <OrderSummary order={order} stalls={stalls} onRefresh={onRefresh} onNotice={setNotice} />
          <Card id="pickup-guide" tabIndex={-1} className="scroll-mt-6 p-5 focus-visible:outline-2 focus-visible:outline-brand-primary md:p-6">
            <h2 className="flex items-center gap-3 text-xl font-bold"><OrderIcon kind="map" className="size-6 shrink-0 text-brand-secondary" />Hướng Dẫn Ghé Các Sạp</h2>
            <p className="mb-5 mt-2 text-xs leading-6 text-foreground-secondary">Các sạp trong đơn của bạn. Sơ đồ đường đi trong chợ chưa có sẵn.</p>
            <ol className="space-y-0 rounded-card bg-surface-container p-4">{groups.map((group, index) => <li key={group.id} className="relative flex gap-3 pb-5 last:pb-0">
              {index < groups.length - 1 && <span aria-hidden="true" className="absolute bottom-0 left-[13px] top-7 border-l-2 border-dashed border-brand-primary/25" />}
              <span className={`z-10 flex size-7 shrink-0 items-center justify-center rounded-full font-data text-xs font-bold ${group.status === 4 || group.status === 5 ? "bg-brand-primary text-white" : "bg-ready-background text-brand-primary"}`}>{index + 1}</span>
              <div className="min-w-0"><Link href={`/stalls/${group.stallId}`} className="text-sm font-semibold text-brand-primary hover:underline">{nameFor(group.stallId)}</Link><p className="mt-1 text-xs text-foreground-secondary">{stalls[group.stallId]?.locationDescription || "Vị trí sạp chưa được cập nhật."}</p><p className="mt-1 text-xs">{fulfillmentLabels[group.status]}</p></div>
            </li>)}{!groups.length && <li className="text-sm text-foreground-secondary">Chưa có sạp để hiển thị.</li>}</ol>
            <h3 className="mb-3 mt-7 font-data text-xs font-bold uppercase tracking-wide">3 bước ghé lấy · Gần gũi như đi chợ</h3>
            <ol className="space-y-3 text-xs leading-6 text-foreground-secondary"><li><strong className="text-brand-primary">1. Ghé sạp:</strong> Đưa thông tin đơn hàng để tiểu thương đối chiếu.</li><li><strong className="text-brand-primary">2. Kiểm tra độ tươi:</strong> Cùng tiểu thương kiểm tra món và số lượng.</li><li><strong className="text-brand-primary">3. Nhận hàng:</strong> Nhờ tiểu thương xác nhận sau khi bàn giao.</li></ol>
          </Card>
        </aside>
      </div>
    </Container>;
  </div>;
}

function OrderLoading() {
  return <Container className="py-12"><div role="status" aria-label="Đang tải đơn hàng" className="grid animate-pulse gap-6 motion-reduce:animate-none"><div className="h-10 w-2/3 rounded-control bg-ready-background" /><div className="grid gap-6 lg:grid-cols-[1.42fr_1fr]"><div className="h-96 rounded-card bg-ready-background" /><div className="h-96 rounded-card bg-ready-background" /></div><span className="sr-only">Đang tải đơn hàng…</span></div></Container>;
}
function OrderMessage({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return <Container className="py-12"><Card className="p-8 text-center"><h1 role="alert" className="text-xl font-semibold">{message}</h1><div className="mt-5 flex flex-wrap items-center justify-center gap-4">{onRetry && <Button onClick={onRetry}>Thử lại</Button>}<Link href="/cart" className="text-sm font-semibold text-brand-primary underline">Về giỏ hàng</Link></div></Card></Container>;
}
