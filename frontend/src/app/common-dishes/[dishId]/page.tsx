import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { DishIngredients } from "@/features/dish-discovery/components";

export const metadata: Metadata = { title: "Chọn nguyên liệu | Haggly" };

export default async function DishIngredientsPage({ params, searchParams }: { params: Promise<{ dishId: string }>; searchParams: Promise<{ q?: string | string[] }> }) {
  const [{ dishId }, search] = await Promise.all([params, searchParams]);
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(dishId)) notFound();
  const query = (typeof search.q === "string" ? search.q : "").trim().slice(0, 200);
  return <DishIngredients dishId={dishId} query={query} />;
}
