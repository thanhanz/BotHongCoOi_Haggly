import type { OrderStatus, StallFulfillmentStatus } from "../api";

export const orderLabels: Record<OrderStatus, string> = {
  0: "Đơn nháp", 1: "Đang thương lượng", 2: "Đã chốt đơn", 3: "Chờ thanh toán",
  4: "Đã thanh toán", 5: "Đã nhận một phần", 6: "Đã hoàn tất", 7: "Đã hủy",
};

export const fulfillmentLabels: Record<StallFulfillmentStatus, string> = {
  0: "Chờ xác nhận", 1: "Đang thương lượng", 2: "Đã chốt với sạp",
  3: "Đang chuẩn bị", 4: "Đã sẵn sàng · Mời ghé nhận", 5: "Đã nhận hàng", 6: "Đã hủy",
};

const units: Record<string, string> = {
  KG: "kg", GRAM: "g", PIECE: "cái", BUNCH: "bó", BOX: "hộp", PACK: "gói", LITER: "lít", OTHER: "đơn vị",
};
export function unitLabel(unit: string) { return units[unit.toUpperCase()] ?? unit; }

export function formatMoney(value: number, currency: string) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency }).format(value);
}

export function formatDate(value: string) {
  return new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short", timeZone: "Asia/Ho_Chi_Minh" }).format(new Date(value));
}
