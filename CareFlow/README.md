# CareFlow — Diagnostic Booking Management System

> **Course**: ITSD81 - Desktop Application Development  
> **Unit**: IV - Data-Driven Application Architecture and Transactional Workflows  
> **Component**: Lecture Performance Task 4  
> **Output**: Functional Desktop Application and Essential Supporting Documentation  

---

## 1. Overview
**CareFlow** is an outpatient diagnostic booking management desktop application developed with **C#, .NET, Avalonia UI, MVVM, and Entity Framework Core with SQLite**.

The application coordinates business rules, relational data integrity, and atomic database transactions to ensure:
* Every requested diagnostic service can be accommodated before a booking is confirmed.
* A booking containing 1 to 3 distinct services is processed as **one atomic database transaction** (all services committed together, or none).
* Overbooking is strictly prevented by dynamically calculating remaining capacity from persisted records and re-validating availability inside the transaction boundary.
* Confirmed bookings persist permanently across application restarts.

---

## 2. Technology Stack & Architecture
* **Language & Runtime**: C# 13 / .NET 10 (compatible with .NET 8 runtimes)
* **UI Framework**: Avalonia UI 12 (XAML, Fluent Theme)
* **Architecture Pattern**: Model-View-ViewModel (MVVM) via `CommunityToolkit.Mvvm`
* **Data Access & ORM**: Entity Framework Core 9.0 (`Microsoft.EntityFrameworkCore.Sqlite`)
* **Relational Database**: SQLite (`careflow.db`)
* **Dependency Injection**: `Microsoft.Extensions.DependencyInjection`
* **Automated Testing**: xUnit (`CareFlow.Tests`)

### Architectural Separation of Concerns
CareFlow strictly adheres to clean layered boundaries:
* **Views & ViewModels**: Handle user interaction and observable UI state. ViewModels never directly manage database connections or EF Core configuration.
* **Business Services (`IBookingService`, etc.)**: Encapsulate all business rules (BR-01 through BR-10), date validation, duplicate detection, and explicit transaction lifecycles (`IDbContextTransaction`).
* **Persistence Layer (`CareFlowDbContext`)**: Configures tables, relational keys, unique indexes, and migrations.

---

## 3. Project Structure

```text
CareFlow/
├── CareFlow.sln                          # Classic Visual Studio solution file
├── CareFlow.slnx                         # Modern XML solution file
├── README.md                             # Quick-start instructions & documentation
├── RELATIONAL_DESIGN.md                  # Entity Relationship Diagram & schema documentation
├── VERIFICATION_CHECKLIST.md             # Acceptance criteria verification matrix
├── STEP_BY_STEP_PROCESS.md               # Complete pedagogical guide
│
├── src/CareFlow/                         # Avalonia MVVM Desktop Application
│   ├── Assets/                           # Application icons & graphics
│   ├── Data/                             # Persistence & EF Core
│   │   ├── CareFlowDbContext.cs          # DbContext configuration & constraints
│   │   ├── CareFlowDbContextFactory.cs   # Design-time factory for EF Core CLI
│   │   ├── DbInitializer.cs              # Database migration & seed data
│   │   └── Migrations/                   # EF Core reproducible migration files
│   ├── Exceptions/                       # Domain validation & capacity exceptions
│   │   ├── CareFlowValidationException.cs
│   │   └── ServiceCapacityExceededException.cs
│   ├── Models/                           # Domain entities & DTOs
│   │   ├── Patient.cs
│   │   ├── DiagnosticService.cs
│   │   ├── Booking.cs
│   │   ├── BookingItem.cs
│   │   ├── BookingStatus.cs
│   │   ├── CreateBookingRequest.cs
│   │   ├── BookingResult.cs
│   │   └── ServiceAvailabilityDto.cs
│   ├── Services/                         # Business logic & transactional boundaries
│   │   ├── IBookingService.cs & BookingService.cs
│   │   ├── IDiagnosticCatalogService.cs & DiagnosticCatalogService.cs
│   │   └── IPatientService.cs & PatientService.cs
│   ├── ViewModels/                       # Observable ViewModels
│   │   ├── ViewModelBase.cs
│   │   ├── MainViewModel.cs
│   │   ├── NewBookingViewModel.cs
│   │   ├── AvailabilityViewModel.cs
│   │   ├── BookingHistoryViewModel.cs
│   │   └── SelectableServiceItemViewModel.cs
│   ├── Views/                            # Avalonia XAML User Controls & Windows
│   │   ├── MainWindow.axaml & .cs
│   │   ├── NewBookingView.axaml & .cs
│   │   ├── AvailabilityView.axaml & .cs
│   │   └── BookingHistoryView.axaml & .cs
│   ├── App.axaml & App.axaml.cs          # Dependency injection container & bootstrap
│   └── Program.cs                        # Application entry point
│
└── tests/CareFlow.Tests/                 # Automated xUnit Test Suite
    ├── AcceptanceCriteriaTests.cs        # Comprehensive tests for AC-01 to AC-06
    ├── BusinessRulesValidationTests.cs   # Validation tests for BR-01 to BR-10
    └── TestDatabaseFactory.cs            # In-memory SQLite isolated test fixture
```

