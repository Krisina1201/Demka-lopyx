using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Lopyxxx.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;

namespace Lopyxxx;

public partial class AddProductWindow : Window
{
    private readonly PostgresContext context = new PostgresContext();
    public bool isNew;
    public Product? updProduct;
    public event EventHandler<Product>? ProductSaved;
    public event EventHandler? WindowClosed;

    private List<string> materialList = new List<string>();
    private string selectedImagePath = "products/noPhoto.jpeg";

    public AddProductWindow()
    {
        InitializeComponent();

        var productTypes = context.ProductTypes.Select(er => er.Title).ToList();
        var materials = context.Materials.Select(e => e.Title).ToList();

        TypeCombobox.ItemsSource = productTypes;
        MaterialCombobox.ItemsSource = materials;
        MaterialListBox.ItemsSource = materialList;
        DeleteButton.IsVisible = false;

        isNew = true;
    }

    public AddProductWindow(Product product)
    {
        InitializeComponent();

        var materialTitleList = context.ProductMaterials
            .Include(pm => pm.Material)
            .Where(pm => pm.ProductId == product.Id)
            .Select(pm => pm.Material.Title)
            .ToList();

        materialList = materialTitleList;
        MaterialListBox.ItemsSource = materialList;
        DeleteButton.IsVisible = true;
        updProduct = product;

        var materials = context.Materials.Select(e => e.Title).ToList();
        var productTypes = context.ProductTypes.Select(er => er.Title).ToList();

        MaterialCombobox.ItemsSource = materials;
        TypeCombobox.ItemsSource = productTypes;

        isNew = false;

        TitleTextBox.Text = product.Title;
        TypeCombobox.SelectedIndex = product.ProductTypeId > 0 ? product.ProductTypeId.Value - 1 : 0;
        ArticylTextBox.Text = product.ArticleNumber;
        PriceTextBox.Text = product.MinCostForAgent.ToString();
        CountPeopleTextBox.Text = product.ProductionPersonCount?.ToString() ?? "";
        WorkShopNumberTextBox.Text = product.ProductionWorkshopNumber?.ToString() ?? "";
        DescriptionTextBox.Text = product.Description ?? "";

        if (!string.IsNullOrEmpty(product.Image) && product.Image != "products/noPhoto.jpeg")
        {
            selectedImagePath = product.Image;
            UpdateImagePreview();
        }
    }

