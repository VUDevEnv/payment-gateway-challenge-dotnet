using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace PaymentGateway.Architecture.Tests
{
    public class ProjectsArchitectureTests
    {
        private const string ApiNamespace = "PaymentGateway.Api";
        private const string ApplicationNamespace = "PaymentGateway.Application";
        private const string DomainNamespace = "PaymentGateway.Domain";
        private const string InfrastructureNamespace = "PaymentGateway.Infrastructure";

        #region Dependency Rules

        [Fact(DisplayName = "Domain should not depend on Application, Infrastructure or API")]
        public void Domain_Should_Not_Have_External_Dependencies()
        {
            var domainAssembly = Assembly.Load(DomainNamespace);

            var result = Types
                .InAssembly(domainAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Domain layer must be fully isolated.");
        }

        [Fact(DisplayName = "Domain should only depend on base libraries")]
        public void Domain_Should_Only_Depend_On_Base_Libraries()
        {
            var domainAssembly = Assembly.Load(DomainNamespace);

            var result = Types
                .InAssembly(domainAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft",
                    "AutoMapper",
                    "FluentValidation",
                    "Newtonsoft.Json",
                    "EntityFrameworkCore",
                    ApplicationNamespace,
                    InfrastructureNamespace,
                    ApiNamespace
                )
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Domain must not depend on infrastructure, app-specific libraries, or 3rd-party frameworks.");
        }

        [Fact(DisplayName = "Application should not depend on Infrastructure or API")]
        public void Application_Should_Not_Have_External_Dependencies()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var result = Types
                .InAssembly(applicationAssembly)
                .ShouldNot()
                .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Application layer must be decoupled from Infrastructure and API.");
        }

        [Fact(DisplayName = "Infrastructure should not depend on API")]
        public void Infrastructure_Should_Not_Depend_On_Api()
        {
            var infrastructureAssembly = Assembly.Load(InfrastructureNamespace);

            var result = Types
                .InAssembly(infrastructureAssembly)
                .ShouldNot()
                .HaveDependencyOn(ApiNamespace)
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Infrastructure layer should not depend on API.");
        }

        #endregion

        #region Namespace & Placement Rules

        [Fact(DisplayName = "Controllers should reside in Api.Controllers namespace")]
        public void Controllers_Should_Reside_In_Api_Controllers_Namespace()
        {
            var apiAssembly = Assembly.Load(ApiNamespace);

            var result = Types
                .InAssembly(apiAssembly)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Controller")
                .Should()
                .ResideInNamespace($"{ApiNamespace}.Controllers")
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Controllers must reside in Api.Controllers namespace.");
        }

        [Fact(DisplayName = "Repositories should reside in Domain.Repositories namespace")]
        public void Repositories_Should_Reside_In_Domain_Repositories_Namespace()
        {
            var domainAssembly = Assembly.Load(DomainNamespace);

            var result = Types
                .InAssembly(domainAssembly)
                .That()
                .HaveNameEndingWith("Repository")
                .Should()
                .ResideInNamespace($"{DomainNamespace}.Interfaces.Repositories")
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Repository interfaces should be placed in Domain.Interfaces.Repositories namespace.");
        }

        [Fact(DisplayName = "Handlers should reside in Application.Commands or Application.Handlers")]
        public void Handlers_Should_Be_In_Commands_Or_Handlers_Namespace()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var handlers = Types
                .InAssembly(applicationAssembly)
                .That()
                .AreClasses()                    
                .And()
                .HaveNameEndingWith("Handler")
                .GetTypes();

            var invalidHandlers = handlers
                .Where(t =>
                    !t.Namespace!.StartsWith($"{ApplicationNamespace}.Commands") &&
                    !t.Namespace!.StartsWith($"{ApplicationNamespace}.Handlers"))
                .ToList();

            invalidHandlers.Should().BeEmpty("Handlers must reside in either Application.Commands or Application.Handlers namespace.");
        }

        #endregion

        #region Naming Conventions

        [Fact(DisplayName = "Service classes should end with 'Service' suffix")]
        public void Services_Should_Have_Service_Suffix()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var result = Types
                .InAssembly(applicationAssembly)
                .That()
                .AreClasses()
                .And()
                .ResideInNamespace($"{ApplicationNamespace}.Services")
                .Should()
                .HaveNameEndingWith("Service")
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Service classes should follow '*Service' naming convention.");
        }

        [Fact(DisplayName = "Interfaces should start with 'I'")]
        public void Interfaces_Should_Start_With_I()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var result = Types
                .InAssembly(applicationAssembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Interfaces should follow the 'I*' naming convention.");
        }

        [Fact(DisplayName = "DTOs should follow '*Request', '*Response' or '*Dto' naming convention")]
        public void Dtos_Should_Have_Correct_Suffix()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var dtos = Types
                .InAssembly(applicationAssembly)
                .That()
                .AreClasses()
                .And()
                .ResideInNamespace($"{ApplicationNamespace}.DTOs")
                .GetTypes();

            var invalidDtos = dtos
                .Where(t => !t.Name.EndsWith("Request")
                            && !t.Name.EndsWith("Response")
                            && !t.Name.EndsWith("Dto"))
                .ToList();

            invalidDtos.Should().BeEmpty("DTO classes should end with 'Request', 'Response' or 'Dto'.");
        }

        #endregion

        #region DTO Isolation

        [Fact(DisplayName = "DTOs should not reference Domain models")]
        public void Dtos_Should_Not_Depend_On_Domain_Models()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var result = Types
                .InAssembly(applicationAssembly)
                .That()
                .ResideInNamespace($"{ApplicationNamespace}.DTOs")
                .ShouldNot()
                .HaveDependencyOn(DomainNamespace)
                .GetResult();

            result.IsSuccessful.Should().BeTrue("DTOs must be isolated from Domain models.");
        }

        #endregion

        #region Interface & Repository Rules

        [Fact(DisplayName = "Repositories should be interfaces")]
        public void Repositories_Should_Be_Interfaces()
        {
            var applicationAssembly = Assembly.Load(ApplicationNamespace);

            var result = Types
                .InAssembly(applicationAssembly)
                .That()
                .HaveNameEndingWith("Repository")
                .Should()
                .BeInterfaces()
                .GetResult();

            result.IsSuccessful.Should().BeTrue("Repositories should be declared as interfaces.");
        }

        #endregion
    }
}
