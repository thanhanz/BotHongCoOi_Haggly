import type { Metadata } from "next";
import { MarketplaceSearch } from "@/features/search/components";

export const metadata: Metadata = {
  title: "Tìm sản phẩm và sạp | Haggly",
  description: "Tìm sạp quen và sản phẩm đang có hàng tại Haggly.",
};

function positiveInteger(value: string | string[] | undefined, fallback: number, max = 2_147_483_647) {
  if (typeof value !== "string" || !/^\d+$/.test(value)) return fallback;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed >= 1 && parsed <= max ? parsed : fallback;
}

export default async function SearchPage({ searchParams }: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const values = await searchParams;
  const params = {
    q: typeof values.q === "string" ? values.q.trim() : "",
    stallPage: positiveInteger(values.stallPage, 1),
    stallPageSize: positiveInteger(values.stallPageSize, 5, 20),
    productPage: positiveInteger(values.productPage, 1),
    productPageSize: positiveInteger(values.productPageSize, 20, 100),
  };

  return <MarketplaceSearch key={JSON.stringify(params)} params={params} />;
}
