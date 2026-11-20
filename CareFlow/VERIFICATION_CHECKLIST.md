# CareFlow — Acceptance & Verification Checklist

This checklist documents the verification of all mandatory business rules and acceptance scenarios defined in **ITSD81 Performance Task 4: CareFlow - Diagnostic Booking Management**.

All scenarios have been verified using both **automated xUnit integration tests** and **functional interactive UI testing**.

---

## 1. Acceptance Scenarios Matrix

| Scenario ID | Scenario Name & Description | Applicable Business Rules | Verification Method | Status | Notes & Evidence |
| :---: | :--- | :--- | :--- | :---: | :--- |
| **AC-01** | **View diagnostic services for a selected date**<br>Remaining capacity is correctly calculated from persisted information. | BR-04, BR-05, BR-09 | Automated Test:<br>`AC01_ViewDiagnosticServices_CalculatesRemainingCapacityCorrectly`<br><br>UI Verification:<br>Service Availability Tab | **VERIFIED (PASSED)** | Capacity is computed dynamically via SQL join/count. Initial slots (e.g. CBC = 5). After 1 booking, capacity updates to 4 remaining. |
| **AC-02** | **Create a booking containing multiple available services**<br>One confirmed booking is recorded, including every selected service. | BR-01, BR-02, BR-04, BR-06, BR-07, BR-09 | Automated Test:<br>`AC02_CreateBookingWithMultipleServices_RecordsOneConfirmedBooking`<br><br>UI Verification:<br>Create Booking Tab -> Select 3 services -> Confirm | **VERIFIED (PASSED)** | Booking reference (e.g. `CF-20261118-XXXX`) is issued. Database persists 1 `Booking` row and 3 `BookingItem` child rows linked via foreign keys. |
| **AC-03** | **Request a fully booked service or submit invalid booking information**<br>The request is rejected without creating incomplete or invalid records. | BR-01, BR-02, BR-03, BR-05, BR-06, BR-10 | Automated Test:<br>`AC03_RequestFullyBookedServiceOrInvalidInfo_IsRejectedWithoutIncompleteRecords`<br><br>UI Verification:<br>Select full service -> Submit | **VERIFIED (PASSED)** | Rejection throws `ServiceCapacityExceededException` or `CareFlowValidationException`. Verified zero partial records or orphaned booking records exist in DB. |
| **AC-04** | **Attempt a booking after previously displayed capacity becomes unavailable**<br>Availability is revalidated, and the system prevents an overbooking. | BR-05, BR-07, BR-08, BR-10 | Automated Test:<br>`AC04_AttemptBookingAfterCapacityBecomesUnavailable_RevalidatesAndPreventsOverbooking`<br><br>UI Verification:<br>Click "AC-04: Exhaust capacity in background" -> Click "Confirm Booking" | **VERIFIED (PASSED)** | Staff's stale UI screen shows remaining slots, but background transaction took the last slot. Re-check inside transaction detects 0 capacity, aborts, and prevents overbooking. |
| **AC-05** | **A required database operation fails before a booking is committed**<br>No partial booking or incorrect capacity change is persisted. | BR-07, BR-10 | Automated Test:<br>`AC05_DatabaseOperationFailsBeforeCommit_RollsBackWithoutPartialRecordsOrCapacityChange`<br><br>UI Verification:<br>Check "AC-05: Force failure before commit" -> Click "Confirm Booking" | **VERIFIED (PASSED)** | `InvalidOperationException` triggered before `CommitAsync()`. Transaction executes `RollbackAsync()`. Total booking and item counts remain strictly identical to pre-test state. |
| **AC-06** | **Close and restart the application after confirming a booking**<br>Previously recorded bookings and their associated services remain available. | BR-09 | Automated Test:<br>`AC06_RestartApplication_PreviouslyRecordedBookingsPersist`<br><br>UI Verification:<br>Confirm booking -> Exit application -> Relaunch -> Check Booking History tab | **VERIFIED (PASSED)** | Database context disposed and reopened against persisted SQLite database file (`careflow.db`). All booking records, references, patient relations, and line items load intact. |

---

## 2. Mandatory Business Rules Verification

| Rule ID | Rule Statement | Implementation Location | Test Method | Result |
| :---: | :--- | :--- | :--- | :---: |
| **BR-01** | Each booking must reference an existing patient. | `BookingService.cs` (`_dbContext.Patients.AnyAsync`) | `BR01_BookingWithNonExistentPatient_ThrowsValidationException` | **PASSED** |
| **BR-02** | Each booking must contain one to three distinct diagnostic services. | `BookingService.cs` (`SelectedServiceIds.Count`) | `BR02_BookingWithEmptyServices_ThrowsValidationException`<br>`BR02_BookingWithMoreThanThreeServices_ThrowsValidationException` | **PASSED** |
| **BR-03** | The booking date cannot be earlier than current local date. | `BookingService.cs` (`request.BookingDate < today`) | `BR03_BookingWithPastDate_ThrowsValidationException` | **PASSED** |
| **BR-04** | Each selected service consumes one unit of its daily booking capacity. | `DiagnosticCatalogService.cs` / `BookingService.cs` | `AC01_ViewDiagnosticServices_CalculatesRemainingCapacityCorrectly` | **PASSED** |
| **BR-05** | Confirmed bookings cannot exceed the daily capacity of a diagnostic service. | `BookingService.cs` (`currentBookedCount + 1 > service.DailyCapacity`) | `BR05_BookingExceedingServiceDailyCapacity_ThrowsServiceCapacityExceededException` | **PASSED** |
| **BR-06** | The same service cannot appear more than once in a booking request. | `BookingService.cs` (`request.SelectedServiceIds.Distinct().Count()`) | `BR06_BookingWithDuplicateServices_ThrowsValidationException` | **PASSED** |
| **BR-07** | All required booking changes must be committed together, or none must be committed. | `BookingService.cs` (`await using var transaction = await _dbContext.Database.BeginTransactionAsync()`) | `AC05_DatabaseOperationFailsBeforeCommit_RollsBackWithoutPartialRecordsOrCapacityChange` | **PASSED** |
| **BR-08** | Availability must be rechecked when the booking is confirmed, rather than relying solely on previously displayed information. | `BookingService.cs` (Capacity counted inside transaction query) | `AC04_AttemptBookingAfterCapacityBecomesUnavailable_RevalidatesAndPreventsOverbooking` | **PASSED** |
| **BR-09** | Confirmed booking information must persist between application executions. | `CareFlowDbContext.cs` / SQLite file storage | `AC06_RestartApplication_PreviouslyRecordedBookingsPersist` | **PASSED** |
| **BR-10** | Invalid requests and unsuccessful database operations must produce understandable feedback. | `NewBookingViewModel.cs` / `CareFlowValidationException.cs` | Functional UI feedback banners and explicit exception messages | **PASSED** |

---

## 3. Summary of Test Run Results

```text
Test run for CareFlow.Tests.dll (.NETCoreApp,Version=v10.0)
Total tests: 12
Passed: 12
Failed: 0
Skipped: 0
Duration: ~1.0 s
```

All acceptance criteria and business rules are satisfied with 100% test pass rate.
