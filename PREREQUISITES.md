# PREREQUISITES — Complete Setup Guide

> This guide explains how to get the application running on your local machine.
> Since the application runs completely offline and stores data in memory, no database setup is required.

---

## What You Will Install

| Software | Why | Required |
|----------|-----|----------|
| Git | Downloads the project from GitHub | Yes |
| .NET 8 SDK | Runs the backend server and test suite | Yes |
| Browser | Runs the frontend UI | Yes (already installed) |

---

## Step 1 — Install Git

1. Go to: **https://git-scm.com/download/win**
2. Click the download button for Windows.
3. Run the downloaded installer and accept the defaults.
4. Open a terminal and run `git --version` to verify.

---

## Step 2 — Install .NET 8 SDK

1. Go to: **https://dotnet.microsoft.com/download/dotnet/8.0**
2. Click **Download .NET SDK x64** (under Windows).
3. Run the installer and click through to finish.
4. Restart your terminal and run `dotnet --version` to verify.

---

## Step 3 — Clone the Project

```bash
git clone https://github.com/AbhijitPatilAJ/Car-rental.git
cd Car-rental
```

---

## Step 4 — Start the Backend

```bash
cd CarRental.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

Keep this terminal window running.

---

## Step 5 — Open the Frontend

Navigate to the `skyroute-ui` folder inside your cloned directory and double-click `index.html` to open it in any browser.

---

## Step 6 — Run the Test Suite

Open a new terminal window, navigate to the project root directory, and run:

```bash
dotnet test
```

Expected output:
```
Passed! - Failed: 0, Passed: 56, Skipped: 0, Total: 56
```
