import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Input } from "./Input";

describe("Input", () => {
  it("Input_WithError_AssociatesLabelAndErrorWithControl", () => {
    // Arrange
    render(<Input label="Tên sản phẩm" error="Tên sản phẩm là bắt buộc" required />);

    // Act
    const input = screen.getByRole("textbox", { name: /Tên sản phẩm/ });
    const error = screen.getByText("Tên sản phẩm là bắt buộc");

    // Assert
    expect(input).toBeRequired();
    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(input).toHaveAttribute("aria-describedby", error.id);
  });
});
