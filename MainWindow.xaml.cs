using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManagedHeapApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        class SomeClass
        {
            public byte[] SomeObjects { get; set; }
        }

        private List<SomeClass> _pList = new List<SomeClass>();

        private void AddObjects_Click(object sender, RoutedEventArgs e)
        {
            var p = new SomeClass();
            p.SomeObjects = new byte[100 * 1024 * 1024]; // allocate memory
            _pList.Add(p);
            lbl1.Content = _pList.Count.ToString();
        }

        private void ListObjects_Click(object sender, RoutedEventArgs e)
        {
            txt1.Clear();
            var files = Directory.GetFiles(@"dumps", "*.dmp", SearchOption.TopDirectoryOnly);

            foreach (var filePath in files)
            {
                using (var dataTarget = DataTarget.LoadCrashDump(filePath))
                {
                    string dacLocation = dataTarget.ClrVersions[0].TryGetDacLocation();
                    ClrRuntime runtime = dataTarget.CreateRuntime(dacLocation);

                    ClrHeap heap = runtime.GetHeap();
                    //foreach (ulong obj in heap.EnumerateObjects())
                    //{
                    //    ClrType type = heap.GetObjectType(obj);
                    //    ulong size = type.GetSize(obj);

                    //    Debug.WriteLine("{0,12:X} {1,8:n0} {2}", obj, size, type.Name);
                    //    //txt1.AppendText(string.Format("{0,12:X} {1,8:n0} {2}", obj, size, type.Name));
                    //    //txt1.ScrollToEnd();
                    //}

                    var stats = from o in heap.EnumerateObjects()
                                let t = heap.GetObjectType(o)
                                where t != null && t.Name != null && !t.Name.StartsWith("System.Xaml") && !t.Name.StartsWith("System.Windows")
                                group o by t into g
                                let size = g.Sum(o => (uint)g.Key.GetSize(o))
                                orderby size
                                select new
                                {
                                    Name = g.Key.Name,
                                    Size = size,
                                    Count = g.Count()
                                };

                    foreach (var item in stats)
                        txt1.AppendText(string.Format("{0,12:n0} {1,12:n0} {2}\n", item.Size, item.Count, item.Name));

                    txt1.AppendText(filePath);
                    txt1.ScrollToEnd();
                }
            }
        }

        private void ClearObjects_Click(object sender, RoutedEventArgs e)
        {
            _pList.Clear();
            lbl1.Content = _pList.Count.ToString();
        }

        private void TriggerGC_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
