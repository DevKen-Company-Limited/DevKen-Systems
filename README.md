# Devken CBC School Management System - Clean Architecture Design

## Overview
This document outlines the clean architecture design for a CBC (Competency-Based Curriculum) School Management System using .NET with Repository Manager pattern instead of traditional services.

## Architecture Layers

### 1. Domain Layer (Core)
**Purpose**: Contains business entities, domain logic, and interfaces. No dependencies on other layers.

```
Devken.CBC.SchoolManagement.Domain/
├── Entities/
│   ├── Academic/
│   │   ├── Student.cs
│   │   ├── Teacher.cs
│   │   ├── Subject.cs
│   │   ├── Grade.cs
│   │   ├── LearningArea.cs
│   │   ├── Strand.cs
│   │   ├── SubStrand.cs
│   │   ├── LearningOutcome.cs
│   │   └── Assessment.cs
│   ├── Curriculum/
│   │   ├── CBCLevel.cs (PP1, PP2, Grade 1-6, JHS 1-3)
│   │   ├── Term.cs
│   │   ├── AcademicYear.cs
│   │   └── LessonPlan.cs
│   ├── Administration/
│   │   ├── School.cs
│   │   ├── Class.cs
│   │   ├── ClassRoom.cs
│   │   └── Department.cs
│   ├── Assessment/
│   │   ├── FormativeAssessment.cs
│   │   ├── SummativeAssessment.cs
│   │   ├── CompetencyAssessment.cs
│   │   └── ProgressReport.cs
│   ├── Finance/
│   │   ├── FeeStructure.cs
│   │   ├── Payment.cs
│   │   └── Invoice.cs
│   └── Common/
│       ├── BaseEntity.cs
│       ├── AuditableEntity.cs
│       └── ValueObjects/
├── Enums/
│   ├── CBCLevel.cs
│   ├── AssessmentType.cs
│   ├── CompetencyLevel.cs (Exceeding, Meeting, Approaching, Below)
│   ├── Term.cs
│   └── PaymentStatus.cs
├── Exceptions/
│   ├── DomainException.cs
│   ├── NotFoundException.cs
│   └── ValidationException.cs
└── Interfaces/
    └── (No repository interfaces here - will be in Application layer)
```

### 2. Application Layer
**Purpose**: Contains business logic orchestration, DTOs, repository managers, and use cases.

```
Devken.CBC.SchoolManagement.Application/
├── Common/
│   ├── Models/
│   │   ├── Result.cs
│   │   ├── PaginatedResult.cs
│   │   └── OperationResult.cs
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs
│   │   └── LoggingBehavior.cs
│   └── Interfaces/
│       ├── IDateTime.cs
│       ├── ICurrentUserService.cs
│       └── IEmailService.cs
├── DTOs/
│   ├── Students/
│   │   ├── StudentDto.cs
│   │   ├── CreateStudentDto.cs
│   │   └── UpdateStudentDto.cs
│   ├── Teachers/
│   │   ├── TeacherDto.cs
│   │   ├── CreateTeacherDto.cs
│   │   └── UpdateTeacherDto.cs
│   ├── Assessments/
│   │   ├── AssessmentDto.cs
│   │   ├── CompetencyAssessmentDto.cs
│   │   └── ProgressReportDto.cs
│   ├── Curriculum/
│   │   ├── LessonPlanDto.cs
│   │   └── LearningOutcomeDto.cs
│   └── Finance/
│       ├── PaymentDto.cs
│       └── InvoiceDto.cs
├── RepositoryManagers/
│   ├── Interfaces/
│   │   ├── IRepositoryManager.cs
│   │   ├── IStudentRepository.cs
│   │   ├── ITeacherRepository.cs
│   │   ├── ISubjectRepository.cs
│   │   ├── IAssessmentRepository.cs
│   │   ├── IClassRepository.cs
│   │   ├── ILessonPlanRepository.cs
│   │   ├── IPaymentRepository.cs
│   │   └── IBaseRepository.cs
│   └── Specifications/
│       ├── BaseSpecification.cs
│       ├── StudentSpecifications/
│       │   ├── StudentsByGradeSpec.cs
│       │   ├── StudentsByClassSpec.cs
│       │   └── ActiveStudentsSpec.cs
│       └── AssessmentSpecifications/
│           ├── AssessmentsByTermSpec.cs
│           └── AssessmentsByStudentSpec.cs
├── Features/
│   ├── Students/
│   │   ├── Commands/
│   │   │   ├── CreateStudent/
│   │   │   │   ├── CreateStudentCommand.cs
│   │   │   │   ├── CreateStudentCommandHandler.cs
│   │   │   │   └── CreateStudentCommandValidator.cs
│   │   │   ├── UpdateStudent/
│   │   │   └── DeleteStudent/
│   │   └── Queries/
│   │       ├── GetStudentById/
│   │       ├── GetStudentsByClass/
│   │       └── GetStudentsByGrade/
│   ├── Teachers/
│   │   ├── Commands/
│   │   └── Queries/
│   ├── Assessments/
│   │   ├── Commands/
│   │   │   ├── RecordFormativeAssessment/
│   │   │   ├── RecordSummativeAssessment/
│   │   │   └── GenerateProgressReport/
│   │   └── Queries/
│   │       ├── GetStudentAssessments/
│   │       └── GetClassAssessmentsSummary/
│   ├── Curriculum/
│   │   ├── Commands/
│   │   │   └── CreateLessonPlan/
│   │   └── Queries/
│   │       └── GetLessonPlansByStrand/
│   └── Finance/
│       ├── Commands/
│       │   ├── RecordPayment/
│       │   └── GenerateInvoice/
│       └── Queries/
│           └── GetStudentPaymentHistory/
├── Mappings/
│   └── MappingProfile.cs
└── Validators/
    ├── StudentValidators/
    ├── AssessmentValidators/
    └── PaymentValidators/
```

