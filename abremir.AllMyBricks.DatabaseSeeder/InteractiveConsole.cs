﻿using abremir.AllMyBricks.Data.Enumerations;
using abremir.AllMyBricks.Data.Interfaces;
using abremir.AllMyBricks.DatabaseSeeder.Configuration;
using abremir.AllMyBricks.DatabaseSeeder.Services;
using abremir.AllMyBricks.DataSynchronizer.Events.SetSynchronizationService;
using abremir.AllMyBricks.DataSynchronizer.Events.SetSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Events.SubthemeSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Events.ThemeSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Events.UserSynchronizationService;
using abremir.AllMyBricks.DataSynchronizer.Events.UserSynchronizer;
using abremir.AllMyBricks.DataSynchronizer.Interfaces;
using Easy.MessageHub;
using LightInject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terminal.Gui;

namespace abremir.AllMyBricks.DatabaseSeeder
{
    public static class InteractiveConsole
    {
        private static FrameView SetsSynchronizationProgressFrame;
        private static FrameView PrimaryUsersSynchronizationProgressFrame;
        private static bool CanExit = true;
        private static object SynchronizeSetsApplicationMainLoopTimeoutToken;
        private static object SynchronizePrimaryUsersApplicationMainLoopTimeoutToken;
        private static MenuBar MenuBar;

