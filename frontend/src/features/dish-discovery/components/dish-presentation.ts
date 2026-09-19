import type { ProposalListing } from "../api";

export const money = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });
export const quantityFormat = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 6 });

const units: Record<string, string> = { KG: "kg", GRAM: "g", PIECE: "cái", BUNCH: "bó", BOX: "hộp", PACK: "gói", LITER: "lít", OTHER: "đơn vị" };
export function unitLabel(unit: string) { return units[unit.toUpperCase()] ?? unit; }
export function minimumQuantity(listing: ProposalListing) { return listing.minimumOrderQuantity > 0 ? listing.minimumOrderQuantity : 1; }
export function canSelect(listing: ProposalListing) { return listing.availableQuantity >= minimumQuantity(listing); }
export function listingName(listing: ProposalListing) { return listing.displayName?.trim() || listing.productName; }
export function searchHref(query: string) { return query ? `/common-dishes?q=${encodeURIComponent(query)}` : "/common-dishes"; }

// Illustrative dish photography from the supplied design; never listing/product photos.
const dishImages: Record<string, string> = {
  "bun bo hue": "https://lh3.googleusercontent.com/aida-public/AB6AXuBFP3OGi9b_sHQHhWqXT68Wi9i2proHPBngyxkfVD9rVFiTg7Ps-_tLOyW9DZxvkWXhg5Iuqa6OOKODZ2Jb6n-L7vGg-CWL68IvylfaIqAZrKY0NIjsFtCo1pWJheDth6JHC4WyQzrezglr8dZ0nd0PwCdwOzEPuZLWUkLlrWV7O_cb6xV7QmV53xdyMXWSYvTuVyCsDjYcGAHT7-8ZgoEN5-Uzomd8pp7tCOBByOEJZ8nntLjdFg06CirPNBjftPdDL5tEoftQ76k",
  "bun rieu cua dong": "https://lh3.googleusercontent.com/aida-public/AB6AXuDIoGco5B2Dzs0JFIolHs6jQeMlFCksL0fIu5yDqCN3qwVz-4qxW4wNi9uNv62Nt4LDGHQsWVxdCz4s9nBU3YlAQRj96ZFmG9pNvdnCI14wDdCui3aUwL8SsdozgHMcnp-d8HLO527RQIYs1LZvKujI3eSR83CQprCGQ0nJZcUyfJFKCH86SzVC7iLgHXTXsseSpmYx19VFWHXjiYLPtG0Tf4ps1UgypN7tvJG8Y99Mxh4g18ZD-tcyyJ-5vLLus9NhmRBVMKJt8qI",
  "bun thit nuong sa": "https://lh3.googleusercontent.com/aida-public/AB6AXuCmszUX8RDjN9JpZxXz-H_7KLXOJQ47dgnmuzDVh8zA-TQghlQPBC-cvYRp0tJlMznjjpf6AnXv0--jRYIXNBBdC2fpQfyw0zWtdQrJs6o1gddE_nayUltxAhhABpJ94vO8RV9iBzDVbBnGUym83bGfqdud1dRLfEuhk4tH5wDw5p7h-KndGq6b_iHjVmSkyp_Walg3pptZ5mYLiAoshZrkD4ZsjcGqEwfe1cCbyobM5Ptme7XtSUomz0_kwDp6YaMQrPdGwIidgXU",
  "bun moc suon non": "https://lh3.googleusercontent.com/aida-public/AB6AXuBZr2FJbXUU07WXSDC4xQNG5UqHfYhD4BGe8_fJjGiUKzpLx4Sr3Ymzd5VHUMgOoMg5kV77QC-LEaAw8JrrLqULqIozRXzcwOVRH-Rfgvgz6H5iMxe52KZGeNQWjZYQ20gkAXOSf-UaRGZA56rNoh6H-ZPRHZDcNBd6KXY_RT5HoVlT_dCf5uijF1qQveJbVAZX6hrMKkEb3Pi95w9RF8yIjXe_sWfouzFyvACGBZiALrH8hP1BdnMOKVGWJmupDRW76pgnuHJMhXk",
  "bun ca doc mung": "https://lh3.googleusercontent.com/aida-public/AB6AXuB3GEjW7YaCa-_ibCZLDBb_GJbZWC9X6GGnm3H1mKr0LV3Hu1TJJHBDVnqMW0It21TUuLPlKwPxmekVxNBPAMRbCu8RbZSgld1KeEDmOOsgkM70gsYkWBz_WyNnSAiveHpYQLYFD-VqnX874993cwBIJwfuzfKrodqqF1G4QGT5ahgkIPOuvCg5PxupIglYn1IHxpsSkixtOaBr3SWEBBuUds2JDQKtpeT_MuJtzfKqJISbIAqHD_CWuATcl4kUULplqIPisoqd_0g",
};

export function dishImage(name: string) {
  return dishImages[name.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/gi, "d").toLowerCase().trim()];
}
