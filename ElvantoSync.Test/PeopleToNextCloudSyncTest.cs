using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Xunit.Priority;
using ElvantoSync.Nextcloud;
using ElvantoSync.ElvantoService;
using ElvantoSync.ElvantoApi.Models;
using Moq;


namespace ElvantoSync.Tests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]

public class PeopleToNextcloudSyncTests:TestBase
{
    private Mock<IElvantoClient> _elvantoClientMock;
    private readonly ISync _peopleToNextcloudSync;
    private readonly INextcloudProvisioningClient client;

    public PeopleToNextcloudSyncTests(NextcloudContainer container) : base(container)
    {
      
        _peopleToNextcloudSync =  ServiceProvider.GetService<PeopleToNextcloudSync>();
    
      //  .Where(service => service.GetType() == typeof(PeopleToNextcloudSync)).First();
        client = ServiceProvider.GetService<INextcloudProvisioningClient>();
    }

    
    [Fact, Priority(0)]
    public async Task GetFromAsync_ShouldReturnExpectedResult()
    {
        // Arrange
      
        // Act
        await _peopleToNextcloudSync.ApplyAsync();
        var result = await client.GetUsers();   
        // Assert
        Assert.Equal(3, result.Count());
      /*  Assert.True(result.Select(x => x.) .ContainsKey("Elvanto-1"));
        Assert.True(result.ContainsKey("Elvanto-2")); */
    }

    // Similar tests can be written for GetToAsync, AddMissingAsync, RemoveAdditionalAsync and IsActive methods.
}