import { forwardRef, useId, type TextareaHTMLAttributes } from "react";
import { Field } from "@/shared/ui/_internal/Field";
import { controlClassName, controlErrorClassName } from "@/shared/ui/_internal/controlStyles";
import { cn } from "@/shared/lib/cn";

export interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  helperText?: string;
  error?: string;
  containerClassName?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, containerClassName, error, helperText, id, label, required, rows = 4, ...props }, ref) => {
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
        <textarea
          ref={ref}
          id={controlId}
          rows={rows}
          required={required}
          aria-invalid={error ? true : undefined}
          aria-describedby={descriptionId}
          className={cn(controlClassName, "min-h-28 resize-y px-sm py-3", error && controlErrorClassName, className)}
          {...props}
        />
      </Field>
    );
  },
);
Textarea.displayName = "Textarea";
