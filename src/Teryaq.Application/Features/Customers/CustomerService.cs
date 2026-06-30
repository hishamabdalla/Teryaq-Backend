namespace Teryaq.Application.Features.Customers;

using AutoMapper;
using Teryaq.Application.Common;
using Teryaq.Application.Common.Pagination;
using Teryaq.Application.Common.Validation;
using Teryaq.Application.Features.Customers.Dtos;
using Teryaq.Domain.Features.Customers;
using Teryaq.Domain.Interfaces;

/// <inheritdoc cref="ICustomerService"/>
public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidationService _validationService;
    private readonly IMapper _mapper;

    /// <summary>Initialises a new instance of <see cref="CustomerService"/>.</summary>
    public CustomerService(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IValidationService validationService,
        IMapper mapper)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _validationService = validationService;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task<Result<PaginatedList<CustomerDto>>> GetPagedAsync(
        string? search,
        PaginationParams pagination,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _customerRepository.SearchAsync(search, pagination.Skip, pagination.PageSize, ct);
        var dtos = _mapper.Map<IReadOnlyList<CustomerDto>>(items);
        return Result.Ok(PaginatedList<CustomerDto>.Create(dtos, totalCount, pagination.PageNumber, pagination.PageSize));
    }

    /// <inheritdoc/>
    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer is null)
            return Result.Fail<CustomerDto>(ResultError.NotFound<Customer>(id));
        return Result.Ok(_mapper.Map<CustomerDto>(customer));
    }

    /// <inheritdoc/>
    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(request, ct);
        if (!validation.IsSuccess)
            return Result.Fail<CustomerDto>(validation.Error);

        var customer = Customer.Create(request.Name, request.Phone, request.Email);
        await _customerRepository.AddAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var saved = await _customerRepository.GetByIdAsync(customer.Id, ct);
        return Result.Ok(_mapper.Map<CustomerDto>(saved!));
    }
}
