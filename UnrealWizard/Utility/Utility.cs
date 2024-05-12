using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnrealWizard
{
   public static class Utility
   {
      public static string GetFilterPath(ProjectItem filterItem)
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         var path = new List<string>();
         ProjectItem currentItem = filterItem;

         // Traverse up the hierarchy until you reach the project root
         while (currentItem != null)
         {
            if (currentItem.Kind == EnvDTE.Constants.vsProjectItemKindVirtualFolder)
            {
               path.Add(currentItem.Name);
            }
            else
            {
               break; // Stop if we reach a non-filter item, such as the project itself
            }

            // Attempt to get the parent as a ProjectItem
            var parent = currentItem.Collection?.Parent as ProjectItem;
            currentItem = parent;
         }

         path.Reverse(); // Reverse to get the correct top-down order
         return string.Join("\\", path); // Combine into a single path string
      }

      public static void SelectItemInSolutionExplorer(ProjectItem item, DTE2 dte)
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         Window solutionExplorer = dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer);
         solutionExplorer.Activate();

         UIHierarchy solutionExplorerHierarchy = (UIHierarchy)solutionExplorer.Object;

         // Iterate over all items to find the matching one
         SelectHierarchyItemRecursively(solutionExplorerHierarchy.UIHierarchyItems, item);
      }

      public static void SelectHierarchyItemRecursively(UIHierarchyItems hierarchyItems, ProjectItem targetItem)
      {
         ThreadHelper.ThrowIfNotOnUIThread();

         foreach (UIHierarchyItem hierarchyItem in hierarchyItems)
         {
            if (hierarchyItem.Object is ProjectItem projectItem && projectItem == targetItem)
            {
               hierarchyItem.Select(vsUISelectionType.vsUISelectionTypeSelect);
               return;
            }

            // Recursively search within child items
            if (hierarchyItem.UIHierarchyItems.Count > 0)
            {
               SelectHierarchyItemRecursively(hierarchyItem.UIHierarchyItems, targetItem);
            }
         }
      }
   }
}
