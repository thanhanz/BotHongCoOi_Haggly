"use client";

import { forwardRef, type ComponentPropsWithoutRef, type ComponentRef } from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { cn } from "@/shared/lib/cn";

export const Dialog = DialogPrimitive.Root;
export const DialogTrigger = DialogPrimitive.Trigger;
export const DialogClose = DialogPrimitive.Close;

export const DialogOverlay = forwardRef<
  ComponentRef<typeof DialogPrimitive.Overlay>,
  ComponentPropsWithoutRef<typeof DialogPrimitive.Overlay>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Overlay
    ref={ref}
    className={cn("fixed inset-0 z-50 bg-surface-canvas/75 backdrop-blur-sm", className)}
    {...props}
  />
));
DialogOverlay.displayName = "DialogOverlay";

export const DialogContent = forwardRef<
  ComponentRef<typeof DialogPrimitive.Content>,
  ComponentPropsWithoutRef<typeof DialogPrimitive.Content>
>(({ className, children, ...props }, ref) => (
  <DialogPrimitive.Portal>
    <DialogOverlay />
    <DialogPrimitive.Content
      ref={ref}
      className={cn(
        "fixed left-1/2 top-1/2 z-50 grid max-h-[calc(100vh-2rem)] w-[calc(100%-2rem)] max-w-lg -translate-x-1/2 -translate-y-1/2 gap-sm overflow-y-auto rounded-modal border border-border-subtle bg-surface-raised p-md shadow-overlay outline-none focus-visible:ring-2 focus-visible:ring-brand-primary md:p-lg",
        className,
      )}
      {...props}
    >
      {children}
      <DialogPrimitive.Close
        aria-label="Đóng"
        className="absolute right-sm top-sm flex size-10 items-center justify-center rounded-control text-xl text-foreground-secondary outline-none hover:bg-surface-sunken hover:text-foreground-primary focus-visible:ring-2 focus-visible:ring-brand-primary"
      >
        <span aria-hidden="true">×</span>
      </DialogPrimitive.Close>
    </DialogPrimitive.Content>
  </DialogPrimitive.Portal>
));
DialogContent.displayName = "DialogContent";

export const DialogHeader = ({ className, ...props }: ComponentPropsWithoutRef<"div">) => (
  <div className={cn("grid gap-xs pr-xl", className)} {...props} />
);

export const DialogFooter = ({ className, ...props }: ComponentPropsWithoutRef<"div">) => (
  <div className={cn("flex flex-col-reverse gap-xs pt-sm sm:flex-row sm:justify-end", className)} {...props} />
);

export const DialogTitle = forwardRef<
  ComponentRef<typeof DialogPrimitive.Title>,
  ComponentPropsWithoutRef<typeof DialogPrimitive.Title>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Title ref={ref} className={cn("text-xl font-semibold leading-relaxed", className)} {...props} />
));
DialogTitle.displayName = "DialogTitle";

export const DialogDescription = forwardRef<
  ComponentRef<typeof DialogPrimitive.Description>,
  ComponentPropsWithoutRef<typeof DialogPrimitive.Description>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Description ref={ref} className={cn("text-sm leading-relaxed text-foreground-secondary", className)} {...props} />
));
DialogDescription.displayName = "DialogDescription";
