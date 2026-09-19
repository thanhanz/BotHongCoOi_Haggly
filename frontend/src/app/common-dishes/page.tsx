import type { Metadata } from "next";
import { DishSearch } from "@/features/dish-discovery/components";

export const metadata: Metadata = { title: "Tìm món để đi chợ | Haggly", description: "Tìm món yêu thích và chọn nguyên liệu từ các sạp tại Haggly." };

export default async function CommonDishesPage({ searchParams }: { searchParams: Promise<{ q?: string | string[] }> }) {
  const params = await searchParams;
  const query = (typeof params.q === "string" ? params.q : "").trim();
  return <DishSearch key={query} query={query} />;
}
