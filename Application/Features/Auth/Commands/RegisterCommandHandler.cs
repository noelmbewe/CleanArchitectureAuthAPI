using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, string>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageBroker _messageBroker;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IMessageBroker messageBroker)
    {
        _unitOfWork = unitOfWork;
        _messageBroker = messageBroker;
    }

    public async Task<string> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _messageBroker.PublishAsync("email_queue", $"Welcome {user.Email}! Your account is created.");

        return "User registered successfully.";
    }
}