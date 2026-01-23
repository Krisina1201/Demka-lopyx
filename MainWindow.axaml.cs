using Avalonia.Controls;
using Lopyxxx.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;

namespace Lopyxxx;

public partial class MainWindow : Window
{
    public ObservableCollection<Product> originalData = new ObservableCollection<Product>();
    public ObservableCollection<Product> filterData = new ObservableCollection<Product>();

    public PostgresContext context = new PostgresContext();

    public int currentList = 1;
    public int currentMinIndex = 1;
    public int currentMaxIndex = 20;
    


    public MainWindow()
    {
        InitializeComponent();
        LoadDataInListBox();
    }

    public void LoadDataInListBox()
    {
        ProductListBox.ItemsSource = context.Products.Where(e => e.Id >= currentMinIndex && e.Id <= currentMaxIndex )
            .Include(e => e.ProductMaterials)
            .Include(e => e.ProductType);


    }
}