        public static void Run()
        {
            Application.Init();

            var topLevel = Application.Top;

            var topLevelWindow = AddTopLevelWindow(topLevel);

            SetsSynchronizationProgressFrame = new FrameView(new Rect(10, 1, 99, topLevel.Frame.Height - 4), "Sets Synchronization Progress...");

            var themeLabel = new Label(4, 3, "".PadRight(70));
            var themeProgress = new ProgressBar(new Rect(4, 4, 88, 1));

            var subthemeLabel = new Label(6, 6, "".PadRight(70));
            var subthemeProgress = new ProgressBar(new Rect(6, 7, 84, 1));

            var setLabel = new Label(8, 9, "".PadRight(70));
            var setProgress = new ProgressBar(new Rect(8, 10, 80, 1));

            var lastUpdatedLabel = new Label(50, 17, "Last Updated: Never                 ");
            var totalUpdatedThemesLabel = new Label(50, 18, "Total Updated Themes: 0      ");
            var totalUpdatedSubthemesLabel = new Label(50, 19, "Total Updated Subthemes: 0      ");
            var totalUpdatedSetsLabel = new Label(50, 20, "Total Updated Sets: 0      ");

            SetsSynchronizationProgressFrame.Add(themeLabel);
            SetsSynchronizationProgressFrame.Add(themeProgress);
            SetsSynchronizationProgressFrame.Add(subthemeLabel);
            SetsSynchronizationProgressFrame.Add(subthemeProgress);
            SetsSynchronizationProgressFrame.Add(setLabel);
            SetsSynchronizationProgressFrame.Add(setProgress);
            SetsSynchronizationProgressFrame.Add(lastUpdatedLabel);
            SetsSynchronizationProgressFrame.Add(totalUpdatedThemesLabel);
            SetsSynchronizationProgressFrame.Add(totalUpdatedSubthemesLabel);
            SetsSynchronizationProgressFrame.Add(totalUpdatedSetsLabel);

            PrimaryUsersSynchronizationProgressFrame = new FrameView(new Rect(10, 1, 99, topLevel.Frame.Height - 4), "Primary Users Synchronization Progress...");

            var userCountLabel = new Label(4, 3, "".PadRight(70));

            var userLabel = new Label(4, 5, "".PadRight(70));
            var userProgress = new ProgressBar(new Rect(4, 6, 88, 1));

            var syncTypeLabel = new Label(4, 8, "".PadRight(70));

            var syncLabel = new Label(4, 10, "".PadRight(70));
            var syncProgress = new ProgressBar(new Rect(4, 11, 88, 1));

            PrimaryUsersSynchronizationProgressFrame.Add(userCountLabel);
            PrimaryUsersSynchronizationProgressFrame.Add(userLabel);
            PrimaryUsersSynchronizationProgressFrame.Add(userProgress);
            PrimaryUsersSynchronizationProgressFrame.Add(syncTypeLabel);
            PrimaryUsersSynchronizationProgressFrame.Add(syncLabel);
            PrimaryUsersSynchronizationProgressFrame.Add(syncProgress);

            var themeCount = 0f;
            var themeIndex = 0f;
            var subthemeCount = 0f;
            var subthemeIndex = 0f;
            var setCount = 0f;
            var setIndex = 0f;
            var totalUpdatedThemes = 0f;
            var totalUpdatedSubthemes = 0f;
            var totalUpdatedSets = 0f;

            var userCount = 0f;
            var userIndex = 0f;
            var syncCount = 0f;
            var syncIndex = 0f;

            var messageHub = IoC.IoCContainer.GetInstance<IMessageHub>();

            Stopwatch stopwatch = null;

            messageHub.Subscribe<SetSynchronizationServiceStart>(_ =>
            {
                totalUpdatedThemes = 0f;
                totalUpdatedSubthemes = 0f;
                totalUpdatedSets = 0;

                SetsSynchronizationProgressFrame.Clear();

                Application.MainLoop.Invoke(SetsSynchronizationProgressFrame.ChildNeedsDisplay);

                stopwatch = Stopwatch.StartNew();
            });
            messageHub.Subscribe<InsightsAcquired>(message =>
            {
                lastUpdatedLabel.Text = $"Last Updated: {(message.SynchronizationTimestamp.HasValue ? message.SynchronizationTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}";

                Application.MainLoop.Invoke(lastUpdatedLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ThemeSynchronizerStart>(_ =>
            {
                themeIndex = 0;
                themeProgress.Fraction = 0;

                Application.MainLoop.Invoke(themeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ThemesAcquired>(message => themeCount = message.Count);
            messageHub.Subscribe<SynchronizingThemeStart>(message =>
            {
                themeLabel.Text = $"Theme: {message.Theme}";
                themeIndex++;
                totalUpdatedThemes++;
                themeProgress.Fraction = themeIndex / themeCount;

                Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(themeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SynchronizingThemeEnd>(_ =>
            {
                themeLabel.Text = string.Empty;

                totalUpdatedThemesLabel.Text = $"Total Updated Themes: {totalUpdatedThemes}";

                Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(totalUpdatedThemesLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ThemeSynchronizerEnd>(_ =>
            {
                themeLabel.Text = string.Empty;
                themeIndex = 0f;
                themeProgress.Fraction = 0;

                Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(themeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ProcessingThemeStart>(message =>
            {
                themeLabel.Text = $"Theme: {message.Name}";

                Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SubthemeSynchronizerStart>(_ =>
            {
                subthemeLabel.Text = string.Empty;
                subthemeIndex = 0;
                subthemeProgress.Fraction = 0;

                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(subthemeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SubthemesAcquired>(message =>
            {
                subthemeCount = message.Count;
                subthemeIndex = 0;
            });
            messageHub.Subscribe<SynchronizingSubthemeStart>(message =>
            {
                subthemeLabel.Text = $"Subtheme: {message.Subtheme}";
                subthemeIndex++;
                totalUpdatedSubthemes++;
                subthemeProgress.Fraction = subthemeIndex / subthemeCount;

                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(subthemeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SynchronizingSubthemeEnd>(_ =>
            {
                subthemeLabel.Text = string.Empty;

                totalUpdatedSubthemesLabel.Text = $"Total Updated Subthemes: {totalUpdatedSubthemes}";

                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(totalUpdatedSubthemesLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SubthemeSynchronizerEnd>(_ =>
            {
                subthemeLabel.Text = string.Empty;
                subthemeIndex = 0f;
                subthemeProgress.Fraction = 0;

                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(subthemeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ProcessingSubthemeStart>(message =>
            {
                subthemeLabel.Text = $"Subtheme: {message.Name}";

                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SetSynchronizerStart>(_ =>
            {
                setLabel.Text = string.Empty;
                setIndex = 0;
                setProgress.Fraction = 0;

                Application.MainLoop.Invoke(setLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(setProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<AcquiringSetsStart>(message =>
            {
                setIndex = 0;
                setProgress.Fraction = 0;
                subthemeLabel.Text = $"Subtheme: {message.Subtheme}, {message.Year}";

                Application.MainLoop.Invoke(setProgress.ChildNeedsDisplay);
                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<AcquiringSetsEnd>(message =>
            {
                setCount = message.Count;
                setIndex = 0;
                setProgress.Fraction = 0;
                if (message.Year.HasValue)
                {
                    subthemeLabel.Text = $"Subtheme: {message.Subtheme}, {message.Year.Value}";

                    Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                }

                Application.MainLoop.Invoke(setProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SynchronizingSetStart>(message =>
            {
                themeLabel.Text = $"Theme: {message.Theme}";
                subthemeLabel.Text = $"Subtheme: {message.Subtheme}{(message.Year.HasValue ? $", {message.Year.Value}" : string.Empty)}";
                setLabel.Text = $"Set: {message.IdentifierShort}";
                setIndex++;
                totalUpdatedSets++;
                setProgress.Fraction = setIndex / setCount;

                Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(setLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(setProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SynchronizingSetEnd>(_ =>
            {
                setLabel.Text = string.Empty;

                totalUpdatedSetsLabel.Text = $"Total Updated Sets: {totalUpdatedSets}";

                Application.MainLoop.Invoke(setLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(totalUpdatedSetsLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SetSynchronizerEnd>(message =>
            {
                if (!message.ForSubtheme)
                {
                    themeLabel.Text = string.Empty;
                    subthemeLabel.Text = string.Empty;

                    Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
                    Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                }
                setLabel.Text = string.Empty;
                setIndex = 0;
                setProgress.Fraction = 0;

                Application.MainLoop.Invoke(setLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(setProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ProcessingSubthemeEnd>(_ =>
            {
                subthemeLabel.Text = string.Empty;
                subthemeIndex++;
                subthemeProgress.Fraction = subthemeIndex / subthemeCount;

                Application.MainLoop.Invoke(subthemeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(subthemeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<ProcessingThemeEnd>(_ =>
            {
                themeLabel.Text = string.Empty;
                themeIndex++;
                themeProgress.Fraction = themeIndex / themeCount;

                if ((int)themeIndex == (int)themeCount)
                {
                    themeProgress.Fraction = 0f;
                    subthemeProgress.Fraction = 0f;

                    Application.MainLoop.Invoke(subthemeProgress.ChildNeedsDisplay);
                }

                Application.MainLoop.Invoke(themeLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(themeProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<SetSynchronizationServiceEnd>(_ =>
            {
                stopwatch.Stop();

                themeLabel.Text = string.Empty;
                subthemeLabel.Text = string.Empty;
                setLabel.Text = string.Empty;
                themeProgress.Fraction = 0;
                subthemeProgress.Fraction = 0;
                setProgress.Fraction = 0;

                themeCount = 0f;
                subthemeCount = 0f;
                setCount = 0f;
                themeIndex = 0f;
                subthemeIndex = 0f;
                setIndex = 0f;

                SetsSynchronizationProgressFrame.Clear();

                Application.MainLoop.Invoke(SetsSynchronizationProgressFrame.ChildNeedsDisplay);
                Application.MainLoop.RemoveTimeout(SynchronizeSetsApplicationMainLoopTimeoutToken);

                var buttonOk = new Button("Ok");
                buttonOk.Clicked += () =>
                {
                    topLevelWindow.Remove(SetsSynchronizationProgressFrame);

                    CanExit = true;

                    Application.RequestStop();
                };

                var dialog = new Dialog("Sets Synchronization finished", 50, 8, buttonOk);

                var totalTimeLabel = new Label($"Sets synchronized in {stopwatch.Elapsed:hh\\:mm\\:ss}")
                {
                    X = Pos.Center(),
                    Y = 1
                };
                dialog.Add(totalTimeLabel);

                Application.Run(dialog);
            });
            messageHub.Subscribe<UserSynchronizationServiceStart>(_ =>
            {
                PrimaryUsersSynchronizationProgressFrame.Clear();

                Application.MainLoop.Invoke(PrimaryUsersSynchronizationProgressFrame.ChildNeedsDisplay);

                stopwatch = Stopwatch.StartNew();
            });
            messageHub.Subscribe<UsersAcquired>(message =>
            {
                userCountLabel.Text = $"Found {message.Count} users to process";
                userCount = message.Count;
                userIndex = 0f;
                userProgress.Fraction = 0f;

                Application.MainLoop.Invoke(userCountLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(userProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<UserSynchronizerStart>(message =>
            {
                userLabel.Text = $"Processing user {message.Username}";
                userIndex++;
                userProgress.Fraction = userIndex / userCount;

                Application.MainLoop.Invoke(userLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(userProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<AllMyBricksToBricksetStart>(_ =>
            {
                syncTypeLabel.Text = "Updating brickset.com with updated owned and wanted sets since previous sync";

                Application.MainLoop.Invoke(syncTypeLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<AllMyBricksToBricksetAcquiringSetsEnd>(message =>
            {
                syncLabel.Text = $"Uploading data of {message.Count} sets to brickset.com...";
                syncCount = message.Count;
                syncIndex = 0f;
                syncProgress.Fraction = 0f;

                Application.MainLoop.Invoke(syncLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(syncProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<BricksetToAllMyBricksStart>(_ =>
            {
                syncTypeLabel.Text = "Downloading from brickset.com all owned and wanted sets missing in AllMyBricks";

                Application.MainLoop.Invoke(syncTypeLabel.ChildNeedsDisplay);
            });
            messageHub.Subscribe<BricksetToAllMyBricksAcquiringSetsEnd>(message =>
            {
                syncLabel.Text = $"Adding {message.Count} sets to AllMyBricks";
                syncCount = message.Count;
                syncIndex = 0f;
                syncProgress.Fraction = 0f;

                Application.MainLoop.Invoke(syncLabel.ChildNeedsDisplay);
                Application.MainLoop.Invoke(syncProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<UserSynchronizerSynchronizingSetStart>(_ =>
            {
                syncIndex++;
                syncProgress.Fraction = syncIndex / syncCount;

                Application.MainLoop.Invoke(syncProgress.ChildNeedsDisplay);
            });
            messageHub.Subscribe<UserSynchronizationServiceEnd>(_ =>
            {
                stopwatch.Stop();

                userCountLabel.Text = string.Empty;
                userProgress.Fraction = 0f;
                userLabel.Text = string.Empty;
                syncTypeLabel.Text = string.Empty;
                syncLabel.Text = string.Empty;
                syncProgress.Fraction = 0f;

                userCount = 0f;
                userIndex = 0f;
                syncIndex = 0f;
                syncCount = 0f;

                PrimaryUsersSynchronizationProgressFrame.Clear();

                Application.MainLoop.Invoke(PrimaryUsersSynchronizationProgressFrame.ChildNeedsDisplay);
                Application.MainLoop.RemoveTimeout(SynchronizePrimaryUsersApplicationMainLoopTimeoutToken);

                var buttonOk = new Button("Ok");
                buttonOk.Clicked += () =>
                {
                    topLevelWindow.Remove(PrimaryUsersSynchronizationProgressFrame);

                    CanExit = true;

                    Application.RequestStop();
                };

                var dialog = new Dialog("Primary Users Synchronization finished", 50, 8, buttonOk);

                var totalTimeLabel = new Label($"Primary users synchronized in {stopwatch.Elapsed:hh\\:mm\\:ss}")
                {
                    X = Pos.Center(),
                    Y = 1
                };
                dialog.Add(totalTimeLabel);

                Application.Run(dialog);
            });

            AddMenuBar(topLevel, topLevelWindow);

            AddBricksetApiKey(topLevelWindow);

            AddCompressedFileIsEncryptedCheckbox(topLevelWindow);

            AddCompressDatabaseFileButton(topLevelWindow);

            AddUncompressDatabaseFileButton(topLevelWindow);

            AddCompactDatabaseButton(topLevelWindow);

            AddPrimaryUsersList(topLevelWindow);

            Application.Run();
        }

        private static Window AddTopLevelWindow(Toplevel topLevel)
        {
            var window = new Window("All My Bricks Database Seeder")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            topLevel.Add(
                window
            );

            return window;
        }

        private static void AddMenuBar(Toplevel topLevel, Window window)
        {
            MenuBar = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem("_File", new MenuItem[]
                {
                    new MenuItem("E_xit", "", () => topLevel.Running &= !CanExit)
                })
            });

            UpdateSynchronizationMenuView(window);

            topLevel.Add(
                MenuBar
            );
        }

        private static void UpdateSynchronizationMenuView(Window window)
        {
            var bricksetApiKey = Settings.BricksetApiKey;
            const string synchronizeMenuTitle = "_Synchronize";

            if (string.IsNullOrWhiteSpace(bricksetApiKey)
                && !MenuBar.Menus.Any(menu => menu.Title == synchronizeMenuTitle))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(bricksetApiKey))
            {
                MenuBar.Menus = MenuBar.Menus.Where(menu => menu.Title != synchronizeMenuTitle).ToArray();

                return;
            }

            var menuBarItem = new MenuBarItem("_Synchronize", new MenuItem[]
            {
                new MenuItem("S_ets", "", () =>
                {
                    if (CanExit)
                    {
                        CanExit = false;

                        window.Add(SetsSynchronizationProgressFrame);

                        // HACK: since there is a bug in Application.MainLoop.Invoke(...) this is needed to force the UI to refresh!
                        SynchronizeSetsApplicationMainLoopTimeoutToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), _ => true);

                        IoC.IoCContainer.GetInstance<ISetSynchronizationService>().SynchronizeAllSets();
                    }
                }),
                new MenuItem("_Primary Users' Sets", "", () =>
                {
                    if (CanExit)
                    {
                        CanExit = false;

                        var userRepository = IoC.IoCContainer.GetInstance<IBricksetUserRepository>();

                        foreach (var primaryUser in Settings.BricksetPrimaryUsers)
                        {
                            if (userRepository.Get(primaryUser.Key) is null)
                            {
                                userRepository.Add(BricksetUserType.Primary, primaryUser.Key);
                            }
                        }

                        window.Add(PrimaryUsersSynchronizationProgressFrame);

                        // HACK: since there is a bug in Application.MainLoop.Invoke(...) this is needed to force the UI to refresh!
                        SynchronizePrimaryUsersApplicationMainLoopTimeoutToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(10), _ => true);

                        IoC.IoCContainer.GetInstance<IUserSynchronizationService>().SynchronizeBricksetPrimaryUsersSets();
                    }
                })
            });

            MenuBar.Menus = MenuBar.Menus.Append(menuBarItem).ToArray();
        }

        private static void AddBricksetApiKey(Window window)
        {
            var bricksetApiKeyEditButton = new Button("Edit")
            {
                X = 3,
                Y = 2
            };

            var bricksetApiKeyLabel = new Label("Brickset API Key:")
            {
                X = Pos.Right(bricksetApiKeyEditButton) + 1,
                Y = 2
            };

            var bricksetApiKeyValue = new Label(Settings.BricksetApiKey)
            {
                X = Pos.Right(bricksetApiKeyLabel) + 4,
                Y = 2
            };

            bricksetApiKeyEditButton.Clicked += () =>
            {
                var bricksetApiKeyTextField = new TextField(Settings.BricksetApiKey);

                var buttonOk = new Button("Ok", false);
                buttonOk.Clicked += () =>
                {
                    var bricksetApiKey = bricksetApiKeyTextField.Text.ToString();

                    Settings.BricksetApiKey = bricksetApiKey;
                    bricksetApiKeyValue.Text = bricksetApiKey;

                    UpdateSynchronizationMenuView(window);

                    Application.RequestStop();
                };

                var buttonCancel = new Button("Cancel", false);
                buttonCancel.Clicked += () => Application.RequestStop();

                var dialog = new Dialog(
                    "Brickset API Key",
                    50,
                    7,
                    buttonOk,
                    buttonCancel)
                    {
                        bricksetApiKeyTextField
                    };

                Application.Run(dialog);
            };

            window.Add(
                bricksetApiKeyEditButton,
                bricksetApiKeyLabel,
                bricksetApiKeyValue
            );
        }

        private static void AddCompressedFileIsEncryptedCheckbox(Window window)
        {
            var checkBox = new CheckBox("Compressed file is encrypted", Settings.CompressedFileIsEncrypted)
            {
                X = 3,
                Y = 4
            };

            checkBox.Toggled += CheckBox_Toggled;

            window.Add(
                checkBox
            );
        }

        private static void CheckBox_Toggled(bool previousValue)
        {
            Settings.CompressedFileIsEncrypted = !previousValue;
        }

        private static void AddCompressDatabaseFileButton(Window window)
        {
            var assetManagementService = IoC.IoCContainer.GetInstance<IAssetManagementService>();

            if (assetManagementService.DatabaseFilePathExists())
            {
                var button = new Button("Compress Database File")
                {
                    X = 3,
                    Y = 6
                };

                button.Clicked += () =>
                {
                    CanExit = false;

                    assetManagementService.CompressDatabaseFile(Settings.CompressedFileIsEncrypted);

                    var buttonOk = new Button("Ok");
                    buttonOk.Clicked += () =>
                    {
                        CanExit = true;

                        Application.RequestStop();
                    };

                    var dialog = new Dialog($"Database File Compressed{(Settings.CompressedFileIsEncrypted ? " and Encrypted" : string.Empty)}", 50, 6, buttonOk);

                    Application.Run(dialog);
                };

                window.Add(
                    button
                );
            }
        }

        private static void AddUncompressDatabaseFileButton(Window window)
        {
            var assetManagementService = IoC.IoCContainer.GetInstance<IAssetManagementService>();

            if (assetManagementService.CompressedDatabaseFilePathExists(Settings.CompressedFileIsEncrypted))
            {
                var button = new Button("Uncompress Database File")
                {
                    X = 3,
                    Y = 8
                };

                button.Clicked += () =>
                {
                    CanExit = false;

                    assetManagementService.UncompressDatabaseFile(Settings.CompressedFileIsEncrypted);

                    var buttonOk = new Button("Ok");
                    buttonOk.Clicked += () =>
                    {
                        CanExit = true;

                        Application.RequestStop();
                    };

                    var dialog = new Dialog($"{(Settings.CompressedFileIsEncrypted ? "Encrypted " : string.Empty)}Database File Uncompressed", 50, 6, buttonOk);

                    Application.Run(dialog);
                };

                window.Add(
                    button
                );
            }
        }

        private static void AddCompactDatabaseButton(Window window)
        {
            var assetManagementService = IoC.IoCContainer.GetInstance<IAssetManagementService>();

            if (assetManagementService.DatabaseFilePathExists())
            {
                var button = new Button("Compact AllMyBricks Database")
                {
                    X = 3,
                    Y = 10
                };

                button.Clicked += () =>
                {
                    CanExit = false;

                    assetManagementService.CompactAllMyBricksDatabase();

                    var buttonOk = new Button("Ok");
                    buttonOk.Clicked += () =>
                    {
                        CanExit = true;

                        Application.RequestStop();
                    };

                    var dialog = new Dialog("AllMyBricks Database Compacted", 50, 6, buttonOk);

                    Application.Run(dialog);
                };

                window.Add(
                    button
                );
            }
        }

        private static void AddPrimaryUsersList(Window window)
        {
            var userRepository = IoC.IoCContainer.GetInstance<IBricksetUserRepository>();

            var primaryUsersLabel = new Label(3, 12, "Primary Users");

            var primaryUsersList = new ListView(new Rect(3, 13, 50, 12), Settings.BricksetPrimaryUsers?.Select(keyValuePair => $"{keyValuePair.Key}/{keyValuePair.Value}").ToList() ?? new List<string>())
            {
                ColorScheme = Colors.Dialog,
                AllowsMultipleSelection = false,
                AllowsMarking = false
            };

            var deletePrimaryUserButton = new Button("Delete primary user")
            {
                X = 3,
                Y = 25
            };

            deletePrimaryUserButton.Clicked += () =>
            {
                var primaryUsers = Settings.BricksetPrimaryUsers;
                var selectedUser = primaryUsers.Skip(primaryUsersList.SelectedItem).Take(1).First();

                var buttonOk = new Button("Ok", false);
                buttonOk.Clicked += () =>
                {
                    primaryUsers.Remove(selectedUser.Key);
                    Settings.BricksetPrimaryUsers = primaryUsers;

                    primaryUsersList.SetSource(primaryUsers.Select(keyValuePair => $"{keyValuePair.Key}/{keyValuePair.Value}").ToList());

                    Application.RequestStop();
                };

                var buttonCancel = new Button("Cancel", false);
                buttonCancel.Clicked += () => Application.RequestStop();

                var dialog = new Dialog(
                    "Delete primary user",
                    50,
                    8,
                    buttonOk,
                    buttonCancel)
                    {
                        new Label($"Do you want to delete primary user {selectedUser.Key}?")
                        {
                            Y = 1
                        }
                    };

                Application.Run(dialog);
            };

            primaryUsersList.SelectedItemChanged += (_) =>
            {
                if (primaryUsersList.SelectedItem == -1)
                {
                    window.Remove(deletePrimaryUserButton);
                }
                else
                {
                    window.Add(deletePrimaryUserButton);
                }
            };

            var addPrimaryUserButton = new Button("Add primary user")
            {
                X = 33,
                Y = 25
            };
            addPrimaryUserButton.Clicked += () =>
            {
                var primaryUserUsername = new TextField(string.Empty)
                {
                    Y = 2
                };
                var primaryUserUserHash = new TextField(string.Empty)
                {
                    Y = 4
                };

                var buttonOk = new Button("Ok", false);
                buttonOk.Clicked += () =>
                {
                    var username = primaryUserUsername.Text.ToString();
                    var userHash = primaryUserUserHash.Text.ToString();

                    var primaryUsers = Settings.BricksetPrimaryUsers;
                    primaryUsers.Add(username, userHash);
                    Settings.BricksetPrimaryUsers = primaryUsers;

                    primaryUsersList.SetSource(primaryUsers.Select(keyValuePair => $"{keyValuePair.Key}/{keyValuePair.Value}").ToList());

                    Application.RequestStop();
                };

                var buttonCancel = new Button("Cancel", false);
                buttonCancel.Clicked += () => Application.RequestStop();

                var dialog = new Dialog(
                    "Add primary user",
                    50,
                    11,
                    buttonOk,
                    buttonCancel)
                    {
                        {
                            new Label("Username:")
                            {
                                Y = 1
                            },
                            primaryUserUsername,
                            new Label("User hash:")
                            {
                                Y = 3
                            },
                            primaryUserUserHash
                        }
                    };

                Application.Run(dialog);
            };

            window.Add(
                primaryUsersLabel,
                primaryUsersList,
                deletePrimaryUserButton,
                addPrimaryUserButton
            );
        }
    }
}
