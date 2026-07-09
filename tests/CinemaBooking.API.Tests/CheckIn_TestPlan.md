# Staff Check-In Functionality - Test Plan

## Overview
This document outlines the comprehensive test plan for the staff check-in feature in the Cinema Booking system. The check-in functionality allows staff members to scan customer QR codes and check them in for their booked showtimes.

## Test Scope

### Features Under Test
1. **Lookup Endpoint** (`POST /api/checkins/lookup`) - Staff can scan a QR code to preview booking details before check-in
2. **Check-In Endpoint** (`POST /api/checkins`) - Staff can perform the actual check-in operation
3. **History Endpoint** (`GET /api/checkins/history`) - Staff, managers, and admins can view check-in history

## Test Scenarios

### 1. Lookup Endpoint Tests

#### 1.1 Happy Path Scenarios
- **TC-LOOKUP-001**: Valid QR code lookup by authorized staff
  - **Given**: Staff is logged in with valid credentials from Cinema A
  - **And**: A valid booking exists for a showtime at Cinema A
  - **When**: Staff scans the QR code via lookup endpoint
  - **Then**: System returns complete booking details including movie, cinema, room, showtime, seats, and products
  - **Expected Result**: HTTP 200 with booking data

#### 1.2 Authorization & Security
- **TC-LOOKUP-002**: Unauthorized access attempt (no authentication)
  - **Expected Result**: HTTP 401 Unauthorized

- **TC-LOOKUP-003**: Insufficient role (Customer role attempting access)
  - **Expected Result**: HTTP 403 Forbidden

- **TC-LOOKUP-004**: Staff from different cinema attempting lookup
  - **Given**: Staff from Cinema A
  - **And**: Booking for showtime at Cinema B
  - **When**: Staff attempts lookup
  - **Expected Result**: HTTP 403 with message "You cannot check in tickets from another cinema."

#### 1.3 Invalid Input Scenarios
- **TC-LOOKUP-005**: QR code not found in system
  - **Expected Result**: HTTP 404 with message "Booking not found."

- **TC-LOOKUP-006**: Missing QR code in request body
  - **Expected Result**: HTTP 400 with validation error

- **TC-LOOKUP-007**: QR code exceeds max length (>100 characters)
  - **Expected Result**: HTTP 400 with validation error

- **TC-LOOKUP-008**: Empty/whitespace QR code
  - **Expected Result**: HTTP 400 with validation error

#### 1.4 Data Integrity Scenarios
- **TC-LOOKUP-009**: Ticket with incomplete booking data (missing booking)
  - **Expected Result**: HTTP 400 with message "Ticket data is incomplete (missing booking)."

- **TC-LOOKUP-010**: Booking with missing showtime/room/cinema data
  - **Expected Result**: HTTP 400 with message "Booking data is incomplete (missing showtime/room/cinema)."

- **TC-LOOKUP-011**: Staff with no cinema assignment
  - **Expected Result**: HTTP 400 with message "Staff cinema assignment not found."

#### 1.5 Check-In Status Display
- **TC-LOOKUP-012**: Lookup booking with already checked-in tickets
  - **Expected Result**: HTTP 200 with CheckedIn=true and individual seat check-in timestamps

- **TC-LOOKUP-013**: Lookup booking with partially checked-in tickets
  - **Expected Result**: HTTP 200 with mixed IsCheckedIn status per seat

### 2. Check-In Endpoint Tests

#### 2.1 Happy Path Scenarios
- **TC-CHECKIN-001**: Valid check-in within time window
  - **Given**: Staff from correct cinema
  - **And**: Current time is between 30 minutes before and 15 minutes after showtime
  - **And**: Booking is paid and not cancelled
  - **And**: Ticket is not already used
  - **When**: Staff performs check-in
  - **Then**: Ticket status changes to "Used"
  - **And**: CheckedInAt timestamp is recorded
  - **And**: Staff ID and IP address are logged
  - **Expected Result**: HTTP 200 with success message, booking code, and check-in timestamp

#### 2.2 Authorization & Security
- **TC-CHECKIN-002**: Unauthorized access attempt
  - **Expected Result**: HTTP 401 Unauthorized

- **TC-CHECKIN-003**: Non-staff role attempting check-in
  - **Expected Result**: HTTP 403 Forbidden

- **TC-CHECKIN-004**: Staff from different cinema
  - **Expected Result**: HTTP 403 with message "You cannot check in tickets from another cinema."

