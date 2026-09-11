import { ProductListingSection } from "@/features/product-listings/components";

interface ProductsPageProps {
  searchParams: Promise<{ categoryId?: string }>;
}

export default async function ProductsPage({ searchParams }: ProductsPageProps) {
  const { categoryId } = await searchParams;

  return (
    <div className="pt-xl md:pt-2xl">
      <ProductListingSection
        categoryId={categoryId}
        title={categoryId ? "Sản phẩm trong danh mục" : "Tất cả sản phẩm"}
      />
    </div>
  );
}
