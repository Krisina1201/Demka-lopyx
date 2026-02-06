using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Lopyxxx.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lopyxxx;

public partial class ChangeCostWindow : Window
{
    private List<Product> selectedProducts;
    private PostgresContext context = new PostgresContext();

    public ChangeCostWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public ChangeCostWindow(List<Product> selectedProducts) : this()
    {
        this.selectedProducts = selectedProducts;
    }

    private async void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SumTextBox.Text))
        {
            ShowErrorDialog("Ошибка!", "Введите сумму на которую хотите повысить стоимость товаров");
            return;
        }

        if (!int.TryParse(SumTextBox.Text, out int sumToAdd) || sumToAdd <= 0)
        {
            ShowErrorDialog("Ошибка", "Введите корректное положительное число");
            return;
        }

        try
        {
            var productIds = selectedProducts.Select(p => p.Id).ToList();

            var productsToUpdate = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in productsToUpdate)
            {
                product.MinCostForAgent += sumToAdd;
            }

            await context.SaveChangesAsync();

            Close();
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Ошибка", $"Не удалось изменить стоимость: {ex.Message}");
        }
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
}