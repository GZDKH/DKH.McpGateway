# Plan: Extract mappers from Management gRPC services

## Context

All 13 Management gRPC services contain inline private static mapping methods (`ToProto`, `FilterTranslations`, `NullIfEmpty`). This creates duplication — `NullIfEmpty` copied 13 times identically, `FilterTranslations` copied 11 times with identical logic. Extracting to separate mapper classes improves maintainability and follows the existing CRUD mapper pattern in `Grpc/Mappers/`.

## Design decisions

- **Location**: `Grpc/Mappers/Management/` subfolder — mirrors `Grpc/Services/Management/`
- **Pattern**: Extension methods in static classes (consistent with existing CRUD mappers)
- **Naming**: `ToProto()` (not `ToContract()`) — differentiates from CRUD mappers that map Entity→Contract
- **`NullIfEmpty`**: single shared `ManagementMapperHelpers` class, `using static` in GlobalUsings
- **`FilterTranslations`**: per-mapper extension method (DTOs share no common interface; 5 lines each)
- **`ResolveValuesAsync`**: stays in `ResourceEntryManagementGrpcService` (requires `dbContext`)
- **Visibility**: `internal` — all usage within Api project

## New files (14)

```
DKH.ReferenceService.Api/Grpc/Mappers/Management/
├── ManagementMapperHelpers.cs         # NullIfEmpty
├── CountryManagementMapper.cs         # ToProto + FilterTranslations
├── CityManagementMapper.cs            # ToProto + FilterTranslations
├── StateProvinceManagementMapper.cs   # ToProto + FilterTranslations
├── StateProvinceTypeManagementMapper.cs # ToProto + FilterTranslations
├── LanguageManagementMapper.cs        # ToProto + FilterTranslations
├── CurrencyManagementMapper.cs        # ToProto + FilterTranslations
├── DimensionManagementMapper.cs       # ToProto + FilterTranslations
├── WeightManagementMapper.cs          # ToProto + FilterTranslations
├── QuantityUnitManagementMapper.cs    # ToProto + FilterTranslations
├── PriceLabelManagementMapper.cs      # ToProto + FilterTranslations
├── DeliveryTimeManagementMapper.cs    # ToProto + FilterTranslations
├── ResourceBundleManagementMapper.cs  # ToProto only
└── ResourceEntryManagementMapper.cs   # ToProto only
```

## Modified files (14)

- `GlobalUsings.cs` — add 2 lines:
  ```csharp
  global using DKH.ReferenceService.Api.Grpc.Mappers.Management;
  global using static DKH.ReferenceService.Api.Grpc.Mappers.Management.ManagementMapperHelpers;
  ```
- 13 services in `Grpc/Services/Management/` — remove private methods, use extension syntax

## Call-site changes in services

Before:
```csharp
FilterTranslations(dto, NullIfEmpty(request.Language));
return new GetCountryResponse { Found = true, Data = ToProto(dto) };
```

After:
```csharp
dto.FilterTranslations(NullIfEmpty(request.Language));
return new GetCountryResponse { Found = true, Data = dto.ToProto() };
```

## Steps

1. Create `ManagementMapperHelpers.cs` with `NullIfEmpty`
2. Create 11 translatable mappers (`ToProto` + `FilterTranslations`)
3. Create 2 non-translatable mappers (`ToProto` only) for ResourceBundle, ResourceEntry
4. Update `GlobalUsings.cs`
5. Update all 13 service files — remove private methods, switch to extension syntax
6. Build (`dotnet build -c Release`) and test (`dotnet test`)
7. Commit: `refactor(management): extract mappers from management gRPC services`

## Verification

```bash
cd /Users/itprodavets/RiderProjects/GZDKH/services/DKH.ReferenceService
dotnet build -c Release   # 0 errors, 0 warnings
dotnet test               # all tests pass
```
