using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SmartParking.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace SmartParking.Core.Utils
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Kiểm tra xem action có sử dụng VehicleUploadDto hay không.
            var uploadDtoParam = context.ApiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Type == typeof(VehicleUploadDto));
            if (uploadDtoParam == null)
                return;

            // Cấu hình schema cho multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["image"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
