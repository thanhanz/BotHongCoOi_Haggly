"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { registerBuyer, type Registration } from "@/features/identity/api";
import { ApiError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/shared/ui/card";
import { Container } from "@/shared/ui/container";
import { Input } from "@/shared/ui/input";
import { Typography } from "@/shared/ui/typography";

export default function RegisterPage() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string>();
  const [registration, setRegistration] = useState<Registration>();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(undefined);
    setRegistration(undefined);

    const form = event.currentTarget;
    const formData = new FormData(form);

    try {
      const result = await registerBuyer({
        email: String(formData.get("email")),
        phoneNumber: String(formData.get("phoneNumber")),
        password: String(formData.get("password")),
        fullName: String(formData.get("fullName")),
      });

      setRegistration(result);
      form.reset();
    } catch (requestError) {
      setError(
        requestError instanceof ApiError
          ? requestError.message
          : "Không thể kết nối đến máy chủ.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="flex min-h-[70vh] items-center py-xl">
      <Container className="max-w-xl">
        <Card>
          <CardHeader className="grid gap-xs">
            <Typography as="h1" variant="headlineSm">
              Đăng ký người mua
            </Typography>
            <Typography variant="bodySm" className="text-foreground-secondary">
              Biểu mẫu này gọi trực tiếp Haggly API để kiểm tra kết nối frontend–backend.
            </Typography>
          </CardHeader>

          <form onSubmit={handleSubmit}>
            <CardContent className="grid gap-sm">
              <Input name="fullName" label="Họ và tên" autoComplete="name" required />
              <Input name="email" type="email" label="Email" autoComplete="email" required />
              <Input name="phoneNumber" type="tel" label="Số điện thoại" autoComplete="tel" required />
              <Input
                name="password"
                type="password"
                label="Mật khẩu"
                autoComplete="new-password"
                minLength={8}
                required
              />

              {error && (
                <p role="alert" className="rounded-control bg-state-error-surface p-sm text-sm text-state-error">
                  {error}
                </p>
              )}

              {registration && (
                <div role="status" className="rounded-control bg-status-ready-background p-sm text-sm text-status-ready-text">
                  Đăng ký thành công: {registration.email} ({registration.role}, {registration.status})
                </div>
              )}
            </CardContent>

            <CardFooter className="justify-between">
              <Link href="/" className="text-sm font-semibold text-brand-secondary hover:underline">
                Về trang chủ
              </Link>
              <Button type="submit" loading={isSubmitting}>
                {isSubmitting ? "Đang đăng ký…" : "Đăng ký"}
              </Button>
            </CardFooter>
          </form>
        </Card>
      </Container>
    </section>
  );
}
