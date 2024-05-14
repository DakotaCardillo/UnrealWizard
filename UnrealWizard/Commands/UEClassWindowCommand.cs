using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UnrealWizard
{
   [Command(PackageIds.UEClassWindow)]
   internal sealed class UEClassWindowCommand : BaseCommand<UEClassWindowCommand>
   {
      private DTE2 dte;

      protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
      {

         if (dte == null)
         {
            dte = await Package.GetServiceAsync(typeof(DTE)) as DTE2;
            VisualStudioServices.DTE = dte;
         }

         await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

         string filterName = "";
         string filterFullPath = "";
         string filterRelativePath = "";
         ProjectItem selectedfilter = null;

         string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);

         // Get the name of the selected filter, it's path relative to the project, and it's full path on disk
         SelectedItems selectedItems = dte.SelectedItems;
         if (selectedItems.Count > 0)
         {
            SelectedItem item = selectedItems.Item(1);

            if (item.ProjectItem != null && item.ProjectItem.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
            {
               selectedfilter = item.ProjectItem;

               filterName = selectedfilter.Name;
               filterRelativePath = Utility.GetFilterPath(selectedfilter);

               string projectName = selectedfilter.ContainingProject.Name;
               filterFullPath = Path.Combine(solutionDir, projectName, filterRelativePath);
            }
         }

         // Create the User Control and pass the filter paths into it
         UEClassWindowControl control = new UEClassWindowControl();

         control.FilterName = filterName;
         control.FilterFullPath = filterFullPath;
         control.FilterRelativePath = filterRelativePath;
         control.SelectedFilter = selectedfilter;

         UEClassWindow window = new UEClassWindow(control);
         control.window = window;


         window.ShowModal();

         //ThreadHelper.JoinableTaskFactory.Run(async () =>
         //{

         //   dialog.ShowDialog();
         //});
      }
   }
}
