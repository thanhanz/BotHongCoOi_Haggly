import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Button } from "./Button";

describe("Button", () => {
  it("Button_Loading_PreventsActivationAndExposesBusyState", async () => {
    // Arrange
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(<Button loading onClick={onClick}>Đang xử lý</Button>);
    const button = screen.getByRole("button", { name: "Đang xử lý" });

    // Act
    await user.click(button);

    // Assert
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("aria-busy", "true");
    expect(onClick).not.toHaveBeenCalled();
  });
});
