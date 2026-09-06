import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Button } from "@/shared/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogTitle, DialogTrigger } from "./Dialog";

describe("Dialog", () => {
  it("Dialog_OpenedThenEscapePressed_ClosesAndReturnsFocus", async () => {
    // Arrange
    const user = userEvent.setup();
    render(
      <Dialog>
        <DialogTrigger asChild><Button>Mở</Button></DialogTrigger>
        <DialogContent>
          <DialogTitle>Xác nhận</DialogTitle>
          <DialogDescription>Nội dung xác nhận</DialogDescription>
        </DialogContent>
      </Dialog>,
    );
    const trigger = screen.getByRole("button", { name: "Mở" });

    // Act
    await user.click(trigger);
    await user.keyboard("{Escape}");

    // Assert
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });
});
