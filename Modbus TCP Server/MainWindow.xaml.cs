using System;
using System.Collections.Generic;
using System.Linq;
using Modbus_TCP_Server;
using System.Windows;
using System.Text.RegularExpressions;
using System.Net;
using EasyModbus;

namespace ModbusTCP_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool started;
        private ModbusServer server;
        private List<Device> devices;
        private Device selected_device;
        public MainWindow()
        {
            InitializeComponent();
            started = false;
            devices = new List<Device>();
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (started)
            {
                //Остановка сервера
                LaunchButton.Content = "Запустить";
                server.StopListening();
                server = null;
            }
            else
            {
                //Запуск сервера
                //Введённые данные можно проверять на валидность как try/catch, TryParse, так и при помощи regex
                Regex IP_regex = new Regex("^(?:\\d{1,3}\\.){3}\\d{1,3}$");
                if (!IP_regex.IsMatch(IP_textbox.Text))
                {
                    MessageBox.Show("Введён неправильный формат IP", "Неверный IP", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!int.TryParse(Port_textbox.Text, out int port) || port < 0 || port > 65535)
                {
                    MessageBox.Show("Порт должен быть в диапозоне от 0 до 65535", "Неверный порт", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                IPAddress ip = IPAddress.Parse(IP_textbox.Text);
                if (server == null)
                    server = new ModbusServer();
                server.LocalIPAddress = ip;
                server.Port = port;
                server.CoilsChanged += Server_CoilsChanged;
                server.HoldingRegistersChanged += Server_HoldingRegistersChanged;
                server.NumberOfConnectedClientsChanged += Server_NumberOfConnectedClientsChanged;
                
                server.Listen();
                Sheet.ItemsSource = devices;
                LaunchButton.Content = "Остановить";
            }
            started = !started;
            UpdateSheet();
        }

        /// <summary>
        /// Обновляет содержимое DataGrid и счётчик подключений
        /// </summary>
        private void UpdateSheet()
        {
            devices.Clear();
            if (started)
            {
                for (int i = 0; i < server.coils.localArray.Length; i++)
                {
                    if (server.coils.localArray[i] || server.discreteInputs.localArray[i] ||
                        server.holdingRegisters.localArray[i] != 0 || server.inputRegisters.localArray[i] != 0)
                    {
                        bool coil = server.coils.localArray[i],
                               di = server.discreteInputs.localArray[i];
                        short hr = server.holdingRegisters.localArray[i],
                              ir = server.inputRegisters.localArray[i];
                        devices.Add(new Device(i, coil, di, hr, ir));
                    }
                }
            }
            Dispatcher.Invoke(() =>
            {
                int old_selection = Sheet.SelectedIndex;
                Sheet.Items.Refresh();
                if (!started)
                    Connections.Content = $"Подключений: 0";
                else
                    Connections.Content = $"Подключений: {server.NumberOfConnections}";
                Device_combobox.Items.Clear();
                foreach (Device d in devices)
                {
                    Device_combobox.Items.Add(d.ID);
                }
                if (Sheet.Items.Count >= old_selection)
                {
                    Sheet.SelectedIndex = old_selection;
                }
            });
        }

        private void Cell_combobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateDataFields();
        }

        private void Sheet_SelectedCellsChanged(object sender, System.Windows.Controls.SelectedCellsChangedEventArgs e)
        {
            Device device = (Device)Sheet.SelectedItem;
            if (device == null)
                return;
            int item_id = Device_combobox.Items.IndexOf(device.ID);
            if (item_id == -1)
            {
                MessageBox.Show("Ошибка", "Не найдено устройство", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Device_combobox.SelectedIndex = item_id;
            UpdateDataFields();
        }

        private void Device_combobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!started || Device_combobox.SelectedItem == null)
                return;
            int device_id = int.Parse(Device_combobox.SelectedItem.ToString());
            selected_device = devices.Find(_ => _.ID == device_id);
            Sheet.SelectedItem = selected_device;
            UpdateDataFields();
        }

        private void Send_button_Click(object sender, RoutedEventArgs e)
        {
            if (!started)
                return;
            if (Cell_combobox.SelectedIndex < 2)
            {
                bool value = Convert.ToBoolean(Coil_combobox.SelectedIndex);
                if (Cell_combobox.SelectedIndex == 0)
                    server.coils.localArray[selected_device.ID] = value;
                else
                    server.discreteInputs.localArray[selected_device.ID] = value;
            }
            else
            {
                if (!short.TryParse(Register_textbox.Text, out short value))
                {
                    MessageBox.Show("Значение должно быть в диапозоне от -32768 до 32767", "Ошибка отправки", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (Cell_combobox.SelectedIndex == 2)
                    server.holdingRegisters.localArray[selected_device.ID] = value;
                else
                    server.inputRegisters.localArray[selected_device.ID] = value;
            }
            UpdateSheet();
        }

        /// <summary>
        /// Обновляет поле ввода данных в соответствии с выбранным пространством
        /// </summary>
        private void UpdateDataFields()
        {
            if (selected_device == null)
                return;
            if (Cell_combobox.SelectedIndex < 2)
            {
                Register_textbox.Visibility = Visibility.Hidden;
                Coil_combobox.Visibility = Visibility.Visible;
                bool value;
                if (Cell_combobox.SelectedIndex == 0)
                    value = selected_device.Coil;
                else
                    value = selected_device.Discrete_Input;
                Coil_combobox.SelectedIndex = Convert.ToInt32(value);
            }
            else
            {
                Coil_combobox.Visibility = Visibility.Hidden;
                Register_textbox.Visibility = Visibility.Visible;
                int value;
                if (Cell_combobox.SelectedIndex == 2)
                    value = selected_device.Holding_Register;
                else
                    value = selected_device.Input_Register;
                Register_textbox.Text = value.ToString();
            }
        }
        private void Server_CoilsChanged(int coil, int numberOfCoils)
        {
            UpdateSheet();
        }

        private void Server_HoldingRegistersChanged(int register, int numberOfRegisters)
        {
            UpdateSheet();
        }

        private void Server_NumberOfConnectedClientsChanged()
        {
            //UpdateSheet();
            //Тут должно быть ограничение на количество подключений, но я не знаю как это сделать
            //Заметка: по какой-то причине событие вызывается каждую секунду, делая ввод данных затруднительным
        }
    }
}