import type { PublicStallDetails } from "@/features/stalls/api";
import { Button } from "@/shared/ui/button";
import { Card } from "@/shared/ui/card";
import type { Order } from "../api";
import { OrderIcon } from "./OrderIcon";
import { formatDate, formatMoney, fulfillmentLabels, orderLabels, unitLabel } from "./order-presentation";

export function OrderSummary({ order, stalls, onRefresh, onNotice }: {
  order: Order; stalls: Record<string, PublicStallDetails>; onRefresh: () => void; onNotice: (message: string) => void;
}) {
  const money = (value: number) => formatMoney(value, order.currency);
  const groups = order.fulfillments;
  const stallCount = new Set(groups.map(group => group.stallId)).size;
  const readyCount = groups.filter(group => group.status === 4).length;
  const cancelled = order.status === 7;
  const completed = order.status === 6;
  const nameFor = (id: string) => stalls[id]?.name ?? `Sạp ${id.slice(0, 8)}`;

  function downloadList() {
    const lines = [
      `Haggly · Đơn ${order.orderNo}`, `Trạng thái: ${orderLabels[order.status]}`, "",
      ...groups.flatMap(group => [
        nameFor(group.stallId), stalls[group.stallId]?.locationDescription ?? "Chưa có thông tin vị trí",
        `Mã đơn tại sạp: ${group.fulfillmentNo}`, `Trạng thái: ${fulfillmentLabels[group.status]}`,
        ...group.items.map(item => `${item.productNameSnapshot} · ${item.finalQuantity} ${unitLabel(item.sellingUnitSnapshot)} · ${money(item.lineTotal)}${item.status === 1 ? " (Đã hủy)" : item.status === 2 ? " (Đã hoàn tiền)" : ""}${item.notes ? ` · ${item.notes}` : ""}`), "",
      ]), `Tổng đơn: ${money(order.totalToCharge)}`, `Đã thanh toán: ${money(order.totalPaid)}`,
      "Thông tin tại thời điểm tải. Kiểm tra trạng thái mới nhất trên Haggly trước khi ghé sạp.",
    ];
    const url = URL.createObjectURL(new Blob(["\uFEFF", lines.join("\n")], { type: "text/plain;charset=utf-8" }));
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `Haggly-${order.id}.txt`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    onNotice("Đã tải danh sách sạp và thông tin đơn hàng.");
  }

  return <Card className="p-5 md:p-6">
    <h2 className="mb-6 text-xl font-bold text-brand-primary">Tổng Đơn Gom {stallCount} Sạp</h2>
    <dl className="space-y-3 text-sm">{groups.map(group => <div key={group.id} className="flex justify-between gap-4"><dt className="text-foreground-secondary">{nameFor(group.stallId)} ({group.items.length} món)</dt><dd className="shrink-0 font-data font-bold">{money(group.finalAmount)}</dd></div>)}</dl>
    <p className="mt-4 flex items-center gap-2 text-xs font-semibold text-brand-primary"><OrderIcon kind="bag" className="size-4" />Nhận hàng trực tiếp tại từng sạp</p>
    <div className="my-5 rounded-card bg-ready-background p-4"><div className="flex flex-wrap items-baseline justify-between gap-2"><h3 className="font-data text-xs font-bold uppercase text-foreground-secondary">Tổng tiền đơn hàng</h3><strong className="font-data text-3xl text-brand-primary">{money(order.totalToCharge)}</strong></div><p className="mt-2 flex justify-between gap-2 text-xs"><span>Đã thanh toán</span><strong className="font-data">{money(order.totalPaid)}</strong></p></div>
    <div className="rounded-card bg-surface-container p-4">
      <h3 className="flex items-center gap-2 text-sm font-semibold text-brand-secondary"><OrderIcon kind="clock" />Tình trạng ghé lấy</h3>
      <p className="mt-2 text-lg font-bold">{cancelled ? "Đơn hàng đã hủy" : completed ? "Bạn đã nhận xong đơn hàng" : `${readyCount}/${groups.length} đơn tại sạp sẵn sàng`}</p>
      {!cancelled && !completed && <p className="mt-1 text-xs leading-5 text-foreground-secondary">Giờ ghé lấy chưa được xác nhận. Vui lòng kiểm tra trạng thái hoặc liên hệ sạp trước khi đến.</p>}
      {order.paymentDueAt && order.status === 3 && <p className="mt-3 text-xs">Hạn thanh toán: <strong>{formatDate(order.paymentDueAt)}</strong></p>}
    </div>
    <fieldset disabled aria-describedby="payment-unavailable" className="mt-5 space-y-2">
      <legend className="mb-2 text-sm font-semibold">Phương thức thanh toán</legend>
      {["VietQR Chợ Nhanh", "Tiền mặt trực tiếp tại từng sạp"].map((label, index) => <label key={label} className="flex items-start gap-3 rounded-card bg-surface-container p-4 text-sm text-foreground-secondary"><input type="radio" name="order-payment" className="mt-1 size-4 shrink-0 accent-brand-primary" /><span><strong className="text-xs">{label}</strong><span className="mt-1 block text-xs">{index === 0 ? "Thanh toán gộp cho các sạp trong đơn." : "Trao đổi với tiểu thương khi ghé nhận hàng."}</span></span></label>)}
    </fieldset>
    <p id="payment-unavailable" className="mt-2 text-xs leading-5 text-foreground-secondary">Chưa hỗ trợ chọn phương thức thanh toán trên trang này.</p>
    <div className="mt-6 space-y-2">
      <Button className="h-auto min-h-12 w-full whitespace-normal py-3" disabled={!groups.length || cancelled} onClick={() => document.getElementById("pickup-guide")?.focus()}><OrderIcon kind="walk" />Xem Danh Sách Ghé Nhận</Button>
      <Button variant="ghost" className="h-auto min-h-10 w-full whitespace-normal bg-ready-background py-2 text-xs" onClick={downloadList}><OrderIcon kind="download" className="size-4" />Tải Danh Sách Sạp & Mã Đơn</Button>
      <Button variant="ghost" className="w-full text-xs" onClick={onRefresh}>↻ Cập nhật trạng thái đơn</Button>
    </div>
    {order.placedAt && <p className="mt-3 text-center text-xs text-foreground-secondary">Đặt lúc {formatDate(order.placedAt)}</p>}
  </Card>;
}
