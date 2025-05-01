using Microsoft.AspNetCore.Http;

namespace SmartParking.Core.Models
{
    public class VehicleUploadDto
    {
        public IFormFile Image { get; set; }
    }
}
