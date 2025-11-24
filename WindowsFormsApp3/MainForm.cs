using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MetranTestApp
{
    public class MainForm : Form
    {
        private CancellationTokenSource cancellationTokenSource;
        private string currentStatus = "Ожидается ввод";

        public MainForm()
        {
            InitializeComponent();
            UpdateStatus();
        }

        private void InitializeComponent()
        {
            txtProductId = new TextBox();
            btnStartTest1 = new Button();
            btnStartTest2 = new Button();
            btnStartTest3 = new Button();
            btnStopTest = new Button();
            lblStatus = new Label();
            lblProductId = new Label();
            SuspendLayout();

            // lblProductId
            lblProductId.Location = new System.Drawing.Point(20, 20);
            lblProductId.Size = new System.Drawing.Size(100, 20);
            lblProductId.Text = "ID изделия:";

            // txtProductId
            txtProductId.Location = new System.Drawing.Point(120, 20);
            txtProductId.Size = new System.Drawing.Size(200, 20);

            // btnStartTest1
            btnStartTest1.Location = new System.Drawing.Point(20, 60);
            btnStartTest1.Size = new System.Drawing.Size(100, 30);
            btnStartTest1.Text = "Тест 1";
            btnStartTest1.Click += new EventHandler(btnStartTest1_Click);

            // btnStartTest2
            btnStartTest2.Location = new System.Drawing.Point(130, 60);
            btnStartTest2.Size = new System.Drawing.Size(100, 30);
            btnStartTest2.Text = "Тест 2";
            btnStartTest2.Click += new EventHandler(btnStartTest2_Click);

            // btnStartTest3
            btnStartTest3.Location = new System.Drawing.Point(240, 60);
            btnStartTest3.Size = new System.Drawing.Size(100, 30);
            btnStartTest3.Text = "Тест 3";
            btnStartTest3.Click += new EventHandler(btnStartTest3_Click);

            // btnStopTest
            btnStopTest.Location = new System.Drawing.Point(20, 100);
            btnStopTest.Size = new System.Drawing.Size(100, 30);
            btnStopTest.Text = "Остановить";
            btnStopTest.Click += new EventHandler(btnStopTest_Click);
            btnStopTest.Enabled = false;

            // lblStatus
            lblStatus.Location = new System.Drawing.Point(20, 150);
            lblStatus.Size = new System.Drawing.Size(320, 50);
            lblStatus.Text = "Статус: " + currentStatus;

            // Form
            ClientSize = new System.Drawing.Size(400, 200);
            Controls.Add(lblProductId);
            Controls.Add(txtProductId);
            Controls.Add(btnStartTest1);
            Controls.Add(btnStartTest2);
            Controls.Add(btnStartTest3);
            Controls.Add(btnStopTest);
            Controls.Add(lblStatus);
            Text = "Metran Test App";
            ResumeLayout(false);
        }

        private TextBox txtProductId;
        private Button btnStartTest1;
        private Button btnStartTest2;
        private Button btnStartTest3;
        private Button btnStopTest;
        private Label lblStatus;
        private Label lblProductId;

        private async void btnStartTest1_Click(object sender, EventArgs e)
        {
            await StartTest(1);
        }

        private async void btnStartTest2_Click(object sender, EventArgs e)
        {
            await StartTest(2);
        }

        private async void btnStartTest3_Click(object sender, EventArgs e)
        {
            await StartTest(3);
        }

        private void btnStopTest_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private async Task StartTest(int testNumber)
        {
            if (string.IsNullOrEmpty(txtProductId.Text))
            {
                MessageBox.Show("Введите идентификатор изделия!");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            SetControlsState(false);

            try
            {
                currentStatus = $"Выполняется тест {testNumber}";
                UpdateStatus();

                var result = await RunTestAsync(testNumber, cancellationTokenSource.Token);

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    SaveResultToFile(result);
                    currentStatus = $"Тест {testNumber} завершен";
                    UpdateStatus();
                    MessageBox.Show($"Тест {testNumber} завершен!\nРезультат: {(result.IsSuccess ? "Успешно" : "Ошибка")}");
                }
                else
                {
                    currentStatus = "Тест остановлен пользователем";
                    UpdateStatus();
                }
            }
            catch (OperationCanceledException)
            {
                currentStatus = "Тест остановлен пользователем";
                UpdateStatus();
            }
            finally
            {
                SetControlsState(true);
                cancellationTokenSource = null;
            }
        }

        private async Task<TestResult> RunTestAsync(int testNumber, CancellationToken cancellationToken)
        {
            Random random = new Random();
            int delay = random.Next(10, 31) * 1000; // 10-30 секунд задержка

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException();
            }

            bool isSuccess = random.Next(0, 2) == 1; // Случайный успех/ошибка
            string errorMessage = isSuccess ? null : GetRandomErrorMessage();

            var result = new TestResult
            {
                TestNumber = testNumber,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                ProductId = txtProductId.Text
            };

            // Генерация уникальных данных для каждого теста
            switch (testNumber)
            {
                case 1:
                    result.Data.Add("Voltage", random.Next(100, 241).ToString() + "V");
                    result.Data.Add("Current", (random.NextDouble() * 5.0).ToString("F2") + "A");
                    break;
                case 2:
                    result.Data.Add("Temperature", random.Next(-40, 86).ToString() + "°C");
                    result.Data.Add("Pressure", (random.NextDouble() * 100.0).ToString("F1") + "kPa");
                    result.Data.Add("Humidity", random.Next(0, 101).ToString() + "%");
                    break;
                case 3:
                    result.Data.Add("Frequency", random.Next(45, 66).ToString() + "Hz");
                    result.Data.Add("Resistance", random.Next(1, 1001).ToString() + "Ω");
                    break;
            }

            return result;
        }

        private string GetRandomErrorMessage()
        {
            string[] errors = {
                "Превышение напряжения",
                "Низкое сопротивление",
                "Перегрев",
                "Обрыв цепи",
                "Короткое замыкание",
                "Нестабильный сигнал"
            };

            Random random = new Random();
            return errors[random.Next(errors.Length)];
        }

        private void SaveResultToFile(TestResult result)
        {
            string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
            
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, $"{result.ProductId}.txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"ID изделия: {result.ProductId}");
                writer.WriteLine($"Номер теста: {result.TestNumber}");
                writer.WriteLine($"Результат: {(result.IsSuccess ? "Успешно" : "Ошибка")}");
                
                if (!result.IsSuccess)
                {
                    writer.WriteLine($"Ошибка: {result.ErrorMessage}");
                }
                
                writer.WriteLine("Данные теста:");
                foreach (var data in result.Data)
                {
                    writer.WriteLine($"  {data.Key}: {data.Value}");
                }
                writer.WriteLine($"Время завершения: {DateTime.Now}");
            }
        }

        private void SetControlsState(bool enabled)
        {
            txtProductId.Enabled = enabled;
            btnStartTest1.Enabled = enabled;
            btnStartTest2.Enabled = enabled;
            btnStartTest3.Enabled = enabled;
            btnStopTest.Enabled = !enabled;
        }

        private void UpdateStatus()
        {
            lblStatus.Text = $"Статус: {currentStatus}";
        }
    }

    public class TestResult
    {
        public int TestNumber { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string ProductId { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}