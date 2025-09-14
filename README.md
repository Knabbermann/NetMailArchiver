# NetMailArchiver

A robust .NET 8 web application for archiving emails from IMAP servers into a PostgreSQL database with a modern web interface for searching and viewing archived emails.

## Features

- **📧 IMAP Email Archiving**: Connect to any IMAP email server and archive emails locally
- **🔍 Advanced Search**: Search through archived emails by sender, subject, date, and content
- **📎 Attachment Support**: Download and store email attachments with full metadata
- **⏰ Automated Archiving**: Schedule automatic email archiving using Quartz.NET
- **🌐 Web Interface**: Modern, responsive web interface for managing and viewing emails
- **🐳 Containerized**: Ready-to-deploy Docker containers and Kubernetes manifests
- **📊 Progress Tracking**: Real-time progress monitoring for archiving operations
- **🔒 Secure**: Encrypted storage of IMAP credentials and secure authentication

## Architecture

The application follows a clean architecture pattern with the following projects:

- **NetMailArchiver.Web**: ASP.NET Core web application with Razor Pages
- **NetMailArchiver.Services**: Business logic and IMAP service implementations
- **NetMailArchiver.DataAccess**: Entity Framework Core data access layer
- **NetMailArchiver.Models**: Domain models and entities

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- Docker (optional, for containerized deployment)
- Kubernetes cluster (optional, for Kubernetes deployment)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/Knabbermann/NetMailArchiver.git
cd NetMailArchiver
```

### 2. Configure Database

Update the connection string in `NetMailArchiver.Web/Program.cs`:

```csharp
options.UseNpgsql("Host=your-postgres-host;Database=NetMailArchiver;Username=your-username;Password=your-password")
```

### 3. Run Database Migrations

```bash
cd NetMailArchiver.Web
dotnet ef database update
```

### 4. Build and Run

```bash
dotnet restore
dotnet build
dotnet run --project NetMailArchiver.Web
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Configuration

### IMAP Server Setup

1. Navigate to the web interface
2. Add your IMAP server configuration:
   - **Host**: Your IMAP server hostname
   - **Port**: IMAP port (usually 993 for SSL, 143 for STARTTLS)
   - **Username**: Your email username
   - **Password**: Your email password
   - **Use SSL**: Enable for secure connections
   - **Auto Archive**: Enable automatic archiving
   - **Archive Interval**: Set archiving frequency

### Environment Variables

You can override configuration using environment variables:

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` or `Production`
- `DATABASE_CONNECTION_STRING`: PostgreSQL connection string

## Docker Deployment

### Build Docker Image

```bash
docker build -t netmailarchiver:latest -f NetMailArchiver.Web/Dockerfile .
```

### Run with Docker Compose

Create a `docker-compose.yml`:

```yaml
version: '3.8'
services:
  netmailarchiver:
    image: netmailarchiver:latest
    ports:
      - "8080:8080"
    environment:
      - DATABASE_CONNECTION_STRING=Host=postgres;Database=NetMailArchiver;Username=postgres;Password=yourpassword
    depends_on:
      - postgres
      
  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=NetMailArchiver
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=yourpassword
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

Run with:

```bash
docker-compose up -d
```

## Kubernetes Deployment

Deploy to Kubernetes using the provided manifests:

```bash
kubectl apply -f k8s/
```

The deployment includes:
- Application deployment with configurable replicas
- Service for load balancing
- Persistent volume claims for database storage

## Usage

### Adding IMAP Accounts

1. Open the web interface
2. Click "Add IMAP Account" (if no accounts exist)
3. Fill in your IMAP server details
4. Test the connection
5. Save the configuration

### Archiving Emails

**Manual Archiving:**
- Click "Archive New Mails" to archive only new emails
- Click "Archive All Mails" to archive all emails from the server

**Automatic Archiving:**
- Enable "Auto Archive" in the IMAP configuration
- Set the desired archive interval
- The system will automatically archive new emails based on the schedule

### Searching and Viewing

1. Navigate to the "Archive" section
2. Use the search box to find specific emails
3. Click "Open" to view email content in a new window
4. Click "Download Attachments" to download email attachments

## API Endpoints

The application provides several API endpoints for integration:

- `GET /Archive/Index?handler=GetEmails`: Retrieve archived emails (with pagination)
- `GET /Archive/Index?handler=OpenEmail`: Open a specific email
- `GET /Archive/Index?handler=DownloadAttachment`: Download email attachments
- `GET /?handler=ArchiveProgress`: Get archiving progress status

## Development

### Setting up Development Environment

1. Install .NET 8 SDK
2. Install PostgreSQL locally
3. Clone the repository
4. Restore packages: `dotnet restore`
5. Update database: `dotnet ef database update --project NetMailArchiver.Web`
6. Run the application: `dotnet run --project NetMailArchiver.Web`

### Project Structure

```
NetMailArchiver/
├── NetMailArchiver.Web/           # Web application (Razor Pages)
├── NetMailArchiver.Services/      # Business logic and services
├── NetMailArchiver.DataAccess/    # Entity Framework data access
├── NetMailArchiver.Models/        # Domain models
├── k8s/                          # Kubernetes manifests
└── README.md
```

### Key Technologies

- **ASP.NET Core 8**: Web framework
- **Entity Framework Core**: ORM for database access
- **MailKit**: IMAP client library
- **Quartz.NET**: Job scheduling
- **PostgreSQL**: Database
- **Bootstrap**: UI framework
- **jQuery**: Frontend interactions

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Guidelines

1. Follow existing code style and conventions
2. Add appropriate tests for new functionality
3. Update documentation as needed
4. Ensure all tests pass before submitting PR

## License

This project is open source. Please check the repository for license details.

## Support

If you encounter any issues or have questions:

1. Check the [Issues](https://github.com/Knabbermann/NetMailArchiver/issues) page
2. Create a new issue with detailed information about your problem
3. Provide logs and configuration details when reporting bugs

## Roadmap

Future enhancements may include:

- Support for additional email protocols (POP3, Exchange)
- Email backup and export functionality
- Advanced filtering and tagging
- Multi-user support with authentication
- Email analytics and reporting
- Mobile-responsive improvements

---

**Note**: This application handles sensitive email data. Ensure proper security measures are in place when deploying to production environments.