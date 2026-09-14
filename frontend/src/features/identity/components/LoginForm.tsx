"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { login } from "@/features/identity/api";
import { ApiError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Typography } from "@/shared/ui/typography";
import { useAuth } from "./AuthProvider";

function safeReturnPath(value: string): string {
  return value.startsWith("/") && !value.startsWith("//") ? value : "/";
}

export function LoginForm({ returnTo = "/" }: { returnTo?: string }) {
  const router = useRouter();
  const { setSession } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string>();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(undefined);
    const formData = new FormData(event.currentTarget);

    try {
      const result = await login({
        email: String(formData.get("email")),
        password: String(formData.get("password")),
      });
      setSession(result);
      router.replace(safeReturnPath(returnTo));
    } catch (requestError) {
      setError(requestError instanceof ApiError && requestError.status === 401
        ? "Email hoặc mật khẩu không đúng."
        : "Không thể đăng nhập lúc này. Vui lòng thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Card>
      <CardHeader className="grid gap-xs text-center">
        <Typography as="h1" variant="headlineSm">Đăng nhập Haggly</Typography>
        <Typography variant="bodySm" className="text-foreground-secondary">
          Đăng nhập để xem giỏ hàng và tiếp tục đặt món tại sạp.
        </Typography>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="grid gap-sm">
          <Input name="email" type="email" label="Email" autoComplete="email" required autoFocus />
          <Input name="password" type="password" label="Mật khẩu" autoComplete="current-password" required />
          {error && <p role="alert" className="rounded-control bg-state-error-surface p-sm text-sm text-state-error">{error}</p>}
        </CardContent>
        <CardFooter className="flex-col">
          <Button type="submit" loading={isSubmitting} className="w-full">
            {isSubmitting ? "Đang đăng nhập…" : "Đăng nhập"}
          </Button>
          <p className="text-sm text-foreground-secondary">
            Chưa có tài khoản? <Link href="/register" className="font-semibold text-brand-secondary hover:underline">Đăng ký</Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
}
