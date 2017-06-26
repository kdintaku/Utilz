﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilz.Data;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;

namespace Utilz
{
    public class Pickers : UIThreadAware
    {
        public const string PICKED_FOLDER_TOKEN = "PickedFolderToken";
        public const string PICKED_SAVE_FILE_TOKEN = "PickedSaveFileToken";
        public const string PICKED_OPEN_FILE_TOKEN = "PickedOpenFileToken";

        public static async Task<StorageFolder> PickDirectoryAsync(string[] extensions, string token = PICKED_FOLDER_TOKEN, PickerLocationId startLocation = PickerLocationId.DocumentsLibrary)
        {
            //bool unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            //if (unsnapped)
            //{

            StorageFolder directory = null;
            try
            {
                Task<StorageFolder> dirTask = null;
                await RunInUiThreadAsync(() =>
                {
                    var openPicker = new FolderPicker
                    {
                        ViewMode = PickerViewMode.List,
                        SuggestedStartLocation = startLocation
                    };

                    foreach (var ext in extensions)
                    {
                        openPicker.FileTypeFilter.Add(ext);
                    }
                    dirTask = openPicker.PickSingleFolderAsync().AsTask();
                });

                directory = await dirTask;
            }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
            }
            finally
            {
                SetLastPickedFolder(directory, token);
                SetLastPickedFolderMRU(directory, token);
            }
            return directory;

            //}
            //return false;
        }

        public static async Task<StorageFile> PickOpenFileAsync(string[] extensions, string token = PICKED_OPEN_FILE_TOKEN, PickerLocationId startLocation = PickerLocationId.DocumentsLibrary)
        {
            StorageFile file = null;
            try
            {
                Task<StorageFile> fileTask = null;
                await RunInUiThreadAsync(() =>
                {
                    var openPicker = new FileOpenPicker
                    {
                        ViewMode = PickerViewMode.List,
                        SuggestedStartLocation = startLocation
                    };

                    foreach (var ext in extensions)
                    {
                        openPicker.FileTypeFilter.Add(ext);
                    }
                    fileTask = openPicker.PickSingleFileAsync().AsTask();
                });

                file = await fileTask;
            }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
            }
            finally
            {
                SetLastPickedOpenFile(file, token);
                SetLastPickedOpenFileMRU(file, token);
            }
            return file;
        }

        public static async Task<StorageFile> PickSaveFileAsync(string[] extensions, string token = PICKED_SAVE_FILE_TOKEN, string suggestedFileName = "", PickerLocationId startLocation = PickerLocationId.DocumentsLibrary)
        {
            StorageFile file = null;
            try
            {
                Task<StorageFile> fileTask = null;
                await RunInUiThreadAsync(() =>
                {
                    var picker = new FileSavePicker { SuggestedStartLocation = startLocation };
                    if (!string.IsNullOrWhiteSpace(suggestedFileName)) picker.SuggestedFileName = suggestedFileName;

                    foreach (var ext in extensions)
                    {
                        var exts = new List<string> { ext };
                        picker.FileTypeChoices.Add(ext + " file", exts);
                    }

                    fileTask = picker.PickSaveFileAsync().AsTask();
                });

                file = await fileTask;
            }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
            }
            finally
            {
                SetLastPickedSaveFile(file, token);
                SetLastPickedSaveFileMRU(file, token);
            }
            return file;
        }

        /// <summary>
        /// Retrieves the folder associated with the given token, returns null if not available
        /// </summary>
        /// <param name="token">a token to retrieve a folder, it can be its path</param>
        /// <param name="cancToken">a cancellation token</param>
        /// <returns>StorageFolder</returns>
        public static async Task<StorageFolder> GetLastPickedFolderAsync(string token, CancellationToken cancToken)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(escapedToken).AsTask(cancToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
                return null;
            }
        }
        /// <summary>
        /// Retrieves the file associated with the given token, returns null if not available
        /// </summary>
        /// <param name="token">a token to retrieve a file, it can be its path</param>
        /// <param name="cancToken">a cancellation token</param>
        /// <returns>StorageFile</returns>
        public static async Task<StorageFile> GetLastPickedOpenFileAsync(string token, CancellationToken cancToken)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(escapedToken).AsTask(cancToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
                return null;
            }
        }
        /// <summary>
        /// Retrieves the file associated with the given token, returns null if not available
        /// </summary>
        /// <param name="token">a token to retrieve a file, it can be its path</param>
        /// <param name="cancToken">a cancellation token</param>
        /// <returns>StorageFile</returns>
        public static async Task<StorageFile> GetLastPickedSaveFileAsync(string token, CancellationToken cancToken)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                return await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(escapedToken).AsTask(cancToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                await Logger.AddAsync(ex.ToString(), Logger.FileErrorLogFilename);
                return null;
            }
        }
        // LOLLO TODO check if these setters really need to set the MRU. It can screw things, particularly when the file is internal to the app!
        public static void SetLastPickedFolder(StorageFolder directory, string token = PICKED_FOLDER_TOKEN)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                if (directory != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(escapedToken, directory);
                }
                else
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(escapedToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.FileErrorLogFilename);
            }
        }

        public static void SetLastPickedOpenFile(StorageFile file, string token = PICKED_OPEN_FILE_TOKEN)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                if (file != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(escapedToken, file);
                }
                else
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(escapedToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.FileErrorLogFilename);
            }
        }
        public static void SetLastPickedSaveFile(StorageFile file, string token = PICKED_SAVE_FILE_TOKEN)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                if (file != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(escapedToken, file);
                }
                else
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(escapedToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Add_TPL(ex.ToString(), Logger.FileErrorLogFilename);
            }
        }
        public static void SetLastPickedFolderMRU(StorageFolder directory, string token = PICKED_FOLDER_TOKEN)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                if (directory != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(escapedToken, directory);
                }
                else
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Remove(escapedToken);
                }
            }
            catch { }
        }

        public static void SetLastPickedOpenFileMRU(StorageFile file, string token = PICKED_OPEN_FILE_TOKEN)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                if (file != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(escapedToken, file);
                }
                else
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Remove(escapedToken);
                }
            }
            catch { }
        }
        public static void SetLastPickedSaveFileMRU(StorageFile file, string token = PICKED_SAVE_FILE_TOKEN)
        {
            try
            {
                var escapedToken = Uri.EscapeDataString(token);
                if (file != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.AddOrReplace(escapedToken, file);
                }
                else
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.Remove(escapedToken);
                }
            }
            catch { }
        }
    }
}