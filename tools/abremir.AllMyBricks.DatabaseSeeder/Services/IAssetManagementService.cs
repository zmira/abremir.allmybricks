﻿namespace abremir.AllMyBricks.DatabaseSeeder.Services
{
    public interface IAssetManagementService
    {
        void CompressDatabaseFile(bool encrypted);
        void UncompressDatabaseFile(bool encrypted);
        void CompactAllMyBricksDatabase();
        bool DatabaseFilePathExists();
        bool CompressedDatabaseFilePathExists(bool encrypted);
    }
}
