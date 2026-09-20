import Link from "next/link";
import type { PublicStallDetails } from "@/features/stalls/api";
import { Button } from "@/shared/ui/button";
import type { OrderFulfillment } from "../api";
import { OrderIcon } from "./OrderIcon";
import { formatDate, formatMoney, fulfillmentLabels, unitLabel } from "./order-presentation";

export function OrderStallCard({ group, stall, currency, onNotice }: {
  group: OrderFulfillment; stall?: PublicStallDetails; currency: string; onNotice: (message: string) => void;
}) {
  const ready = group.status === 4 || group.status === 5;
  const preparing = group.status === 3;
  const name = stall?.name ?? `Sạp ${group.stallId.slice(0, 8)}`;
  const money = (value: number) => formatMoney(value, currency);
  async function copyReference() {
    try {
      await navigator.clipboard.writeText(group.fulfillmentNo);
      onNotice(`Đã sao chép mã đơn tại ${name}.`);
    } catch { onNotice("Không thể sao chép tự động. Vui lòng chọn và sao chép mã đơn hiển thị tại sạp."); }
  }
  return <article aria-label={name} className={`overflow-hidden rounded-card border border-l-[6px] bg-surface-raised p-4 shadow-card sm:p-6 ${ready ? "border-l-brand-primary" : preparing ? "border-l-brand-secondary" : "border-l-foreground-secondary"}`}>
    <header className="mb-5 flex flex-wrap items-center justify-between gap-3">
      <div className="flex min-w-0 items-center gap-3"><span className="flex size-12 shrink-0 items-center justify-center rounded-full bg-ready-background text-brand-primary"><OrderIcon kind="stall" className="size-6" /></span><div className="min-w-0"><span className="rounded-pill bg-preparing-background px-2 py-1 font-data text-[10px] font-bold">{stall?.code ? `Sạp ${stall.code}` : "Sạp trong đơn"}</span><h2 className="mt-1 text-lg font-bold leading-snug"><Link href={`/stalls/${group.stallId}`} className="hover:text-brand-primary hover:underline">{name}</Link></h2>{stall?.locationDescription && <p className="mt-1 text-xs text-foreground-secondary">{stall.locationDescription}</p>}</div></div>
      <span className={`inline-flex items-center gap-2 rounded-pill px-3 py-1 font-data text-[11px] font-semibold ${ready ? "bg-ready-background text-ready-text" : preparing ? "bg-awaiting-pickup-background text-awaiting-pickup-text" : "bg-surface-container text-foreground-secondary"}`}><OrderIcon kind={ready ? "check" : "clock"} className="size-3.5" />{fulfillmentLabels[group.status] ?? "Đang cập nhật"}</span>
    </header>
    <ul className="space-y-2">{group.items.map(item => <li key={item.id} className="rounded-control bg-surface-container p-3"><div className="flex flex-wrap items-center gap-3"><span aria-hidden="true" className="flex size-11 shrink-0 items-center justify-center rounded-control bg-ready-background text-brand-primary"><OrderIcon kind="bag" /></span><div className="min-w-0 flex-1"><h3 className="text-sm font-semibold">{item.productNameSnapshot}</h3><div className="mt-1 flex flex-wrap gap-2 font-data text-[11px] font-semibold text-foreground-secondary"><span>{new Intl.NumberFormat("vi-VN").format(item.finalQuantity)} {unitLabel(item.sellingUnitSnapshot)}</span>{item.isNegotiated && <span className="rounded bg-awaiting-pickup-background px-1.5 text-awaiting-pickup-text">Đã trả giá</span>}{item.status !== 0 && <span className="text-state-error">{item.status === 1 ? "Đã hủy" : "Đã hoàn tiền"}</span>}</div></div><div className="ml-auto text-right"><strong className="font-data text-sm text-brand-primary sm:text-base">{money(item.lineTotal)}</strong>{item.isNegotiated && item.publicUnitPriceSnapshot > item.finalUnitPrice && <p className="font-data text-[10px] text-foreground-secondary">Giá gốc: {money(item.publicUnitPriceSnapshot)}/{unitLabel(item.sellingUnitSnapshot)}</p>}</div></div>{item.notes && <p className="mt-3 border-t border-brand-primary/10 pt-2 text-xs leading-6 text-foreground-secondary"><strong className="text-brand-secondary">Lời nhắn cho sạp:</strong> {item.notes}</p>}</li>)}</ul>
    {group.cancellationReason && <p className="mt-3 text-xs text-state-error">Lý do hủy: {group.cancellationReason}</p>}
    <div className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-card bg-ready-background p-4"><div className="min-w-0"><p className="font-data text-[10px] font-bold uppercase text-foreground-secondary">Mã đơn tại sạp</p><div className="flex items-center gap-1"><span className="break-all font-data text-xl font-bold tracking-wider text-brand-primary">{group.fulfillmentNo}</span><Button variant="ghost" size="icon" className="shrink-0 text-brand-primary" onClick={() => void copyReference()} aria-label={`Sao chép mã đơn ${group.fulfillmentNo}`}><OrderIcon kind="copy" className="size-4" /></Button></div></div><div className="text-xs text-foreground-secondary">{group.pickedUpAt ? <p>Đã nhận: {formatDate(group.pickedUpAt)}</p> : group.readyAt ? <p>Sẵn sàng từ: {formatDate(group.readyAt)}</p> : <p>Theo dõi trạng thái trước khi ghé</p>}<p className="mt-1">Mã đối chiếu đơn · Chưa có mã PIN / QR</p></div></div>
  </article>;
}
