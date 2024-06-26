﻿namespace Pango.Application.Common.Interfaces.Persistence;

public interface IUserDataRepository
{
    /// <summary>
    /// Removes all user data, except for credentials
    /// </summary>
    /// <param name="userName">Name of a user</param>
    Task DeleteAllUserDataAsync(string userName);
}
