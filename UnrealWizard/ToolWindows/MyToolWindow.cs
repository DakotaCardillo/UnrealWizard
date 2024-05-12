using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace UnrealWizard
{
   public class MyToolWindow : BaseToolWindow<MyToolWindow>
   {
      public override string GetTitle(int toolWindowId) => "UE C++ Class Wizard";

      public override Type PaneType => typeof(Pane);

      public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
      {
         return Task.FromResult<FrameworkElement>(new MyToolWindowControl());
      }

      [Guid("252cf685-cded-4bd7-8d49-5fd125157a3b")]
      internal class Pane : ToolkitToolWindowPane
      {
         public Pane()
         {
            BitmapImageMoniker = KnownMonikers.ToolWindow;
         }
      }
   }
}