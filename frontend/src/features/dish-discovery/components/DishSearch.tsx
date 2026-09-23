"use client";

import { useEffect, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { searchCommonDishes, type CommonDishCandidate } from "../api";
import { Button } from "@/shared/ui/button";
import { Container } from "@/shared/ui/container";
import { Input } from "@/shared/ui/input";
import { Select } from "@/shared/ui/select";
import { DishCard } from "./DishCard";
import { DishIcon } from "./DishIcon";
import { searchHref } from "./dish-presentation";

export function DishSearch({ query }: { query: string }) {
  const router = useRouter();
  const [input, setInput] = useState(query);
  const [candidates, setCandidates] = useState<CommonDishCandidate[]>([]);
  const [loading, setLoading] = useState(!!query && query.length <= 200);
  const [error, setError] = useState("");
  const [retry, setRetry] = useState(0);
  const [category, setCategory] = useState("");
  const [sort, setSort] = useState("relevance");
  useEffect(() => {
    if (!query || query.length > 200) return;
    const controller = new AbortController();
    void searchCommonDishes(query, { signal: controller.signal }).then(result => {
      if (!controller.signal.aborted) { setCandidates(result.candidates); setLoading(false); }
    }).catch(() => {
      if (!controller.signal.aborted) { setError("Chưa tìm được món lúc này. Vui lòng thử lại."); setLoading(false); }
    });
    return () => controller.abort();
  }, [query, retry]);
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const next = input.trim();
    if (!next || next.length > 200) return;
    if (next === query) { setLoading(true); setError(""); setRetry(value => value + 1); }
    else router.push(searchHref(next));
  }
  const categories = [...new Set(candidates.flatMap(dish => dish.category ? [dish.category] : []))];
  const visible = candidates.filter(dish => !category || dish.category === category);
  if (sort === "name") visible.sort((a, b) => a.name.localeCompare(b.name, "vi"));
  return (
    <div className="min-h-[70vh] bg-ready-background/35 py-6 md:py-8">
      <Container className="max-w-none space-y-7 px-4 md:px-6 xl:px-8">
        <nav aria-label="Đường dẫn" className="flex flex-wrap gap-2 text-xs text-foreground-secondary"><Link href="/" className="hover:underline">Trang chủ</Link><span aria-hidden="true">›</span><Link href="/products" className="hover:underline">Dạo chợ</Link><span aria-hidden="true">›</span><span aria-current="page">Tìm món để nấu</span></nav>
        <section className="relative overflow-hidden rounded-card border border-border-subtle bg-surface-raised p-6 shadow-card sm:p-10">
          <div aria-hidden="true" className="absolute -right-12 -top-16 size-72 rounded-full bg-ready-background/70 blur-3xl" />
          <div className="relative w-full">
            <p className="mb-3 inline-flex items-center gap-2 rounded-pill border border-brand-primary/15 bg-ready-background/60 px-3 py-1 font-data text-xs text-brand-primary"><DishIcon className="size-3.5" /> HÔM NAY ĂN GÌ?</p>
            <h1 className="text-3xl font-bold tracking-tight text-brand-primary sm:text-4xl">Hôm nay mình nấu món gì?</h1>
            <p className="mt-3 text-sm leading-relaxed text-foreground-secondary">Gõ tên món ăn yêu thích, Haggly sẽ tra cứu danh sách nguyên liệu và kết nối với các sạp đang có hàng. Chọn món, gom nguyên liệu, sẵn sàng đi chợ.</p>
            <form onSubmit={submit} role="search" aria-label="Tìm món ăn" className="mt-6 flex flex-col gap-2 rounded-control border-2 border-brand-primary/20 bg-ready-background/60 p-2 focus-within:border-brand-primary sm:flex-row">
              <Input type="search" aria-label="Tên món ăn" placeholder="Ví dụ: Bún bò Huế, canh chua…" value={input} onChange={event => setInput(event.target.value)} maxLength={200} required leftIcon={<DishIcon kind="search" />} containerClassName="min-w-0 flex-1" className="h-12 bg-surface-raised md:h-12" />
              <Button type="submit" size="lg" disabled={!input.trim()}><DishIcon kind="search" /> Tìm món</Button>
            </form>
            <p className="mt-3 text-xs text-foreground-secondary"><span className="font-semibold text-brand-secondary">Gợi ý: </span>Nhập tên món đầy đủ. Có thể gõ không dấu, ví dụ “bun bo hue”.</p>
          </div>
        </section>
        <div className="flex items-start gap-3 rounded-control border border-brand-primary/15 bg-ready-background/60 p-4 text-sm"><DishIcon kind="leaf" className="mt-1 size-6 shrink-0 text-brand-primary" /><p className="flex-1"><strong className="text-brand-primary">Đi chợ theo món, chọn hàng theo sạp.</strong> Giá và lượng hàng được lấy từ các sạp. Nguyên liệu được nhóm theo từng sạp để bạn thuận tiện ghé nhận.</p><span className="hidden shrink-0 rounded bg-surface-raised px-2 py-1 font-data text-xs sm:block">Tự lấy tại sạp</span></div>
        {query.length > 200 ? <p role="alert">Tên món không được vượt quá 200 ký tự. Vui lòng nhập lại.</p> : loading ? <div role="status" className="rounded-card bg-surface-raised p-8 text-center text-foreground-secondary">Đang tìm món phù hợp…</div> : error ? <div role="alert" className="rounded-card border border-border-prominent bg-surface-raised p-8 text-center"><p>{error}</p><Button className="mt-4" onClick={() => { setError(""); setLoading(true); setRetry(value => value + 1); }}>Thử lại</Button></div> : !query ? <section className="rounded-card border border-dashed border-brand-primary/20 p-10 text-center"><DishIcon className="mx-auto mb-4 size-12 text-brand-primary" /><h2 className="text-xl font-semibold">Bữa ngon bắt đầu từ món bạn thích</h2><p className="mt-2 text-sm text-foreground-secondary">Tìm tên món để xem các nguyên liệu và sạp phù hợp.</p><div className="mt-5 flex flex-wrap justify-center gap-2">{["Bún bò Huế", "Canh chua", "Thịt kho tàu"].map(name => <Link key={name} href={searchHref(name)} className="rounded-pill border border-border-prominent bg-surface-raised px-4 py-2 text-sm text-brand-primary hover:bg-ready-background">{name}</Link>)}</div></section> : <>
          <div className="flex flex-wrap items-center justify-between gap-4 border-b border-border-subtle pb-4"><div className="flex flex-wrap items-center gap-3"><h2 aria-live="polite" className="text-xl font-bold">{visible.length} món phù hợp</h2><span className="rounded-pill bg-awaiting-pickup-background px-3 py-1 text-sm text-awaiting-pickup-text">{query}</span><Link href="/common-dishes" className="text-xs text-foreground-secondary underline">Xóa tìm kiếm</Link></div><Select aria-label="Sắp xếp món ăn" value={sort} onChange={event => setSort(event.target.value)}><option value="relevance">Theo kết quả tìm kiếm</option><option value="name">Tên món A–Z</option></Select></div>
          {categories.length > 0 && <div className="flex flex-wrap gap-2" aria-label="Lọc loại món">{["", ...categories].map(value => <Button key={value} size="sm" variant={category === value ? "primary" : "outline"} className="rounded-pill" aria-pressed={category === value} onClick={() => setCategory(value)}>{value || "Tất cả"} ({value ? candidates.filter(dish => dish.category === value).length : candidates.length})</Button>)}</div>}
          {visible.length ? <div className="grid grid-cols-[repeat(auto-fit,minmax(min(100%,22rem),1fr))] gap-6">{visible.map(dish => <DishCard key={dish.dishId} dish={dish} query={query} />)}</div> : <div className="rounded-card bg-surface-raised p-10 text-center"><h2 className="text-xl font-semibold">Chưa tìm thấy món phù hợp</h2><p className="mt-2 text-sm text-foreground-secondary">Hãy thử tên đầy đủ như “Bún bò Huế” thay vì “bún”, hoặc chọn một món khác.</p></div>}
        </>}
        <aside className="flex flex-wrap items-center justify-between gap-4 rounded-card border border-border-subtle bg-surface-raised p-6"><div><h2 className="font-semibold">Một bữa ngon, nhiều sạp quen</h2><p className="mt-1 text-sm text-foreground-secondary">Bạn cũng có thể tự chọn thêm rau, thịt và gia vị cho bữa ăn của mình.</p></div><Link href="/products" className="rounded-pill bg-brand-secondary px-5 py-3 font-data text-sm font-semibold text-white hover:bg-brand-secondary-hover">Dạo chợ hôm nay →</Link></aside>
      </Container>
    </div>
  );
}
