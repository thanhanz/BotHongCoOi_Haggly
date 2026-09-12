import { CategorySection } from "@/features/categories/components";
import { ProductListingSection } from "@/features/product-listings/components";
import { StallInformation } from "@/features/stalls/components";

interface StallDetailsPageProps {
  params: Promise<{ stallId: string }>;
  searchParams: Promise<{ categoryId?: string }>;
}

export default async function StallDetailsPage({ params, searchParams }: StallDetailsPageProps) {
  const [{ stallId }, { categoryId }] = await Promise.all([params, searchParams]);

  return (
    <>
      <StallInformation stallId={stallId} />
      <CategorySection stallId={stallId} selectedCategoryId={categoryId} />
      <ProductListingSection
        stallId={stallId}
        categoryId={categoryId}
        title={categoryId ? "Sản phẩm theo danh mục" : "Sản phẩm của sạp"}
        useDemoData={false}
      />
    </>
  );
}
