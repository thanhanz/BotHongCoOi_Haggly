"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  clearCart,
  createOrder,
  getCart,
  removeCartItem,
  updateCartItem,
  type Cart,
  type CartItem,
  type UpdateCartItemRequest,
} from "@/features/cart/api";
import { PRODUCT_UNITS, type ProductUnit } from "@/features/products/api";
import { useAuth } from "@/features/identity/components";
import { ApiError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Typography } from "@/shared/ui/typography";
import { IndeterminateCheckbox } from "./IndeterminateCheckbox";

const UNIT_LABELS: Record<ProductUnit, string> = {
  [PRODUCT_UNITS.KG]: "kg", [PRODUCT_UNITS.GRAM]: "g", [PRODUCT_UNITS.PIECE]: "cái",
  [PRODUCT_UNITS.BUNCH]: "bó", [PRODUCT_UNITS.BOX]: "hộp", [PRODUCT_UNITS.PACK]: "gói",
  [PRODUCT_UNITS.LITER]: "lít", [PRODUCT_UNITS.OTHER]: "đơn vị",
};

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });
const UPDATE_DEBOUNCE_MS = 2000;

function messageFor(error: unknown): string {
  if (!(error instanceof ApiError)) return "Không thể kết nối đến máy chủ.";
  if (error.status === 409) return "Giỏ hàng vừa thay đổi. Vui lòng tải lại và thử lại.";
  if (error.status === 404) return "Món này không còn trong giỏ hàng.";
  if (error.status === 400) return error.message || "Số lượng hoặc ghi chú chưa hợp lệ.";
  return "Không thể cập nhật giỏ hàng lúc này.";
}

function itemName(item: CartItem) {
  return item.offering.displayName?.trim() || item.product.name;
}

function BinIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true" className="size-5">
      <path d="M4 7h16M9 7V4h6v3m3 0-1 13H7L6 7m4 4v5m4-5v5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

