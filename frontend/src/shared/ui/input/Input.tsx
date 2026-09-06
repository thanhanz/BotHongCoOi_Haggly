import { forwardRef, useId, type InputHTMLAttributes, type ReactNode } from "react";
import { Field } from "@/shared/ui/_internal/Field";
import { controlClassName, controlErrorClassName } from "@/shared/ui/_internal/controlStyles";
import { cn } from "@/shared/lib/cn";

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  helperText?: string;
  error?: string;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
  containerClassName?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({
    className,
    containerClassName,
    error,
    helperText,
    id,
    label,
    leftIcon,
    required,
    rightIcon,
    ...props
  }, ref) => {
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
        <div className="relative">
          {leftIcon && (
            <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 left-sm flex items-center text-foreground-secondary">
              {leftIcon}
            </span>
          )}
          <input
            ref={ref}
            id={controlId}
            required={required}
            aria-invalid={error ? true : undefined}
            aria-describedby={descriptionId}
            className={cn(
              controlClassName,
              "h-12 px-sm md:h-10",
              leftIcon && "pl-10",
              rightIcon && "pr-10",
              error && controlErrorClassName,
              className,
            )}
            {...props}
          />
          {rightIcon && (
            <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 right-sm flex items-center text-foreground-secondary">
              {rightIcon}
            </span>
          )}
        </div>
      </Field>
    );
  },
);
Input.displayName = "Input";
