namespace Nextcloud.Models;

internal record IdResponse<T>(T Id);
internal record IdResponse(string Id) : IdResponse<string>(Id);
