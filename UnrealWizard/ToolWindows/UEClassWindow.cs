using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace UnrealWizard
{
   public class UEClassWindow : DialogWindow
   {
      //[Guid("252cf685-cded-4bd7-8d49-5fd125157a3b")]
      //private static readonly Guid windowGuid = new Guid("252cf685-cded-4bd7-8d49-5fd125157a3b");

     public  UEClassWindow(UEClassWindowControl control)
      {
         HasMaximizeButton = false;
         HasMinimizeButton = false;

         Content = control;
         Title = "Unreal C++ Class Wizard";
         Width = 1000;
         Height = 600;
         WindowStartupLocation = WindowStartupLocation.CenterScreen;
      }

      public static void ShowWindow()
      {
         //UEClassWindowControl control = new UEClassWindowControl();

         //Window dialog = new Window
         //{
         //   Content = control,
         //   Title = "Unreal C++ Class Wizard",
         //   Height = 300,
         //   Width = 500,
         //   WindowStartupLocation = WindowStartupLocation.CenterScreen
         //};

         //dialog.Closed += Dialog_Closed;

         //dialog.ShowDialog();
      }

      private static void Dialog_Closed(object sender, EventArgs e)
      {
         //var dialog = sender as Window;
         //if (dialog != null)
         //{
         //   dialog.Closed -= Dialog_Closed;

         //   if (dialog.Content is IDisposable disposable)
         //   {
         //      disposable.Dispose();
         //   }

         //   dialog.Dispatcher.InvokeShutdown();
         //}
      }
   }
}
