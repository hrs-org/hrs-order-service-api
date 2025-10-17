# HikingRentalStore – Setup Guide

A .NET 8 Web API + MySQL backend for the Hiking Rental Store.  
This document explains how to set up the project on **Windows** or **macOS**, using **Visual Studio (VS)**, **Visual Studio Code (VS Code)**, or **JetBrains Rider**.

---

## 📋 Prerequisites

### Common (MAC use homebrew)

-   [.NET 8 SDK - 8.0.119](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
-   [MySQL Server - 8.0.43](https://dev.mysql.com/downloads/mysql/)
-   [Git](https://git-scm.com/downloads)
-   [DBeaver](https://dbeaver.io/download/)

### Windows

-   [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (install **ASP.NET and web development** workload)  
    or
-   [VS Code](https://code.visualstudio.com/) with extensions:
    -   `ms-dotnettools.csharp`
    -   `EditorConfig.EditorConfig`  
        or
-   [JetBrains Rider](https://www.jetbrains.com/rider/)

### macOS

-   Install .NET via Homebrew:
    ```bash
    brew install dotnet@8.0.119
    ```
-   Install MySQL:
    ```bash
    brew install mysql@8.0.43
    ```
-   Start MySQL:
    ```bash
    brew services start mysql@8.0
    ```
-   Verify MySQL is running:
    ```bash
    brew services list
    ```
-   mysql CLI:
    ```bash
    mysql -u root -p
    ```
-   IDE options: **Rider** or **VS Code** (Visual Studio for Mac is deprecated).

---

## ⚙️ Project Structure

```
HikingRentalStore.sln
├── HRS.API              → ASP.NET Core Web API (controllers, JWT, DI)
├── HRS.Infrastructure   → EF Core DbContext, repositories
├── HRS.Domain           → Entities and enums
└── HRS.Migrations       → EF Core migrations project
```

---

## 🛠️ Local Setup

### 1. Clone Repository (SKIP THIS FOR NOW, NO REPO YET)

```bash
git clone <->
cd HikingRentalStore
```

### 2. Configure MySQL

Create a database and user:

```sql
CREATE DATABASE hrsdb;
CREATE USER 'devuser'@'localhost' IDENTIFIED BY 'devpassword';
GRANT ALL PRIVILEGES ON HikingRentalStore.* TO 'devuser'@'localhost';
```

Update **`appsettings.Development.json`**:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;port=3306;database=hrsdb;user=devuser;password=devpassword"
}
```

### 3. Apply Git Hooks

Ensure code quality:

```bash
chmod +x .git/hooks/pre-push
```

---

## 📦 Database Migration

Run EF Core migration:

```bash
dotnet ef database update --project HRS.Migrations --startup-project HRS.API
```

This will update your current database

Create EF Core migration:

```bash
dotnet ef migrations add <migration-name> --project HRS.Migrations --startup-project HRS.API
```

---

## 🔑 JWT Configuration (MOVE IT TO ENV VARIABLE LATER, NO NEED TO PAY ATTENTION TO THIS FOR NOW)

In **`appsettings.Development.json`**:

```json
"Jwt": {
  "Key": "your-very-long-random-secret",
  "Issuer": "HRSApp",
  "Audience": "HRSUsers"
}
```

For Railway or other hosts, configure as environment variables:

```
Jwt__Key=your-secret
Jwt__Issuer=HRSApp
Jwt__Audience=HRSUsers
```

---

## ▶️ Running the API

### CLI

```bash
dotnet run --project HRS.API
```

Swagger UI:  
👉 [https://localhost:5001/swagger](https://localhost:5001/swagger)

### Rider

-   Open `HikingRentalStore.sln`
-   Set **HRS.API** as startup project
-   Run

### Visual Studio

-   Open solution
-   Right-click `HRS.API` → _Set as Startup Project_
-   Press **F5**

### VS Code

-   Install `C# Dev Kit` + `EditorConfig`
-   Run:
    ```bash
    dotnet run --project HRS.API
    ```

---

## 🧹 Code Style & Linting

-   This repo uses **.editorconfig** to enforce:
    -   4-space indent
    -   `_camelCase` private fields
    -   `IUpperCamelCase` interfaces
    -   `PascalCase` for classes/methods
    -   Async methods end with `Async`

### Apply Automatically

-   **Rider** → enable _Actions on Save → Reformat Code_
-   **VS** → supported by default
-   **VS Code** → install _EditorConfig_ extension

### Manual Apply

```bash
dotnet format
```

---

## ✅ Verify Setup

1. Run the API:
    ```bash
    dotnet run --project HRS.API
    ```
2. Open Swagger at:
    ```
    https://localhost:5001/swagger
    ```
3. Login with seeded admin:
    ```json
    {
        "email": "admin@hrs.com",
        "password": "Admin123!"
    }
    ```
4. Authorize 🔒 in Swagger with the returned JWT. Value: "Bearer {token}" - replace your with your token from login
5. Access secure endpoints.

---

## 🧑‍🤝‍🧑 Team Notes

-   Always use **async/await** for DB calls.
-   Don’t commit secrets; use **environment variables** for `Jwt:Key` and DB passwords.
-   Frontend apps (Angular PWA) can point to `https://localhost:5001` during dev.
-   In production (Railway), use environment variables for all sensitive settings.
