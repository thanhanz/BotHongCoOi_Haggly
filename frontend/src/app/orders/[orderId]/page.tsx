import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { OrderDetails } from "@/features/orders/components";

export const metadata: Metadata = { title: "Chi tiết đơn hàng · Haggly" };

export default async function OrderDetailsPage({ params }: { params: Promise<{ orderId: string }> }) {
  const { orderId } = await params;
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(orderId)) notFound();
  return <OrderDetails key={orderId} orderId={orderId} />;
}