    private async void SelectImage_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите изображение",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Изображения") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" } }
            }
        });

        if (files.Count > 0 && files[0].TryGetLocalPath() is string filePath)
        {
            string projectImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "products");

            if (!Directory.Exists(projectImagesDir))
            {
                Directory.CreateDirectory(projectImagesDir);
            }

            string fileName = $"product_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(filePath)}";
            string destinationPath = Path.Combine(projectImagesDir, fileName);

            try
            {
                File.Copy(filePath, destinationPath, true);

                selectedImagePath = $"products/{fileName}";
                UpdateImagePreview();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Ошибка!", $"Не удалось загрузить изображение: {ex.Message}");
            }
        }
    }

    private void UpdateImagePreview()
    {
        try
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selectedImagePath);
            if (File.Exists(fullPath))
            {
                ImagePreview.Source = new Avalonia.Media.Imaging.Bitmap(fullPath);
            }
            else
            {
                ImagePreview.Source = null;
                selectedImagePath = "products/noPhoto.jpeg";
            }
        }
        catch
        {
            ImagePreview.Source = null;
            selectedImagePath = "products/noPhoto.jpeg";
        }
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string title = TitleTextBox.Text ?? "";
        int productType = TypeCombobox.SelectedIndex + 1;
        string articyl = ArticylTextBox.Text ?? "";
        string price = PriceTextBox.Text ?? "";
        string countPeople = CountPeopleTextBox.Text ?? "";
        string workShopNumber = WorkShopNumberTextBox.Text ?? "";
        string description = DescriptionTextBox.Text ?? "";

        if (string.IsNullOrEmpty(title) ||
            string.IsNullOrEmpty(articyl) ||
            string.IsNullOrEmpty(price) ||
            string.IsNullOrEmpty(countPeople) ||
            string.IsNullOrEmpty(workShopNumber) ||
            string.IsNullOrEmpty(description))
        {
            ShowErrorDialog("Ошибка!", "Все поля должны быть заполнены");
            return;
        }

        try
        {
            if (decimal.TryParse(price, out decimal price_int) &&
                int.TryParse(countPeople, out int countPeople_int) &&
                int.TryParse(workShopNumber, out int workShopNumber_int))
            {
                Product savedProduct;

                if (isNew)
                {
                    int maxId = context.Products.Any() ? context.Products.Max(p => p.Id) : 0;
                    int id = maxId + 1;

                    Product product = new Product
                    {
                        Id = id,
                        Title = title,
                        ArticleNumber = articyl,
                        MinCostForAgent = price_int,
                        ProductTypeId = productType,
                        Image = selectedImagePath,
                        ProductionPersonCount = countPeople_int,
                        ProductionWorkshopNumber = workShopNumber_int,
                        Description = description
                    };

                    context.Products.Add(product);
                    context.SaveChanges();
                    savedProduct = product;

                    SaveProductMaterials(product.Id, materialList);
                }
                else if (updProduct != null)
                {
                    var existingProduct = context.Products.Find(updProduct.Id);

                    if (existingProduct == null)
                    {
                        ShowErrorDialog("Ошибка!", "Продукт не найден в базе данных");
                        return;
                    }

                    existingProduct.Title = title;
                    existingProduct.ArticleNumber = articyl;
                    existingProduct.MinCostForAgent = price_int;
                    existingProduct.ProductTypeId = productType;
                    existingProduct.Image = selectedImagePath;
                    existingProduct.ProductionPersonCount = countPeople_int;
                    existingProduct.ProductionWorkshopNumber = workShopNumber_int;
                    existingProduct.Description = description;

                    context.SaveChanges();
                    savedProduct = existingProduct;

                    UpdateProductMaterials(updProduct.Id, materialList);
                }
                else
                {
                    ShowErrorDialog("Ошибка!", "Не удалось определить продукт для обновления");
                    return;
                }

                ProductSaved?.Invoke(this, savedProduct);
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                Close();
            }
            else
            {
                ShowErrorDialog("Ошибка!", "Цена, количество людей и номер цеха должны быть числами");
            }
        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $"\nВнутренняя ошибка: {ex.InnerException.Message}";
            }
            ShowErrorDialog("Ошибка!", $"При сохранении произошла ошибка: {errorMessage}");
        }
    }

    private void MaterialCombobox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MaterialCombobox.SelectedItem is string selectedMaterial)
        {
            if (!string.IsNullOrEmpty(selectedMaterial) && !materialList.Contains(selectedMaterial))
            {
                materialList.Add(selectedMaterial);
                MaterialListBox.ItemsSource = null;
                MaterialListBox.ItemsSource = materialList;
            }

            MaterialCombobox.SelectedIndex = -1;
        }
    }

    private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is string materialTitle)
        {
            materialList.Remove(materialTitle);
            var newList = new List<string>(materialList);
            MaterialListBox.ItemsSource = newList;
            materialList = newList;

            if (!isNew && updProduct != null)
            {
                var material = context.Materials.FirstOrDefault(m => m.Title == materialTitle);

                if (material != null)
                {
                    var productMaterial = context.ProductMaterials
                        .FirstOrDefault(pm => pm.ProductId == updProduct.Id && pm.MaterialId == material.Id);

                    if (productMaterial != null)
                    {
                        context.ProductMaterials.Remove(productMaterial);
                        context.SaveChanges();
                    }
                }
            }
        }
    }

    private void SaveProductMaterials(int productId, List<string> materialTitles)
    {
        foreach (var materialTitle in materialTitles)
        {
            var material = context.Materials.FirstOrDefault(m => m.Title == materialTitle);

            if (material != null)
            {
                var productMaterial = new ProductMaterial
                {
                    ProductId = productId,
                    MaterialId = material.Id
                };

                context.ProductMaterials.Add(productMaterial);
            }
        }
        context.SaveChanges();
    }

    private void UpdateProductMaterials(int productId, List<string> newMaterialTitles)
    {
        var currentMaterials = context.ProductMaterials
            .Include(pm => pm.Material)
            .Where(pm => pm.ProductId == productId)
            .ToList();

        foreach (var currentMaterial in currentMaterials)
        {
            if (!newMaterialTitles.Contains(currentMaterial.Material.Title))
            {
                context.ProductMaterials.Remove(currentMaterial);
            }
        }

        foreach (var materialTitle in newMaterialTitles)
        {
            if (!currentMaterials.Any(cm => cm.Material.Title == materialTitle))
            {
                var material = context.Materials.FirstOrDefault(m => m.Title == materialTitle);

                if (material != null)
                {
                    var productMaterial = new ProductMaterial
                    {
                        ProductId = productId,
                        MaterialId = material.Id
                    };

                    context.ProductMaterials.Add(productMaterial);
                }
            }
        }

        context.SaveChanges();
    }

    private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowClosed?.Invoke(this, EventArgs.Empty);
        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
        Close();
    }

    private void ShowErrorDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            }
        };
        dialog.ShowDialog(this);
    }

    private void Button_Click_Delete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (updProduct == null) return;

        try
        {
            var productMaterials = context.ProductMaterials
                .Where(pm => pm.ProductId == updProduct.Id)
                .ToList();

            if (productMaterials.Any())
            {
                context.ProductMaterials.RemoveRange(productMaterials);
            }

            context.Products.Remove(updProduct);
            context.SaveChanges();

            ProductSaved?.Invoke(this, updProduct);
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Ошибка!", $"При удалении произошла ошибка: {ex.Message}");
        }
    }
}