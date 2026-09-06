import { forwardRef, useId, type SelectHTMLAttributes } from "react";
import { Field } from "@/shared/ui/_internal/Field";
import { controlClassName, controlErrorClassName } from "@/shared/ui/_internal/controlStyles";
import { cn } from "@/shared/lib/cn";

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  helperText?: string;
  error?: string;
  containerClassName?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, containerClassName, error, helperText, id, label, required, children, ...props }, ref) => {
    const generatedId = useId();
    const controlId = id ?? generatedId;
    const descriptionId = error || helperText ? `${controlId}-description` : undefined;

    return (
      <Field
        className={containerClassName}
        controlId={controlId}
        descriptionId={descriptionId}
        error={error}
        helperText={helperText}
        label={label}
        required={required}
      >
        <select
          ref={ref}
          id={controlId}
          required={required}
          aria-invalid={error ? true : undefined}
          aria-describedby={descriptionId}
          className={cn(controlClassName, "h-12 px-sm md:h-10", error && controlErrorClassName, className)}
          {...props}
        >
          {children}
        </select>
      </Field>
    );
  },
);
Select.displayName = "Select";
