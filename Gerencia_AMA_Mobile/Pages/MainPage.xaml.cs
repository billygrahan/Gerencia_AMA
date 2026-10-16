using Microsoft.Maui.Controls.Shapes;

namespace Gerencia_AMA;

public partial class MainPage : ContentPage
{
    private static readonly GridLength Star = new(1, GridUnitType.Star);
    private readonly Color Ink = Color.FromArgb("#172B3A");
    private readonly Color Muted = Color.FromArgb("#82919C");
    private readonly Color Teal = Color.FromArgb("#00A896");
    private readonly Color Canvas = Color.FromArgb("#F5F8FA");
    public MainPageViewModel ViewModel { get; } = new();
    private string _filter = "Todos";
    private string _search = "";
    private DateTime _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _showingTreasury;

    public MainPage() { InitializeComponent(); ShowSplash(); }

}
