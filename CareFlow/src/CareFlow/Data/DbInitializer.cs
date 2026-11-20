using Microsoft.EntityFrameworkCore;
using CareFlow.Models;

namespace CareFlow.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(CareFlowDbContext context)
    {
        // Applies any pending migrations and ensures database is created
        await context.Database.MigrateAsync();

        // Seed Diagnostic Services if empty
        if (!await context.DiagnosticServices.AnyAsync())
        {
            var services = new List<DiagnosticService>
            {
                new()
                {
                    Code = "CBC",
                    Name = "Complete Blood Count",
                    Department = "Hematology Laboratory",
                    DailyCapacity = 5,
                    Description = "Standard automated CBC with white blood cell differential and platelet count."
                },
                new()
                {
                    Code = "URN",
                    Name = "Urinalysis Routine",
                    Department = "Clinical Microscopy",
                    DailyCapacity = 4,
                    Description = "Complete routine physical, chemical, and microscopic urine analysis."
                },
                new()
                {
                    Code = "CXR",
                    Name = "Chest X-Ray (PA View)",
                    Department = "Radiology & Imaging",
                    DailyCapacity = 3,
                    Description = "Single posterior-anterior thoracic diagnostic radiograph examination."
                },
                new()
                {
                    Code = "ECG",
                    Name = "Electrocardiogram (12-Lead)",
                    Department = "Cardiology Diagnostic",
                    DailyCapacity = 2,
                    Description = "Resting standard 12-lead cardiovascular electrical activity recording."
                },
                new()
                {
                    Code = "LIP",
                    Name = "Lipid Profile Panel",
                    Department = "Clinical Chemistry",
                    DailyCapacity = 4,
                    Description = "Comprehensive lipid battery: Total Cholesterol, HDL, LDL, and Triglycerides."
                },
                new()
                {
                    Code = "FBS",
                    Name = "Fasting Blood Sugar",
                    Department = "Clinical Chemistry",
                    DailyCapacity = 6,
                    Description = "Quantitative determination of plasma glucose after an 8-hour overnight fast."
                }
            };

            await context.DiagnosticServices.AddRangeAsync(services);
            await context.SaveChangesAsync();
        }

        // Seed Fictional Patients if empty
        if (!await context.Patients.AnyAsync())
        {
            var patients = new List<Patient>
            {
                new()
                {
                    PatientCode = "PAT-1001",
                    FullName = "Juan Dela Cruz",
                    DateOfBirth = new DateOnly(1985, 4, 12),
                    Gender = "Male",
                    ContactNumber = "0917-555-0101"
                },
                new()
                {
                    PatientCode = "PAT-1002",
                    FullName = "Maria Clara Santos",
                    DateOfBirth = new DateOnly(1992, 9, 21),
                    Gender = "Female",
                    ContactNumber = "0918-555-0102"
                },
                new()
                {
                    PatientCode = "PAT-1003",
                    FullName = "Andres Bonifacio",
                    DateOfBirth = new DateOnly(1978, 11, 30),
                    Gender = "Male",
                    ContactNumber = "0919-555-0103"
                },
                new()
                {
                    PatientCode = "PAT-1004",
                    FullName = "Gabriela Silang",
                    DateOfBirth = new DateOnly(1988, 3, 19),
                    Gender = "Female",
                    ContactNumber = "0920-555-0104"
                },
                new()
                {
                    PatientCode = "PAT-1005",
                    FullName = "Jose Rizal",
                    DateOfBirth = new DateOnly(1995, 6, 19),
                    Gender = "Male",
                    ContactNumber = "0921-555-0105"
                },
                new()
                {
                    PatientCode = "PAT-1006",
                    FullName = "Corazon Aquino",
                    DateOfBirth = new DateOnly(1970, 1, 25),
                    Gender = "Female",
                    ContactNumber = "0922-555-0106"
                }
            };

            await context.Patients.AddRangeAsync(patients);
            await context.SaveChangesAsync();
        }
    }
}