#### 2.3 Payment & Booking Status Validations
- **TC-CHECKIN-005**: Check-in with unpaid booking
  - **Given**: Payment status is "pending" or "failed"
  - **Expected Result**: HTTP 400 with message "Booking has not been paid."

- **TC-CHECKIN-006**: Check-in cancelled booking
  - **Given**: Booking status is "Cancelled"
  - **Expected Result**: HTTP 400 with message "Booking has been cancelled."

- **TC-CHECKIN-007**: Check-in refunded booking
  - **Given**: Booking has a completed refund
  - **Expected Result**: HTTP 400 with message "Ticket has been refunded."

#### 2.4 Ticket Status Validations
- **TC-CHECKIN-008**: Check-in already used ticket
  - **Given**: Ticket status is "Used"
  - **Expected Result**: HTTP 409 Conflict with message "Ticket has already been checked in."

- **TC-CHECKIN-009**: Check-in cancelled ticket
  - **Given**: Ticket status is "Cancelled"
  - **Expected Result**: HTTP 400 with message "This ticket has been cancelled."

- **TC-CHECKIN-010**: Check-in refunded ticket
  - **Given**: Ticket status is "Refunded"
  - **Expected Result**: HTTP 400 with message "This ticket has been refunded."

#### 2.5 Time Window Validations
- **TC-CHECKIN-011**: Check-in too early (>30 minutes before showtime)
  - **Given**: Current time is 31 minutes before showtime start
  - **Expected Result**: HTTP 400 with message "Check-in time has expired."

- **TC-CHECKIN-012**: Check-in at earliest allowed time (30 minutes before)
  - **Expected Result**: HTTP 200 - successful check-in

- **TC-CHECKIN-013**: Check-in at showtime start
  - **Expected Result**: HTTP 200 - successful check-in

- **TC-CHECKIN-014**: Check-in at latest allowed time (15 minutes after start)
  - **Expected Result**: HTTP 200 - successful check-in

- **TC-CHECKIN-015**: Check-in too late (>15 minutes after showtime)
  - **Given**: Current time is 16 minutes after showtime start
  - **Expected Result**: HTTP 400 with message "Check-in time has expired."

#### 2.6 Data Integrity & Audit
- **TC-CHECKIN-016**: IP address logging
  - **Verify**: CheckedInByIP field is populated with client IP
  
- **TC-CHECKIN-017**: Staff ID logging
  - **Verify**: CheckedInBy field is populated with staff user ID

- **TC-CHECKIN-018**: Timestamp accuracy
  - **Verify**: CheckedInAt timestamp is within 1 second of request time

- **TC-CHECKIN-019**: Booking-level check-in update
  - **Verify**: Booking.CheckedInAt is updated when first ticket is checked in

#### 2.7 Invalid Input
- **TC-CHECKIN-020**: Missing QR code
  - **Expected Result**: HTTP 400 with validation error

- **TC-CHECKIN-021**: Ticket not found
  - **Expected Result**: HTTP 404 with message "Ticket not found."

### 3. History Endpoint Tests

#### 3.1 Happy Path Scenarios
- **TC-HISTORY-001**: Retrieve check-in history without filters
  - **Expected Result**: HTTP 200 with paginated list of all check-ins

- **TC-HISTORY-002**: Filter by specific staff ID
  - **Expected Result**: HTTP 200 with check-ins performed by that staff member only

- **TC-HISTORY-003**: Filter by specific cinema ID
  - **Expected Result**: HTTP 200 with check-ins at that cinema only

- **TC-HISTORY-004**: Filter by date range
  - **Given**: From date and To date provided
  - **Expected Result**: HTTP 200 with check-ins within the date range

- **TC-HISTORY-005**: Combine multiple filters
  - **Given**: StaffId, CinemaId, and date range all provided
  - **Expected Result**: HTTP 200 with check-ins matching all criteria

#### 3.2 Pagination
- **TC-HISTORY-006**: Default pagination (page 1, default page size)
  - **Expected Result**: First page of results with correct total count

- **TC-HISTORY-007**: Navigate to specific page
  - **Expected Result**: Correct page of results returned

- **TC-HISTORY-008**: Custom page size
  - **Expected Result**: Results limited to requested page size

- **TC-HISTORY-009**: Empty results (no check-ins match criteria)
  - **Expected Result**: HTTP 200 with empty array and totalCount=0

