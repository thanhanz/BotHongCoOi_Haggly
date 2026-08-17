using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Haggly.Infrastructure.Persistence.Migrations;

public partial class RefactorContinuousInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE catalog.product_stalls
                RENAME COLUMN "DefaultUnitPrice" TO "CurrentUnitPrice";
            ALTER TABLE catalog.product_stalls
                ADD COLUMN "Version" bigint NOT NULL DEFAULT 0;

            CREATE TABLE inventory.inventories (
                "Id" uuid NOT NULL,
                "StallId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" uuid NULL,
                CONSTRAINT "PK_inventories" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_inventories_stalls_StallId" FOREIGN KEY ("StallId")
                    REFERENCES markets.stalls ("Id") ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX "IX_inventories_StallId"
                ON inventory.inventories ("StallId");

            INSERT INTO inventory.inventories
                ("Id", "StallId", "CreatedAt", "CreatedBy")
            SELECT COALESCE(latest."Id", gen_random_uuid()),
                   stall."Id",
                   COALESCE(latest."CreatedAt", CURRENT_TIMESTAMP),
                   latest."CreatedBy"
            FROM markets.stalls stall
            LEFT JOIN LATERAL (
                SELECT session."Id", session."CreatedAt", session."CreatedBy"
                FROM inventory.inventory_sessions session
                WHERE session."StallId" = stall."Id"
                ORDER BY session."BusinessDate" DESC, session."Id" DESC
                LIMIT 1
            ) latest ON TRUE;

            UPDATE catalog.product_stalls product_stall
            SET "CurrentUnitPrice" = latest."PublicUnitPrice"
            FROM (
                SELECT DISTINCT ON (listing."ProductStallId")
                       listing."ProductStallId", listing."PublicUnitPrice"
                FROM inventory.daily_product_listings listing
                INNER JOIN inventory.inventory_sessions session
                    ON session."Id" = listing."InventorySessionId"
                ORDER BY listing."ProductStallId", session."BusinessDate" DESC, listing."Id" DESC
            ) latest
            WHERE latest."ProductStallId" = product_stall."Id";

            CREATE TABLE inventory.inventory_items (
                "Id" uuid NOT NULL,
                "InventoryId" uuid NOT NULL,
                "ProductStallId" uuid NOT NULL,
                "CurrentQuantity" numeric(18,3) NOT NULL,
                "ReservedQuantity" numeric(18,3) NOT NULL,
                "Version" bigint NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL,
                "UpdatedBy" uuid NULL,
                CONSTRAINT "PK_inventory_items" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_inventory_items_quantity_bounds"
                    CHECK ("CurrentQuantity" >= 0 AND "ReservedQuantity" >= 0
                        AND "ReservedQuantity" <= "CurrentQuantity"),
                CONSTRAINT "FK_inventory_items_inventories_InventoryId" FOREIGN KEY ("InventoryId")
                    REFERENCES inventory.inventories ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_inventory_items_product_stalls_ProductStallId" FOREIGN KEY ("ProductStallId")
                    REFERENCES catalog.product_stalls ("Id") ON DELETE RESTRICT
            );

            INSERT INTO inventory.inventory_items
                ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity",
                 "Version", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy")
            SELECT ranked."Id", inv."Id", ranked."ProductStallId", ranked."CurrentQuantity",
                   ranked."ReservedQuantity", ranked."Version", ranked."CreatedAt", ranked."CreatedBy",
                   ranked."UpdatedAt", ranked."UpdatedBy"
            FROM (
                SELECT listing.*, session."StallId",
                       ROW_NUMBER() OVER (
                           PARTITION BY session."StallId", listing."ProductStallId"
                           ORDER BY session."BusinessDate" DESC, listing."Id" DESC) AS row_number
                FROM inventory.daily_product_listings listing
                INNER JOIN inventory.inventory_sessions session
                    ON session."Id" = listing."InventorySessionId"
            ) ranked
            INNER JOIN inventory.inventories inv ON inv."StallId" = ranked."StallId"
            WHERE ranked.row_number = 1;

            CREATE UNIQUE INDEX "IX_inventory_items_InventoryId_ProductStallId"
                ON inventory.inventory_items ("InventoryId", "ProductStallId");
            CREATE UNIQUE INDEX "IX_inventory_items_ProductStallId"
                ON inventory.inventory_items ("ProductStallId");

            ALTER TABLE inventory.inventory_ledgers
                DROP CONSTRAINT "FK_inventory_ledgers_daily_product_listings_DailyProductListin~";
            ALTER TABLE inventory.inventory_ledgers
                DROP CONSTRAINT "FK_inventory_ledgers_inventory_sessions_InventorySessionId";
            DROP INDEX inventory."IX_inventory_ledgers_DailyProductListingId_OccurredAt_Id";
            DROP INDEX inventory."IX_inventory_ledgers_InventorySessionId_OccurredAt_Id";

            ALTER TABLE inventory.inventory_ledgers ADD COLUMN "NewInventoryId" uuid NULL;
            ALTER TABLE inventory.inventory_ledgers ADD COLUMN "NewInventoryItemId" uuid NULL;

            UPDATE inventory.inventory_ledgers ledger
            SET "NewInventoryId" = inv."Id",
                "NewInventoryItemId" = target_item."Id"
            FROM inventory.daily_product_listings source_listing
            INNER JOIN inventory.inventory_sessions source_session
                ON source_session."Id" = source_listing."InventorySessionId"
            INNER JOIN inventory.inventories inv
                ON inv."StallId" = source_session."StallId"
            INNER JOIN inventory.inventory_items target_item
                ON target_item."InventoryId" = inv."Id"
               AND target_item."ProductStallId" = source_listing."ProductStallId"
            WHERE ledger."DailyProductListingId" = source_listing."Id";

            ALTER TABLE inventory.inventory_ledgers ALTER COLUMN "NewInventoryId" SET NOT NULL;
            ALTER TABLE inventory.inventory_ledgers ALTER COLUMN "NewInventoryItemId" SET NOT NULL;
            ALTER TABLE inventory.inventory_ledgers DROP COLUMN "DailyProductListingId";
            ALTER TABLE inventory.inventory_ledgers DROP COLUMN "InventorySessionId";
            ALTER TABLE inventory.inventory_ledgers RENAME COLUMN "NewInventoryId" TO "InventoryId";
            ALTER TABLE inventory.inventory_ledgers RENAME COLUMN "NewInventoryItemId" TO "InventoryItemId";
            ALTER TABLE inventory.inventory_ledgers DROP CONSTRAINT "CK_inventory_ledgers_price_bounds";
            ALTER TABLE inventory.inventory_ledgers DROP COLUMN "UnitPriceBefore";
            ALTER TABLE inventory.inventory_ledgers DROP COLUMN "UnitPriceAfter";

            CREATE INDEX "IX_inventory_ledgers_InventoryId_OccurredAt_Id"
                ON inventory.inventory_ledgers ("InventoryId", "OccurredAt", "Id");
            CREATE INDEX "IX_inventory_ledgers_InventoryItemId_OccurredAt_Id"
                ON inventory.inventory_ledgers ("InventoryItemId", "OccurredAt", "Id");
            ALTER TABLE inventory.inventory_ledgers
                ADD CONSTRAINT "FK_inventory_ledgers_inventories_InventoryId"
                FOREIGN KEY ("InventoryId") REFERENCES inventory.inventories ("Id") ON DELETE RESTRICT;
            ALTER TABLE inventory.inventory_ledgers
                ADD CONSTRAINT "FK_inventory_ledgers_inventory_items_InventoryItemId"
                FOREIGN KEY ("InventoryItemId") REFERENCES inventory.inventory_items ("Id") ON DELETE RESTRICT;

            DROP TABLE inventory.daily_product_listings;
            DROP TABLE inventory.inventory_sessions;

            CREATE SCHEMA IF NOT EXISTS sales;
            CREATE TABLE sales.pos_sales (
                "Id" uuid NOT NULL, "StallId" uuid NOT NULL,
                "SaleNo" varchar(64) NOT NULL, "ClientRequestId" varchar(100) NOT NULL,
                "Status" varchar(32) NOT NULL, "TotalAmount" numeric(18,2) NOT NULL,
                "CompletedBy" uuid NOT NULL, "CompletedAt" timestamp with time zone NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "CreatedBy" uuid NULL,
                "UpdatedAt" timestamp with time zone NULL, "UpdatedBy" uuid NULL,
                CONSTRAINT "PK_pos_sales" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_pos_sales_total_amount_bounds" CHECK ("TotalAmount" >= 0)
            );
            CREATE UNIQUE INDEX "IX_pos_sales_SaleNo" ON sales.pos_sales ("SaleNo");
            CREATE UNIQUE INDEX "IX_pos_sales_StallId_ClientRequestId"
                ON sales.pos_sales ("StallId", "ClientRequestId");

            CREATE TABLE sales.pos_sale_items (
                "Id" uuid NOT NULL, "PosSaleId" uuid NOT NULL, "InventoryItemId" uuid NOT NULL,
                "ProductNameSnapshot" varchar(200) NOT NULL, "SellingUnitSnapshot" varchar(32) NOT NULL,
                "UnitPrice" numeric(18,2) NOT NULL, "Quantity" numeric(18,3) NOT NULL,
                "LineTotal" numeric(18,2) NOT NULL, "CreatedAt" timestamp with time zone NOT NULL,
                "CreatedBy" uuid NULL, "UpdatedAt" timestamp with time zone NULL, "UpdatedBy" uuid NULL,
                CONSTRAINT "PK_pos_sale_items" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_pos_sale_items_price_bounds" CHECK ("UnitPrice" >= 0 AND "LineTotal" >= 0),
                CONSTRAINT "CK_pos_sale_items_quantity_bounds" CHECK ("Quantity" > 0),
                CONSTRAINT "FK_pos_sale_items_pos_sales_PosSaleId" FOREIGN KEY ("PosSaleId")
                    REFERENCES sales.pos_sales ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_pos_sale_items_PosSaleId_InventoryItemId"
                ON sales.pos_sale_items ("PosSaleId", "InventoryItemId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Continuous inventory consolidates daily history and cannot be rolled back without losing meaning. Restore a pre-migration backup instead.");
    }
}
