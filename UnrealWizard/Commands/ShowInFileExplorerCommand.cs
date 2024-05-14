using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UnrealWizard
{
   [Command(PackageIds.ShowInFileExplorer)]
   internal sealed class ShowInFileExplorerCommand : BaseCommand<ShowInFileExplorerCommand>
   {
      private DTE2 dte;

      [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
      private static extern IntPtr ILCreateFromPathW(string pszPath);

      [DllImport("shell32.dll", ExactSpelling = true)]
      private static extern void ILFree(IntPtr pidl);

      [DllImport("shell32.dll", ExactSpelling = true)]
      private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr apidl, uint dwFlags);

      protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
      {

         if (dte == null)
         {
            dte = await Package.GetServiceAsync(typeof(DTE)) as DTE2;
            VisualStudioServices.DTE = dte;
         }

         await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();


         string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);

         string filterFullPath = "";
         string filterRelativePath = "";
         ProjectItem selectedfilter = null;

         SelectedItems selectedItems = dte.SelectedItems;
         if (selectedItems.Count > 0)
         {
            SelectedItem item = selectedItems.Item(1);

            if (item.ProjectItem != null)
            {
               selectedfilter = item.ProjectItem;

               filterRelativePath = Utility.GetFilterPath(selectedfilter);

               string projectName = selectedfilter.ContainingProject.Name;
               filterFullPath = Path.Combine(solutionDir, projectName, filterRelativePath);

               OpenFileExplorerAndSelectItem(filterFullPath);
            }
         }
      }

      private void OpenFileExplorerAndSelectItem(string path)
      {
         if (string.IsNullOrEmpty(path)) return;

         IntPtr pidl = ILCreateFromPathW(path);
         if (pidl != IntPtr.Zero)
         {
            try
            {
               SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            }
            finally
            {
               ILFree(pidl);
            }
         }
      }
   }
}
