using System.Windows;
using System.Windows.Controls;

namespace UnrealWizard
{
   public partial class MyToolWindowControl : UserControl
   {
      public MyToolWindowControl()
      {
         InitializeComponent();
      }

      private void button1_Click(object sender, RoutedEventArgs e)
      {
         VS.MessageBox.Show("UnrealWizard", "Button clicked");
      }
   }
}