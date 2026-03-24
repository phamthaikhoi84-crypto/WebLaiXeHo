using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverService.Domain.Enums;

public enum TransmissionType
{
    Auto = 1,   // Số tự động
    Manual = 2  // Số sàn
}

public enum VehicleType
{
    Car4Seats = 1,
    Car7Seats = 2,
    Luxury = 3
}

public enum LicenseType
{
    B1 = 1, // Chỉ lái xe tự động
    B2 = 2, // Lái được cả số sàn và tự động
    C = 3   // Tải, chuyên dụng...
}