import { LoginForm } from "@/features/identity/components";
import { Container } from "@/shared/ui/container";

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ returnTo?: string }> }) {
  const { returnTo } = await searchParams;
  return (
    <section className="py-lg md:py-xl">
      <Container>
        <LoginForm returnTo={returnTo} />
      </Container>
    </section>
  );
}
