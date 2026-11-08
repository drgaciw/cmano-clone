namespace ProjectAegis.Data.Excel;

using Platform;

/// <summary>Req-21 / ADR-011: edge factories for <see cref="PlatformWorkbookIoSelection"/>.</summary>
public static class PlatformWorkbookIoFactories
{
    public static IPlatformWorkbookIo ClosedXml() => new ClosedXmlPlatformWorkbookIo();
}