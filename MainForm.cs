using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace FaceDetection;

/// <summary>
/// Ana pencere: webcam karelerini gösterir, yüzleri yeşil çerçeveyle işaretler.
/// UI güncellemeleri bir Timer ile UI thread'inde yapılır.
/// </summary>
public sealed class MainForm : Form
{
    private const int UiIntervalMs = 33;

    private readonly WebcamService _webcam = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new();
    private readonly PictureBox _pictureBox = new();
    private readonly FlowLayoutPanel _controlPanel = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly CheckBox _showFramesCheckBox = new();
    private readonly Label _faceCountLabel = new();
    private readonly Label _fpsLabel = new();
    private readonly Label _statusLabel = new();

    private FaceDetector? _faceDetector;
    private int _processedFrames;
    private DateTime _fpsWindowStart = DateTime.UtcNow;

    public MainForm()
    {
        InitializeLayout();
        LoadDetector();

        _uiTimer.Interval = UiIntervalMs;
        _uiTimer.Tick += UiTimer_Tick;

        UpdateButtons();
    }

    private void InitializeLayout()
    {
        Text = "OpenCV ile Yüz Tanıma";
        ClientSize = new System.Drawing.Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;

        _pictureBox.Dock = DockStyle.Fill;
        _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        _pictureBox.BackColor = System.Drawing.Color.Black;

        _startButton.Text = "Başlat";
        _startButton.AutoSize = true;
        _startButton.Click += (_, _) => StartCapture();

        _stopButton.Text = "Durdur";
        _stopButton.AutoSize = true;
        _stopButton.Click += (_, _) => StopCapture();

        _showFramesCheckBox.Text = "Yüz çerçevelerini göster";
        _showFramesCheckBox.Checked = true;
        _showFramesCheckBox.AutoSize = true;

        _faceCountLabel.Text = "Yüz sayısı: 0";
        _faceCountLabel.AutoSize = true;

        _fpsLabel.Text = "FPS: 0";
        _fpsLabel.AutoSize = true;

        _statusLabel.AutoSize = true;

        _controlPanel.Dock = DockStyle.Bottom;
        _controlPanel.AutoSize = true;
        _controlPanel.Padding = new Padding(8);
        _controlPanel.Controls.AddRange(new Control[]
        {
            _startButton,
            _stopButton,
            _showFramesCheckBox,
            _faceCountLabel,
            _fpsLabel,
            _statusLabel
        });

        Controls.Add(_pictureBox);
        Controls.Add(_controlPanel);
    }

    private void LoadDetector()
    {
        string cascadePath = Path.Combine(AppContext.BaseDirectory, "Assets", "haarcascade_frontalface_default.xml");
        try
        {
            _faceDetector = new FaceDetector(cascadePath);
            _statusLabel.Text = "Hazır.";
        }
        catch (Exception ex)
        {
            _faceDetector = null;
            _statusLabel.Text = "Cascade yüklenemedi: " + ex.Message;
        }
    }

    private void StartCapture()
    {
        if (_faceDetector is null)
        {
            MessageBox.Show(this, "Yüz algılama modeli yüklenemediği için başlatılamıyor.", "Uyarı",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _webcam.Start(0);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Webcam başlatılamadı: " + ex.Message, "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _processedFrames = 0;
        _fpsWindowStart = DateTime.UtcNow;
        _uiTimer.Start();
        UpdateButtons();
    }

    private void StopCapture()
    {
        _uiTimer.Stop();
        _webcam.Stop();
        _fpsLabel.Text = "FPS: 0";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _startButton.Enabled = !_webcam.IsRunning && _faceDetector is not null;
        _stopButton.Enabled = _webcam.IsRunning;
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        if (_faceDetector is null)
        {
            return;
        }

        using Mat? frame = _webcam.GetLatestFrame();
        if (frame is null || frame.Empty())
        {
            return;
        }

        Rect[] faces = _faceDetector.DetectFaces(frame);

        if (_showFramesCheckBox.Checked)
        {
            foreach (Rect face in faces)
            {
                Cv2.Rectangle(frame, face, new Scalar(0, 255, 0), 2);
            }
        }

        Bitmap bitmap = BitmapConverter.ToBitmap(frame);
        Image? oldImage = _pictureBox.Image;
        _pictureBox.Image = bitmap;
        oldImage?.Dispose();

        _faceCountLabel.Text = $"Yüz sayısı: {faces.Length}";
        UpdateFps();
    }

    private void UpdateFps()
    {
        _processedFrames++;
        double elapsed = (DateTime.UtcNow - _fpsWindowStart).TotalSeconds;
        if (elapsed >= 1.0)
        {
            _fpsLabel.Text = $"FPS: {_processedFrames / elapsed:F1}";
            _processedFrames = 0;
            _fpsWindowStart = DateTime.UtcNow;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _uiTimer.Stop();
        _webcam.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _uiTimer.Tick -= UiTimer_Tick;
            _uiTimer.Dispose();

            _webcam.Dispose();

            _faceDetector?.Dispose();
            _faceDetector = null;

            Image? image = _pictureBox.Image;
            _pictureBox.Image = null;
            image?.Dispose();
        }

        base.Dispose(disposing);
    }
}
