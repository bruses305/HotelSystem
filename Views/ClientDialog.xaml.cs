using System.Windows;
using System.ComponentModel;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class ClientDialog : DialogBase
{
    public Client Client { get; private set; }
    private readonly bool _isEdit;
        
    private string _originalFullName = "";
    private string _originalPassport = "";
    private string _originalPhone = "";
    private string _originalEmail = "";

    public ClientDialog(Client? client = null)
    {
        InitializeComponent();
        _isEdit = client != null;
        Client = client ?? new Client();
        
        if (_isEdit)
        {
            InitializeForm();
        }
        else
        {
            InitializeForm();
        }
    }

    /// <summary>
    /// Устанавливает имя клиента после создания диалога (для сценария создания из BookingDialog)
    /// </summary>
    public void SetClientName(string name)
    {
        Client.FullName = name;
        FullNameTextBox.Text = name;
        _originalFullName = name;
    }

    protected override bool HasChanges => 
        FullNameTextBox.Text?.Trim() != _originalFullName ||
        PassportTextBox.Text?.Trim() != _originalPassport ||
        PhoneTextBox.Text?.Trim() != _originalPhone ||
        EmailTextBox.Text?.Trim() != _originalEmail;
    
    protected override void Save()
    {
        if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            MessageBoxHelper.ShowError("Введите ФИО");
            return;
        }

        Client.FullName = FullNameTextBox.Text;
        Client.Passport = PassportTextBox.Text;
        Client.Phone = PhoneTextBox.Text;
        Client.Email = EmailTextBox.Text;
        
        MarkAsSaved();
        DialogResult = true;
        CloseWithoutPrompt();
    }

    protected override void Cancel()
    {
        base.Cancel();
        CloseWithoutPrompt();
    }

    private void InitializeForm()
    {
        FullNameTextBox.Text = Client.FullName;
        PassportTextBox.Text = Client.Passport;
        PhoneTextBox.Text = Client.Phone;
        EmailTextBox.Text = Client.Email;
        
        _originalFullName = Client.FullName ?? "";
        _originalPassport = Client.Passport ?? "";
        _originalPhone = Client.Phone ?? "";
        _originalEmail = Client.Email ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancel();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Логика перенесена в базовый класс DialogBase
    }
}
