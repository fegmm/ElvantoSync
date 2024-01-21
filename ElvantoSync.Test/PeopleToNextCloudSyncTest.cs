using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Xunit.Priority;
using ElvantoSync.Nextcloud;
using ElvantoSync.ElvantoService;
using ElvantoSync.ElvantoApi.Models;
using Moq;
using Nextcloud.Models.Provisioning;


namespace ElvantoSync.Tests;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]

public class PeopleToNextcloudSyncTests:TestBase
{
    private Mock<IElvantoClient> _elvantoClientMock;
    private ISync _peopleToNextcloudSync;
    private readonly INextcloudProvisioningClient client;
    private readonly IServiceProvider _serviceProvider;

    public PeopleToNextcloudSyncTests() : base()
    {

        _serviceProvider = BuildServiceProvider();  
        
      
      
        client = _serviceProvider.GetService<INextcloudProvisioningClient>();
    }


    protected override void ConfigureServices(NextcloudContainer nextcloud)
    {
        base.ConfigureServices(nextcloud);
        _elvantoClientMock = new Mock<IElvantoClient>();    
        Services.AddTransient<IElvantoClient>(x => _elvantoClientMock.Object);   
    }

    [Fact, Priority(0)]
    public override async Task ApplyAsync_ShouldAddNewPersonsFromElvanto()
    {

        var people = new List<Person> { new Person { Id = "1", Firstname ="Test", Lastname="Tester",Email = "MyEmail"  }, new Person { Id = "2", Firstname ="Test", Lastname="Tester",Email = "MyEmail" } };
        _elvantoClientMock.Setup(x => x.PeopleGetAllAsync(It.IsAny<GetAllPeopleRequest>()))
            .ReturnsAsync(new PeopleGetAllResponse { People = new People { Person = people.ToArray() } });
        // Act
        _peopleToNextcloudSync =  _serviceProvider.GetService<PeopleToNextcloudSync>();
        await _peopleToNextcloudSync.ApplyAsync();
        var result = await client.GetUsers();   
        // Assert
        
        Assert.True(result.Where(x => x.DisplayName.Contains(people.First().Lastname)).Any());
        Assert.True(result.Where(x => x.DisplayName.Contains(people.Last().Firstname)).Any());
        Assert.True(result.
        Where(x => x.DisplayName.Contains(people.Last().Firstname))
        .Select(x => x.Email.ToLower().Equals(people.Last().Email.ToLower())
        ).Any());

    }

      [Fact, Priority(0)]
    public override async Task ApplyAsync_ShouldNotAddIfNoNewPerson()
    {

        var people = new List<Person> { new Person { Id = "1", Firstname ="Test", Lastname="Tester",Email = "MyEmail"  }, new Person { Id = "2", Firstname ="Test", Lastname="Tester",Email = "MyEmail" } };
        _elvantoClientMock.Setup(x => x.PeopleGetAllAsync(It.IsAny<GetAllPeopleRequest>()))
            .ReturnsAsync(new PeopleGetAllResponse { People = new People { Person = people.ToArray() } });
        _peopleToNextcloudSync =  _serviceProvider.GetService<PeopleToNextcloudSync>();
        await _peopleToNextcloudSync.ApplyAsync();
        var initialApply = await client.GetUsers();   
        // Act
        await _peopleToNextcloudSync.ApplyAsync();
        var secondApply = await client.GetUsers();   
        // Assert
   

        Assert.True(initialApply.Where(x => !x.DisplayName.Equals("admin"))
        .SequenceEqual(secondApply.Where(x => !x.DisplayName.Equals("admin"))));

    }


   


    // Similar tests can be written for GetToAsync, AddMissingAsync, RemoveAdditionalAsync and IsActive methods.
}