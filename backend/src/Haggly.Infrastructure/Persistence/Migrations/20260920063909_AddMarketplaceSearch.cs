using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
                CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;

                CREATE OR REPLACE FUNCTION public.haggly_normalize_search_text(value text)
                RETURNS text
                LANGUAGE sql
                IMMUTABLE
                PARALLEL SAFE
                RETURNS NULL ON NULL INPUT
                AS $function$
                    SELECT BTRIM(
                        REGEXP_REPLACE(
                            REPLACE(LOWER(public.unaccent('public.unaccent'::regdictionary, value)), 'đ', 'd'),
                            '[^[:alnum:]]+',
                            ' ',
                            'g'));
                $function$;

                CREATE INDEX "IX_stalls_MarketplaceSearchName"
                ON markets.stalls
                USING gin (public.haggly_normalize_search_text("Name") gin_trgm_ops)
                WHERE "Status" = 'ACTIVE' AND "DeletedAt" IS NULL;

                CREATE INDEX "IX_products_MarketplaceSearchName"
                ON catalog.products
                USING gin (public.haggly_normalize_search_text("Name") gin_trgm_ops)
                WHERE "Status" = 'ACTIVE' AND "DeletedAt" IS NULL;

                CREATE INDEX "IX_product_stalls_MarketplaceSearchDisplayName"
                ON catalog.product_stalls
                USING gin (public.haggly_normalize_search_text("DisplayName") gin_trgm_ops)
                WHERE "IsActive" = TRUE AND "DeletedAt" IS NULL AND "DisplayName" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS markets."IX_stalls_MarketplaceSearchName";
                DROP INDEX IF EXISTS catalog."IX_products_MarketplaceSearchName";
                DROP INDEX IF EXISTS catalog."IX_product_stalls_MarketplaceSearchDisplayName";
                DROP FUNCTION IF EXISTS public.haggly_normalize_search_text(text);
                """);
        }
    }
}
