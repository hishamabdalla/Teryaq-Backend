namespace Teryaq.UnitTests.Features.Customers;

using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Customers;
using Teryaq.Application.Features.Customers.Dtos;
using Teryaq.Application.Features.Customers.Profiles;
using Teryaq.Domain.Features.Customers;
using Teryaq.Domain.Interfaces;
using Xunit;

/// <summary>Unit tests for <see cref="CustomerService"/>.</summary>
public sealed class CustomerServiceTests
{
    private readonly ICustomerRepository _repo = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IValidationService _validation = Substitute.For<IValidationService>();
    private readonly IMapper _mapper;
    private readonly CustomerService _sut;

    /// <summary>Initialises a new instance of <see cref="CustomerServiceTests"/>.</summary>
    public CustomerServiceTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CustomerProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _validation.ValidateAsync(Arg.Any<CreateCustomerRequest>(), default).Returns(Result.Ok());

        _sut = new CustomerService(_repo, _uow, _validation, _mapper);
    }

    /// <summary>A valid request creates and returns a customer.</summary>
    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCustomer()
    {
        Customer? captured = null;
        _repo.When(r => r.AddAsync(Arg.Any<Customer>(), default))
            .Do(call => captured = call.Arg<Customer>());
        _repo.GetByIdAsync(Arg.Any<Guid>(), default)
            .Returns(call => Task.FromResult<Customer?>(captured));

        var request = new CreateCustomerRequest("Ahmed Ali", "01012345678", null);
        var result = await _sut.CreateAsync(request);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Ahmed Ali");
        await _uow.Received(1).SaveChangesAsync(default);
    }

    /// <summary>Validation failure short-circuits and does not call SaveChanges.</summary>
    [Fact]
    public async Task CreateAsync_ValidationFails_ReturnsValidationError()
    {
        _validation.ValidateAsync(Arg.Any<CreateCustomerRequest>(), default)
            .Returns(Result.Fail(ResultError.Validation(new Dictionary<string, string[]>
            {
                { "Name", ["Name is required."] },
            })));

        var request = new CreateCustomerRequest(string.Empty, null, null);
        var result = await _sut.CreateAsync(request);

        result.IsFailure.ShouldBeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(default);
    }

    /// <summary>GetByIdAsync returns not-found when the customer does not exist.</summary>
    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), default).Returns(Task.FromResult<Customer?>(null));

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("NotFound");
    }
}
