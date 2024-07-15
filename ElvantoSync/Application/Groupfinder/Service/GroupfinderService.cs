using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ElvantoSync.GroupFinder.Model;
using ElvantoSync.GroupFinder.Service;

namespace ElvantoSync.GroupFinder.service;

public class GroupFinderService(HttpClient client) : IGroupFinderService
{
    public async Task createGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {   
        
        var response = await client.PostAsJsonAsync($"http://nextcloud.local/index.php/apps/app_api/proxy/simpleapi/group", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
public async Task<SmallGroup[]> GetGroupAsync( CancellationToken cancellationToken = default)
    {   
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // Macht die Deserialisierung unempfindlich gegenüber Groß-/Kleinschreibung
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Ignoriert nicht gemappte Attribute
            ,
            WriteIndented = true
        };
        var request = await client.GetAsync($"http://nextcloud.local/index.php/apps/app_api/proxy/simpleapi/group", cancellationToken);
        try{var response = await request.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<GroupId>(options, cancellationToken);
          return response.smallGroups;
       }catch (JsonException ex)
        {
            Console.WriteLine($"JSON error: {ex.Message}");
            Console.WriteLine($"Path: {ex.Path}");
            Console.WriteLine($"LineNumber: {ex.LineNumber}");
            Console.WriteLine($"BytePositionInLine: {ex.BytePositionInLine}");
        }
        return null;
      
    }


}