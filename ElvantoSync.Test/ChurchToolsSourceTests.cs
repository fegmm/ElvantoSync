using System.Net;
using System.Text;
using ElvantoSync.ChurchTools;
using Fegmm.ChurchTools;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace ElvantoSync.Tests;

public class ChurchToolsSourceTests
{
    [Fact]
    public async Task LoadsAllPagesAndActiveMembersWithLeaderRolesOncePerSnapshot()
    {
        using var handler = new SourceHandler();
        using var http = new HttpClient(handler);
        using var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http);
        adapter.BaseUrl = "https://churchtools.test/api";
        var source = new ChurchToolsSource(new ChurchToolsClient(adapter));

        var groups = (await source.GetGroupsAsync()).ToList();
        var people = (await source.GetPeopleAsync()).ToList();
        await source.GetGroupsAsync();
        await source.GetPeopleAsync();

        Assert.Equal(new[] { "42", "43" }, people.Select(p => p.Id));
        Assert.Equal(new[] { 1, 3 }, people[0].AreaIds);
        Assert.Empty(people[1].AreaIds);
        Assert.Equal(new[] { "73", "74" }, groups.Select(g => g.Id));
        var leader = Assert.Single(groups[0].Members);
        Assert.Equal("42", leader.Id);
        Assert.Equal("alex@example.org", leader.Email);
        Assert.True(leader.IsLeader);
        Assert.False(Assert.Single(groups[1].Members).IsLeader);
        Assert.Equal(6, handler.RequestCount); // Two people pages, two group pages, roles, memberships.
    }

    [Fact]
    public async Task MissingSourceDataFailsInsteadOfReturningAnEmptySnapshot()
    {
        using var handler = new SourceHandler { MissingPeople = true };
        using var http = new HttpClient(handler);
        using var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http);
        adapter.BaseUrl = "https://churchtools.test/api";
        var source = new ChurchToolsSource(new ChurchToolsClient(adapter));
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetPeopleAsync());
    }

    private sealed class SourceHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public bool MissingPeople { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var uri = request.RequestUri!;
            bool second = uri.Query.Contains("page=2");
            string data = uri.AbsolutePath switch
            {
                "/api/persons" when MissingPeople => "{}",
                "/api/persons" when second => """
                    {"data":[{"id":43,"firstName":"Chris","lastName":"Smith","email":"chris@example.org"},{"id":44,"isArchived":true},{"id":45,"isDead":true}],"meta":{"pagination":{"lastPage":2}}}
                    """,
                "/api/persons" => """
                    {"data":[{"id":42,"firstName":"Alex","lastName":"Johnson","email":"alex@example.org","birthday":"1990-01-02","departmentIds":[1,3,null]}],"meta":{"pagination":{"lastPage":2}}}
                    """,
                "/api/group/roles" => """
                    {"data":[{"id":10,"isLeader":true},{"id":11,"isLeader":false}]}
                    """,
                "/api/groups/members" => """
                    {"data":[{"groupId":73,"personId":42,"groupTypeRoleId":10,"groupMemberStatus":"active"},{"groupId":74,"personId":43,"groupTypeRoleId":11,"groupMemberStatus":"active"},{"groupId":73,"personId":43,"groupMemberStatus":"requested"},{"groupId":73,"personId":43,"groupMemberStatus":"active","deleted":true},{"groupId":73,"personId":44,"groupMemberStatus":"active"}]}
                    """,
                "/api/groups" when second => """
                    {"data":[{"id":74,"name":"Group two"}],"meta":{"pagination":{"lastPage":2}}}
                    """,
                "/api/groups" => """
                    {"data":[{"id":73,"name":"Group one"}],"meta":{"pagination":{"lastPage":2}}}
                    """,
                _ => throw new InvalidOperationException($"Unexpected request: {uri}"),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(data, Encoding.UTF8, "application/json"),
            });
        }
    }
}
