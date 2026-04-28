namespace NICE.Platform.Collaboration.Tests.Unit.Features.Messages;

using FluentAssertions;
using NUnit.Framework;
using NICE.Platform.Collaboration.Application.Features.Messages.Commands.SendMessage;

[TestFixture]
[Category("Unit")]
public class SendMessageCommandHandlerTests
{
    [Test]
    public void SendMessageCommand_EmptyContent_Should_Fail_Validation()
    {
        var validator = new SendMessageCommandValidator();
        var command   = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "", null);
        var result    = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void SendMessageCommand_ValidContent_Should_Pass_Validation()
    {
        var validator = new SendMessageCommandValidator();
        var command   = new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), "Hello!", null);
        var result    = validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
