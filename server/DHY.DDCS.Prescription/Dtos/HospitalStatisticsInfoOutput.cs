namespace DHY.DDCS.Module.Prescription.Dtos;

public class HospitalStatisticsInfoOutput
{
    public List<string> HospitalNames { get; set; }

    public List<string> DeliveryMethods { get; set; }

    public List<List<int>> DeliveryMethodGroupCount { get; set; } = [];
}
