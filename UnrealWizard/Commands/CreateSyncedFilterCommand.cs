﻿using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UnrealWizard
{
   [Command(PackageIds.CreateSyncedFilter)]
   internal sealed class CreateSyncedFilterCommand : BaseCommand<CreateSyncedFilterCommand>
   {
      private DTE2 dte;

      private string fullFilterPath;

      protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
      {
         ThreadHelper.JoinableTaskFactory.Run(async () =>
         {
            if (dte == null)
            {
               dte = await Package.GetServiceAsync(typeof(DTE)) as DTE2;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Find the selected filter and get it's full path on disk (this will be the parent for the new filter)
            ProjectItem parentFilterProjectItem = null;
            SelectedItems selectedItems = dte.SelectedItems;
            if (selectedItems.Count > 0)
            {
               SelectedItem item = selectedItems.Item(1);

               if (item.ProjectItem != null && item.ProjectItem.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
               {
                  parentFilterProjectItem = item.ProjectItem;
                  string projectPath = parentFilterProjectItem.ContainingProject.FullName;
                  fullFilterPath = Directory.GetParent(projectPath).FullName + "\\" + Utility.GetFilterPath(parentFilterProjectItem);
               }
            }

            // Create the new filter under the selected parent filter
            ProjectItem newFilter = parentFilterProjectItem.ProjectItems.AddFolder("NewFilter", EnvDTE.Constants.vsProjectItemKindVirtualFolder);

            // Select the newly created filter
            Utility.SelectItemInSolutionExplorer(newFilter, dte);

            // Subscribe to the item rename event to create the new directory when the user names the new filter
            var events = (Events2)dte.Events;
            var projectItemsEvents = events.ProjectItemsEvents;
            projectItemsEvents.ItemRenamed += OnItemRenamed;

            // Force a rename command on the newly selected filter
            dte.ExecuteCommand("File.Rename");
         });
      }

      private void OnItemRenamed(ProjectItem ProjectItem, string OldName)
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         if (fullFilterPath.Length > 0)
         {
            // Create the new directory to match the renamed filter
            string newName = ProjectItem.Name;
            string directoryPath = Path.Combine(fullFilterPath, newName);

            Directory.CreateDirectory(directoryPath);

            System.Diagnostics.Debug.WriteLine($"Item renamed from {OldName} to {newName}");
         }

         fullFilterPath = "";

         // Unsubscribe from future rename events
         var events = (Events2)dte.Events;
         var projectItemsEvents = events.ProjectItemsEvents;
         projectItemsEvents.ItemRenamed -= OnItemRenamed;
      }
   }
}
