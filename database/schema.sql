-- ============================================================
-- SkyRoute Car Rental — MySQL Database Schema
-- ============================================================
-- Run this file ONCE to create the database and table.
--
-- HOW TO RUN:
--   Option A (Command Prompt):
--     mysql -u root -p < database\schema.sql
--
--   Option B (MySQL Workbench):
--     File → Open SQL Script → select this file → Execute (⚡)
-- ============================================================

CREATE DATABASE IF NOT EXISTS CarRentalDb
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE CarRentalDb;

CREATE TABLE IF NOT EXISTS Bookings (
    Id                 INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ReferenceNumber    VARCHAR(50)   NOT NULL,
    Provider           VARCHAR(100)  NOT NULL,
    VehicleId          VARCHAR(100)  NOT NULL,
    Pickup             VARCHAR(200)  NOT NULL,
    FromDate           DATE          NOT NULL,
    ToDate             DATE          NOT NULL,
    TotalPrice         DECIMAL(10,2) NOT NULL,
    InsuranceType      VARCHAR(100)  NOT NULL,
    CancellationPolicy VARCHAR(500)  NOT NULL,
    DriverName         VARCHAR(300)  NOT NULL,
    DocumentType       VARCHAR(50)   NOT NULL,
    DocumentNumber     VARCHAR(200)  NOT NULL,
    ConfirmedAt        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_reference (ReferenceNumber),
    INDEX idx_reference (ReferenceNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Verify: USE CarRentalDb; SHOW TABLES;
-- Expected: Bookings
