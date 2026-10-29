using System.Collections.Generic;
using System.Threading.Tasks;
using ElvantoSync.ChurchTools.Models;

namespace ElvantoSync.ChurchTools;

public interface IChurchToolsSource
{
    Task<IEnumerable<Person>> GetPeopleAsync();
    Task<IEnumerable<Group>> GetGroupsAsync();
}
