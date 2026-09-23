import { forwardRef, type ButtonHTMLAttributes } from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/shared/lib/cn";

export const buttonVariants = cva(
  "inline-flex items-center justify-center gap-xs whitespace-nowrap font-data text-sm font-semibold transition-colors outline-none focus-visible:ring-2 focus-visible:ring-brand-primary focus-visible:ring-offset-2 focus-visible:ring-offset-surface-canvas disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        primary: "bg-brand-primary text-foreground-inverse hover:bg-brand-primary-hover active:bg-brand-primary-active",
        secondary: "bg-brand-secondary text-foreground-inverse hover:bg-brand-secondary-hover active:bg-brand-secondary-active",
        tertiary: "bg-brand-tertiary text-foreground-primary hover:bg-brand-tertiary-hover active:bg-brand-tertiary-active",
        outline: "border border-border-prominent bg-surface-raised text-foreground-primary hover:bg-surface-sunken active:bg-surface-container",
        ghost: "text-foreground-primary hover:bg-surface-sunken active:bg-surface-container",
        destructive: "bg-state-error text-foreground-inverse hover:bg-state-error-hover active:bg-state-error-active",
        bargain: "rounded-pill bg-brand-secondary text-foreground-inverse hover:bg-brand-secondary-hover active:bg-brand-secondary-active",
      },
      size: {
        sm: "h-8 rounded-control px-sm",
        md: "h-10 rounded-control px-md",
        lg: "h-12 rounded-control px-lg text-base",
        icon: "size-10 rounded-control p-0",
      },
    },
    compoundVariants: [
      { variant: "bargain", size: "sm", className: "rounded-pill" },
      { variant: "bargain", size: "md", className: "rounded-pill" },
      { variant: "bargain", size: "lg", className: "rounded-pill" },
    ],
    defaultVariants: {
      variant: "primary",
      size: "md",
    },
  },
);

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof buttonVariants> {
  loading?: boolean;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, children, disabled, loading = false, type = "button", variant, size, ...props }, ref) => (
    <button
      ref={ref}
      type={type}
      className={cn(buttonVariants({ variant, size }), className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {loading && (
        <span
          aria-hidden="true"
          className="size-sm animate-spin rounded-full border-2 border-current border-r-transparent"
        />
      )}
      {children}
    </button>
  ),
);

Button.displayName = "Button";
