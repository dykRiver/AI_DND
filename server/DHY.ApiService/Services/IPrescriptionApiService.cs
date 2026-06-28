[Client("default")]
public interface IPrescriptionApiService : IHttpClientApiService
{
    [Post("prescriptionInfo/queryPrescriptionInfoByDDCSPid")]
    Task<AdminResult<PrescriptionInfoOutput>> QueryPrescriptionInfoByDDCSPidAsync([Body] BaseIdInput input);

    [Post("prescriptionInfo/SubDetails")]
    Task<AdminResult<IList<PrescriptionInfoDetailOutput>>> QueryDetailsAsync([Body] BaseIdInput input);
}