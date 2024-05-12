global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnrealWizard
{
   [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
   [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
   [ProvideMenuResource("Menus.ctmenu", 1)]
   [Guid(PackageGuids.UnrealWizardString)]
   [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
   public sealed class UnrealWizardPackage : ToolkitPackage
   {
      protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
      {
         await this.RegisterCommandsAsync();

         //RegisterEditorFactory(new UnrealWizard.UnealWizardEditorFactory(this));

         VisualStudioServices.ServiceProvider = this;
         VisualStudioServices.OLEServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)VisualStudioServices.ServiceProvider.GetService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider));
      }
   }
}