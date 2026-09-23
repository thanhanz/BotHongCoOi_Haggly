import { forwardRef, type HTMLAttributes } from "react";
import { cn } from "@/shared/lib/cn";

export const Container = forwardRef<HTMLDivElement, HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={cn("mx-auto w-full max-w-[1360px] px-sm md:px-md xl:px-10", className)}
      {...props}
    />
  ),
);

Container.displayName = "Container";
