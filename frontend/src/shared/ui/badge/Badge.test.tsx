import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Badge } from "./Badge";

describe("Badge", () => {
  it("Badge_ReadyVariant_UsesReadySemanticTokens", () => {
    // Arrange
    render(<Badge variant="ready">Đã sẵn sàng</Badge>);

    // Act
    const badge = screen.getByText("Đã sẵn sàng");

    // Assert
    expect(badge).toHaveClass("bg-ready-background", "text-ready-text");
  });
});
