import { forwardRef, type HTMLAttributes } from "react";
import { cn } from "@/shared/lib/cn";

export const Divider = forwardRef<HTMLHRElement, HTMLAttributes<HTMLHRElement>>(
  ({ className, ...props }, ref) => (
    <hr ref={ref} className={cn("m-0 border-0 border-t border-border-subtle", className)} {...props} />
  ),
);

Divider.displayName = "Divider";
