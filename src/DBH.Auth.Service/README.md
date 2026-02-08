# DBH.Auth.Service

Central Authentication Service for the DBH EHR System. Handles user registration, login (JWT), refresh tokens, and role-based access control.

## 🚀 Getting Started

### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)
*   [PostgreSQL Client](https://www.postgresql.org/download/) (Optional, for manual DB checks)

---

### 🐳 Running with Docker (Recommended)

This sets up both the **Database** and the **Auth Service** automatically.

1.  **Navigate to the project directory**:
    ```bash
    cd src/DBH.Auth.Service
    ```

2.  **Start the services**:
    ```bash
    docker-compose up -d --build
    ```

3.  **Access the application**:
    *   **Swagger UI**: [http://localhost:5001/swagger](http://localhost:5001/swagger)
    *   **Health Check**: [http://localhost:5001/health](http://localhost:5001/health)

4.  **Database Connection (External)**:
    *   **Host**: `localhost`
    *   **Port**: `5431`
    *   **User**: `dbh_admin`
    *   **Password**: `dbh_123`
    *   **Database**: `dbh_ehr`

---

### 💻 Running Locally (Development)

If you want to debug the C# code while keeping the database in Docker.

1.  **Start ONLY the Database**:
    ```bash
    docker-compose up -d auth-db
    ```
    *(Ensure the container is running on port 5431)*.

2.  **Run the Application**:
    ```bash
    dotnet run
    ```

3.  **Access the application**:
    *   **Swagger UI**: [http://localhost:5048/swagger](http://localhost:5048/swagger)

---

### 🛠️ Database Migrations

If you modify the database schema (Models), create a new migration:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

*(Note: `dotnet ef` tool must be installed globally: `dotnet tool install --global dotnet-ef`)*

---

### ⚠️ Troubleshooting

**1. "Database dbh_ehr does not exist"**
If the database wasn't created automatically:
```bash
docker exec -i dbh_auth_db createdb -p 5431 -U dbh_admin dbh_ehr
docker-compose restart auth-service
```

**2. "Connection Refused"**
Ensure no other service is using port **5431**. Check running containers:
```bash
docker ps
```
