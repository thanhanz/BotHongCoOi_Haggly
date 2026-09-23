import type { ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

interface FieldProps {
  children: ReactNode;
  controlId: string;
  descriptionId?: string;
  error?: string;
  helperText?: string;
  label?: string;
  required?: boolean;
  className?: string;
}

export function Field({
  children,
  className,
  controlId,
  descriptionId,
  error,
  helperText,
  label,
  required,
}: FieldProps) {
  return (
    <div className={cn("grid gap-xs", className)}>
      {label && (
        <label htmlFor={controlId} className="font-data text-sm font-semibold text-foreground-primary">
          {label}
          {required && (
            <span className="ml-2xs text-state-error" aria-hidden="true">
              *
            </span>
          )}
        </label>
      )}
      {children}
      {(error || helperText) && (
        <p
          id={descriptionId}
          className={cn("m-0 text-sm leading-relaxed", error ? "text-state-error" : "text-foreground-secondary")}
        >
          {error ?? helperText}
        </p>
      )}
    </div>
  );
}
