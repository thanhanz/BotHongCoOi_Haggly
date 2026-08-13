using Dapper;
using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Dtos;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Infrastructure.Persistence.Queries.Identity;

public sealed class DapperVendorAdminQuery(DapperDbContext dbContext) : IVendorAdminQuery
{
    public async Task<PagedResult<VendorQueryDto>> GetPageAsync(
        VendorListFilter filter,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM identity.vendor_profiles vp
            INNER JOIN identity.users u ON u."Id" = vp."UserId"
            WHERE u."DeletedAt" IS NULL
              AND (@ApprovalStatus IS NULL OR vp."ApprovalStatus" = @ApprovalStatus)
              AND (
                    @Search IS NULL
                    OR u."Email" ILIKE @Search
                    OR u."PhoneNumber" ILIKE @Search
                    OR u."FullName" ILIKE @Search
                    OR vp."BusinessName" ILIKE @Search
                  );

            SELECT
                vp."UserId" AS "UserId",
                u."Email" AS "Email",
                u."PhoneNumber" AS "PhoneNumber",
                u."FullName" AS "FullName",
                vp."BusinessName" AS "BusinessName",
                vp."BusinessRegistrationNo" AS "BusinessRegistrationNo",
                vp."TaxCode" AS "TaxCode",
                u."Status" AS "UserStatus",
                vp."ApprovalStatus" AS "ApprovalStatus",
                vp."ApprovedAt" AS "ApprovedAt",
                vp."ApprovedBy" AS "ApprovedBy",
                vp."CreatedAt" AS "CreatedAt",
                vp."UpdatedAt" AS "UpdatedAt",
                vp."UpdatedBy" AS "UpdatedBy"
            FROM identity.vendor_profiles vp
            INNER JOIN identity.users u ON u."Id" = vp."UserId"
            WHERE u."DeletedAt" IS NULL
              AND (@ApprovalStatus IS NULL OR vp."ApprovalStatus" = @ApprovalStatus)
              AND (
                    @Search IS NULL
                    OR u."Email" ILIKE @Search
                    OR u."PhoneNumber" ILIKE @Search
                    OR u."FullName" ILIKE @Search
                    OR vp."BusinessName" ILIKE @Search
                  )
            ORDER BY vp."CreatedAt" DESC, vp."UserId"
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = await dbContext.OpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                ApprovalStatus = filter.ApprovalStatus?.ToString(),
                Search = string.IsNullOrWhiteSpace(filter.Search) ? null : $"%{filter.Search}%",
                Offset = (filter.Page - 1) * filter.PageSize,
                filter.PageSize
            },
            cancellationToken: cancellationToken);
        
        using var results = await connection.QueryMultipleAsync(command);
      
        //Get total pages
        var totalCount = checked((int)await results.ReadSingleAsync<long>());
        
        //Get detail of total items
        var rows = (await results.ReadAsync<VendorRow>()).AsList();
        var items = rows.Select(VendorRowMapper.ToVendorQueryDto).ToArray();

        return new PagedResult<VendorQueryDto>(items, filter.Page, filter.PageSize, totalCount);
    }
}

public sealed record VendorRow(
    Guid UserId,
    string Email,
    string PhoneNumber,
    string FullName,
    string BusinessName,
    string? BusinessRegistrationNo,
    string? TaxCode,
    string UserStatus,
    string ApprovalStatus,
    DateTime? ApprovedAt,
    Guid? ApprovedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);

public static class VendorRowMapper
{
    public static VendorQueryDto ToVendorQueryDto(VendorRow row)
        => new(
            row.UserId,
            row.Email,
            row.PhoneNumber,
            row.FullName,
            row.BusinessName,
            row.BusinessRegistrationNo,
            row.TaxCode,
            Enum.Parse<UserStatus>(row.UserStatus, ignoreCase: true),
            Enum.Parse<ApprovalStatus>(row.ApprovalStatus, ignoreCase: true),
            ToUtcOffset(row.ApprovedAt),
            row.ApprovedBy,
            ToUtcOffset(row.CreatedAt)!.Value,
            ToUtcOffset(row.UpdatedAt),
            row.UpdatedBy);

    private static DateTimeOffset? ToUtcOffset(DateTime? value)
        => value is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}
