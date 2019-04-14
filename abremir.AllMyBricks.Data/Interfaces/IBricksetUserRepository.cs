﻿using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Models;
using System.Collections.Generic;

namespace abremir.AllMyBricks.Data.Interfaces
{
    public interface IBricksetUserRepository
    {
        BricksetUser Add(BricksetUserTypeEnum userType, string username);
        BricksetUser Get(string username);
        bool Exists(string username);
        bool Remove(string username);
        BricksetUserSet AddOrUpdateSet(string username, BricksetUserSet bricksetUserSet);
        BricksetUserSet GetSet(string username, long setId);
        IEnumerable<string> GetAllUsernames(BricksetUserTypeEnum userType);
    }
}
