using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nextcloud.Interfaces;
using Xunit.Priority;
using ElvantoSync.Nextcloud;
using ElvantoSync.ElvantoService;
using ElvantoSync.ElvantoApi.Models;
using Moq;
using Nextcloud.Models.Provisioning;
using Nextcloud.Tests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElvantoSync.Tests
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class GroupMembersToNextcloudSyncTests : TestBase
    {
        private ISync _groupMembersToNextcloudSync;
     
        private readonly INextcloudProvisioningClient client;
          private readonly IServiceProvider _serviceProvider;
        public GroupMembersToNextcloudSyncTests() : base()
        {

             _serviceProvider = BuildServiceProvider();



        client = _serviceProvider.GetRequiredService<INextcloudProvisioningClient>();
        }

         protected override void ConfigureServices(NextcloudContainer nextcloud)
    {
        base.ConfigureServices(nextcloud);
        _elvantoClientMock = new Mock<IElvantoClient>();
        Services.AddTransient<IElvantoClient>(x => _elvantoClientMock.Object);
    }

     [Fact, Priority(0)]
        public override async Task ApplyAsync_ShouldAddNewElementFromElvanto()
        {
            IEnumerable<ElvantoApi.Models.Person> peoples = setUpPeopleMock();
            IEnumerable<ElvantoApi.Models.Group> groups = setUpGroupMock(peoples.ToArray());
             var _groupsToNextcloudSync = fetchSyncImplementation<GroupsToNextcloudSync>(_serviceProvider);
          var _peopleToNextcloudSync = fetchSyncImplementation<PeopleToNextcloudSync>(_serviceProvider);
       
            _groupMembersToNextcloudSync = fetchSyncImplementation<GroupMembersToNextcloudSync>(_serviceProvider);
            await _peopleToNextcloudSync.ApplyAsync();
             await _groupsToNextcloudSync.ApplyAsync();
            await _groupMembersToNextcloudSync.ApplyAsync();
            
            var result = await client.GetGroups();

               
            var groupMembersTasks = result
            .Where(g => g.DisplayName != "Admin")
            .Where(g => g.DisplayName.Contains(Settings.GroupLeaderSuffix))
            .Select(async g => new { GroupId = g.Id, Members = await client.GetMembers(g.Id) });
            var groupMembers = await Task.WhenAll(groupMembersTasks);


        // Check that all `peoples` are in every group's members
            bool allPeoplesAreInAllGroups = groupMembers.All(gm =>
                peoples.Select(p => p.Id).All(person => gm.Members.Any(member => member == person)));

            Assert.True(allPeoplesAreInAllGroups);

        }

        public override async Task ApplyAsync_ShouldNotAddIfNoNewElement()
        {
            throw new NotImplementedException();
        }

        
    }
}