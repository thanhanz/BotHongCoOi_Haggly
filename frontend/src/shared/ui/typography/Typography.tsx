import { createElement, type HTMLAttributes, type ElementType } from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/shared/lib/cn";

export const typographyVariants = cva("text-foreground-primary", {
  variants: {
    variant: {
      display: "text-5xl leading-[1.18] font-bold tracking-[-0.03em] md:text-6xl",
      displayMobile: "text-4xl leading-[1.2] font-bold tracking-[-0.025em]",
      headlineLg: "text-4xl leading-[1.25] font-bold tracking-[-0.02em] md:text-5xl",
      headlineLgMobile: "text-3xl leading-[1.3] font-bold tracking-[-0.015em]",
      headlineMd: "text-3xl leading-[1.35] font-semibold tracking-[-0.015em]",
      headlineSm: "text-2xl leading-[1.4] font-semibold",
      titleMd: "text-xl leading-[1.5] font-semibold",
      bodyLg: "text-lg leading-[1.75] font-normal",
      bodyMd: "text-base leading-[1.7] font-normal",
      bodySm: "text-sm leading-[1.65] font-normal",
      labelLg: "font-data text-base leading-[1.5] font-semibold",
      labelMd: "font-data text-sm leading-[1.5] font-semibold",
      labelSm: "font-data text-xs leading-[1.5] font-semibold tracking-[0.02em]",
      priceDisplay: "font-data text-4xl leading-[1.25] font-bold tabular-nums tracking-[-0.02em]",
      priceTabular: "font-data text-base leading-[1.5] font-semibold tabular-nums",
    },
  },
  defaultVariants: { variant: "bodyMd" },
});

export interface TypographyProps
  extends HTMLAttributes<HTMLElement>,
    VariantProps<typeof typographyVariants> {
  as?: ElementType;
}

export function Typography({ as = "p", variant, className, ...props }: TypographyProps) {
  return createElement(as, {
    className: cn(typographyVariants({ variant }), className),
    ...props,
  });
}
