import { CategorySection } from "@/features/categories/components";

export default function Home() {
  return (
    <>
      {/* <section className="overflow-hidden bg-brand-primary py-xl text-foreground-inverse md:py-2xl">
        <Container className="grid items-center gap-lg lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)]">
          <div className="max-w-3xl">
            <Typography as="p" variant="labelLg" className="text-brand-tertiary">
              Haggly · Chợ truyền thống trực tuyến
            </Typography>
            <Typography as="h1" variant="display" className="mt-sm text-foreground-inverse">
              Đi chợ tươi, chuẩn vị mẹ nấu.
            </Typography>
            <Typography variant="bodyLg" className="mt-sm max-w-2xl text-white/75">
              Khám phá thực phẩm tươi ngon từ những sạp quen, ngay trong khu chợ Việt gần bạn.
            </Typography>
            <Link
              href="#categories"
              className="mt-md inline-flex h-12 items-center rounded-control bg-brand-secondary px-md font-data text-sm font-semibold text-foreground-inverse transition hover:bg-brand-secondary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-brand-primary"
            >
              Khám phá danh mục
            </Link>
          </div>

          <div aria-hidden="true" className="relative hidden min-h-64 overflow-hidden rounded-modal bg-[#f7efe2] p-md shadow-overlay lg:block">
            <div className="absolute -right-10 -top-10 size-40 rounded-full bg-brand-tertiary/35" />
            <div className="absolute -bottom-14 -left-8 size-48 rounded-full bg-brand-secondary/25" />
            <div className="relative grid h-full grid-cols-2 gap-sm">
              {['Rau củ', 'Trái cây', 'Thịt cá', 'Gia vị'].map((label, index) => (
                <div key={label} className="flex min-h-24 items-end rounded-card bg-white/90 p-sm shadow-card">
                  <span className={`mr-xs size-3 rounded-full ${index % 2 === 0 ? 'bg-brand-primary' : 'bg-brand-secondary'}`} />
                  <span className="font-data text-sm font-semibold text-foreground-primary">{label}</span>
                </div>
              ))}
            </div>
          </div>
        </Container>
      </section> */}

      <CategorySection />
    </>
  );
}
