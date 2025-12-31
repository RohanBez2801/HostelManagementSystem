# Hostel Management System (HostelPro)

HostelPro is a comprehensive Hostel Management System designed to streamline the administrative tasks of managing a student hostel. It provides a unified interface for managing learners, staff, rooms, inventory, parents, financial records, and communications.

## Features

### Core Management
*   **Dashboard:** Real-time overview of hostel statistics including occupancy, maintenance requests, and low stock alerts.
*   **Room Mapping:** Visual or list-based management of hostel rooms and bed assignments.
*   **Learners:** Complete student database with search and filtering capabilities.
*   **Staff Portal:** Manage staff details and roles.

### Operations
*   **Attendance & Leave:** Track daily attendance. Includes a **Roll Call** feature that generates a printable, room-sorted list of students for physical verification.
*   **Discipline:** Record and track disciplinary incidents.
*   **Maintenance:** Log and monitor maintenance requests for rooms and facilities.
*   **Inventory:** Track hostel assets, furniture, and supplies. Monitor quantities and conditions.
*   **Dining & Kitchen:** Log kitchen supplies and meal records.
*   **Events Calendar:** Schedule and track hostel events (meetings, sports, inspections).

### Communication & People
*   **Parents:** Manage parent/guardian contact information and link multiple learners to a single parent account.
*   **Communication Hub:** 
    *   **Email:** Compose and send emails to parents (All, Specific Grade, or Single Parent). Logs all outgoing messages.
    *   **SMS:** (Phase 2 - Planned) Placeholder for future SMS integration.

### Administration
*   **Financials:** Basic financial tracking (fees, expenses, income).
*   **Settings & Personalization:** Configure hostel details (Name, Logo, Contact Info, Fees) and manage application **License Keys**.
*   **User Management:** Role-based access control (Admin vs. Staff).

## Technology Stack

*   **Backend:** ASP.NET Core Web API (.NET 8.0)
*   **Database:** Microsoft Access (`.accdb`) via OLEDB.
*   **Frontend:** Vanilla JavaScript, HTML5, CSS3.
*   **Styling:** Custom CSS with a responsive sidebar layout.

## Prerequisites

Due to the dependency on the Microsoft Access Database Engine (ACE.OLEDB), this application has specific environment requirements:

1.  **Operating System:** Windows (Required for OLEDB support).
2.  **.NET SDK:** .NET 8.0 or later.
3.  **Database Driver:** [Microsoft Access Database Engine 2010 Redistributable](https://www.microsoft.com/en-us/download/details.aspx?id=13255) (or 2016).
    *   *Note:* Ensure the bitness (x86 or x64) of the driver matches your .NET runtime installation.

## Setup and Installation

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/your-repo/HostelManagementSystem.git
    cd HostelManagementSystem
    ```

2.  **Database Setup:**
    *   The application looks for `Data/HostelDb.accdb`.
    *   The application uses a "Code First" approach for table creation. On the first run, it will automatically create necessary tables (`tbl_Parents`, `tbl_CommunicationLog`, `tbl_Events`, etc.) if they do not exist.

3.  **Run the Application:**
    ```bash
    dotnet run
    ```

4.  **Access the UI:**
    *   Open your browser and navigate to `http://localhost:5000` (or the port indicated in the console).

## Project Structure

*   `Controllers/`: ASP.NET Core API Controllers handling business logic and database interactions.
*   `Data/`: Contains the Microsoft Access database file.
*   `wwwroot/`: Static files (HTML, CSS, JS) serving the frontend application.
    *   `js/`: Modular JavaScript files for each feature (e.g., `parents.js`, `communication.js`, `dashboard.js`).
*   `Models/`: (Optional) C# models representing data structures.

## API Endpoints (Key Modules)

*   `api/dashboard`: Aggregated statistics.
*   `api/parent`: CRUD operations for parents and child linking.
*   `api/communication`: Email logging and sending.
*   `api/settings`: System configuration and license activation.
*   `api/inventory`: Inventory management.
*   `api/learner`: Student records.
*   `api/room`: Room assignments.

## License

[MIT License](LICENSE)
