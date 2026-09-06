import { forwardRef, useId, type InputHTMLAttributes, type ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

export interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label?: ReactNode;
  description?: ReactNode;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, description, id, label, ...props }, ref) => {
    const generatedId = useId();
    const controlId = id ?? generatedId;
    const descriptionId = description ? `${controlId}-description` : undefined;

    return (
      <div className="flex items-start gap-xs">
        <input
          ref={ref}
          type="checkbox"
          id={controlId}
          aria-describedby={descriptionId}
          className={cn(
            "mt-0.5 size-6 shrink-0 cursor-pointer accent-brand-primary outline-none focus-visible:ring-2 focus-visible:ring-brand-primary focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
            className,
          )}
          {...props}
        />
        {(label || description) && (
          <div className="grid min-h-6 gap-2xs">
            {label && <label htmlFor={controlId} className="cursor-pointer text-sm font-medium leading-6">{label}</label>}
            {description && <p id={descriptionId} className="m-0 text-sm text-foreground-secondary">{description}</p>}
          </div>
        )}
      </div>
    );
  },
);
Checkbox.displayName = "Checkbox";
