import { forwardRef, type HTMLAttributes } from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/shared/lib/cn";

export const badgeVariants = cva(
  "inline-flex w-fit items-center whitespace-nowrap rounded-badge font-data font-semibold leading-none",
  {
    variants: {
      variant: {
        default: "bg-surface-sunken text-foreground-primary",
        primary: "bg-brand-primary text-foreground-inverse",
        secondary: "bg-brand-secondary text-foreground-inverse",
        tertiary: "bg-brand-tertiary text-foreground-primary",
        neutral: "border border-border-subtle bg-surface-container text-foreground-secondary",
        success: "bg-ready-background text-ready-text",
        warning: "bg-preparing-background text-preparing-text",
        error: "bg-state-error-surface text-state-error",
        preparing: "bg-preparing-background text-preparing-text",
        ready: "bg-ready-background text-ready-text",
        awaitingPickup: "bg-awaiting-pickup-background text-awaiting-pickup-text",
        unit: "rounded-pill border border-border-prominent bg-surface-raised text-foreground-primary",
        stall: "rounded-pill bg-surface-sunken text-brand-primary",
      },
      size: {
        sm: "min-h-6 px-xs py-2xs text-xs",
        md: "min-h-8 px-sm py-xs text-sm",
      },
    },
    defaultVariants: { variant: "default", size: "md" },
  },
);

export interface BadgeProps
  extends HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {}

export const Badge = forwardRef<HTMLSpanElement, BadgeProps>(({ className, variant, size, ...props }, ref) => (
  <span ref={ref} className={cn(badgeVariants({ variant, size }), className)} {...props} />
));
Badge.displayName = "Badge";