export function CartDetails() {
  const router = useRouter();
  const { session, isReady } = useAuth();
  const [cart, setCart] = useState<Cart>();
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [isLoading, setIsLoading] = useState(true);
  const [pendingId, setPendingId] = useState<string>();
  const [error, setError] = useState<string>();
  const [success, setSuccess] = useState<string>();
  const [drafts, setDrafts] = useState<Record<string, UpdateCartItemRequest>>({});
  const [savingIds, setSavingIds] = useState<Set<string>>(new Set());
  const updateTimers = useRef(new Map<string, ReturnType<typeof setTimeout>>());
  const updateRevisions = useRef(new Map<string, number>());

  const load = useCallback(async (initial = false) => {
    if (!session) return;
    setIsLoading(true);
    setError(undefined);
    try {
      const result = await getCart();
      setCart(result);
      const available = result.stalls.flatMap(group => group.items).filter(item => item.isQuantityAvailable).map(item => item.cartItemId);
      setSelected(previous => initial ? new Set(available) : new Set(available.filter(id => previous.has(id))));
    } catch (requestError) {
      setError(messageFor(requestError));
    } finally {
      setIsLoading(false);
    }
  }, [session]);

  useEffect(() => {
    if (!isReady) return;
    if (!session) {
      router.replace("/login?returnTo=%2Fcart");
      return;
    }
    if (!session.roles.some(role => role.toLowerCase() === "buyer")) return;
    const timeoutId = window.setTimeout(() => void load(true), 0);
    return () => window.clearTimeout(timeoutId);
  }, [isReady, load, router, session]);

  useEffect(() => () => {
    updateTimers.current.forEach(timer => clearTimeout(timer));
    updateTimers.current.clear();
  }, []);

  const allItems = useMemo(() => (cart?.stalls.flatMap(group => group.items) ?? []).map(item => {
    const draft = drafts[item.cartItemId];
    return draft ? {
      ...item,
      quantity: draft.quantity,
      notes: draft.notes,
      lineTotal: item.offering.currentUnitPrice * draft.quantity,
    } : item;
  }), [cart, drafts]);
  const availableItems = useMemo(() => allItems.filter(item => item.isQuantityAvailable), [allItems]);
  const selectedItems = useMemo(() => availableItems.filter(item => selected.has(item.cartItemId)), [availableItems, selected]);
  const selectedTotal = selectedItems.reduce((sum, item) => sum + item.lineTotal, 0);
  const selectedStalls = cart?.stalls.filter(group => group.items.some(item => selected.has(item.cartItemId))).length ?? 0;

  function applyCart(next: Cart) {
    setCart(next);
    const valid = new Set(next.stalls.flatMap(group => group.items).filter(item => item.isQuantityAvailable).map(item => item.cartItemId));
    setSelected(previous => new Set([...previous].filter(id => valid.has(id))));
  }

  function discardDraft(cartItemId: string) {
    const timer = updateTimers.current.get(cartItemId);
    if (timer) clearTimeout(timer);
    updateTimers.current.delete(cartItemId);
    updateRevisions.current.delete(cartItemId);
    setDrafts(current => {
      const next = { ...current };
      delete next[cartItemId];
      return next;
    });
    setSavingIds(current => {
      const next = new Set(current);
      next.delete(cartItemId);
      return next;
    });
  }

  function discardAllDrafts() {
    updateTimers.current.forEach(timer => clearTimeout(timer));
    updateTimers.current.clear();
    updateRevisions.current.clear();
    setDrafts({});
    setSavingIds(new Set());
  }

  async function mutate(id: string, action: () => Promise<Cart>) {
    discardDraft(id);
    setPendingId(id);
    setError(undefined);
    try {
      applyCart(await action());
    } catch (requestError) {
      setError(messageFor(requestError));
    } finally {
      setPendingId(undefined);
    }
  }

  function scheduleUpdate(item: CartItem, quantity: number, notes = item.notes) {
    if (!session) return;
    const request = { quantity, notes };
    const revision = (updateRevisions.current.get(item.cartItemId) ?? 0) + 1;
    updateRevisions.current.set(item.cartItemId, revision);
    setDrafts(current => ({ ...current, [item.cartItemId]: request }));
    setSavingIds(current => new Set(current).add(item.cartItemId));

    const existingTimer = updateTimers.current.get(item.cartItemId);
    if (existingTimer) clearTimeout(existingTimer);
    updateTimers.current.set(item.cartItemId, setTimeout(async () => {
      updateTimers.current.delete(item.cartItemId);
      try {
        const nextCart = await updateCartItem(item.cartItemId, request);
        if (updateRevisions.current.get(item.cartItemId) !== revision) return;
        discardDraft(item.cartItemId);
        applyCart(nextCart);
      } catch (requestError) {
        if (updateRevisions.current.get(item.cartItemId) !== revision) return;
        discardDraft(item.cartItemId);
        setError(messageFor(requestError));
        void load(false);
      }
    }, UPDATE_DEBOUNCE_MS));
  }

  async function submitOrder() {
    if (!session || selectedItems.length === 0) return;
    setPendingId("order");
    setError(undefined);
    setSuccess(undefined);
    discardAllDrafts();
    try {
      const order = await createOrder({ items: selectedItems.map(item => ({
        inventoryItemId: item.inventoryItemId,
        quantity: item.quantity,
        notes: item.notes,
      })) });
      setSuccess(`Đã tạo đơn ${order.orderNo} thành công.`);
      await load(false);
    } catch (requestError) {
      setError(messageFor(requestError));
    } finally {
      setPendingId(undefined);
    }
  }

  if (!isReady) return <CartSkeleton />;
  if (!session) return <CartSkeleton />;
  if (!session.roles.some(role => role.toLowerCase() === "buyer")) {
    return <CartMessage message="Tài khoản này không có quyền truy cập giỏ hàng người mua." actionLabel="Về trang chủ" href="/" />;
  }
  if (isLoading) return <CartSkeleton />;
  if (!cart && error) return <CartMessage message={error} action={() => void load(true)} />;
  if (!cart || allItems.length === 0) return (
    <CartMessage message="Giỏ hàng của bạn đang trống." actionLabel="Chọn món ở chợ" href="/products" />
  );

  const allChecked = availableItems.length > 0 && selectedItems.length === availableItems.length;
  const partlyChecked = selectedItems.length > 0 && !allChecked;

  return (
    <section className="py-lg md:py-xl">
      <Container>
        <div className="mb-md flex flex-col gap-sm sm:flex-row sm:items-end sm:justify-between">
          <div>
            <Typography as="h1" variant="headlineMd">Giỏ hàng của bạn</Typography>
            <p className="mt-2xs text-sm text-foreground-secondary">{cart.itemCount} món từ {cart.stalls.length} sạp độc lập</p>
          </div>
          <div className="flex flex-wrap items-center gap-xs">
            <Link href="/products" className="text-sm font-semibold text-brand-primary hover:underline">← Tiếp tục chọn món</Link>
            <Button variant="ghost" size="sm" disabled={Boolean(pendingId)} onClick={() => {
              if (confirm("Xóa toàn bộ giỏ hàng?")) {
                discardAllDrafts();
                void mutate("clear", () => clearCart());
              }
            }}>Xóa toàn bộ</Button>
          </div>
        </div>

        {(error || success) && <div role={error ? "alert" : "status"} className={`mb-sm rounded-control p-sm text-sm ${error ? "bg-state-error-surface text-state-error" : "bg-ready-background text-ready-text"}`}>{error ?? success}</div>}

        <div className="mb-sm rounded-card border border-border-subtle bg-surface-raised px-sm py-xs shadow-card">
          <IndeterminateCheckbox
            label={`Chọn tất cả ${availableItems.length} món có sẵn`}
            checked={allChecked}
            indeterminate={partlyChecked}
            onChange={() => setSelected(allChecked ? new Set() : new Set(availableItems.map(item => item.cartItemId)))}
          />
        </div>

        <div className="grid items-start gap-md lg:grid-cols-[minmax(0,2fr)_minmax(19rem,0.9fr)]">
          <div className="grid gap-sm">
            {cart.stalls.map(group => {
              const selectable = group.items.filter(item => item.isQuantityAvailable);
              const selectedCount = selectable.filter(item => selected.has(item.cartItemId)).length;
              const groupChecked = selectable.length > 0 && selectedCount === selectable.length;
              const groupTotal = allItems
                .filter(item => group.items.some(groupItem => groupItem.cartItemId === item.cartItemId))
                .reduce((sum, item) => sum + item.lineTotal, 0);
              return (
                <article key={group.stall.id} className="overflow-hidden rounded-card border border-border-subtle bg-surface-raised shadow-card">
                  <header className="flex flex-col gap-xs border-b border-border-subtle bg-surface-container p-sm sm:flex-row sm:items-center sm:justify-between">
                    <IndeterminateCheckbox
                      label={<span><strong>{group.stall.code}</strong> · {group.stall.name}</span>}
                      checked={groupChecked}
                      indeterminate={selectedCount > 0 && !groupChecked}
                      disabled={selectable.length === 0}
                      onChange={() => setSelected(previous => {
                        const next = new Set(previous);
                        selectable.forEach(item => {
                          if (groupChecked) next.delete(item.cartItemId);
                          else next.add(item.cartItemId);
                        });
                        return next;
                      })}
                    />
                    <div className="text-sm text-foreground-secondary sm:text-right">
                      {group.stall.locationDescription && <div>{group.stall.locationDescription}</div>}
                      <strong className="font-data text-foreground-primary">Tổng sạp: {currency.format(groupTotal)}</strong>
                    </div>
                  </header>
                  <div className="divide-y divide-border-subtle px-sm">
                    {group.items.map(serverItem => {
                      const item = allItems.find(candidate => candidate.cartItemId === serverItem.cartItemId) ?? serverItem;
                      const step = item.offering.minimumOrderQuantity > 0 ? item.offering.minimumOrderQuantity : 1;
                      const disabled = pendingId === item.cartItemId;
                      const isSaving = savingIds.has(item.cartItemId);
                      return (
                        <div key={item.cartItemId} className="grid grid-cols-[auto_4rem_minmax(0,1fr)] items-center gap-x-sm gap-y-xs py-sm lg:grid-cols-[auto_5rem_minmax(10rem,1fr)_auto_auto_auto]">
                          <IndeterminateCheckbox
                            aria-label={`Chọn ${itemName(item)}`}
                            checked={selected.has(item.cartItemId)}
                            disabled={!item.isQuantityAvailable}
                            onChange={() => setSelected(previous => {
                              const next = new Set(previous);
                              if (next.has(item.cartItemId)) next.delete(item.cartItemId);
                              else next.add(item.cartItemId);
                              return next;
                            })}
                          />
                          <div className="size-16 overflow-hidden rounded-control bg-ready-background bg-cover bg-center lg:size-20" style={item.product.imageUrl ? { backgroundImage: `url(${JSON.stringify(item.product.imageUrl)})` } : undefined}>
                            {!item.product.imageUrl && <span className="flex size-full items-center justify-center font-data text-2xl font-bold text-brand-primary/40">{itemName(item).charAt(0)}</span>}
                          </div>
                          <div className="min-w-0">
                            <Typography as="h2" variant="labelLg">{itemName(item)}</Typography>
                            <p className="text-sm text-foreground-secondary">{currency.format(item.offering.currentUnitPrice)} / {UNIT_LABELS[item.offering.sellingUnit]}</p>
                          </div>
                          <div className="col-start-2 col-span-2 flex h-10 w-fit items-center rounded-control border border-border-prominent lg:col-auto lg:col-span-1">
                            <button type="button" aria-label={`Giảm ${itemName(item)}`} disabled={disabled || item.quantity <= step} onClick={() => scheduleUpdate(item, Math.max(step, item.quantity - step))} className="size-10 disabled:opacity-40">−</button>
                            <span className="min-w-16 text-center font-data text-sm font-semibold">{item.quantity} {UNIT_LABELS[item.offering.sellingUnit]}</span>
                            <button type="button" aria-label={`Tăng ${itemName(item)}`} disabled={disabled || item.quantity + step > item.remainingQuantity} onClick={() => scheduleUpdate(item, item.quantity + step)} className="size-10 disabled:opacity-40">+</button>
                          </div>
                          <strong className="col-start-2 font-data text-brand-primary lg:col-auto">{currency.format(item.lineTotal)}</strong>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="text-state-error"
                            aria-label={`Xóa ${itemName(item)} khỏi giỏ hàng`}
                            title="Xóa món"
                            disabled={disabled}
                            onClick={() => void mutate(item.cartItemId, () => removeCartItem(item.cartItemId))}
                          >
                            <BinIcon />
                          </Button>
                          <div className="col-span-3 grid gap-2xs lg:col-start-3 lg:col-span-4">
                            {!item.isQuantityAvailable && <p role="status" className="rounded-control bg-state-error-surface px-xs py-2xs text-sm text-state-error">Chỉ còn {item.remainingQuantity} {UNIT_LABELS[item.offering.sellingUnit]}. Hãy cập nhật số lượng.</p>}
                            <label className="block text-sm text-foreground-secondary">
                              <span className="sr-only">Ghi chú cho {itemName(item)}</span>
                              <input
                                value={item.notes ?? ""}
                                placeholder="Lời dặn cho sạp…"
                                disabled={disabled}
                                onChange={event => scheduleUpdate(item, item.quantity, event.currentTarget.value || null)}
                                className="h-9 w-full rounded-control bg-ready-background px-xs text-sm outline-none focus-visible:ring-2 focus-visible:ring-brand-primary"
                              />
                            </label>
                            {isSaving && <span role="status" className="text-xs text-foreground-secondary">Đang chờ lưu…</span>}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </article>
              );
            })}
          </div>

          <aside className="rounded-card border border-border-subtle bg-surface-raised p-md shadow-card lg:sticky lg:top-20">
            <Typography as="h2" variant="headlineSm">Tóm tắt đơn hàng</Typography>
            <div className="my-sm border-y border-border-subtle py-sm text-sm">
              <div className="flex justify-between"><span>Đã chọn</span><strong>{selectedItems.length} món · {selectedStalls} sạp</strong></div>
              <div className="mt-xs flex justify-between"><span>Phí nhận tại sạp</span><strong>{currency.format(0)}</strong></div>
            </div>
            <div className="mb-sm flex items-baseline justify-between rounded-card bg-ready-background p-sm">
              <span className="font-data text-sm font-semibold">TỔNG DỰ KIẾN</span>
              <strong className="font-data text-2xl text-brand-primary">{currency.format(selectedTotal)}</strong>
            </div>
            <Button size="lg" className="w-full" loading={pendingId === "order"} disabled={selectedItems.length === 0 || Boolean(pendingId)} onClick={() => void submitOrder()}>
              Đặt {selectedItems.length} món đã chọn →
            </Button>
            <p className="mt-sm rounded-control bg-surface-container p-xs text-xs leading-relaxed text-foreground-secondary">Giỏ hàng chưa giữ trước tồn kho. Số lượng và giá được máy chủ xác nhận khi tạo đơn.</p>
          </aside>
        </div>
      </Container>
    </section>
  );
}

function CartSkeleton() {
  return <section className="py-xl"><Container><div role="status" aria-label="Đang tải giỏ hàng" className="grid animate-pulse gap-sm"><div className="h-10 w-64 rounded bg-surface-sunken" /><div className="h-64 rounded-card bg-surface-sunken" /><div className="h-48 rounded-card bg-surface-sunken" /></div></Container></section>;
}

function CartMessage({ message, action, actionLabel = "Thử lại", href }: { message: string; action?: () => void; actionLabel?: string; href?: string }) {
  return <section className="py-xl"><Container><div className="rounded-card border border-border-subtle bg-surface-raised p-lg shadow-card"><Typography as="h1" variant="headlineSm">{message}</Typography>{href ? <Link href={href} className="mt-sm inline-flex h-10 items-center rounded-control bg-brand-primary px-md font-data text-sm font-semibold text-white">{actionLabel}</Link> : action && <Button className="mt-sm" onClick={action}>{actionLabel}</Button>}</div></Container></section>;
}
