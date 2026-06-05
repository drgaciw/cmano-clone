namespace ProjectAegis.Data.Catalog;

public sealed record QuarantinedCatalogBinding(CatalogSensorBinding Binding, string RejectionReason);