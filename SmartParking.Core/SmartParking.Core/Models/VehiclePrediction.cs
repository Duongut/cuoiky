using Microsoft.ML.Data;

namespace SmartParking.Core.Models
{
    public class VehiclePrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
    }
}