---

## 4. Getting Started & Instructions

### Prerequisites
* [.NET 10 or .NET 8 SDK](https://dotnet.microsoft.com/download) installed.
* Optional: `dotnet-ef` CLI tool (`dotnet tool install --global dotnet-ef`).

### Step 1: Restore Dependencies & Build Solution
Open a terminal in the `CareFlow` directory:
```powershell
cd CareFlow
dotnet restore
dotnet build
```

### Step 2: Database Initialization & Migrations
The application **automatically creates the database, applies all pending migrations, and seeds fictional patients and diagnostic services** upon launch via `DbInitializer.InitializeAsync()`.

If you wish to manually inspect or apply migrations via the EF Core CLI:
```powershell
# From the CareFlow solution folder:
dotnet ef database update --project src/CareFlow --startup-project src/CareFlow
```
The database will be created at `careflow.db` in the working directory.

### Step 3: Run the Application
To launch the CareFlow desktop application:
```powershell
dotnet run --project src/CareFlow
```

### Step 4: Run the Automated Verification Suite
To execute all 12 acceptance criteria and business rule tests:
```powershell
dotnet test
```
Expected output:
```text
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12, Duration: ~1.0 s
```

---

## 5. Seed Information

### Diagnostic Services Catalog
| Code | Service Name | Department | Daily Capacity |
| :---: | :--- | :--- | :---: |
| **CBC** | Complete Blood Count | Hematology Laboratory | 5 |
| **URN** | Urinalysis Routine | Clinical Microscopy | 4 |
| **CXR** | Chest X-Ray (PA View) | Radiology & Imaging | 3 |
| **ECG** | Electrocardiogram (12-Lead) | Cardiology Diagnostic | 2 |
| **LIP** | Lipid Profile Panel | Clinical Chemistry | 4 |
| **FBS** | Fasting Blood Sugar | Clinical Chemistry | 6 |

### Fictional Patients
* `PAT-1001`: Juan Dela Cruz (Male, DOB: 1985-04-12)
* `PAT-1002`: Maria Clara Santos (Female, DOB: 1992-09-21)
* `PAT-1003`: Andres Bonifacio (Male, DOB: 1978-11-30)
* `PAT-1004`: Gabriela Silang (Female, DOB: 1988-03-19)
* `PAT-1005`: Jose Rizal (Male, DOB: 1995-06-19)
* `PAT-1006`: Corazon Aquino (Female, DOB: 1970-01-25)

---

## 6. How to Test Acceptance Scenarios in the UI

1. **AC-01 (View Capacity for Date)**:
   - Go to the **Service Availability** tab.
   - Select any date. Observe remaining capacities computed directly from persisted records.
2. **AC-02 (Multi-Service Booking)**:
   - Go to the **Create Booking** tab.
   - Select a patient (e.g. `Juan Dela Cruz`), pick a valid date, select 2 or 3 services (e.g. `CBC`, `URN`), and click **Confirm Booking**.
   - Notice the success notification, remaining capacity decrement, and switch to **Booking History** to view the full details.
3. **AC-03 (Exceeding Capacity / Validation)**:
   - Attempt to book an appointment with 0 services selected, duplicate services, or on a past date.
   - The UI blocks the request with a specific business rule explanation (e.g., `[BR-02]`, `[BR-03]`).
4. **AC-04 (Stale UI & Concurrency Protection)**:
   - On the **Create Booking** tab, select a service (e.g. `ECG`).
   - In the **Verification Scenarios Sandbox**, click **"AC-04: Exhaust capacity in background"**. This books the remaining slots behind the scenes without refreshing the UI screen.
   - Click **"Confirm Booking"**. The transaction boundary re-validates current availability against the database, detects the capacity is 0, and safely aborts with an informative warning banner!
5. **AC-05 (Transaction Rollback on Failure)**:
   - On the **Create Booking** tab, check **"AC-05: Force failure before commit"**.
   - Click **"Confirm Booking"**.
   - The application triggers an intentional exception right before commit. The transaction rolls back cleanly; no partial bookings or corrupted records exist in the database.
6. **AC-06 (Persistence Across Restart)**:
   - Confirm a booking in the application.
   - Close the application window.
   - Relaunch (`dotnet run --project src/CareFlow`).
   - Go to **Booking History**. The booking and all its included services remain intact.

---

## 7. Academic Integrity & Acknowledgements
This project was developed for **ITSD81 Desktop Application Development**. Third-party libraries utilized include **Avalonia UI**, **CommunityToolkit.Mvvm**, and **Microsoft Entity Framework Core**. AI-assisted tools were consulted for architecture scaffolding and documentation in compliance with course policy.
