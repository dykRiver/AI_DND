using DHY.DDCS.Module.Common.Entity;

namespace DHY.DDCS.Module.Prescription.PrescriptionSplit;

public abstract class AbstractSplitProvider
{
    public abstract Task<IEnumerable<DDCSPrescription>> SplitAsync(PrescriptionInfo prescriptionInfo);
}
