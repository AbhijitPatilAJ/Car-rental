# PREREQUISITES — Complete A-to-Z Setup Guide

> This guide assumes you know nothing about software development.
> Every single step is explained from scratch. Follow every step in order.

---

## What You Will Install

| Software | Why | Required |
|----------|-----|----------|
| Git | Downloads the project from GitHub | Yes |
| .NET 8 SDK | Runs the backend server | Yes |
| MySQL Server 8 | Stores booking data | Yes |
| MySQL Workbench | Visual tool to manage the database | Recommended |
| Chrome / Edge | Runs the frontend | Yes (already installed) |

---

## Step 1 — Check What You Already Have

Open **Command Prompt** (press Windows key, type `cmd`, press Enter) and run:

```
git --version
dotnet --version
mysql --version
```

If a command shows a version number, that tool is already installed — skip its section.

---

## Step 2 — Install Git

1. Go to: **https://git-scm.com/download/win**
2. Click the blue Download button
3. Run the downloaded `.exe`, click Next on every screen, click Install
4. Restart Command Prompt and run `git --version` to verify

---

## Step 3 — Install .NET 8 SDK

1. Go to: **https://dotnet.microsoft.com/download/dotnet/8.0**
2. Click **Download .NET SDK x64** (under Windows)
3. Run the installer, click Install (allow administrator permission)
4. Restart Command Prompt and run `dotnet --version`
5. You should see `8.0.x` — any number after 8.0 is fine

---

## Step 4 — Install MySQL Server

### Download
1. Go to: **https://dev.mysql.com/downloads/installer/**
2. Click Download next to **MySQL Installer for Windows** (the ~400MB file)
3. Click "No thanks, just start my download"

### Install
1. Open the downloaded `.msi` file
2. Setup Type: select **Developer Default** → Next
3. If prompted to install prerequisites: click Execute, then Next
4. Click Execute to install all products, wait for green checkmarks → Next
5. Click Next through Product Configuration
6. Authentication Method: leave default → Next
7. **Root Password**: Type a password you will remember and write it down
   - Example: `MyPassword123!`
   - This is your **MySQL root password** — you will need it for this project
8. Click Next → Execute → Finish
9. Restart Command Prompt, run `mysql --version`

---

## Step 5 — Install MySQL Workbench (Recommended)

1. Go to: **https://dev.mysql.com/downloads/workbench/**
2. Click Download, then "No thanks, just start my download"
3. Run the installer with all defaults

---

## Step 6 — Clone the Project

```
cd %USERPROFILE%\Downloads
git clone https://github.com/YOUR_USERNAME/car-rental.git
cd car-rental
```

Replace `YOUR_USERNAME` with the actual GitHub username.

---

## Step 7 — Configure the Database Connection

1. Run:
   ```
   copy .env.example .env
   notepad .env
   ```
2. Find the line: `DB_PASSWORD=YOUR_MYSQL_PASSWORD_HERE`
3. Replace `YOUR_MYSQL_PASSWORD_HERE` with your MySQL root password
4. Save (Ctrl+S) and close Notepad

---

## Step 8 — Create the Database

```
mysql -u root -p < database\schema.sql
```

Type your MySQL password when prompted. No output = success.

**Verify it worked:**
```
mysql -u root -p -e "USE CarRentalDb; SHOW TABLES;"
```
Expected output: `Bookings`

---

## Step 9 — Start the Backend

```
cd CarRental.Api
dotnet run
```

Expected output:
```
Now listening on: http://localhost:5000
```

Leave this terminal window open.

---

## Step 10 — Open the Frontend

Open File Explorer → navigate to the `car-rental` folder → open `skyroute-ui` → double-click `index.html`.

---

## Final Checklist

- [ ] Git installed (`git --version` works)
- [ ] .NET 8 SDK installed (`dotnet --version` starts with `8.`)
- [ ] MySQL Server running
- [ ] `.env` file exists with your MySQL password
- [ ] `CarRentalDb.Bookings` table exists
- [ ] `dotnet run` shows "Now listening on: http://localhost:5000"
- [ ] `index.html` opens in browser without errors

---

## Common Problems

| Problem | Fix |
|---------|-----|
| "Access denied for user 'root'" | Wrong password in `.env` |
| "Can't connect to MySQL server" | MySQL service not running → Windows Services → MySQL80 → Start |
| "'dotnet' is not recognized" | Restart terminal after .NET install |
| "'mysql' is not recognized" | Add MySQL bin to PATH (see README troubleshooting) |
| "Failed to fetch" in browser | Backend (`dotnet run`) not running |
| "Database does not exist" | Run: `mysql -u root -p < database\schema.sql` |