### 3. Infrastructure Layer
**Purpose**: Implements data access, external services, and cross-cutting concerns.

```
Devken.CBC.SchoolManagement.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   ├── StudentConfiguration.cs
│   │   ├── TeacherConfiguration.cs
│   │   ├── SubjectConfiguration.cs
│   │   ├── AssessmentConfiguration.cs
│   │   └── PaymentConfiguration.cs
│   ├── Repositories/
│   │   ├── RepositoryManager.cs (Main implementation)
│   │   ├── BaseRepository.cs
│   │   ├── StudentRepository.cs
│   │   ├── TeacherRepository.cs
│   │   ├── SubjectRepository.cs
│   │   ├── AssessmentRepository.cs
│   │   ├── ClassRepository.cs
│   │   ├── LessonPlanRepository.cs
│   │   └── PaymentRepository.cs
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Services/
│   ├── DateTimeService.cs
│   ├── EmailService.cs
│   ├── FileStorageService.cs
│   └── ReportGenerationService.cs
├── Identity/
│   ├── ApplicationUser.cs
│   └── IdentityService.cs
└── DependencyInjection.cs
```

### 4. Presentation Layer (API)
**Purpose**: Handles HTTP requests, authentication, and API documentation.

```
Devken.CBC.SchoolManagement.API/
├── Controllers/
│   ├── BaseApiController.cs
│   ├── StudentsController.cs
│   ├── TeachersController.cs
│   ├── SubjectsController.cs
│   ├── AssessmentsController.cs
│   ├── ClassesController.cs
│   ├── LessonPlansController.cs
│   ├── PaymentsController.cs
│   └── ReportsController.cs
├── Filters/
│   ├── ApiExceptionFilter.cs
│   └── ValidationFilter.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

### 5. Web Layer (Optional - for MVC/Blazor)
```
Devken.CBC.SchoolManagement.Web/
├── Pages/ or Views/
├── wwwroot/
└── Program.cs
```

## Key Design Patterns

### Repository Manager Pattern
Instead of traditional services, we use a Repository Manager that coordinates multiple repositories:

```csharp
public interface IRepositoryManager
{
    IStudentRepository Students { get; }
    ITeacherRepository Teachers { get; }
    ISubjectRepository Subjects { get; }
    IAssessmentRepository Assessments { get; }
    IClassRepository Classes { get; }
    ILessonPlanRepository LessonPlans { get; }
    IPaymentRepository Payments { get; }
    
    Task<int> SaveAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### CQRS Pattern (Command Query Responsibility Segregation)
- Commands for write operations (Create, Update, Delete)
- Queries for read operations (Get, List, Search)
- Handled using MediatR library

### Specification Pattern
For complex queries and reusable query logic:

```csharp
public class StudentsByGradeSpec : BaseSpecification<Student>
{
    public StudentsByGradeSpec(string gradeName) 
        : base(s => s.Grade.Name == gradeName)
    {
        AddInclude(s => s.Class);
        AddInclude(s => s.Assessments);
    }
}
```

## CBC-Specific Considerations

### Competency-Based Assessment
- Support for formative and summative assessments
- Four competency levels: Exceeding Expectations, Meeting Expectations, Approaching Expectations, Below Expectations
- Learning Areas: Languages, Mathematics, Environmental Activities, Psychomotor & Creative Activities, Religious Education

### Grade Structure
- PP1 (Pre-Primary 1)
- PP2 (Pre-Primary 2)
- Grade 1-6 (Primary)
- JHS 1-3 (Junior High School)

### Learning Areas by Level
Different learning areas for different CBC levels with appropriate strands and sub-strands.

## Project Dependencies

### Domain Layer
- No external dependencies

### Application Layer
- Domain Layer
- MediatR
- FluentValidation
- AutoMapper

### Infrastructure Layer
- Application Layer
- Entity Framework Core
- Microsoft.AspNetCore.Identity
- Any third-party integrations

### API Layer
- Application Layer
- Infrastructure Layer
- Swashbuckle (Swagger)
- Serilog

## Configuration Files

### Domain Layer - No packages needed

### Application Layer - Packages
```xml
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
<PackageReference Include="MediatR" Version="12.2.0" />
```

### Infrastructure Layer - Packages
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.1" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.1" />
```

### API Layer - Packages
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.1" />
```

## Benefits of This Architecture

1. **Separation of Concerns**: Each layer has a specific responsibility
2. **Testability**: Easy to unit test business logic without infrastructure dependencies
3. **Maintainability**: Changes in one layer don't affect others
4. **Scalability**: Can easily add new features following the same pattern
5. **Repository Manager Pattern**: Centralized data access coordination instead of scattered services
6. **CQRS**: Clear separation between read and write operations
7. **Domain-Driven Design**: Business logic stays in the domain, not scattered across services

## Getting Started

1. Create solution with all projects
2. Set up project references (following dependency rules)
3. Implement Domain entities
4. Create repository interfaces in Application layer
5. Implement repositories and RepositoryManager in Infrastructure
6. Set up DbContext and configurations
7. Implement CQRS handlers in Application layer
8. Create API controllers that use MediatR to send commands/queries

## Notes for Devken Team

- This architecture is specifically tailored for CBC curriculum requirements
- Repository Manager replaces traditional service layer, providing cleaner data access
- All business logic flows through MediatR handlers
- Perfect for team collaboration with clear boundaries
- Easily extendable for future CBC curriculum updates