#### 3.3 Authorization
- **TC-HISTORY-010**: Staff role access
  - **Expected Result**: HTTP 200 - authorized

- **TC-HISTORY-011**: Manager role access
  - **Expected Result**: HTTP 200 - authorized

- **TC-HISTORY-012**: Admin role access
  - **Expected Result**: HTTP 200 - authorized

- **TC-HISTORY-013**: Customer role access attempt
  - **Expected Result**: HTTP 403 Forbidden

- **TC-HISTORY-014**: Unauthenticated access
  - **Expected Result**: HTTP 401 Unauthorized

#### 3.4 Data Completeness
- **TC-HISTORY-015**: Verify all required fields in response
  - **Verify**: Each record contains BookingId, BookingCode, CustomerName, MovieTitle, CinemaName, RoomName, ShowtimeStart, CheckedInAt, StaffName, SeatCount, TotalAmount

## Test Data Requirements

### Test Users
- **Staff User A**: Assigned to Cinema 1, Staff role
- **Staff User B**: Assigned to Cinema 2, Staff role
- **Manager User**: Manager role with access to multiple cinemas
- **Admin User**: Admin role
- **Customer User**: Customer role (for negative testing)

### Test Bookings
- **Booking 1**: Paid, confirmed, Cinema 1, showtime in 20 minutes (valid for check-in)
- **Booking 2**: Paid, confirmed, Cinema 2, showtime in 20 minutes
- **Booking 3**: Unpaid booking
- **Booking 4**: Cancelled booking
- **Booking 5**: Refunded booking
- **Booking 6**: Already checked-in booking
- **Booking 7**: Showtime started 2 hours ago (expired check-in window)
- **Booking 8**: Showtime starts in 1 hour (too early for check-in)

### Test Tickets
- Valid tickets linked to above bookings with QR codes
- Tickets with various statuses: Active, Used, Cancelled, Refunded

## Test Execution Strategy

### Unit Tests
- Mock repositories (ITicketRepository, IBookingRepository)
- Test business logic in CheckInService in isolation
- Focus on validation rules and error conditions

### Integration Tests
- Test full HTTP request/response cycle
- Test with in-memory database or test database
- Verify authorization policies
- Test actual database interactions

### Manual Testing Checklist
- [ ] Test with real QR code scanner device
- [ ] Verify UI responsiveness on staff terminals
- [ ] Test network latency scenarios
- [ ] Verify concurrent check-in attempts
- [ ] Test with production-like data volumes

## Edge Cases & Error Scenarios

1. **Concurrent Check-In**: Two staff members scan the same ticket simultaneously
2. **Network Interruption**: Check-in request times out - verify transaction rollback
3. **Timezone Handling**: Verify UTC conversion for showtime comparisons
4. **Leap Second**: Check-in at exactly midnight during leap second
5. **Database Constraints**: Verify foreign key relationships and cascading behavior
6. **Large Product Orders**: Booking with 20+ products - ensure all display correctly

## Performance Criteria

- Lookup endpoint response time: < 500ms (p95)
- Check-in endpoint response time: < 1000ms (p95)
- History endpoint response time: < 2000ms for 100 records (p95)
- Support concurrent check-ins: 10 simultaneous requests without errors

## Security Testing

- [ ] SQL Injection attempts on QR code input
- [ ] XSS attempts in QR code
- [ ] CSRF token validation
- [ ] JWT token expiration handling
- [ ] Role escalation attempts
- [ ] API rate limiting verification

## Regression Testing

After any changes to check-in functionality, verify:
- [ ] Existing checked-in bookings remain accessible
- [ ] Historical check-in data integrity preserved
- [ ] Audit logs remain complete and accurate
- [ ] Related features (booking, payment, refund) unaffected

## Test Environment Setup

### Prerequisites
- Test database with seed data
- Test users with appropriate roles
- Valid JWT tokens for each role
- QR code generation utility
- Time manipulation capability for testing time windows

### Database State
- Clean state before each test run
- Seed data includes all test scenarios
- Transaction rollback after each test

## Success Criteria

All tests must pass with:
- 100% of happy path scenarios passing
- 100% of authorization tests passing
- 95%+ of edge cases handled correctly
- No critical security vulnerabilities
- Performance criteria met under load testing

## Test Reporting

For each test execution:
- Document test environment details
- Record pass/fail status for each test case
- Log any defects found with reproduction steps
- Track test coverage metrics
- Generate HTML test report
