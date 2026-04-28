namespace NICE.Platform.Collaboration.Tests.Unit.Features.Collaborations;

using FluentAssertions;
using Moq;
using NUnit.Framework;
using NICE.Platform.Collaboration.Application.Features.Collaborations.Commands.StartCollaboration;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;
using NICE.Platform.Collaboration.Application.Interfaces.Services;

[TestFixture]
[Category("Unit")]
public class StartCollaborationCommandHandlerTests
{
    private Mock<ICollaborationRepository> _collabRepo = null!;
    private Mock<ISignalRNotifier> _notifier = null!;

    [SetUp]
    public void SetUp()
    {
        _collabRepo = new Mock<ICollaborationRepository>();
        _notifier = new Mock<ISignalRNotifier>();
    }

    [Test]
    public async Task Handle_ValidCommand_Should_Return_CollaborationResponse()
    {
        // Arrange
        var command = new StartCollaborationCommand(
            UserId: Guid.NewGuid(),
            AgentId: Guid.NewGuid(),
            ApplicationId: Guid.NewGuid());

        // TODO: setup mocks and instantiate handler
        // var handler = new StartCollaborationCommandHandler(_collabRepo.Object, _notifier.Object, ...);

        // Act + Assert — replace with real assertions once handler is implemented
        await Task.CompletedTask;
        true.Should().BeTrue("placeholder until handler is implemented");
    }
}
