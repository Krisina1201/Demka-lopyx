using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting.Unicode;
using Lopyxxx.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public int maxList;

    private List<Product> selectedProducts = new List<Product>();


    public MainWindow()
    {
        InitializeComponent();

        var lict = context.ProductTypes.Select(e => e.Title).ToList();
        lict.Add("Âñ¸");
        filtercombobox.ItemsSource = lict;

        var products = context.Products
            .Include(e => e.ProductMaterials)
            .ThenInclude(pm => pm.Material)
            .Include(e => e.ProductType)
            .ToList();

        originalData = new ObservableCollection<Product>(products);
        filterData = new ObservableCollection<Product>(products);

        UpdateMaxList();
        EndButton.Content = maxList;
        OneButton.Background = new SolidColorBrush(Color.Parse("#00CC76"));

        LoadDataInListBox();

        SortByNameCombobox.SelectionChanged += SortByName;
        SortByNumberCombobox.SelectionChanged += SortByNumberWorkshop; 
        SortByCostCombobox.SelectionChanged += SortByCost;

        filtercombobox.SelectionChanged += FilterByType;

        ProductListBox.SelectionChanged += ProductListBox_SelectionChanged;

        ChangeButton.IsVisible = false;
    }

    private void ProductListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        selectedProducts = ProductListBox.SelectedItems.Cast<Product>().ToList();

        if (selectedProducts.Count == 1)
        {
            AddProductWindow addProductWindow = new AddProductWindow(selectedProducts[0]);
            addProductWindow.Show();
            this.Close();
        } else
        {
            selectedProducts = ProductListBox.SelectedItems.Cast<Product>().ToList();
            ChangeButton.IsVisible = true;
        }


    }

    private void UpdateMaxList()
    {
        int count = filterData.Count;
        if (count % 20 != 0)
            maxList = count / 20 + 1;
        else
            maxList = count / 20;

        if (maxList == 0) maxList = 1;

        EndButton.Content = maxList.ToString();
    }

    private void FilterByType(object? sender, SelectionChangedEventArgs e)
    {
        int selecttedIndex = filtercombobox.SelectedIndex;

        currentList = 1;
        currentMinIndex = 1;
        currentMaxIndex = 20;

        if (selecttedIndex < 0 || selecttedIndex >= filtercombobox.Items.Count - 1)
        {
            filterData = new ObservableCollection<Product>(originalData);
        }
        else
        {
            var filt = originalData.Where(eq => eq.ProductTypeId == selecttedIndex + 1).ToList();
            filterData = new ObservableCollection<Product>(filt);
        }

        UpdateMaxList();
        LoadDataInListBox();
    }

    private void SortByCost(object? sender, SelectionChangedEventArgs e)
    {
        int selectedIndex = SortByCostCombobox.SelectedIndex;

        currentList = 1;
        currentMinIndex = 1;
        currentMaxIndex = 20;

        if (selectedIndex == 0)
        {
            var data = filterData.OrderBy(eñ => eñ.MinCostForAgent).ToList();
            filterData = new ObservableCollection<Product>(data);
        }
        else
        {
            var data = filterData.OrderByDescending(eñ => eñ.MinCostForAgent).ToList();
            filterData = new ObservableCollection<Product>(data);
        }

        LoadDataInListBox();
    }

    private void SortByNumberWorkshop(object? sender, SelectionChangedEventArgs e) 
    {
        int selectedIndexWorkShop = SortByNumberCombobox.SelectedIndex;

        currentList = 1;
        currentMinIndex = 1;
        currentMaxIndex = 20;

        if (selectedIndexWorkShop == 0)
        {
            var data = filterData.OrderBy(eñ => eñ.ProductionWorkshopNumber).ToList();
            filterData = new ObservableCollection<Product>(data);
        }
        else
        {
            var data = filterData.OrderByDescending(eñ => eñ.ProductionWorkshopNumber).ToList();
            filterData = new ObservableCollection<Product>(data);
        }

        LoadDataInListBox();
    }

    private void SortByName(object? sender, SelectionChangedEventArgs e)
    {
        int selectedIndexName = SortByNameCombobox.SelectedIndex;

        currentList = 1;
        currentMinIndex = 1;
        currentMaxIndex = 20;

        if (selectedIndexName == 0)
        {
            var data = filterData.OrderBy(eñ => eñ.Title).ToList();
            filterData = new ObservableCollection<Product>(data);
        }
        else
        {
            var data = filterData.OrderByDescending(eñ => eñ.Title).ToList();
            filterData = new ObservableCollection<Product>(data);
        }

        LoadDataInListBox();
    }

    public void LoadDataInListBox()
    {
        UpdateMaxList();

        ButtonStackPanel.IsVisible = true;

        if (filterData.Count == 0)
        {
            ProductListBox.ItemsSource = new ObservableCollection<Product>();
            ButtonStackPanel.IsVisible = false;
            return;
        }

        var itemsToShow = filterData
            .Skip((currentList - 1) * 20)
            .Take(20)
            .ToList();

        ProductListBox.ItemsSource = itemsToShow;

        if (filterData.Count <= 20)
        {
            ButtonStackPanel.IsVisible = false;
        }
        else if (filterData.Count <= 40)
        {
            ThreePointButton.IsVisible = false;
            EndButton.IsVisible = false;
        }
        else
        {
            ThreePointButton.IsVisible = true;
            EndButton.IsVisible = true;
        }

        SwitchingButtons();
    }

    public void SwitchingButtons()
    {
        EndButton.Background = new SolidColorBrush(Colors.White);
        OneButton.Background = new SolidColorBrush(Colors.White);
        ThreePointButton.Background = new SolidColorBrush(Colors.White);
        TwoButton.Background = new SolidColorBrush(Colors.White);

        EndButton.Content = maxList.ToString();

        if (currentList > 2 && currentList < maxList)
        {
            ThreePointButton.Content = currentList.ToString();
        }

        if (currentList == 1)
        {
            OneButton.Background = new SolidColorBrush(Color.Parse("#00CC76"));
        }
        else if (currentList == 2)
        {
            TwoButton.Background = new SolidColorBrush(Color.Parse("#00CC76"));
        }
        else if (currentList == maxList)
        {
            EndButton.Background = new SolidColorBrush(Color.Parse("#00CC76"));
        }
        else if (currentList > 2 && currentList < maxList)
        {
            ThreePointButton.Background = new SolidColorBrush(Color.Parse("#00CC76"));
            ThreePointButton.Content = currentList.ToString();
        }
    }

    private void Forward_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (currentList >= maxList)
        {
            return;
        }

        currentMinIndex += 20;
        currentMaxIndex += 20;
        currentList += 1;

        LoadDataInListBox();
    }

    private void Back_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (currentList == 1)
        {
            return;
        }

        currentMinIndex -= 20;
        currentMaxIndex -= 20;
        currentList -= 1;

        LoadDataInListBox();
    }

    private void One_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        currentList = 1;
        currentMinIndex = 1;
        currentMaxIndex = 20;

        LoadDataInListBox();
    }

    private void Two_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        currentList = 2;
        currentMinIndex = 21;
        currentMaxIndex = 40;

        LoadDataInListBox();
    }

    private void End_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        currentList = maxList;
        currentMinIndex = (maxList - 1) * 20 + 1;
        currentMaxIndex = Math.Min(maxList * 20, filterData.Count);

        LoadDataInListBox();
    }

    private async void ChangePriceClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (selectedProducts == null || selectedProducts.Count == 0)
        {
            return;
        }

        var changeCostWindow = new ChangeCostWindow(selectedProducts);

        await changeCostWindow.ShowDialog(this);

        LoadDataInListBox();
    }

    private void AddNewProduct_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AddProductWindow addProductWindow = new AddProductWindow();
        addProductWindow.Show();
        this.Close();
    }

   
}