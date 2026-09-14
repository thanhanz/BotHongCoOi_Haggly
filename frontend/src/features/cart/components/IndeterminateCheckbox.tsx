"use client";

import { useEffect, useRef, type ComponentProps } from "react";
import { Checkbox } from "@/shared/ui/checkbox";

interface Props extends ComponentProps<typeof Checkbox> {
  indeterminate?: boolean;
}

export function IndeterminateCheckbox({ indeterminate = false, ...props }: Props) {
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = indeterminate;
  }, [indeterminate]);
  return <Checkbox ref={ref} aria-checked={indeterminate ? "mixed" : props.checked} {...props} />;
}
