# TutorMamu – Backend Template

Tutor marketplace for Bangladesh: students subscribe to a topic ("where there is a problem, there is a solution"),
book 1h / 2h / 3h classes with a dedicated tutor, pay through the platform, and the platform keeps a commission.

Clean architecture, same layout as Akkhor: `Domain` → `Application` → `Infrastructure` → `API`.
This is a template: no migrations, no real payment gateway, no real SMS/email. Those are marked with TODOs.

## Structure

```
src/
  TutorMamu.Domain          entities, enums (Topic, TutorProfile, Booking, Payment, Review ...)
  TutorMamu.Application     DTOs, interfaces, services (BookingService has price + escrow logic)
  TutorMamu.Infrastructure  EF Core DbContext, repositories, JWT/Auth, notification stub, seeder
  TutorMamu.API             controllers (Admin / Student / Teacher), middleware, expiry worker
tests/
  TutorMamu.Tests           pricing unit tests
```

## Run

1. Install the .NET 8 SDK and SQL Server (or LocalDB).
2. Set the JWT key (do not keep the placeholder):
   `dotnet user-secrets set "Jwt:Key" "<random 32+ char secret>" --project src/TutorMamu.API`
3. Create the first migration (one time):
   `dotnet ef migrations add InitialCreate -p src/TutorMamu.Infrastructure -s src/TutorMamu.API`
4. `dotnet run --project src/TutorMamu.API` and open http://localhost:5080/swagger
   In Development it applies migrations and seeds roles, an admin (`SeedAdmin` in appsettings) and sample topics.
5. `dotnet test`

## Main flow

1. `POST /api/auth/register` as Teacher, then `PUT /api/teacher/profile` (rate, bio, topic ids).
2. Admin: `GET /api/admin/tutors` then `PUT /api/admin/tutors/{id}/status` with `{ "status": "Approved" }`.
3. Student: `GET /api/tutors?topicId=...` → `POST /api/student/bookings` (1–3 hours, price auto-calculated).
4. `POST /api/student/bookings/{id}/pay` → money held in escrow, tutor notified (SMS + email stub).
5. Tutor: `POST /api/teacher/bookings/{id}/respond` within the response window (default 2h), otherwise the
   background worker refunds the student.
6. Student: `POST /api/student/bookings/{id}/confirm` → payment released, commission kept by the platform.

## Settings (`appsettings.json`)

`Marketplace:CommissionRate` (default 0.20, a test value), `ResponseWindowHours`, `MinDurationHours`, `MaxDurationHours`.

## Next to build

- Payment gateway (bKash / Nagad / card) and verified callback calling `BookingService.PayAsync`
- Real SMS + email in `LoggingNotificationService`
- Topic packs (monthly subscription per topic), reviews endpoints, tutor document upload
- Phone OTP login, rate limiting, audit logs
