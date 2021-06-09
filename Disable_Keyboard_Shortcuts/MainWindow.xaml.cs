using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CombinationKeys_Lates
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       public static KeyboardHook kh;
        
        public MainWindow()
        {
            kh = new KeyboardHook("None");
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            kh.KeyIntercepted += new KeyboardHook.KeyboardHookEventHandler(kh_Intercepted);
        }

        void kh_Intercepted(KeyboardHook.KeyboardHookEventArgs e)
        {
            if (e.PassThrough)
            {
                this.Topmost = false;
            }
        }
    }
}
