using System.Windows;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class ClientDialog : Window
{
 public Client Client { get; private set; }
 private readonly bool _isEdit;
 private bool _isSaved = false;

 // Сохраняем оригинальные значения для проверки изменений
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
 if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
 {
 MessageBox.Show("Введите ФИО", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 Client.FullName = FullNameTextBox.Text;
 Client.Passport = PassportTextBox.Text;
 Client.Phone = PhoneTextBox.Text;
 Client.Email = EmailTextBox.Text;

 _isSaved = true;
 DialogResult = true;
 Close();
 }

 private void Cancel_Click(object sender, RoutedEventArgs e)
 {
 Close();
 }

 private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
 {
 if (_isSaved) return;
        
 var currentFullName = FullNameTextBox.Text ?? "";
 var currentPassport = PassportTextBox.Text ?? "";
 var currentPhone = PhoneTextBox.Text ?? "";
 var currentEmail = EmailTextBox.Text ?? "";
        
 bool hasChanges = _isEdit 
 ? currentFullName != _originalFullName || currentPassport != _originalPassport || 
 currentPhone != _originalPhone || currentEmail != _originalEmail
 : !string.IsNullOrEmpty(currentFullName) || !string.IsNullOrEmpty(currentPassport) || 
 !string.IsNullOrEmpty(currentPhone) || !string.IsNullOrEmpty(currentEmail);
        
 if (hasChanges)
 {
 var result = MessageBox.Show("Есть несохранённые изменения. Закрыть?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
 if (result == MessageBoxResult.No)
 {
 e.Cancel = true;
 }
 }
 }
}
